#ifndef _OUTPUT_VIDEO_CREATOR_H_
#define _OUTPUT_VIDEO_CREATOR_H_

#include <string>
#include "worker_thread.h"
#include "vmd.h"

using namespace std;

class VideoDefinition
{
private:
	int m_id;
	wstring m_name;
	int m_frame_count;

public:
	VideoDefinition(int id, wstring name, int frame_count) {
		m_id = id;
		m_name = name;
		m_frame_count = frame_count;
	}

	int getId() { return m_id; }
	wstring getName() { return m_name; }
	int getFrameCount() { return m_frame_count; }
};

class ObjectDefinition
{
private:
	int m_video_id;
	int m_frame_id;
	int m_object_id;
	double m_confidence_level;
	int m_x;
	int m_y;
	int m_width;
	int m_height;

public:
	ObjectDefinition(int video_id, int frame_id) {
		m_video_id = video_id;
		m_frame_id = frame_id;
		m_object_id = -1;
		m_confidence_level = 0.0;
		m_x = -1;
		m_y = -1;
		m_width = -1;
		m_height = -1;
	}
	ObjectDefinition(int video_id, int frame_id, int object_id, double confidence_level, int x, int y, int width, int height) {
		m_video_id = video_id;
		m_frame_id = frame_id;
		m_object_id = object_id;
		m_confidence_level = confidence_level;
		m_x = x;
		m_y = y;
		m_width = width;
		m_height = height;
	}

	int getVideoId() { return m_video_id; }
	int getFrameId() { return m_frame_id; }
	int getObjectId() { return m_object_id; }
	double getConfidenceLevel() { return m_confidence_level; }
	int getX() { return m_x; }
	int getY() { return m_y; }
	int getWidth() { return m_width; }
	int getHeight() { return m_height; }

	static bool Comparer(ObjectDefinition o1, ObjectDefinition o2) {
		if (o1.getVideoId() < o2.getVideoId()) {
			return true;
		}
		else if ((o1.getVideoId() == o2.getVideoId()) && (o1.getFrameId() < o2.getFrameId())) {
			return true;
		}
		return false;
	}
};

class VideoFrameObjectDetails {
private:
	int m_video_idx;
	int m_frame_idx;
	int m_object_idx;
	int m_frame_id;
	int m_object_id;
	double m_confidence;
	int m_x;
	int m_y;
	int m_width;
	int m_height;
	bool m_mark_to_delete;
public:
	VideoFrameObjectDetails(const VideoFrameObjectDetails& o) {
		m_video_idx = o.m_video_idx;
		m_frame_idx = o.m_frame_idx;
		m_object_idx = o.m_object_idx;
		m_frame_id = o.m_frame_id;
		m_object_id = o.m_object_id;
		m_confidence = o.m_confidence;
		m_x = o.m_x;
		m_y = o.m_y;
		m_width = o.m_width;
		m_height = o.m_height;
		m_mark_to_delete = o.m_mark_to_delete;
	}

	VideoFrameObjectDetails(int video_idx, int frame_idx, int object_idx, int frame_id, int object_id, double confidence, int x, int y, int width, int height) {
		m_video_idx = video_idx;
		m_frame_idx = frame_idx;
		m_object_idx = object_idx;
		m_object_id = object_id;
		m_frame_id = frame_id;
		m_confidence = confidence;
		m_x = x;
		m_y = y;
		m_width = width;
		m_height = height;
		m_mark_to_delete = false;
	}

	void MarkToDelete() { m_mark_to_delete = true; }

	int GetVideoIdx() { return m_video_idx; }
	int GetFrameIdx() { return m_frame_idx; }
	int GetObjectIdx() { return m_object_idx; }
	int GetFrameId() { return m_frame_id; }
	int GetObjectId() { return m_object_id; }
	double GetConfidence() { return m_confidence; }
	int GetX() { return m_x; }
	int GetY() { return m_y; }
	int GetWidth() { return m_width; }
	int GetHeight() { return m_height; }
	bool GetMarkToDelete() { return m_mark_to_delete; }	
};

class VirtualCamDataCreator
{
private:
	wstring m_saved_model;
	void PreProcessFrameObjects(VideoMetaData* metadata);
public:
	VirtualCamDataCreator();
	bool Create(wstring metadata_file, WorkerThread* owner);
};

class OutputVideoCreator
{
private:
	wstring m_saved_model;
	bool file_exists(const std::wstring& s);
public:
	OutputVideoCreator();
	bool Create(wstring meta_data_file, bool display_video, WorkerThread* owner);
};

#endif
