#include <iostream>
#include <codecvt>
#include <locale>
#include <time.h>

#ifdef LINUX
#include <sys/stat.h>
#endif

#include <opencv2/core.hpp> 
#include <opencv2/videoio.hpp>
#include <opencv2/opencv.hpp>
#include <omp.h>

#include "meta_data_creator.h"
#include "object_detection.h"
#include "utils.h"
#include "vmd.h"
#include "motion_tracker.h"

//using namespace std;
using namespace cv;

bool MetaDataCreator::file_exists(const std::wstring& s)
{
#ifdef LINUX
    string s_utf8 = wstring_convert <codecvt_utf8<wchar_t>>().to_bytes(s);
    struct stat buffer;
    return (stat(s_utf8.c_str(), &buffer) == 0);
#else
    DWORD dwAttrib = GetFileAttributes(s.c_str());
    return ((dwAttrib != INVALID_FILE_ATTRIBUTES) && !(dwAttrib & FILE_ATTRIBUTE_DIRECTORY));
#endif
}

MetaDataCreator::MetaDataCreator(wstring saved_model) {
    m_saved_model = saved_model;
}

bool MetaDataCreator::Create(wstring input_videos_selection, wstring meta_data_file, WorkerThread* owner)
{
    //Store indexes of the videos to be processed
    vector<int> videos_selection = string_vector_to_int(split_string(input_videos_selection, L","));

    //Load meta data datatabase object from disk
    VideoMetaData vmd;
    vmd.Load(meta_data_file);

    //Declare a variable to count the number of frames being processed
    int counter = 0;

    //Go over every video index provided
    for(int i = 0;i < videos_selection.size();i++)
    {
        //Get video
        Video* selected_video = vmd.GetVideoById(videos_selection[i]);

        //If video not found, just return an error and abort
        if (selected_video == NULL)
        {
            wcout << L"Invalid video id" << endl;
                return(false);
        }

        int selected_video_index = videos_selection[i] - 1;

        wstring selected_video_name = selected_video->GetName();

        int selected_video_id = selected_video->GetId();

        double confidence_level = vmd.GetTargetConfidenceLevel();

        //Try using a relative path if filename does not point to a valid filename
        if (!file_exists(selected_video_name))
        {
            //Get path of meta data file
            const size_t last_slash_idx = meta_data_file.rfind('\\');
            wstring meta_data_file_path = (std::string::npos != last_slash_idx) ? meta_data_file.substr(0, last_slash_idx+1) : L"";
            
            //Build filename using path of the meta data file
            wstring video_name_with_path = meta_data_file_path + selected_video_name;

            //If file with relative path is not valid, just return an error
            if (!file_exists(video_name_with_path))
            {
                wcout << L"Could not open the input video: " << selected_video->GetName() << endl;
                return(false);
            }

            //Update file name using the relative path one
            selected_video_name = video_name_with_path;
        }

        //Open video
        string input_video_utf8 = wstring_convert <codecvt_utf8<wchar_t>>().to_bytes(selected_video_name);
        VideoCapture inputVideo(input_video_utf8);

        //Check if we actually have a video opened
        if (!inputVideo.isOpened())
        {
            wcout << L"Could not open the input video: " << selected_video_name << endl;
            return(false);
        }

        //Store video parameters in helper variables
        Size frame_size = Size((int)inputVideo.get(CAP_PROP_FRAME_WIDTH), (int)inputVideo.get(CAP_PROP_FRAME_HEIGHT));
        double fps = inputVideo.get(CAP_PROP_FPS);
        int fourcc = static_cast<int>(inputVideo.get(CAP_PROP_FOURCC));
        int frame_count = (int)inputVideo.get(cv::CAP_PROP_FRAME_COUNT);
        
        //Create tensorflow tracker
        ObjectDetection* ts_tracker = new ObjectDetection(m_saved_model);

        //Cancel the meta data creation if initialization fails
        if (!ts_tracker->Initialized())
        {
            wcout << L"Object Detection initialization failed (error: " << ts_tracker->GetLastError() << L")" << endl;
            return(false);
        }

        //Display a console message with CUDA status
        if (ts_tracker->CudaAvailable())
            wcout << L"CUDA active" << endl;
        else
            wcout << L"CUDA NOT active" << endl;

        //Start loop that will go over all frames in the video file
        Mat frame;
       
        //Clear current video frames
        vmd.ClearVideoFrames(selected_video_id);

        //Declare vector to store mats motion detected areas
        int core_count = 1; // omp_get_num_procs();
        vector<Mat> mat_list;
        vector<int> frame_id_list;

        int empty_frames_in_seq = 0;

        for (int frame_idx = 0; frame_idx < frame_count; frame_idx++)
        {
            //Read video frame
            inputVideo.read(frame);

            //If frame is empty, move frame index back and read next frame
            // WE DID THIS BECAUSE GOPROs GENERATE VIDEOS WITH EMPTY FRAMES AND THIS WAS
            // CAUSING THE TRADITIONAL while(true) ... read() ... TO FAIL
            if (frame.empty()) {
                frame_idx--;
                empty_frames_in_seq++;

                //If we detect more than 1000 empty frames in sequence
                //we consider the video as finished
                if (empty_frames_in_seq > 1000)
                    break;

                continue;
            }

            empty_frames_in_seq = 0;

            //Call status handler to update whoever is watching us
            if (owner != NULL)
                owner->UpdateStatus(selected_video_id, frame_idx, frame_count);

            if (owner != NULL) {
                if (owner->HasToStop()) {
                    //Delete TensorFlow tracker
                    delete ts_tracker;

                    //Save metadata object back to a file
                    vmd.Save();

                    //return false as process was cancelled
                    return(false);
                }
            }

            //Increment our counter
            counter++;

            //Add new frame to our frame list
            int new_frame_id = vmd.AddVideoFrame(selected_video_id);

            //Only process every N frames
            if ((counter % 3) != 0)
                continue;

            //Add frame to frame list and also store frame_id
            mat_list.push_back(frame.clone());
            frame_id_list.push_back(new_frame_id);
 
            //Check if we have to process frames and write to vmd or if we should only store the frame in our list and move to next frame
            if (((int)mat_list.size() >= core_count) ||
                ((int)mat_list.size() > 0 && (frame_idx == frame_count - 1)))
            {
                //omp_set_dynamic(0);
                //omp_set_num_threads(core_count);

                #pragma omp parallel for
                for (int x = 0; x < (int)mat_list.size(); x++)
                {                       
                    process_frame(frame_id_list[x], ts_tracker, &vmd, selected_video_id, confidence_level, mat_list[x]);
                }
                mat_list.clear();
                frame_id_list.clear();
            }
        }
        
        //Delete TensorFlow tracker
        delete ts_tracker;
    }

    //Finally save metadata object back to a file
    vmd.Save();

    return(true);
}
void MetaDataCreator::process_frame(int frame_id, ObjectDetection* ts_tracker, VideoMetaData* vmd, int selected_video_id, double confidence_level, cv::Mat mat)
{
    //Execute detection on frame 
    vector<Region> region_ts = ts_tracker->Detect(&mat);

    //Go over each detected region and add it to TB_FRAME_OBJECTS
    for (int j = 0; j < (int)region_ts.size(); j++)
    {
        if (region_ts[j].getConfidenceLevel() >= confidence_level)
        {
            #pragma omp critical
            vmd->AddVideoFrameObject(selected_video_id, frame_id,
                region_ts[j].getLabelId(),
                region_ts[j].getConfidenceLevel(),
                region_ts[j].getX(),
                region_ts[j].getY(),
                region_ts[j].getWidth(),
                region_ts[j].getHeight());
        }
    }
}

vector<Region> MetaDataCreator::process_frame_segmented_ts(cv::Mat mat, ObjectDetection* ts_tracker) {

    const int RECT_SIZE = 768;

    vector<Region> result;

    int x = 0;
    while (x < mat.size().width) {
        int width = ((x + RECT_SIZE) < mat.size().width) ? RECT_SIZE : mat.size().width - x;

        int y = 0;
        while (y < mat.size().height) {
            int height = ((y + RECT_SIZE) < mat.size().height) ? RECT_SIZE : mat.size().height - y;

            cv::Rect area = cv::Rect(x, y, width, height);

            cv::Mat cropped_mat = mat(area);

            vector<Region> area_rects = ts_tracker->Detect(&cropped_mat);

            for (int z = 0; z < (int)area_rects.size(); z++) {
                result.push_back(cv::Rect(area_rects[z].getX() + x, area_rects[z].getY() + y, area_rects[z].getWidth(), area_rects[z].getHeight()));
            }

            y = y + RECT_SIZE;
        }

        x = x + RECT_SIZE;
    }

    return(result);
}
