#ifndef _VMD_H_
#define _VMD_H_
#include <string>
#include <iostream>

#ifndef LINUX
#include <fileapi.h>
#endif

#include <vector>
#include <opencv2/core.hpp> 
#include <opencv2/videoio.hpp>
#include <opencv2/opencv.hpp>

#include "db.h"
#include "object_detection.h"
#include "base64.h"
#include "utils.h"

using namespace std;

#define VMD_SCHEMA_VERSION_UNDEFINED 0
#define VMD_SCHEMA_VERSION_100 1
#define MAX_LOGO_IMAGE_SIZE 200000

class VirtualCamPosition {
private:
    int m_x0;
    int m_y0;
    int m_x1;
    int m_y1;
public:
    VirtualCamPosition() {
        m_x0 = 0;
        m_y0 = 0;
        m_x1 = 0;
        m_y1 = 0;
    }
    VirtualCamPosition(int x0, int y0, int x1, int y1) {
        m_x0 = x0;
        m_y0 = y0;
        m_x1 = x1;
        m_y1 = y1;
    }

    int GetX0() { return m_x0; }
    int GetY0() { return m_y0; }
    int GetX1() { return m_x1; }
    int GetY1() { return m_y1; }

    void SetX0(int v) { m_x0 = v; }
    void SetY0(int v) { m_y0 = v; }
    void SetX1(int v) { m_x1 = v; }
    void SetY1(int v) { m_y1 = v; }
};

class VideoFrameObject {
private:
    int m_object_id;
    double m_confidence;
    int m_x;
    int m_y;
    int m_width;
    int m_height;
public:
    VideoFrameObject(const VideoFrameObject &o) {
        m_object_id = o.m_object_id;
        m_confidence = o.m_confidence;
        m_x = o.m_x;
        m_y = o.m_y;
        m_width = o.m_width;
        m_height = o.m_height;
    }

    VideoFrameObject(int object_id, double confidence, int x, int y, int width, int height) {
        m_object_id = object_id;
        m_confidence = confidence;
        m_x = x;
        m_y = y;
        m_width = width;
        m_height = height;
    }

    int GetObjectId() { return m_object_id;  }
    double GetConfidence() { return m_confidence; }
    int GetX() { return m_x; }
    int GetY() { return m_y; }
    int GetWidth() { return m_width; }
    int GetHeight() { return m_height; }
};

class VideoFrame {
private:
    int m_video_id;
    int m_frame_id;
    vector<VideoFrameObject> m_objects;
    vector<VirtualCamPosition> m_vcam_positions;

public:
    VideoFrame(int video_id, int frame_id) {
        m_video_id = video_id;
        m_frame_id = frame_id;
    }
    int GetVideoId() { return m_video_id; }
    int GetFrameId() { return m_frame_id; }
    vector<VideoFrameObject>* GetObjects() { return &m_objects; }
    vector<VirtualCamPosition>* GetVirtualCamPositions() { return &m_vcam_positions; }
};

class Video {
private:
    int m_id;
    wstring m_name;
    int m_frame_count;
    wstring m_field_location;
    int m_camera_positon;
    int m_selection_start;
    int m_selection_end;
    double m_fps;
    int m_video_frame_width;
    int m_video_frame_height;
    vector<VideoFrame> m_frames;
public:
    Video(int id, wstring name, int frame_count, wstring field_location,
        int camera_positon, int selection_start, int selection_end, double fps,
        int video_frame_width, int video_frame_height) {
        m_id = id;
        m_name = name;
        m_frame_count = frame_count;
        m_field_location = field_location;
        m_camera_positon = camera_positon;
        m_selection_start = selection_start;
        m_selection_end = selection_end;
        m_fps = fps;
        m_video_frame_width = video_frame_width;
        m_video_frame_height = video_frame_height;
    }

    int GetId() { return m_id; }
    wstring GetName() { return m_name; }
    int GetFrameCount() { return m_frame_count; }
    wstring GetFieldLocation() { return m_field_location; }
    int GetCameraPosition() { return m_camera_positon; }
    int GetSelectionStart() { return m_selection_start; }
    int GetSelectionEnd() { return m_selection_end; }
    double GetFps() { return m_fps; }
    int GetWidth() { return m_video_frame_width; }
    int GetHeigth() { return m_video_frame_height; }

    vector<VideoFrame>* GetFrames() {
        return &m_frames;
    }

    vector<cv::Point> GetFieldLimits() {
        vector<cv::Point> result;

        if (m_field_location != L"") {
            vector<int> coords = string_vector_to_int(split_string(m_field_location, L","));

            for (int i = 0; i < (int)coords.size() - 1; i = i + 2) {
                int x = coords[i];
                int y = (i < (int)coords.size() - 1) ? coords[i + 1] : 0;
                result.push_back(cv::Point(x, y));
            }
        }

        return (result);
    }
};

class VideoMetaData {
private:
    int m_schema_version;
    wstring m_filename;

    wstring m_output_video_filename;

    wstring m_output_video_codec;

    int m_target_video_frame_width;
    int m_target_video_frame_height;

    double m_target_video_confidence_level;

    int m_detection_margin;
    int m_camera_reaction;
    int m_reaction_resting;

    vector<BYTE> m_logo_image;
    vector<Video> m_videos;

public:
    VideoMetaData() {
        m_schema_version = VMD_SCHEMA_VERSION_UNDEFINED;
        m_filename = L"";
        m_output_video_filename = L"";
        m_target_video_frame_width = -1;
        m_target_video_frame_height = -1;
        m_target_video_confidence_level = -1.0;
        m_detection_margin = -1;
        m_camera_reaction = -1;
        m_reaction_resting = -1;
    }

    void ClearVideoFrames(int video_id) {
        Video* video = GetVideoById(video_id);
        video->GetFrames()->clear();
    }

    void ClearVirtualCamPositions() {
        for (int i = 0; i < (int)m_videos.size(); i++) {
            for (int j = 0; j < (int)m_videos[i].GetFrames()->size(); j++) {
                (*m_videos[i].GetFrames())[j].GetVirtualCamPositions()->clear();
            }
        }
    }

    int AddVideoFrame(int video_id) {
        Video* video = GetVideoById(video_id);
        video->GetFrames()->push_back(VideoFrame(video_id, (int)video->GetFrames()->size()));

        return (int)video->GetFrames()->size() - 1;
    }

    void AddVideoFrameObject(int video_id, int frame_id, int object_id, double confidence, int x, int y, int width, int height) {
        Video* video = GetVideoById(video_id);
        (*(video->GetFrames()))[frame_id].GetObjects()->push_back(VideoFrameObject(object_id, confidence, x, y, width, height));
    }

    void AddVirtualCamPositon(int video_id, int frame_id, int x0, int y0, int x1, int y1) {
        Video* video = GetVideoById(video_id);
        (*(video->GetFrames()))[frame_id].GetVirtualCamPositions()->push_back(VirtualCamPosition(x0, y0, x1, y1));
    }

    bool Save() {
         if (m_filename == L"")
            throw new runtime_error("Cannot call Save() without calling Load() first.");

        SQLITEDatabase meta_data_db;
        if (!meta_data_db.Open(m_filename))
        {
            wcout << L"Error opening meta data file" << endl;
            return(false);
        }

        if (!meta_data_db.ExecuteSQLStatement(L"DELETE FROM TB_VIRTUAL_CAMERA_POSITIONS;")) {
            wcout << L"Error creating meta data file" << endl;
            meta_data_db.Close();
            return(false);
        }

        if (!meta_data_db.ExecuteSQLStatement(L"DELETE FROM TB_VIDEO_FRAME_OBJECTS;")) {
            wcout << L"Error creating meta data file" << endl;
            meta_data_db.Close();
            return(false);
        }

        if (!meta_data_db.ExecuteSQLStatement(L"DELETE FROM TB_VIDEO_FRAMES;")) {
            wcout << L"Error creating meta data file" << endl;
            meta_data_db.Close();
            return(false);
        }

        //Begin database transaction to write data back to database
        meta_data_db.ExecuteSQLStatement(L"BEGIN TRANSACTION;");

        //Write metadata header to database
        wchar_t sql_command[1024];

        //Write meta data contents
        for (int i = 0; i < (int)m_videos.size(); i++) {
            //Write each frame of current video to TB_FRAMES
            for (int j = 0; j < (int)(*m_videos[i].GetFrames()).size(); j++) {
                swprintf(sql_command, sizeof(sql_command), L"INSERT INTO TB_VIDEO_FRAMES VALUES (%d, %d);", m_videos[i].GetId(), (*m_videos[i].GetFrames())[j].GetFrameId());
                meta_data_db.ExecuteSQLStatement(sql_command);

                //Write each existing detected object in current frame
                for (int k = 0; k < (int)(*(*m_videos[i].GetFrames())[j].GetObjects()).size(); k++) {                   
                    swprintf(sql_command, sizeof(sql_command), L"INSERT INTO TB_VIDEO_FRAME_OBJECTS VALUES (%d, %d, %d, %f, %d, %d, %d, %d);",
                        m_videos[i].GetId(), 
                        (*m_videos[i].GetFrames())[j].GetFrameId(),
                        (*(*m_videos[i].GetFrames())[j].GetObjects())[k].GetObjectId(),
                        (*(*m_videos[i].GetFrames())[j].GetObjects())[k].GetConfidence(),
                        (*(*m_videos[i].GetFrames())[j].GetObjects())[k].GetX(),
                        (*(*m_videos[i].GetFrames())[j].GetObjects())[k].GetY(),
                        (*(*m_videos[i].GetFrames())[j].GetObjects())[k].GetWidth(),
                        (*(*m_videos[i].GetFrames())[j].GetObjects())[k].GetHeight());
                    meta_data_db.ExecuteSQLStatement(sql_command);
                }

                //Write each existing calculated virtual cam in current frame (SHOULD BE 1 PER FRAME FOR NOW)
                for (int k = 0; k < (int)(*(*m_videos[i].GetFrames())[j].GetVirtualCamPositions()).size(); k++) {
                    swprintf(sql_command, sizeof(sql_command), L"INSERT INTO TB_VIRTUAL_CAMERA_POSITIONS VALUES (%d, %d, %d, %d, %d, %d);",
                        m_videos[i].GetId(),
                        (*m_videos[i].GetFrames())[j].GetFrameId(),
                        (*(*m_videos[i].GetFrames())[j].GetVirtualCamPositions())[k].GetX0(),
                        (*(*m_videos[i].GetFrames())[j].GetVirtualCamPositions())[k].GetY0(),
                        (*(*m_videos[i].GetFrames())[j].GetVirtualCamPositions())[k].GetX1(),
                        (*(*m_videos[i].GetFrames())[j].GetVirtualCamPositions())[k].GetY1());
                    meta_data_db.ExecuteSQLStatement(sql_command);
                }
            }
        }

        //Begin database transaction to write data back to database
        meta_data_db.ExecuteSQLStatement(L"COMMIT;");

        meta_data_db.Close();

        return(true);
    }

    bool SaveVirtualCamPositions() {
        if (m_filename == L"")
            throw new runtime_error("Cannot call Save() without calling Load() first.");

        SQLITEDatabase meta_data_db;
        if (!meta_data_db.Open(m_filename))
        {
            wcout << L"Error opening meta data file" << endl;
            return(false);
        }

        if (!meta_data_db.ExecuteSQLStatement(L"DELETE FROM TB_VIRTUAL_CAMERA_POSITIONS;")) {
            wcout << L"Error creating meta data file" << endl;
            meta_data_db.Close();
            return(false);
        }

        meta_data_db.ExecuteSQLStatement(L"BEGIN TRANSACTION;");

        //Write metadata header to database
        wchar_t sql_command[1024];

        for (int i = 0; i < (int)m_videos.size(); i++) {
            for (int j = 0; j < (int)(*m_videos[i].GetFrames()).size(); j++) {
                for (int k = 0; k < (int)(*(*m_videos[i].GetFrames())[j].GetVirtualCamPositions()).size(); k++) {
                    swprintf(sql_command, sizeof(sql_command), L"INSERT INTO TB_VIRTUAL_CAMERA_POSITIONS VALUES (%d, %d, %d, %d, %d, %d);",
                        m_videos[i].GetId(),
                        (*m_videos[i].GetFrames())[j].GetFrameId(),
                        (*(*m_videos[i].GetFrames())[j].GetVirtualCamPositions())[k].GetX0(),
                        (*(*m_videos[i].GetFrames())[j].GetVirtualCamPositions())[k].GetY0(),
                        (*(*m_videos[i].GetFrames())[j].GetVirtualCamPositions())[k].GetX1(),
                        (*(*m_videos[i].GetFrames())[j].GetVirtualCamPositions())[k].GetY1());
                    meta_data_db.ExecuteSQLStatement(sql_command);
                }
            }
        }

        //Begin database transaction to write data back to database
        meta_data_db.ExecuteSQLStatement(L"COMMIT;");

        meta_data_db.Close();

        return(true);
    }

    bool Load(wstring filename) {

        m_filename = filename;

        SQLITEDatabase meta_data_db;

        if (!meta_data_db.Open(filename))
        {
            wcout << L"Error opening meta data file" << endl;
            return(false);
        }

        //Load meta data from TB_OUTPUT
        SQLITERecordSet* header_recordset = meta_data_db.GetRecordSet(L"SELECT SCHEMA_VERSION FROM TB_HEADER;");
        if (header_recordset->GotoNextRow())
        {
            m_schema_version = header_recordset->GetColumnValueAsInt(0);
        }
        else {
            wcout << L"Error opening meta data file (TB_HEADER is empty)" << endl;
            return(false);
        }
        delete header_recordset;

        //Make sure we recognize the schema version
        if (m_schema_version != VMD_SCHEMA_VERSION_100)
        {
            wcout << L"Schema version not supported" << endl;
            return(false);
        }

        //Load meta data from TB_OUTPUT
        SQLITERecordSet* output_recordset = meta_data_db.GetRecordSet(L"SELECT VIDEO_FILENAME, TARGET_CODEC, TARGET_WIDTH, TARGET_HEIGHT, TARGET_CONFIDENCE_LEVEL, DETECTION_MARGIN, CAMERA_REACTION, REACTION_RESTING, LOGO_IMAGE FROM TB_OUTPUT;");
        if (output_recordset->GotoNextRow())
        {
            m_output_video_filename = output_recordset->GetColumnValueAsString(0);
            m_output_video_codec = output_recordset->GetColumnValueAsString(1);
            m_target_video_frame_width = output_recordset->GetColumnValueAsInt(2);
            m_target_video_frame_height = output_recordset->GetColumnValueAsInt(3);
            m_target_video_confidence_level = output_recordset->GetColumnValueAsDouble(4);
            m_detection_margin = output_recordset->GetColumnValueAsInt(5);
            m_camera_reaction = output_recordset->GetColumnValueAsInt(6);
            m_reaction_resting = output_recordset->GetColumnValueAsInt(7);
            m_logo_image = base64::decode(output_recordset->GetColumnValueAsString(8));
        }
        else {
            wcout << L"Error opening meta data file (TB_OUTPUT is empty)" << endl;
            return(false);
        }
        delete output_recordset;

        m_videos.clear();

        //Load videos table
        SQLITERecordSet* videos_recordset = meta_data_db.GetRecordSet(L"SELECT ID, NAME, FRAME_COUNT, FIELD_LOCATION, CAMERA_POSITION, SELECTION_START, SELECTION_END, FPS, FRAME_WIDTH, FRAME_HEIGHT FROM TB_VIDEOS ORDER BY ID;");
        while (videos_recordset->GotoNextRow())
        {
            m_videos.push_back(Video(
                videos_recordset->GetColumnValueAsInt(0),
                videos_recordset->GetColumnValueAsString(1),
                videos_recordset->GetColumnValueAsInt(2),
                videos_recordset->GetColumnValueAsString(3),
                videos_recordset->GetColumnValueAsInt(4),
                videos_recordset->GetColumnValueAsInt(5),
                videos_recordset->GetColumnValueAsInt(6),
                videos_recordset->GetColumnValueAsDouble(7),
                videos_recordset->GetColumnValueAsInt(8),
                videos_recordset->GetColumnValueAsInt(9)));
        }
        delete videos_recordset;

        //Load frames
        SQLITERecordSet* frames_recordset = meta_data_db.GetRecordSet(L"SELECT FRAME_IDX FROM TB_VIDEO_FRAMES WHERE VIDEO_ID = ? ORDER BY FRAME_IDX;");
        for (int i = 0; i < (int)m_videos.size(); i++) {
            //Load frames table
            frames_recordset->Reset();
            frames_recordset->BindParameterAsInt(1, m_videos[i].GetId());
            while (frames_recordset->GotoNextRow())
            {
                m_videos[i].GetFrames()->push_back(VideoFrame(m_videos[i].GetId(), frames_recordset->GetColumnValueAsInt(0)));
            }
        }
        delete frames_recordset;

        //Load frame objects
        SQLITERecordSet* objects_recordset = meta_data_db.GetRecordSet(L"SELECT OBJECT_ID, CONFIDENCE, X, Y, WIDTH, HEIGHT FROM TB_VIDEO_FRAME_OBJECTS WHERE VIDEO_ID = ? AND FRAME_IDX = ?;");
        for (int i = 0; i < (int)m_videos.size(); i++) {

            for (int j = 0; j < (int)m_videos[i].GetFrames()->size(); j++) {
                //Load frames objects table
                objects_recordset->Reset();
                objects_recordset->BindParameterAsInt(1, m_videos[i].GetId());
                objects_recordset->BindParameterAsInt(2, (*m_videos[i].GetFrames())[j].GetFrameId());
                while (objects_recordset->GotoNextRow())
                {
                    (*m_videos[i].GetFrames())[j].GetObjects()->push_back(VideoFrameObject(
                        objects_recordset->GetColumnValueAsInt(0),
                        objects_recordset->GetColumnValueAsDouble(1),
                        objects_recordset->GetColumnValueAsInt(2),
                        objects_recordset->GetColumnValueAsInt(3),
                        objects_recordset->GetColumnValueAsInt(4),
                        objects_recordset->GetColumnValueAsInt(5)));
                }
            }
        }
        delete objects_recordset;

        //Load frame virtual camera position
        objects_recordset = meta_data_db.GetRecordSet(L"SELECT X0, Y0, X1, Y1 FROM TB_VIRTUAL_CAMERA_POSITIONS WHERE VIDEO_ID = ? AND FRAME_IDX = ?;");
        for (int i = 0; i < (int)m_videos.size(); i++) {

            for (int j = 0; j < (int)m_videos[i].GetFrames()->size(); j++) {
                //Load frames objects table
                objects_recordset->Reset();
                objects_recordset->BindParameterAsInt(1, m_videos[i].GetId());
                objects_recordset->BindParameterAsInt(2, (*m_videos[i].GetFrames())[j].GetFrameId());
                while (objects_recordset->GotoNextRow())
                {
                    (*m_videos[i].GetFrames())[j].GetVirtualCamPositions()->push_back(VirtualCamPosition(
                        objects_recordset->GetColumnValueAsInt(0),
                        objects_recordset->GetColumnValueAsInt(1),
                        objects_recordset->GetColumnValueAsInt(2),
                        objects_recordset->GetColumnValueAsInt(3)));
                }
            }
        }
        delete objects_recordset;

        meta_data_db.Close();

        return(true);
    }

    wstring GetOutputVideoName() {
        return m_output_video_filename;
    }

    int GetTargetHeight() {
        return m_target_video_frame_height;
    }
    int GetTargetWidth() {
        return m_target_video_frame_width;
    }

    double GetTargetConfidenceLevel() {
        return m_target_video_confidence_level;
    }

    int GetDetectionMargin() {
        return m_detection_margin;
    }
    int GetCameraReaction() {
        return m_camera_reaction;
    }
    int GetReactiontimeFromResting() {
        return m_reaction_resting;
    }

    wstring GetOutputVideoCodec() {
        return m_output_video_codec;
    }

    bool LogoExists() {
        return (m_logo_image.size() > 0) ? true : false;
    }
    cv::Mat GetLogoImage() {
        return imdecode(m_logo_image, IMREAD_UNCHANGED);
    }
    void ResetLogoImage() {
        m_logo_image.clear();
    }

    vector<Video>* GetVideos() {
        return &m_videos;
    }

    Video* GetVideoById(int id) {
        for (int i = 0; i < (int)m_videos.size(); i++)
        {
            if (m_videos[i].GetId() == id)
                return &m_videos[i];
        }

        return NULL;
    }
 };

#endif