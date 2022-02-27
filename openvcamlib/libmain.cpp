#define WIN32_LEAN_AND_MEAN

#ifndef LINUX
#include <Windows.h>
#endif

#include <vector>
#include <iostream>
#include <string>
#include <codecvt>

#include <opencv2/core.hpp> 
#include <opencv2/videoio.hpp>
#include <opencv2/opencv.hpp>

#include "libmain.h"
#include "openvcamlib.h"
#include "worker_thread.h"
#include "base64.h"
#include "platform_defs.h"

using namespace std;
using namespace cv;

WorkerThread worker_thread_pool[MAX_THREADS];

#ifndef LINUX
BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
		break;

	case DLL_THREAD_ATTACH:
		break;

	case DLL_THREAD_DETACH:
		break;

	case DLL_PROCESS_DETACH:
		break;
	}
	return(TRUE);
}
#endif
LIB_EXPORT int APIENTRY StartCreateVideoMetaData(const WCHAR* video_list, const WCHAR* metadata_file, const WCHAR* tensorflow_saved_model_dir, ProcessingStatusHandler processing_status_callback)
{
	for (int i = 0; i < MAX_THREADS; i++) {
		if (!worker_thread_pool[i].IsInitialized() || worker_thread_pool[i].IsFinalized()) {
			worker_thread_pool[i].RunMetaDataCreation(video_list, metadata_file, tensorflow_saved_model_dir, processing_status_callback);
			return i;
		}
	}
	return -1;
}

LIB_EXPORT int APIENTRY StartCreateVirtualCamData(const WCHAR* metadata_file, ProcessingStatusHandler processing_status_callback)
{
	for (int i = 0; i < MAX_THREADS; i++) {
		if (!worker_thread_pool[i].IsInitialized() || worker_thread_pool[i].IsFinalized()) {
			worker_thread_pool[i].RunVCamDataCreation(metadata_file, processing_status_callback);
			return i;
		}
	}
	return -1;
}

LIB_EXPORT int APIENTRY StartCreateOutputVideo(const WCHAR* metadata_file, BOOL display_video, ProcessingStatusHandler processing_status_callback)
{
	for (int i = 0; i < MAX_THREADS; i++) {
		if (!worker_thread_pool[i].IsInitialized() || worker_thread_pool[i].IsFinalized()) {
			worker_thread_pool[i].RunOutVideoCreation(metadata_file, (display_video == TRUE), processing_status_callback);
			return i;
		}
	}
	return -1;
}

LIB_EXPORT int APIENTRY GetOperationStatus(int thread_idx)
{
	if ((thread_idx >= MAX_THREADS) || (thread_idx < 0))
		return(ERROR_INVALID_THREAD_IDX_RANGE);

	return static_cast<int>(worker_thread_pool[thread_idx].GetStatus());
}

LIB_EXPORT int APIENTRY StopOperation(int thread_idx)
{
	if ((thread_idx >= MAX_THREADS) || (thread_idx < 0))
	{
		//Return error
		return(ERROR_INVALID_THREAD_IDX_RANGE);
	}

	return worker_thread_pool[thread_idx].Stop();
}

LIB_EXPORT int APIENTRY WaitForOperationToFinish(int thread_idx)
{
	if ((thread_idx >= MAX_THREADS) || (thread_idx < 0))
		return(ERROR_INVALID_THREAD_IDX_RANGE);

	int result = worker_thread_pool[thread_idx].Join();

	return(result);
}

LIB_EXPORT int APIENTRY GetSnapshotFromVideo(const WCHAR* video_file, int width, int height, BYTE* buffer, int* buffer_size)
{
	string video_file_utf8 = wstring_convert <codecvt_utf8<wchar_t>>().to_bytes(video_file);
	VideoCapture inputVideo(video_file_utf8);

	//Check if we actually have a video opened
	if (!inputVideo.isOpened())
	{
		wcout << L"Could not open the video: " << video_file << endl;
		return(0);
	}

	Mat frame;

	if (inputVideo.read(frame))
	{
		//Create a resized frame if one is needed
		cv::Mat resized_frame;
		if ((width > 0) && (height > 0))
			cv::resize(frame, resized_frame, cv::Size(width, height));

		std::vector<uchar> buff;//buffer for coding
		std::vector<int> param(2);
		//param[0] = cv::IMWRITE_JPEG_QUALITY;
		//param[1] = 100;
		cv::imencode(".jpg", (((width > 0) && (height > 0)) ? resized_frame : frame), buff); // buff, param);

		memcpy(buffer, buff.data(), buff.size());

		*buffer_size = (int)buff.size();

		return (int)buff.size();
	}

	return 0;
}

LIB_EXPORT int APIENTRY GetVideoDetails(const WCHAR* video_file, int* frame_count, double* fps, int* width, int* height, int* size)
{
	string video_file_utf8 = wstring_convert <codecvt_utf8<wchar_t>>().to_bytes(video_file);
	VideoCapture inputVideo(video_file_utf8);

	//Check if we actually have a video opened
	if (!inputVideo.isOpened())
	{
		wcout << L"Could not open the video: " << video_file << endl;
		return(0);
	}

	*frame_count = (int)inputVideo.get(cv::CAP_PROP_FRAME_COUNT);
	*fps = inputVideo.get(CAP_PROP_FPS);
	Size frame_size = Size((int)inputVideo.get(CAP_PROP_FRAME_WIDTH), (int)inputVideo.get(CAP_PROP_FRAME_HEIGHT));
	*width = frame_size.width;
	*height = frame_size.height;
	*size = 0;

	return 1;
}

LIB_EXPORT int APIENTRY GetImageAsBase64(const WCHAR* file, WCHAR* base64_buffer, int buffer_size)
{
	//Load image file
	std::string logo_filename_utf8 = std::wstring_convert <std::codecvt_utf8<wchar_t>>().to_bytes(file);
	cv::Mat logo = imread(logo_filename_utf8);

	//Return false if we fail opening image
	if (logo.empty())
		return 0;

	//If image is greater than received buffer, just return false (0)
	if (logo.rows * logo.cols * logo.channels() > buffer_size)
		return 0;

	//Resize our BYTE buffer based on image size
	vector<BYTE> logo_image; 
	logo_image.resize(logo.rows * logo.cols * logo.channels());

	//Encode image, store in our logo_image and encode to base64
	cv::imencode(".png", logo, logo_image);
	wstring logo_base64 = base64::encode(logo_image);

	//Copy enconded image to the return buffer
	wcscpy(base64_buffer, logo_base64.c_str());

	return 1;
}