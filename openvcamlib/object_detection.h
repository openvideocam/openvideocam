#ifndef _OBJECT_DETECTION_H_
#define _OBJECT_DETECTION_H_

#include <iostream>
#include <opencv2/opencv.hpp>
#include <torch/script.h>

using namespace std;
using namespace cv;

const int ID_BALL = 1;
const int ID_GOAL = 2;
const int ID_FLAG = 3;
const int ID_MOTION_AREA = 4;

class Region {
protected:
    int m_x;
    int m_y;
    int m_width;
    int m_height;
    int m_label_id;
    double m_confidence_level;

public:
    Region(int x, int y, int width, int height) {
        m_x = x;
        m_y = y;
        m_width = width;
        m_height = height;
        m_label_id = -1;
        m_confidence_level = 0.0;
    }
    Region(Rect r) {
        m_x = r.x;
        m_y = r.y;
        m_width = r.width;
        m_height = r.height;
        m_label_id = -1;
        m_confidence_level = 0.0;
    }
    Region(int x, int y, int width, int height, int label_id, double confidence_level) {
        m_x = x;
        m_y = y;
        m_width = width;
        m_height = height;
        m_label_id = label_id;
        m_confidence_level = confidence_level;
    }

    int getX() { return(m_x); }
    int getY() { return(m_y); }
    int getWidth() { return(m_width); }
    int getHeight() { return(m_height); }
    int getLabelId() { return(m_label_id); }
    double getConfidenceLevel() { return(m_confidence_level); }

    void setX(int v) { m_x = v; }
    void setY(int v) { m_y = v; }
    void setWidth(int v) { m_width = v; }
    void setHeight(int v) { m_height = v; }
    void setLabelId(int v) { m_label_id = v; }
    void setConfidenceLevel(int v) { m_confidence_level = v; }
};

struct OD_Result {
  float* boxes;
  float* scores;
  float* label_ids;
  float* num_detections;
};

class ObjectDetection
{
private:
    bool m_cuda_available;
    bool m_initialized;
    torch::jit::script::Module m_module;
    c10::DeviceType m_device_type;
    std::wstring m_last_error_msg;

    std::vector<torch::Tensor> non_max_suppression(torch::Tensor preds, float score_thresh = 0.5, float iou_thresh = 0.5);
public:
    ObjectDetection(wstring saved_model);
    ~ObjectDetection();
    vector<Region> Detect(Mat* img);
    bool Initialized() {
        return (m_initialized);
    }
    std::wstring GetLastError() {
        return (m_last_error_msg);
    }
    bool CudaAvailable() {
        return(m_cuda_available);
    }
};

#endif
