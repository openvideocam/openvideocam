#include <algorithm>
#include "motion_tracker.h"

const int OFFSET = 20;

MdTracker::MdTracker(int width, int height, cv::Rect field_area, double fps) {
    m_width = width;
    m_height = height;
    m_field_area = field_area;
    m_fps = static_cast<int>(round(fps));
    m_frame_count = 0;
    m_initialized = false;
}

MdTracker::~MdTracker() {
}

vector<Rect> MdTracker::Detect(Mat* frame) {
    m_frame_count++;

    if ((m_frame_count % (m_fps * 2)) != 0) {
        ProcessFrame(frame);
    } 

    if ((m_frame_count % m_fps) != 0) {
        m_last_detected_regions = Detect();
    }

    return(m_last_detected_regions);
}

void MdTracker::ProcessFrame(Mat* frame) {

#ifdef CUDA
    cv::cuda::GpuMat frame_cuda;
    cv::cuda::GpuMat frame_gray;

    frame_cuda.upload(*frame);

    cv::cuda::cvtColor(frame_cuda, frame_gray, COLOR_BGR2GRAY);

    cv::Ptr<cv::cuda::Filter> filter = cv::cuda::createGaussianFilter(frame_gray.type(), frame_gray.type(), Size(21, 21), 0);
    filter->apply(frame_gray, frame_gray);

    if (!m_initialized) {
        m_previous_frame = frame_gray.clone();
        m_initialized = true;
        return;
    }
        cv::cuda::GpuMat diff_frame;
        cv::cuda::absdiff(m_previous_frame, frame_gray, diff_frame);

        //Store current frame in out previous frame variable so we can use it next time 
        m_previous_frame = frame_gray.clone();

        cv::cuda::GpuMat thresh_frame;
        cv::cuda::threshold(diff_frame, thresh_frame, 30, 255, cv::THRESH_BINARY);

        cv::cuda::GpuMat img_dilate_cuda;

        int erosionDilation_size = 5;
        Mat element = cv::getStructuringElement(MORPH_RECT, Size(2 * erosionDilation_size + 1, 2 * erosionDilation_size + 1));

        cv::Ptr<cuda::Filter> dilateFilter = cuda::createMorphologyFilter(MORPH_DILATE, thresh_frame.type(), element);
        dilateFilter->apply(thresh_frame, img_dilate_cuda);

        Mat img_dilate;

        img_dilate_cuda.download(img_dilate);
#else
    Mat frame_gray;
    cvtColor(*frame, frame_gray, COLOR_BGR2GRAY);

    cv::GaussianBlur(frame_gray, frame_gray, Size(21, 21), 0);

    if (!m_initialized) {
        m_previous_frame = frame_gray.clone();
        m_initialized = true;
        return;
    }

    Mat diff_frame;
    cv::absdiff(m_previous_frame, frame_gray, diff_frame);

    //Store current frame in out previous frame variable so we can use it next time 
    m_previous_frame = frame_gray.clone();

    Mat thresh_frame;
    cv::threshold(diff_frame, thresh_frame, 30, 255, cv::THRESH_BINARY);

    Mat img_dilate;
    cv::dilate(thresh_frame, img_dilate, Mat(), cv::Point(-1, -1), 2);
#endif

    vector<vector<Point>> contours;
    cv::findContours(img_dilate.clone(), contours, cv::RETR_EXTERNAL, cv::CHAIN_APPROX_SIMPLE);

    for (size_t i = 0; i < contours.size(); i++)
    {
        if (cv::contourArea(contours[i]) < 150)
            continue;

        Rect roi = cv::boundingRect(contours[i]);
 
        AddFramePositionToList(Rect(roi.x, roi.y, roi.width, roi.height));
        //m_regions.push_back(Rect(roi.x, roi.y, roi.width, roi.height));
    }
}

void MdTracker::AddFramePositionToList(Rect rct)
{
    for (int i = 0; i < m_regions.size(); i++) {
        if ((rct.x                <= (m_regions[i].x + m_regions[i].width  + OFFSET)) &&
            ((rct.x + rct.width)  >= (m_regions[i].x                       - OFFSET)) &&
            (rct.y                <= (m_regions[i].y + m_regions[i].height + OFFSET)) &&
            ((rct.y + rct.height) >= (m_regions[i].y                       - OFFSET))
            ) {
                int new_x = std::min<int>(m_regions[i].x, rct.x);
                int new_y = std::min<int>(m_regions[i].y, rct.y);
                int new_width = ((m_regions[i].x + m_regions[i].width) > (rct.x + rct.width)) ? (m_regions[i].x + m_regions[i].width - new_x) : (rct.x + rct.width - new_x);
                int new_heigth = ((m_regions[i].y + m_regions[i].height) > (rct.y + rct.height)) ? (m_regions[i].y + m_regions[i].height - new_y) : (rct.y + rct.height - new_y);

                m_regions[i].x = new_x;
                m_regions[i].y = new_y;
                m_regions[i].width = new_width;
                m_regions[i].height = new_heigth;

                return;
        }
    }  
    m_regions.push_back(rct);
}

vector<Rect> MdTracker::Detect() {

    vector<Rect> tmp_list;

    //Add detection areas that are out of the field area to a temp list
    for (int i = 0;i < m_regions.size(); i++) {

        if ((m_regions[i].x <= m_field_area.x) ||
            (m_regions[i].y <= m_field_area.y) ||
            ((m_regions[i].x + m_regions[i].width) >= (m_field_area.x + m_field_area.width)) ||
            ((m_regions[i].y + m_regions[i].height) >= (m_field_area.y + m_field_area.height))) {
            continue;
        }

        if (m_regions[i].height * 1.5 >= m_regions[i].width) {
            tmp_list.push_back(m_regions[i]);
        }
    }

    //Clear our regions list
    m_regions.clear();

    //Add our temp list back to the regions list
    for (int i = 0; i < tmp_list.size(); i++) {
        AddFramePositionToList(tmp_list[i]);
    }

    //Copy the regions to the result list
    vector<Rect> result;

    for (int i = 0; i < m_regions.size(); i++) {
        result.push_back(m_regions[i]);
    }

    //Clear the result list to it is ready for the next round
    m_regions.clear();

    return(result);
}

Rect MdTracker::GetContourFromRegions(vector<Rect> result) {
    int x0 = 10000;
    int x1 = 0;
    int y0 = 10000;
    int y1 = 0;

    for (int i = 0; i < result.size(); i++) {
        x0 = min(result[i].x, x0);
        y0 = min(result[i].y, y0);
        x1 = max(result[i].x + result[i].width, x1);
        y1 = max(result[i].y + result[i].height, y1);
    }

    return(Rect(x0, y0, x1 - x0, y1 - y0));
}

Rect MdTracker::DetectContour() {

    vector<Rect> result = Detect();

    int x0 = 10000;
    int y0 = 10000;
    int x1 = 0;
    int y1 = 0;

    for (int i = 0; i < result.size(); i++) {
        x0 = min(result[i].x, x0);
        y0 = min(result[i].y, y0);
        x1 = max(result[i].x + result[i].width, x1);
        y1 = max(result[i].y + result[i].height, y1);
    }

    return(Rect(x0, y0, x1 - x0, y1 - y0));
}