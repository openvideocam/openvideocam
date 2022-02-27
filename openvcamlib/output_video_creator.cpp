#include <iostream>
#include <string>
#include <codecvt>
#include <locale>
#include <time.h>

#include <opencv2/core.hpp> 
#include <opencv2/videoio.hpp>
#include <opencv2/opencv.hpp>

#ifdef LINUX
#include <sys/stat.h>
#endif

#include "utils.h"
#include "output_video_creator.h"
#include "vmd.h"
#include "worker_thread.h"
#include "auto_camera.h"
#include "geometry.h"

using namespace std;
using namespace cv;

//Constants to be used in final video size (always HD, assuming the input in Ultra HD)
const int HD_WIDTH = 1920;
const int HD_HEIGHT = 1080;

VirtualCamDataCreator::VirtualCamDataCreator() {
}

void VirtualCamDataCreator::PreProcessFrameObjects(VideoMetaData* metadata)
{
    //Declare our helper frame objects vector
    vector<VideoFrameObjectDetails> frame_objects;

    //Load existing frame objects to our vector
    vector<Video>* videos = metadata->GetVideos();
    for (int i = 0; i < (*videos).size(); i++)
    {
        vector<VideoFrame>* video_frames = (*videos)[i].GetFrames();
        for (int j = 0; j < (*video_frames).size(); j++)
        {
            vector<VideoFrameObject>* video_frame_objects = (*video_frames)[j].GetObjects();
            for (int k = 0; k < (*video_frame_objects).size(); k++)
            {
                //We only load frme objects of ID_BALL type
                if ((*video_frame_objects)[k].GetObjectId() == ID_BALL) {
                    frame_objects.push_back(VideoFrameObjectDetails(
                        i, j, k,
                        (*video_frames)[j].GetFrameId(),
                        (*video_frame_objects)[k].GetObjectId(),
                        (*video_frame_objects)[k].GetConfidence(),
                        (*video_frame_objects)[k].GetX(),
                        (*video_frame_objects)[k].GetY(),
                        (*video_frame_objects)[k].GetWidth(),
                        (*video_frame_objects)[k].GetHeight()));
                }
            }
        }
    }

    //Mark all frames that are outside of the field limits for removal
    for (int i = 0; i < (int)frame_objects.size(); i++)
    {
        vector<cv::Point> field_limits = (*videos)[frame_objects[i].GetVideoIdx()].GetFieldLimits();
        int ball_x = frame_objects[i].GetX();
        int ball_y = frame_objects[i].GetY();
        int ball_w = frame_objects[i].GetWidth();
        int ball_h = frame_objects[i].GetHeight();

        if (!Polygon::IsInside(field_limits, cv::Rect(ball_x, ball_y, ball_w, ball_h)))
        {
            wcout << L"Deleting ball (" << ball_x << L"," << ball_y << L") at " << frame_objects[i].GetFrameId() << endl;
            frame_objects[i].MarkToDelete();
        }
    }

    //Mark all frames that are outside of the field limits for removal
    //for (int i = 0; i < (int)frame_objects.size(); i++)
    //{
    //    int s_x = frame_objects[i].GetX();
    //    int s_y = frame_objects[i].GetY();
    //    int s_w = frame_objects[i].GetWidth();
    //    int s_h = frame_objects[i].GetHeight();

    //    int count = 0;

    //    for (int j = 0; j < (int)frame_objects.size(); j++) {
    //        if (i == j)
    //            continue;

    //        int d_x = frame_objects[j].GetX();
    //        int d_y = frame_objects[j].GetY();
    //        int d_w = frame_objects[j].GetWidth();
    //        int d_h = frame_objects[j].GetHeight();

    //        if ((d_x > (int)(s_x * 0.95)) && (d_x < (int)(s_x * 1.05)) &&
    //            (d_y > (int)(s_y * 0.95)) && (d_y < (int)(s_y * 1.05)) &&
    //            (d_w > (int)(s_w * 0.95)) && (d_w < (int)(s_w * 1.05)) &&
    //            (d_h > (int)(s_h * 0.95)) && (d_h < (int)(s_h * 1.05)))
    //            count++;
    //    }

    //    if (count > 200) {
    //        for (int j = 0; j < (int)frame_objects.size(); j++) {

    //            int d_x = frame_objects[j].GetX();
    //            int d_y = frame_objects[j].GetY();
    //            int d_w = frame_objects[j].GetWidth();
    //            int d_h = frame_objects[j].GetHeight();

    //            if ((d_x > (int)(s_x * 0.95)) && (d_x < (int)(s_x * 1.05)) &&
    //                (d_y > (int)(s_y * 0.95)) && (d_y < (int)(s_y * 1.05)) &&
    //                (d_w > (int)(s_w * 0.95)) && (d_w < (int)(s_w * 1.05)) &&
    //                (d_h > (int)(s_h * 0.95)) && (d_h < (int)(s_h * 1.05)));
    //                    frame_objects[j].MarkToDelete();
    //        }
    //    }       
    //}

    //Now mark all frames that don't seem to be correct by using the following rational:
    //We check the frame before and after each frame being analized and if the frame
    //Is more than 20% greater of lesser in a delta of 30 frames (1 sec), this points
    //to a frame that is most likely incorrect (false positive for a ball)
    long i = 1;
    while (i < (int)frame_objects.size()-1)
    {
        //Only do any processing if the current 3 frame sequence is inside the same video
        if (frame_objects[i - (long)1].GetVideoIdx() == frame_objects[i + 1].GetVideoIdx())
        {
            int frame_delta = frame_objects[i + 1].GetFrameId() - frame_objects[i - 1].GetFrameId();

            int x0 = frame_objects[i - 1].GetX();
            int x1 = frame_objects[i].GetX();
            int x2 = frame_objects[i + 1].GetX();
            double x_avg = (x0 + x2) / 2.0;
            bool x_stable = (abs(x0 - x_avg) < x_avg * 0.2) && (abs(x2 - x_avg) < x_avg * 0.2);
            bool x1_outlier = (abs(x1 - x_avg) > x_avg * 0.2);

            int y0 = frame_objects[i - 1].GetY();
            int y1 = frame_objects[i].GetY();
            int y2 = frame_objects[i + 1].GetY();
            double y_avg = (y0 + y2) / 2.0;
            bool y_stable = (abs(y0 - y_avg) < y_avg * 0.2) && (abs(y2 - y_avg) < y_avg * 0.2);
            bool y1_outlier = (abs(y1 - y_avg) > y_avg * 0.2);

            if ((frame_delta < 30) && x_stable && y_stable && x1_outlier && y1_outlier)
            {
                frame_objects[i].MarkToDelete();
                i = i + 2;
                continue;
            }
        }
        i = i + 1;
    }

    //Actually remove the false positive frames
    for (int i = (int)frame_objects.size() - 1; i >= 0; i--)
    {
        if (frame_objects[i].GetMarkToDelete())
        {
            int video_idx = frame_objects[i].GetVideoIdx();
            int frame_idx = frame_objects[i].GetFrameIdx();
            int object_id = frame_objects[i].GetObjectIdx();

            vector<VideoFrameObject>* vfo = (*((*videos)[video_idx].GetFrames()))[frame_idx].GetObjects();

            vfo->erase(vfo->begin() + frame_objects[i].GetObjectIdx());
        }
    }
}

bool VirtualCamDataCreator::Create(wstring metadata_file, WorkerThread* owner)

{
    //Open meta data database
    VideoMetaData metadata;
    if (!metadata.Load(metadata_file))
    {
        wcout << endl << L"Error opening meta data file " << metadata_file << endl;
        return false;
    }

    //Pre process frame object as to remove anomalies
    PreProcessFrameObjects(&metadata);

    //Get target resolution
    int target_width = metadata.GetTargetWidth();
    int target_height = metadata.GetTargetHeight();

    //Clear current virtual camera data
    metadata.ClearVirtualCamPositions();

    //Add all frames to a frame list
    vector<VideoFrame*> frames;
    for (int i = 0; i < (int)(*metadata.GetVideos()).size(); i++)
    {
        for (int j = 0; j < (*(*metadata.GetVideos())[i].GetFrames()).size(); j++)
        {
            frames.push_back(&(*(*metadata.GetVideos())[i].GetFrames())[j]);
        }
    }

    if (metadata.GetVideos()->size() <= 0)   
    {
        wcout << endl << L"No input videos to process" << metadata_file << endl;
        return false;
    }
    
    cv::Size input_video_size = cv::Size((*metadata.GetVideos())[0].GetWidth(), (*metadata.GetVideos())[0].GetHeigth());
    cv::Size output_video_size = cv::Size(target_width, target_height);

    int min_y_field_limit = INT_MAX;
    vector<Point> field_limits = (*metadata.GetVideos())[0].GetFieldLimits();

    for (int i = 0; i < (int)field_limits.size(); i++) {
        min_y_field_limit = (field_limits[i].y < min_y_field_limit) ? field_limits[i].y : min_y_field_limit;
    }

    VirtualCamera camera(
        input_video_size, 
        frames,
        output_video_size, 
        metadata.GetDetectionMargin(),
        metadata.GetTargetConfidenceLevel(),
        metadata.GetCameraReaction(),
        (*metadata.GetVideos())[0].GetFps(),
        metadata.GetReactiontimeFromResting(),
        min_y_field_limit);

    int frame_idx = 0;
    int frame_count = (int)frames.size();
    while (frame_idx < frame_count - 1)
    {
        //Call status handler to update whoever is watching us
        if (owner != NULL) {
            owner->UpdateStatus(frames[frame_idx]->GetVideoId(), frames[frame_idx]->GetFrameId(),
                (int)metadata.GetVideoById(frames[frame_idx]->GetVideoId())->GetFrames()->size());
        }

        //Check if user wants us to stop the thread
        if (owner != NULL) {
            if (owner->HasToStop()) {
                return(false);
            }
        }

        //Get the list with camera positions
        vector<VirtualCamPosition> camera_positions = camera.MoveCamera(frame_idx);

        //Add camera positions to our metadata database
        for (int i = 0; i < (int)camera_positions.size(); i++) {
            metadata.AddVirtualCamPositon(frames[frame_idx]->GetVideoId(), frames[frame_idx]->GetFrameId(), camera_positions[i].GetX0(), camera_positions[i].GetY0(), camera_positions[i].GetX1(), camera_positions[i].GetY1());
            frame_idx++;
        }
    }

    metadata.SaveVirtualCamPositions();

    return true;
}

bool OutputVideoCreator::file_exists(const std::wstring& s)
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

OutputVideoCreator::OutputVideoCreator() {

}

bool OutputVideoCreator::Create(wstring meta_data_file, bool display_video, WorkerThread* owner)
{
    //Open meta data database
    VideoMetaData metadata;
    if (!metadata.Load(meta_data_file))
    {
        wcout << endl << L"Error opening meta data file " << meta_data_file << endl;
        return false;
    }
    
    //Get fourcc from metadata (if one exists)
    int output_video_fourcc = -1;
    wstring output_video_codec = metadata.GetOutputVideoCodec();
    if ((int)output_video_codec.size() == 4) {
        string output_video_codec_utf8 = wstring_convert <codecvt_utf8<wchar_t>>().to_bytes(output_video_codec.c_str());
        output_video_fourcc = cv::VideoWriter::fourcc(output_video_codec_utf8[0], output_video_codec_utf8[1], output_video_codec_utf8[2], output_video_codec_utf8[3]);
    } 

    //Get confidence level to be used
    double target_confidence_level = metadata.GetTargetConfidenceLevel();

    //Check if we have a logo to use and load it
    cv::Mat resized_logo;
    if (metadata.LogoExists()) {
        cv::Mat original_logo;
        original_logo = metadata.GetLogoImage();
        int logo_width = metadata.GetTargetWidth() / 10;
        int logo_height = (int)(original_logo.rows * (logo_width * 1.0 / original_logo.cols));
        cv::resize(original_logo, resized_logo, cv::Size(logo_width, logo_height));
    }

    //Create our video writer object
    VideoWriter outputVideo;

    //Create video window
    if (display_video) {
        namedWindow("Video", cv::WINDOW_NORMAL);
        resizeWindow("Video", 720, 480);
    }

    //Get frame_count based on all videos
    int frame_count = 0;
    for (int idx = 0; idx < (int)metadata.GetVideos()->size(); idx++)
    {
        Video* video = &(*metadata.GetVideos())[idx];
        frame_count = frame_count + video->GetFrameCount();
    }

    //Set initial frame to 1
    int current_frame = 1;

    //Go over all videos
    for (int idx = 0; idx < (int)metadata.GetVideos()->size(); idx++)
    {
        Video* video = &(*metadata.GetVideos())[idx];

        //Extract meta data file directory as we assume all videos defined in TB_VIDEOS are in the same directory as the meta data file
        //const size_t last_slash_idx = meta_data_file.rfind('\\');
        //wstring dir = (std::string::npos != last_slash_idx) ? meta_data_file.substr(0, last_slash_idx + 1) : L"";
        wstring selected_video_name = video->GetName();

        //Try using a relative path if filename does not point to a valid filename
        if (!file_exists(selected_video_name))
        {
            //Get path of meta data file
            const size_t last_slash_idx = meta_data_file.rfind('\\');
            wstring meta_data_file_path = (std::string::npos != last_slash_idx) ? meta_data_file.substr(0, last_slash_idx + 1) : L"";

            //Build filename using path of the meta data file
            wstring video_name_with_path = meta_data_file_path + selected_video_name;

            //If file with relative path is not valid, just return an error
            if (!file_exists(video_name_with_path))
            {
                wcout << L"Could not open the input video: " << video->GetName() << endl;
                return(false);
            }

            //Update file name using the relative path one
            selected_video_name = video_name_with_path;
        }

        //Open video
        string input_video_utf8 = wstring_convert <codecvt_utf8<wchar_t>>().to_bytes(selected_video_name.c_str());
        VideoCapture inputVideo(input_video_utf8);

        //Check if we actually have a video opened
        if (!inputVideo.isOpened())
        {
            wcout << L"Could not open the input video: " << video->GetName() << endl;
            return false;
        }

        //Store video parameters in helper variables
        double fps = inputVideo.get(CAP_PROP_FPS);
        int fourcc = (output_video_fourcc == -1) ? static_cast<int>(inputVideo.get(CAP_PROP_FOURCC)) : output_video_fourcc;
        int video_frame_count = (int)inputVideo.get(cv::CAP_PROP_FRAME_COUNT);
        int input_video_width = (int)inputVideo.get(cv::CAP_PROP_FRAME_WIDTH);
        int input_video_height = (int)inputVideo.get(cv::CAP_PROP_FRAME_HEIGHT);

        //Create the output video if we are in the first input video 
        if (idx == 0) {
            //Try opening the video 
            string output_video_utf8 = wstring_convert <codecvt_utf8<wchar_t>>().to_bytes(metadata.GetOutputVideoName());
            if (!outputVideo.open(output_video_utf8, 828601953 /*fourcc*/, fps, Size(metadata.GetTargetWidth(), metadata.GetTargetHeight()), true))
            {
                wcout << L"Could not open the output video: " << metadata.GetOutputVideoName() << endl;
                return false;
            }
        }

        //Start loop that will go over all frames in the video file
        Mat frame;
        Rect rect(0,0,0,0);

        //Get possibble start and end frame
        int start_frame = (int)std::round(video->GetSelectionStart() * fps);
        int end_frame = (int)std::round(video->GetSelectionEnd() * fps);

        //Make sure start_frame is inside bounds, otherwise set it to 0 (initial frame)
        if ((start_frame < 0) && (start_frame >= video_frame_count))
            start_frame = 0;

        //Make sure end frame is lesser than start frame and inside bounds, otherwise set it to video_frame_count
        if ((end_frame >= start_frame) || (end_frame >= video_frame_count))
            end_frame = video_frame_count;

        int empty_frames_in_seq = 0;

        for (int i = 0; i < end_frame; i++)
        {
            //Read video frame
            inputVideo.read(frame);

            //If frame is empty, move frame index back and read next frame
            // WE DID THIS BECAUSE GOPROs GENERATE VIDEOS WITH EMPTY FRAMES AND THIS WAS
            // CAUSING THE TRADITIONAL while(true) ... read() ... TO FAIL
            if (frame.empty()) {
                i--;

                empty_frames_in_seq++;

                //If we detect more than 1000 empty frames in sequence
                //we consider the video as finished
                if (empty_frames_in_seq > 1000)
                    break;

                continue;
            }

            empty_frames_in_seq = 0;

            //Increment out current frame count
            current_frame++;

            //Call status handler to update whoever is watching us
            if (owner != NULL)
                owner->UpdateStatus(idx, current_frame, frame_count);

            if (owner != NULL) {
                if (owner->HasToStop()) {
                    return(false);
                }
            }

            //If current frame is lesser than start_frame, just skip to next frame without doing anything
            if (i < start_frame)
                continue;

            //Get virtual camera position
            if ((int)(*(*video->GetFrames())[i].GetVirtualCamPositions()).size() > 0) {
                rect.x = (*(*video->GetFrames())[i].GetVirtualCamPositions())[0].GetX0();
                rect.y = (*(*video->GetFrames())[i].GetVirtualCamPositions())[0].GetY0();
                rect.width = (*(*video->GetFrames())[i].GetVirtualCamPositions())[0].GetX1() - (*(*video->GetFrames())[i].GetVirtualCamPositions())[0].GetX0();
                rect.height = (*(*video->GetFrames())[i].GetVirtualCamPositions())[0].GetY1() - (*(*video->GetFrames())[i].GetVirtualCamPositions())[0].GetY0();
            }

            //Just return false if we get invalid coordinates
            if ((rect.x < 0) || (rect.y < 0) || (rect.width <= 0) || (rect.height <= 0)) {
                return(false);
            }

            //Crop frame
            cv::Mat cropped_mat = frame(rect);

            //Resize the cropped_frame if it does not have the size of the target video
            if ((rect.width != metadata.GetTargetWidth()) || rect.height != metadata.GetTargetHeight()) {
                cv::resize(cropped_mat, cropped_mat, cv::Size(metadata.GetTargetWidth(), metadata.GetTargetHeight()));
            }

            //Add logo to output (if it is not empty)
            if (!resized_logo.empty()) 
                resized_logo.copyTo(cropped_mat(cv::Rect(cropped_mat.cols - resized_logo.cols - 10, 10, resized_logo.cols, resized_logo.rows)));

            //Write to video output
            outputVideo.write(cropped_mat);         
            
            //If user did set display_video flag, add visual clues (ball, field size, etc...) to fram and display it
            if (display_video) {
                vector<cv::Point> field_limits = video->GetFieldLimits();

                for (int j = 0; j < (int)field_limits.size(); j++) {
                    int next = (j + 1) % (int)field_limits.size();
                    cv::line(frame, cv::Point(field_limits[j].x, field_limits[j].y), cv::Point(field_limits[next].x, field_limits[next].y), CV_RGB(4, 212, 2), 4, cv::LineTypes::LINE_4);
                }

                cv::rectangle(frame, cv::Point(rect.x+100, rect.y+100), cv::Point(rect.x + rect.width - 100, rect.y + rect.height - 100), CV_RGB(100, 100, 100), 4, cv::LineTypes::LINE_4);
                cv::rectangle(frame, cv::Point(rect.x, rect.y), cv::Point(rect.x + rect.width, rect.y + rect.height), CV_RGB(0, 0, 0), 4, cv::LineTypes::LINE_4);

                int total_x = 0;
                int total_y = 0;
                int total_weight = 0;
            
                for (int j = 0; j < (*video->GetFrames())[i].GetObjects()->size(); j++) {
                    VideoFrameObject o = (*(*video->GetFrames())[i].GetObjects())[j];

                    int x0 = o.GetX();
                    int y0 = o.GetY();
                    int x1 = (x0 + o.GetWidth() > input_video_width) ? input_video_width - x0 : x0 + o.GetWidth();
                    int y1 = (y0 + o.GetHeight() > input_video_height) ? input_video_height - y0 : y0 + o.GetHeight();

                    if ((o.GetConfidence() >= target_confidence_level) && (o.GetObjectId() == ID_BALL)) {
                        if (o.GetConfidence() < 0.5)
                            cv::rectangle(frame, Point(x0, y0), Point(x1, y1), CV_RGB(255, 0, 0), 4, cv::LineTypes::LINE_4);
                        else if ((o.GetConfidence() >= 0.5) && (o.GetConfidence() <= 0.8))
                            cv::rectangle(frame, Point(x0, y0), Point(x1, y1), CV_RGB(255, 255, 0), 4, cv::LineTypes::LINE_4);
                        if (o.GetConfidence() > 0.8)
                            cv::rectangle(frame, Point(x0, y0), Point(x1, y1), CV_RGB(0, 128, 0), 4, cv::LineTypes::LINE_4);
                    } else if (o.GetObjectId() == ID_MOTION_AREA) {
                            //cv::rectangle(frame, Point(x0, y0), Point(x1, y1), CV_RGB(255, 165, 0), 4, cv::LineTypes::LINE_4);

                            //double vertical_factor = 1.0 + ((field_limits.y + field_limits.height) - min((field_limits.y + field_limits.height), o.GetY())) / (field_limits.y + field_limits.height);
                            //double weight = o.GetWidth() * o.GetHeight() * vertical_factor;
                            //total_x = total_x + o.GetX() * weight;
                            //total_y = total_y + o.GetY() * weight;
                            //total_weight = total_weight + weight;
                    }
                }

                int avg_x = (total_weight == 0) ? 0 : total_x / total_weight;
                int avg_y = (total_weight == 0) ? 0 : total_y / total_weight;

                if ((avg_x != 0) && (avg_y != 0)) {
                    cv::rectangle(frame, Point(avg_x - 10, avg_y - 10), Point(avg_x + 20, avg_y + 20), CV_RGB(248, 24, 148), 4, cv::LineTypes::LINE_4);
                }

                //Add logo to output (if it is not empty)
                if (!resized_logo.empty())
                    resized_logo.copyTo(frame(cv::Rect(frame.cols - resized_logo.cols - 10, 10, resized_logo.cols, resized_logo.rows)));

                imshow("Video", frame);

                if (waitKey(1) >= 0)
                    break;                
            }
        }
    }

    return true;
}
