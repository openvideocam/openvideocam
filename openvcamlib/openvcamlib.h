#ifndef _OPENVCAMLIBH_
#define _OPENVCAMLIBH_

#ifndef LINUX
#define WIN32_LEAN_AND_MEAN
#include <Windows.h>
#endif 

#include "platform_defs.h"

#define OPERATION_OK                     1
#define OPERATION_FAILED                -1
#define ERROR_NO_THREAD_SPOT_AVAILABLE  -2
#define ERROR_NO_THREAD_FAILED_TO_START -3
#define ERROR_INVALID_THREAD_IDX_RANGE  -4
#define ERROR_OPERATION_NOT_RUNNING     -5
#define ERROR_FAILED_STOPING_OPERATION  -6

#ifdef LINUX
#define LIB_EXPORT
#define APIENTRY
#else
#define LIB_EXPORT __declspec(dllexport)
#endif

typedef int(APIENTRY* ProcessingStatusHandler)(int, int, int);

extern "C"
{
	LIB_EXPORT int APIENTRY StartCreateVideoMetaData(const WCHAR* video_list, const WCHAR* metadata_file, const WCHAR* tensorflow_saved_model_dir, ProcessingStatusHandler processing_status_callback);

	LIB_EXPORT int APIENTRY StartCreateVirtualCamData(const WCHAR* metadata_file, ProcessingStatusHandler processing_status_callback);

	LIB_EXPORT int APIENTRY StartCreateOutputVideo(const WCHAR* metadata_file, BOOL display_video, ProcessingStatusHandler processing_status_callback);

	LIB_EXPORT int APIENTRY GetOperationStatus(int thread_idx);

	LIB_EXPORT int APIENTRY StopOperation(int thread_idx);

	LIB_EXPORT int APIENTRY WaitForOperationToFinish(int thread_idx);

	LIB_EXPORT int APIENTRY GetSnapshotFromVideo(const WCHAR* video_file, int width, int height, BYTE* buffer, int* buffer_size);

	LIB_EXPORT int APIENTRY GetVideoDetails(const WCHAR* video_file, int* frame_count, double* fps, int* width, int* height, int* size);

	LIB_EXPORT int APIENTRY GetImageAsBase64(const WCHAR* file, WCHAR* base64_buffer, int buffer_size);
}

#endif


