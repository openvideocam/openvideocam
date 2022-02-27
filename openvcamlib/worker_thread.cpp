#include "worker_thread.h"
#include "output_video_creator.h"
#include "meta_data_creator.h"

using namespace std;

void WorkerThread::CreateMetadataThreadFunc(wstring videos, wstring metadata, wstring saved_model) {
	MetaDataCreator meta_data_creator(saved_model);
	meta_data_creator.Create(videos, metadata, this);
	m_finalized = true;
	m_status = WorkerThreadStatus::Finalized;
}

void WorkerThread::CreateVirtualCamDataThreadFunc(wstring metadata) {
	VirtualCamDataCreator virtual_camera_creator;
	virtual_camera_creator.Create(metadata, this);
	m_finalized = true;
	m_status = WorkerThreadStatus::Finalized;
}

void WorkerThread::CreateVideoThreadFunc(wstring metadata, bool display_video) {
	OutputVideoCreator output_video_creator;
	output_video_creator.Create(metadata, display_video, this);
	m_finalized = true;
	m_status = WorkerThreadStatus::Finalized;
}

WorkerThread::WorkerThread() {
	m_initialized = false;
	m_finalized = false;
	m_status = WorkerThreadStatus::NotInitialized;
	m_stop_signal = false;
}

void WorkerThread::UpdateStatus(int video_idx, int frame_idx, int frame_count) {
	if (m_processing_status_callback != NULL)
		m_processing_status_callback(video_idx, frame_idx, frame_count);
}

void WorkerThread::RunMetaDataCreation(wstring video_list, wstring metadata_file, wstring tensorflow_saved_model_dir, ProcessingStatusHandler processing_status_callback) {
	m_processing_status_callback = processing_status_callback;
	m_initialized = true;
	m_finalized = false;
	m_status = WorkerThreadStatus::Running;
	m_stop_signal = false;
	m_thread = thread(&WorkerThread::CreateMetadataThreadFunc, this, video_list, metadata_file, tensorflow_saved_model_dir);
}

void WorkerThread::RunVCamDataCreation(wstring metadata_file, ProcessingStatusHandler processing_status_callback) {
	m_processing_status_callback = processing_status_callback;
	m_initialized = true;
	m_finalized = false;
	m_status = WorkerThreadStatus::Running;
	m_stop_signal = false;
	m_thread = thread(&WorkerThread::CreateVirtualCamDataThreadFunc, this, metadata_file);
}

void WorkerThread::RunOutVideoCreation(wstring metadata_file, bool display_video, ProcessingStatusHandler processing_status_callback) {
	m_processing_status_callback = processing_status_callback;
	m_initialized = true;
	m_finalized = false;
	m_status = WorkerThreadStatus::Running;
	m_stop_signal = false;
	m_thread = thread(&WorkerThread::CreateVideoThreadFunc, this, metadata_file, display_video);
}

bool WorkerThread::IsInitialized() {
	return m_initialized;
}

bool WorkerThread::IsFinalized() {
	return m_finalized;
}

WorkerThreadStatus WorkerThread::GetStatus() {
	return m_status;
}

int WorkerThread::Stop() {
	m_stop_signal = true;
	m_thread.join();
	m_finalized = true;
	m_status = WorkerThreadStatus::Finalized;

	return static_cast<int>(WorkerThreadStatus::Finalized);
}

int WorkerThread::Join() {
	m_thread.join();
	m_finalized = true;
	m_status = WorkerThreadStatus::Finalized;

	return static_cast<int>(WorkerThreadStatus::Finalized);
}

bool WorkerThread::HasToStop() {
	return m_stop_signal;
}
