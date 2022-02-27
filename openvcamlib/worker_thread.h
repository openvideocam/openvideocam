#ifndef _WORKER_THREAD_H_
#define _WORKER_THREAD_H_
#define WIN32_LEAN_AND_MEAN
#include <string>
#include <thread>
#include <atomic>
#include "openvcamlib.h"

using namespace std;

const int MAX_THREADS = 10;

enum class WorkerThreadStatus {
	NotInitialized = 0,
	Running = 1,
	Finalized = 2
};

class WorkerThread {
private:
	thread m_thread;
	atomic<bool> m_initialized;
	atomic<bool> m_finalized;
	atomic<WorkerThreadStatus> m_status;
	atomic<bool> m_stop_signal;
	ProcessingStatusHandler m_processing_status_callback;

	void CreateMetadataThreadFunc(wstring videos, wstring metadata, wstring saved_model);
	void CreateVirtualCamDataThreadFunc(wstring metadata);
	void CreateVideoThreadFunc(wstring metadata, bool display_video);
public:
	WorkerThread();
	void UpdateStatus(int video_idx, int frame_idx, int frame_count);
	void RunMetaDataCreation(wstring video_list, wstring metadata_file, wstring tensorflow_saved_model_dir, ProcessingStatusHandler processing_status_callback);
	void RunVCamDataCreation(wstring metadata_file, ProcessingStatusHandler processing_status_callback);
	void RunOutVideoCreation(wstring metadata_file, bool display_video, ProcessingStatusHandler processing_status_callback);
	bool IsInitialized();
	bool IsFinalized();
	WorkerThreadStatus GetStatus();
	int Stop();
	int Join();
	bool HasToStop();
};

#endif
