#include <opencv2/core.hpp> 
#include <opencv2/videoio.hpp>
#include <opencv2/opencv.hpp>
#include <opencv2/core/types_c.h>
#include <opencv2/imgproc.hpp>
#include <opencv2/imgproc/imgproc_c.h>
#include <string>
#include <codecvt>
#include <locale>
#include <torch/torch.h>
#include <torch/script.h>

#include "object_detection.h"

using namespace std;
using namespace cv;

const double CONFIDENCE_LEVEL = 0.3;

ObjectDetection::ObjectDetection(wstring saved_model) {

	m_cuda_available = (torch::cuda::is_available()) ? true : false;
	m_device_type = m_cuda_available ? c10::kCUDA : c10::kCPU;

	try {
		string saved_model_utf8 = wstring_convert <codecvt_utf8<wchar_t>>().to_bytes(saved_model);
		m_module = torch::jit::load(saved_model_utf8);
		m_module.to(m_device_type);
		m_last_error_msg = L"";
		m_initialized = true;
	}
	catch (const c10::Error& e) {
		wstring error_msg = std::wstring_convert<std::codecvt_utf8<wchar_t>>().from_bytes(e.what());
		m_last_error_msg = error_msg;
		m_initialized = false;
	}
}

ObjectDetection::~ObjectDetection() {
	//Free memory
}

std::vector<torch::Tensor> ObjectDetection::non_max_suppression(torch::Tensor preds, float score_thresh, float iou_thresh)
{
	std::vector<torch::Tensor> output;
	for (size_t i = 0; i < preds.sizes()[0]; ++i)
	{
		torch::Tensor pred = preds.select(0, i).to(m_device_type);

		// Filter by scores
		torch::Tensor scores = pred.select(1, 4) * std::get<0>(torch::max(pred.slice(1, 5, pred.sizes()[1]), 1));
		pred = torch::index_select(pred, 0, torch::nonzero(scores > score_thresh).select(1, 0));
		if (pred.sizes()[0] == 0) continue;

		// (center_x, center_y, w, h) to (left, top, right, bottom)
		pred.select(1, 0) = pred.select(1, 0) - pred.select(1, 2) / 2;
		pred.select(1, 1) = pred.select(1, 1) - pred.select(1, 3) / 2;
		pred.select(1, 2) = pred.select(1, 0) + pred.select(1, 2);
		pred.select(1, 3) = pred.select(1, 1) + pred.select(1, 3);

		// Computing scores and classes
		std::tuple<torch::Tensor, torch::Tensor> max_tuple = torch::max(pred.slice(1, 5, pred.sizes()[1]), 1);
		pred.select(1, 4) = pred.select(1, 4) * std::get<0>(max_tuple);
		pred.select(1, 5) = std::get<1>(max_tuple);

		torch::Tensor  dets = pred.slice(1, 0, 6);

		torch::Tensor keep = torch::empty({ dets.sizes()[0] }).to(m_device_type);
		torch::Tensor areas = (dets.select(1, 3) - dets.select(1, 1)) * (dets.select(1, 2) - dets.select(1, 0));
		std::tuple<torch::Tensor, torch::Tensor> indexes_tuple = torch::sort(dets.select(1, 4), 0, 1);
		torch::Tensor v = std::get<0>(indexes_tuple);
		torch::Tensor indexes = std::get<1>(indexes_tuple);
		int count = 0;
		while (indexes.sizes()[0] > 0)
		{
			keep[count] = (indexes[0].item().toInt());
			count += 1;

			// Computing overlaps
			torch::Tensor lefts = torch::empty(indexes.sizes()[0] - 1).to(m_device_type);
			torch::Tensor tops = torch::empty(indexes.sizes()[0] - 1).to(m_device_type);
			torch::Tensor rights = torch::empty(indexes.sizes()[0] - 1).to(m_device_type);
			torch::Tensor bottoms = torch::empty(indexes.sizes()[0] - 1).to(m_device_type);
			torch::Tensor widths = torch::empty(indexes.sizes()[0] - 1).to(m_device_type);
			torch::Tensor heights = torch::empty(indexes.sizes()[0] - 1).to(m_device_type);
			for (size_t i = 0; i < indexes.sizes()[0] - 1; ++i)
			{
				lefts[i] = std::max(dets[indexes[0]][0].item().toFloat(), dets[indexes[i + 1]][0].item().toFloat());
				tops[i] = std::max(dets[indexes[0]][1].item().toFloat(), dets[indexes[i + 1]][1].item().toFloat());
				rights[i] = std::min(dets[indexes[0]][2].item().toFloat(), dets[indexes[i + 1]][2].item().toFloat());
				bottoms[i] = std::min(dets[indexes[0]][3].item().toFloat(), dets[indexes[i + 1]][3].item().toFloat());
				widths[i] = std::max(float(0), rights[i].item().toFloat() - lefts[i].item().toFloat());
				heights[i] = std::max(float(0), bottoms[i].item().toFloat() - tops[i].item().toFloat());
			}
			torch::Tensor overlaps = widths * heights;

			// FIlter by IOUs
			torch::Tensor ious = overlaps / (areas.select(0, indexes[0].item().toInt()) + torch::index_select(areas, 0, indexes.slice(0, 1, indexes.sizes()[0])) - overlaps);
			indexes = torch::index_select(indexes, 0, torch::nonzero(ious <= iou_thresh).select(1, 0) + 1);
		}
		keep = keep.toType(torch::kInt64);
		output.push_back(torch::index_select(dets, 0, keep.slice(0, 0, count)));
	}
	return output;
}

vector<Region> ObjectDetection::Detect(Mat* frame)
{
	//Declare our output regions vector
	vector<Region> m_region;

	//Resize and adjust image (converting BGR to RGB)
	cv::Mat img;
	cv::resize(*frame, img, cv::Size(1280, 1280), 0, 0, cv::INTER_AREA);
	cv::cvtColor(img, img, cv::COLOR_BGR2RGB);

	//Create inpit tensor
	torch::Tensor imgTensor = torch::from_blob(img.data, { img.rows, img.cols, 3 }, torch::kByte);
	imgTensor = imgTensor.permute({ 2,0,1 });
	imgTensor = imgTensor.toType(torch::kFloat);
	imgTensor = imgTensor.div(255);
	imgTensor = imgTensor.unsqueeze(0);
	imgTensor = imgTensor.to(m_device_type);

	//Execute model, returning the following tensor [<# of detections>, 25200, 8]
	torch::Tensor preds;
	try {
		preds = m_module.forward({ imgTensor }).toTuple()->elements()[0].toTensor();
	}
	catch (const c10::Error& e) {
		cout << e.msg() << endl;
	}
	bool neg_found = false;

	//Process output detection tensor
	std::vector<torch::Tensor> dets;
	try {
		dets = non_max_suppression(preds, 0.4, 0.5);
	}
	catch (const c10::Error& e) {
		cout << e.msg() << endl;
	}

	//If any valid detection area, add to our regions vector
	if (dets.size() > 0)
	{
		// Visualize result
		for (size_t i = 0; i < dets[0].sizes()[0]; ++i)
		{			 
			float left = dets[0][i][0].item().toFloat() * frame->cols / img.cols;
			float top = dets[0][i][1].item().toFloat() * frame->rows / img.rows;
			float right = dets[0][i][2].item().toFloat() * frame->cols / img.cols;
			float bottom = dets[0][i][3].item().toFloat() * frame->rows / img.rows;
			float score = dets[0][i][4].item().toFloat();
			int classID = dets[0][i][5].item().toInt() + 1;

			//For now, ignore detections with negative coordinates
			if ((dets[0][i][0].item().toFloat() >= 0) &&
				(dets[0][i][1].item().toFloat() >= 0) &&
				(dets[0][i][2].item().toFloat() >= 0) &&
				(dets[0][i][3].item().toFloat() >= 0)) {
				m_region.push_back(Region(left, top, (right - left), (bottom - top), classID, score));
			}
		}
	}

	return(m_region);
}