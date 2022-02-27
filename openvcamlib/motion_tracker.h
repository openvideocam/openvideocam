#ifndef _MOTION_TRACKER_H_
#define _MOTION_TRACKER_H_

#include <iostream>
#include <opencv2/opencv.hpp>

#ifdef CUDA
#include <opencv2/cudaimgproc.hpp>
#include <opencv2/cudafilters.hpp>
#include <opencv2/cudaarithm.hpp>
#endif

using namespace std;
using namespace cv;

class MdFrame {
private:
    int m_video_id;
    int m_frame_idx;
    vector<cv::Rect> m_areas;
public:
    MdFrame(int video_id, int frame_idx, vector<cv::Rect> areas) {
        m_video_id = video_id;
        m_frame_idx = frame_idx;

        for (int i = 0; i < (int)areas.size(); i++)
            m_areas.push_back(areas[i]);
    }

    int GetVideoId() { return(m_video_id); }
    int GetFrameIdx() { return(m_frame_idx); }
    vector<cv::Rect>* GetAreas() { return(&m_areas); }
};

class MdTracker {
private:
    bool m_initialized;
    int m_fps;
    int m_frame_count;
    vector<Rect> m_last_detected_regions;
#ifdef CUDA
    cv::cuda::GpuMat m_previous_frame;
#else
    Mat m_previous_frame;
#endif
    cv::Rect m_field_area;
    int m_width;
    int m_height;
    vector<Rect> m_regions;
    void ProcessFrame(Mat* frame);
    vector<Rect> Detect();
    void AddFramePositionToList(Rect position);
public:
    MdTracker(int width, int height, cv::Rect field_area, double fps);
    ~MdTracker();
    vector<Rect> Detect(Mat* frame);
    Rect DetectContour();
    Rect GetContourFromRegions(vector<Rect> result);
};

#endif
