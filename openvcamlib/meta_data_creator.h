#ifndef _META_DATA_CREATOR_H_
#define _META_DATA_CREATOR_H_

#include <string>
#include "worker_thread.h"
#include "object_detection.h"
#include "vmd.h"
#include "motion_tracker.h"

using namespace std;

class MetaDataCreator
{
private:
	wstring m_saved_model;
	bool file_exists(const std::wstring& s);
	void process_frame(int frame_id, ObjectDetection* ts_tracker, VideoMetaData* vmd, int selected_video_id, double confidence_level, cv::Mat mat);
	vector<Region> process_frame_segmented_ts(cv::Mat mat, ObjectDetection* ts_tracker);
public:
	MetaDataCreator(wstring saved_model);
	bool Create(wstring input_videos_selection, wstring meta_data_file, WorkerThread* owner);
};

#endif