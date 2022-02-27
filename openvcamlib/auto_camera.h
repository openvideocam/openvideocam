#ifndef _AUTO_CAMERA_H_
#define _AUTO_CAMERA_H_

#include <iostream>
#include <algorithm>
#include <opencv2/opencv.hpp>
#include "object_detection.h"

using namespace std;
using namespace cv;

enum class CameraState { StandBy = 0, Active = 1 };

enum class Direction { Left, Up, Stopped, Down, Right };

class TrajectoryState {
private:
    int m_time_in_frames;
    double m_distance_in_pixes;
    double m_velocity;
    cv::Point m_current_position;
public:
    TrajectoryState(int time_in_frames, double distance_in_pixes, double velocity) {
        m_time_in_frames = time_in_frames;
        m_distance_in_pixes = distance_in_pixes;
        m_velocity = velocity;
        m_current_position = cv::Point(0,0);
    }
    void SetPosition(int x, int y) {
        m_current_position.x = x;
        m_current_position.y = y;
    }
    cv::Point GetPosition() {
        return m_current_position;
    }
    double GetDistanceInPixels() {
        return m_distance_in_pixes;
    }
};

class VirtualCamera {
private:
    cv::Size m_input_video_size;
    vector<VideoFrame*> m_frames;
    cv::Size m_output_video_size;
    int m_output_video_detection_margin;
    double m_confidence_level;
    int m_camera_reaction_time;
    double m_fps;
    int m_minimum_time_from_rest_to_ball;

    const int MAX_INTERVAL = 10;
    const double MAX_SPEED = 250.0; //Pixels per second
    int m_min_y_field_limit;

    int m_frame_count;

    CameraState m_current_camera_state;
    cv::Point m_current_camera_location;

    vector<TrajectoryState> BuildBallTrajectory(cv::Point current_ball_location, cv::Point new_ball_location, int frame_count) {

        vector<TrajectoryState> ball_trajectory;

        if (frame_count <= 0) {
            return(ball_trajectory);
        }
        else if (frame_count == 1) {
            TrajectoryState o(0, 0.0, 0.0);
            o.SetPosition(current_ball_location.x, current_ball_location.y);
            ball_trajectory.push_back(o);

            TrajectoryState d(0, 0.0, 0.0);
            o.SetPosition(new_ball_location.x, new_ball_location.y);
            ball_trajectory.push_back(d);

            return(ball_trajectory);
        }

        //Calculate trajectory and start moving
        double abs_x = abs(current_ball_location.x - new_ball_location.x);
        double abs_y = abs(current_ball_location.y - new_ball_location.y);

        bool distance_x_based = (abs_x > abs_y) ? true : false;
        double tg = distance_x_based ? abs_y / abs_x : abs_x / abs_y;
        double distante_in_pixels = distance_x_based ? abs_x : abs_y;
        double time_in_frames = frame_count;

        double projected_acceleration = distante_in_pixels  / pow(time_in_frames / 2.0, 2.0);

        //Add trajectory state for all accelerating frames
        double current_distance = 0.0;
        double current_velocity = 0.0;
        for (int i = 1; i <= (time_in_frames / 2); i++) {
            current_distance = 0.5 * projected_acceleration * pow(i, 2.0);
            current_velocity = projected_acceleration * i;
            ball_trajectory.push_back(TrajectoryState(i, current_distance, current_velocity));
        }

        //If we have an odd number of frames, add a single frame in between the acceleration and deacceleration
        if ((frame_count % 2) != 0) {
            ball_trajectory.push_back(ball_trajectory[(int)ball_trajectory.size()-1]);
        }

        //Add trajectory state for all deaccelerating frames
        double initial_distance = current_distance;
        double initial_velocity = current_velocity;
        for (int i = 1; i <= (time_in_frames / 2); i++) {
            current_distance = initial_distance + initial_velocity * i - 0.5 * projected_acceleration * pow(i, 2.0);
            current_velocity = initial_velocity - projected_acceleration * i;
            ball_trajectory.push_back(TrajectoryState(i, current_distance, current_velocity));
        }

        //Now calculate and update real x,y coordinates for the trajectory
        int x = current_ball_location.x;
        int y = current_ball_location.y;
        int x_step = (current_ball_location.x < new_ball_location.x) ? 1 : -1;
        int y_step = (current_ball_location.y < new_ball_location.y) ? 1 : -1;
        for (int i = 0; i < (int)ball_trajectory.size(); i++) {
            if (distance_x_based) {
                x = current_ball_location.x + x_step * (int)ball_trajectory[i].GetDistanceInPixels();
                y = current_ball_location.y + y_step * (int)(ball_trajectory[i].GetDistanceInPixels() * tg);
            }
            else {
                y = current_ball_location.y + y_step * (int)ball_trajectory[i].GetDistanceInPixels();
                x = current_ball_location.x + x_step * (int)(ball_trajectory[i].GetDistanceInPixels() * tg);
            }
            ball_trajectory[i].SetPosition(x, y);
        }

        return ball_trajectory;
    }

    vector<TrajectoryState> BuildCameraTrajectory(cv::Point current_camera_location, cv::Point new_camera_location, int frame_count) {

        vector<TrajectoryState> camera_trajectory;

        if (frame_count <= 0) {
            return(camera_trajectory);
        }
        else if (frame_count == 1) {
            TrajectoryState o(0, 0.0, 0.0);
            o.SetPosition(current_camera_location.x, current_camera_location.y);
            camera_trajectory.push_back(o);

            TrajectoryState d(0, 0.0, 0.0);
            o.SetPosition(new_camera_location.x, new_camera_location.y);
            camera_trajectory.push_back(d);

            return(camera_trajectory);
        }

        //Object is not moving, so calculate a new trajectory and start moving
        double abs_x = abs(current_camera_location.x - new_camera_location.x);
        double abs_y = abs(current_camera_location.y - new_camera_location.y);

        //Camera has not moved, just return without adding a new trajectory
        if ((abs_x == 0) && (abs_y == 0))
            return camera_trajectory;

        bool distance_x_based = (abs_x > abs_y) ? true : false;
        double tg = distance_x_based ? abs_y / abs_x : abs_x / abs_y;
        double distante_in_pixels = distance_x_based ? abs_x : abs_y;
        double time_in_frames = frame_count;

        double projected_acceleration = distante_in_pixels / pow(time_in_frames / 2.0, 2.0);

        //Add trajectory state for all accelerating frames
        double current_distance = 0.0;
        double current_velocity = 0.0;
        for (int i = 1; i <= (time_in_frames / 2); i++) {
            current_distance = 0.5 * projected_acceleration * pow(i, 2.0);
            current_velocity = projected_acceleration * i;
            camera_trajectory.push_back(TrajectoryState(i, current_distance, current_velocity));
        }


        //If we have an odd number of frames, add a single frame in between the acceleration and deacceleration
        if ((frame_count % 2) != 0) {
            camera_trajectory.push_back(camera_trajectory[(int)camera_trajectory.size() - 1]);
        }

        //Add trajectory state for all deaccelerating frames
        double initial_distance = current_distance;
        double initial_velocity = current_velocity;
        for (int i = 1; i <= (time_in_frames / 2); i++) {
            current_distance = initial_distance + initial_velocity * i - 0.5 * projected_acceleration * pow(i, 2.0);
            current_velocity = initial_velocity - projected_acceleration * i;
            camera_trajectory.push_back(TrajectoryState(i, current_distance, current_velocity));
        }

        //Now calculate and update real x,y coordinates for the trajectory
        int x = current_camera_location.x;
        int y = current_camera_location.y;
        int x_step = (current_camera_location.x < new_camera_location.x) ? 1 : -1;
        int y_step = (current_camera_location.y < new_camera_location.y) ? 1 : -1;
        for (int i = 0; i < (int)camera_trajectory.size(); i++) {
            if (distance_x_based) {
                x = current_camera_location.x + x_step * (int)camera_trajectory[i].GetDistanceInPixels();
                y = current_camera_location.y + y_step * (int)(camera_trajectory[i].GetDistanceInPixels() * tg);
            }
            else {
                y = current_camera_location.y + y_step * (int)camera_trajectory[i].GetDistanceInPixels();
                x = current_camera_location.x + x_step * (int)(camera_trajectory[i].GetDistanceInPixels() * tg);
            }
            camera_trajectory[i].SetPosition(x, y);
        }

        return camera_trajectory;
    }

    vector<cv::Rect> BuildFrameRects(vector<TrajectoryState> ball_trajectory, cv::Point camera_location) {
        vector<cv::Point> bt;
        vector<cv::Point> ct;

        for (int i = 0; i < (int)ball_trajectory.size(); i++) {
            bt.push_back(ball_trajectory[i].GetPosition());
        }

        for (int i = 0; i < (int)ball_trajectory.size(); i++) {
            ct.push_back(camera_location);
        }

        return BuildFrameRects(bt, ct);
    }

    vector<cv::Rect> BuildFrameRects(vector<TrajectoryState> ball_trajectory, vector<TrajectoryState> camera_trajectory) {
        vector<cv::Point> bt;
        vector<cv::Point> ct;

        for (int i = 0; i < (int)ball_trajectory.size(); i++) {
            bt.push_back(ball_trajectory[i].GetPosition());
        }

        for (int i = 0; i < (int)camera_trajectory.size(); i++) {
            ct.push_back(camera_trajectory[i].GetPosition());
        }

        return BuildFrameRects(bt, ct);
    }

    vector<cv::Rect> BuildFrameRects(vector<cv::Point> ball_trajectory, cv::Point camera_trajectory) {
        vector<cv::Point> ct;

        for (int i = 0; i < (int)ball_trajectory.size(); i++) {
            ct.push_back(camera_trajectory);
        }

        return BuildFrameRects(ball_trajectory, ct);
    }

    vector<cv::Rect> BuildFrameRects(vector<cv::Point> ball_trajectory, vector<cv::Point> camera_trajectory) {

        vector<cv::Rect> rects;
        for (int i = 0; i < (int)ball_trajectory.size(); i++) {

            //Calculate target rect based on current ball location
            cv::Rect target_rect = cv::Rect(ball_trajectory[i].x - (m_output_video_size.width / 2),
                ball_trajectory[i].y - (m_output_video_size.height / 2),
                m_output_video_size.width,
                m_output_video_size.height);

            //Adjust target rect to make sure it is in a visible area
            target_rect.x = (target_rect.x < 0) ? 0 : ((target_rect.x > m_input_video_size.width - m_output_video_size.width) ? m_input_video_size.width - m_output_video_size.width : target_rect.x);
            target_rect.y = (target_rect.y < 0) ? 0 : ((target_rect.y > m_input_video_size.height - m_output_video_size.height) ? m_input_video_size.height - m_output_video_size.height : target_rect.y);

            //rects.push_back(target_rect);

            //Calculate delta for resizing the camera
            double largest_distance_from_camera = max(m_input_video_size.height - camera_trajectory[i].y, camera_trajectory[i].y - m_min_y_field_limit);

            double distance_from_camera = abs(camera_trajectory[i].y - ball_trajectory[i].y);

            double delta = 1 - (distance_from_camera / largest_distance_from_camera);
            
            //wcout << L"Ball: (" << ball_trajectory[i].x << L"," << ball_trajectory[i].y << L") " <<
            //         L"Camera: (" << camera_trajectory[i].x << L"," << camera_trajectory[i].y << L") " <<
            //         L"Distance: " << distance_from_camera << " " <<
            //         L"Max: " << largest_distance_from_camera << endl;

            //Resize target_rect
              cv::Rect rect = cv::Rect(target_rect.x - (int)((delta * (m_input_video_size.width - m_output_video_size.width)) / 2),
                target_rect.y - (int)((delta * (m_input_video_size.height - m_output_video_size.height)) / 2),
                  target_rect.width + (int)(delta * (m_input_video_size.width - m_output_video_size.width)),
                  target_rect.height + (int)(delta * (m_input_video_size.height - m_output_video_size.height)));

            //Adjust final rect to make sure it is in a visible area
            rect.x = (rect.x < 0) ? 0 : ((rect.x > m_input_video_size.width - rect.width) ? ((m_input_video_size.width - rect.width < 0) ? 0 : m_input_video_size.width - rect.width) : rect.x);
            rect.y = (rect.y < 0) ? 0 : ((rect.y > m_input_video_size.height - rect.height) ? ((m_input_video_size.height - rect.height < 0) ? 0 : m_input_video_size.height - rect.height) : rect.y);

            rects.push_back(rect);
        }

        return(rects);
    }

    vector<cv::Rect> Move(cv::Point new_ball_location, CameraState new_state, int frame_count) {

        if ((m_current_camera_state == CameraState::StandBy) && (new_state == CameraState::StandBy)) {
            //Camera was in standby and did not change its state
            vector<cv::Point> ball_trajectory;
            for (int i = 0; i < frame_count; i++) {
                ball_trajectory.push_back(m_current_camera_location);
            }

            return BuildFrameRects(ball_trajectory, cv::Point((m_input_video_size.width / 2), m_input_video_size.height / 2));

        }
        else if ((m_current_camera_state == CameraState::StandBy) && (new_state == CameraState::Active)) {
            //camera was in standby and is now active
            vector<TrajectoryState> ball_trajectory = BuildBallTrajectory(m_current_camera_location, new_ball_location, frame_count);
            vector<TrajectoryState> camera_trajectory = BuildCameraTrajectory(cv::Point((m_input_video_size.width / 2), m_input_video_size.height / 2),
                                                                              cv::Point((m_input_video_size.width / 2), m_input_video_size.height),
                                                                              frame_count);

            m_current_camera_location = new_ball_location;
            m_current_camera_state = new_state;

            return BuildFrameRects(ball_trajectory, camera_trajectory);

        }
        else if ((m_current_camera_state == CameraState::Active) && (new_state == CameraState::StandBy)) {
            //camera was active and is now going to standby
            vector<TrajectoryState> ball_trajectory = BuildBallTrajectory(m_current_camera_location, new_ball_location, frame_count);
            vector<TrajectoryState> camera_trajectory = BuildCameraTrajectory(cv::Point((m_input_video_size.width / 2), m_input_video_size.height),
                                                                              cv::Point((m_input_video_size.width / 2), m_input_video_size.height / 2),
                                                                              frame_count);

            m_current_camera_location = new_ball_location;
            m_current_camera_state = new_state;

            return BuildFrameRects(ball_trajectory, camera_trajectory);

        }
        else if ((m_current_camera_state == CameraState::Active) && (new_state == CameraState::Active)) {
            if ((m_current_camera_location.x == new_ball_location.x) &&
                (m_current_camera_location.y == new_ball_location.y)) {
                //Camera was active and did not change its state, nor ball changed position
                vector<cv::Point> ball_trajectory;
                for (int i = 0; i < frame_count; i++) {
                    ball_trajectory.push_back(m_current_camera_location);
                }

                return BuildFrameRects(ball_trajectory, cv::Point((m_input_video_size.width / 2), m_input_video_size.height));
            }

            //Calculate coordinates for the target rectangle
            int x0 = m_current_camera_location.x - (m_output_video_size.width / 2) + m_output_video_detection_margin;
            int x1 = m_current_camera_location.x + (m_output_video_size.width / 2) - m_output_video_detection_margin;
            int y0 = m_current_camera_location.y - (m_output_video_size.height / 2) + m_output_video_detection_margin;
            int y1 = m_current_camera_location.y + (m_output_video_size.height / 2) - m_output_video_detection_margin;

            //Check where new camera needs to go and compare with last camera position
            if ((new_ball_location.x >= x0) &&
                (new_ball_location.x <= x1) &&
                (new_ball_location.y >= y0) &&
                (new_ball_location.y <= y1)) {
                //If new camera position is inside our "no need to move" range, just return a fixed result with last position
                vector<cv::Point> ball_trajectory;
                for (int i = 0; i < frame_count; i++) {
                    ball_trajectory.push_back(m_current_camera_location);
                }
 
                return BuildFrameRects(ball_trajectory, cv::Point((m_input_video_size.width / 2), m_input_video_size.height));

            }
            else {

                //If we get here, camera position needs to be updated
                vector<TrajectoryState> ball_trajectory = BuildBallTrajectory(m_current_camera_location, new_ball_location, frame_count);
 
                m_current_camera_location = new_ball_location;

                return BuildFrameRects(ball_trajectory, cv::Point((m_input_video_size.width / 2), m_input_video_size.height));
            }
        }
        else {
            //We should never get to this point, so return and empty vector which means there is an error
            vector<cv::Rect> empty_ball_trajectory;

            return empty_ball_trajectory;
        }
    }

public:
    VirtualCamera(
        cv::Size input_video_size,
        const vector<VideoFrame*>& frames,
        cv::Size output_video_size, 
        int output_video_detection_margin,
        double confidence_level, 
        int camera_reaction_time, 
        double fps, 
        int minimum_time_from_rest_to_ball,
        int min_y_field_limit) {

        //Store received parameters
        m_input_video_size = input_video_size;
        m_frames = frames;
        m_output_video_size = output_video_size;
        m_output_video_detection_margin = output_video_detection_margin;
        m_confidence_level = confidence_level;
        m_camera_reaction_time = camera_reaction_time;
        m_fps = fps;
        m_minimum_time_from_rest_to_ball = minimum_time_from_rest_to_ball;

        m_frame_count = (int)m_frames.size();

        m_min_y_field_limit = min_y_field_limit;

        m_current_camera_state = CameraState::StandBy;
        m_current_camera_location = cv::Point(m_input_video_size.width / 2, m_input_video_size.height / 2);

        //Make sure we have frames and input and output video size width & height are even numbers
        if (m_frame_count == 0)
            throw new runtime_error("Input video frame count equals zero. Must be greater than zero.");
        if ((input_video_size.height % 2) != 0)
            throw new runtime_error("Input video height unsupported. Must be an even number.");
        if ((input_video_size.width % 2) != 0)
            throw new runtime_error("Input video width unsupported. Must be an even number.");
        if ((output_video_size.height % 2) != 0)
            throw new runtime_error("Output video height unsupported. Must be an even number.");
        if ((output_video_size.width % 2) != 0)
            throw new runtime_error("Output video width unsupported. Must be an even number.");
    }

    int GetNextFrameWithBall(int current_frame) {

        //Search for the next frame that has a ball detected
        int frame_with_ball_idx = current_frame + 1;

        while (frame_with_ball_idx < m_frame_count) {
            for (int k = 0; k < m_frames[frame_with_ball_idx]->GetObjects()->size(); k++) {
                VideoFrameObject obj = (*m_frames[frame_with_ball_idx]->GetObjects())[k];
                if ((obj.GetObjectId() == ID_BALL) && (obj.GetConfidence() >= m_confidence_level)) {
                    return(frame_with_ball_idx);
                }
            }
            frame_with_ball_idx++;
        }

        return(-1);
    }

    cv::Point GetFirstBallLocationInFrame(int selected_frame) {

        if ((selected_frame >= 0) && (selected_frame < (int)m_frames.size())) {
            for (int k = 0; k < m_frames[selected_frame]->GetObjects()->size(); k++) {
                VideoFrameObject obj = (*m_frames[selected_frame]->GetObjects())[k];
                if ((obj.GetObjectId() == ID_BALL) && (obj.GetConfidence() >= m_confidence_level)) {
                    return(cv::Point(obj.GetX(), obj.GetY()));
                }
            }
        }

        return(cv::Point(-1, -1));
    }

    int GetDistanceBetweenBalls(cv::Point ball1, cv::Point ball2)
    {
        int diff_x = abs(ball2.x - ball1.x);
        int diff_y = abs(ball2.y - ball1.y);

        return((int)(std::sqrt(diff_x * diff_x + diff_y * diff_y)));
    }

    cv::Rect GetAdjustedCameraFrame(cv::Point camera_center) {
        cv::Rect result = cv::Rect(camera_center.x - m_output_video_size.width / 2,
            camera_center.y - m_output_video_size.height / 2,
            m_output_video_size.width,
            m_output_video_size.height);

        //Adjust rect to make sure it is in a visible area
        result.x = (result.x < 0) ? 0 : (((result.x + m_output_video_size.width) > m_input_video_size.width) ? m_input_video_size.width - m_output_video_size.width : result.x);
        result.y = (result.y < 0) ? 0 : (((result.y + m_output_video_size.height) > m_input_video_size.height) ? m_input_video_size.height - m_output_video_size.height : result.y);
        
        return(result);
    }

    bool PointInsideRect(cv::Point p, cv::Rect r) {
        return ((p.x >= r.x) && (p.x <= r.x + r.width) && (p.y >= r.y) && (p.y <= r.y + r.height));
    }

    vector<VirtualCamPosition> MoveCamera(int frame_idx) {

        //Declare our return vector with camera positions
        vector<VirtualCamPosition> camera_positions;

        int destination_frame_idx;
        CameraState next_camera_state;
        cv::Point next_ball_location;

        vector<cv::Rect> rects;

        //Decide what to do based on camera state and current frame
        if (m_current_camera_state == CameraState::StandBy) {
            //CAMERA IS IN STANDBY

            //Get next frame with a detected ball
            int next_frame_with_ball = GetNextFrameWithBall(frame_idx);

            if (next_frame_with_ball == -1) {
                // No current and next ball detected, we have to finish the video in standby mode
                destination_frame_idx = m_frame_count - 1;
                next_ball_location = cv::Point(m_input_video_size.width / 2, m_input_video_size.height / 2);
                next_camera_state = CameraState::StandBy;
            }
            else if ((next_frame_with_ball - frame_idx) > (int)(m_minimum_time_from_rest_to_ball * m_fps)) {
                //Next frame wiht a detected ball is too far, advance to a position closer to the ball
                //so in the next call we can move to the ball
                destination_frame_idx = next_frame_with_ball - (int)(m_minimum_time_from_rest_to_ball * m_fps);
                next_ball_location = cv::Point(m_input_video_size.width / 2, m_input_video_size.height / 2);
                next_camera_state = CameraState::StandBy;
            }
            else if ((next_frame_with_ball - frame_idx) == (int)(m_minimum_time_from_rest_to_ball * m_fps)) {
                // Next ball is exactly where at point we can move the camera to it
                destination_frame_idx = next_frame_with_ball;
                next_ball_location = GetFirstBallLocationInFrame(next_frame_with_ball);
                next_camera_state = CameraState::Active;
            }
            else { // ((next_frame_with_ball - frame_idx) < (int)(m_minimum_time_from_rest_to_ball * m_fps))
                // Next frame with a detected ball is too close, but we have no option but to move to the ball
                // this should be reevaluated in the future so we do not move to quickly to the ball
                destination_frame_idx = next_frame_with_ball;
                next_ball_location = GetFirstBallLocationInFrame(next_frame_with_ball);
                next_camera_state = CameraState::Active;
            }

            //Now go from STANDBY to ACTIVE and move ball to destination frame idx
            rects = Move(next_ball_location, next_camera_state, destination_frame_idx - frame_idx);
        }
        else if (m_current_camera_state == CameraState::Active) {
            //CAMERA ACTIVE

            //Store current frame ball position
            int current_frame_with_ball = frame_idx;
            cv::Point current_ball_positon = GetFirstBallLocationInFrame(current_frame_with_ball);

            //Get current camera center and frame (based on current ball position)
            cv::Point current_camera_center = GetFirstBallLocationInFrame(current_frame_with_ball);
            cv::Rect current_camera_frame = GetAdjustedCameraFrame(current_camera_center);

            //Get next frame with a detected ball and the ball position
            int next_frame_with_ball = GetNextFrameWithBall(current_frame_with_ball);
            cv::Point next_ball_positon = GetFirstBallLocationInFrame(next_frame_with_ball);

            //Initialize our list of frames and camera centers that will be later used to 
            //execute the actual ball/camera move
            vector<int> list_frames_indexes;
            vector<cv::Point> list_camera_centers;

            list_frames_indexes.push_back(current_frame_with_ball);
            list_camera_centers.push_back(current_camera_center);

            //Keep advancing to every frame with a ball detected unil we either reach the end of the frame list
            //or if the time between current and next frame witha detected ball is greater than 10 SECONDS
            while ((next_frame_with_ball != -1) && (((int)next_frame_with_ball - (int)current_frame_with_ball) < (MAX_INTERVAL * m_fps))) {

                if (!PointInsideRect(next_ball_positon, current_camera_frame)) {
                    //Next ball position IS NOT visible, we need to move the camera

                    //Calculated required camera speed to move the frame to where the ball is
                    int distance_between_balls = GetDistanceBetweenBalls(current_camera_center, next_ball_positon);
                    int frames_between_balls = (next_frame_with_ball - current_frame_with_ball);
                    double required_speed_to_move_between_points = 1.0 * distance_between_balls / frames_between_balls;

                    if (required_speed_to_move_between_points <= 1.0 * MAX_SPEED / m_fps) {
                        //Required speed to move camera is acceptable, just go ahead and move it
                        list_frames_indexes.push_back(next_frame_with_ball);
                        list_camera_centers.push_back(next_ball_positon);

                        current_camera_center = next_ball_positon;
                        current_camera_frame = GetAdjustedCameraFrame(current_camera_center);
                    }
                    else {

                        //Required speed is greater than what we can do, but we can try moving directly from previous steps
                        while ((int)list_frames_indexes.size() > 1) {
                           
                            list_frames_indexes.erase(list_frames_indexes.end()-1);
                            list_camera_centers.erase(list_camera_centers.end()-1);

                            current_frame_with_ball = list_frames_indexes[list_frames_indexes.size()-1];
                            current_ball_positon = list_camera_centers[list_camera_centers.size()-1];

                            //RECALCULATE required camera speed to move the frame to where the ball is
                            int distance_between_balls = GetDistanceBetweenBalls(current_ball_positon, next_ball_positon);
                            int frames_between_balls = (next_frame_with_ball - current_frame_with_ball);
                            double required_speed_to_move_between_points = 1.0 * distance_between_balls / frames_between_balls;

                            if (required_speed_to_move_between_points <= 1.0 * MAX_SPEED / m_fps) {
                                //Required speed to move camera is acceptable, just go ahead and move it
                                list_frames_indexes.push_back(next_frame_with_ball);
                                list_camera_centers.push_back(next_ball_positon);

                                current_camera_center = next_ball_positon;
                                current_camera_frame = GetAdjustedCameraFrame(current_camera_center);
                                break;
                            }
                        }

                        //If list_frames_indexes.size() equals, that means we were not able to find a previous point in time from
                        //wehere to move from at a slower speed. For now, we are moving from the first element in list, but
                        //this can be improved in the future
                        if (list_frames_indexes.size() == 1) {
                            list_frames_indexes.push_back(next_frame_with_ball);
                            list_camera_centers.push_back(next_ball_positon);

                            current_camera_center = next_ball_positon;
                            current_camera_frame = GetAdjustedCameraFrame(current_camera_center);
                        }
                    }
                }
                else {
                    //Next ball position IS visible, camera does not need to be moved

                    list_frames_indexes.push_back(next_frame_with_ball);
                    list_camera_centers.push_back(current_camera_center);
                }

                //Advance to next frame by storing it as the current frame
                current_frame_with_ball = next_frame_with_ball;
                current_ball_positon = GetFirstBallLocationInFrame(current_frame_with_ball);

                //Get next fram with ball
                next_frame_with_ball = GetNextFrameWithBall(current_frame_with_ball);
                next_ball_positon = GetFirstBallLocationInFrame(next_frame_with_ball);
            }

            //When we finsih loop, we need to add the last frame to out list
            if (next_frame_with_ball == -1) {

                //If we left while with no next ball, this means we reached the end of the frame list
                list_frames_indexes.push_back(m_frame_count - 1);
                list_camera_centers.push_back(cv::Point(m_input_video_size.width / 2, m_input_video_size.height / 2));
            }
            else {  

                //Next frame is too far, we need to move back to standby
                list_frames_indexes.push_back(current_frame_with_ball + m_minimum_time_from_rest_to_ball * m_fps);
                list_camera_centers.push_back(cv::Point(m_input_video_size.width / 2, m_input_video_size.height / 2));
            }

            //Add all frames to our rects vector
            for (int i = 1; i < (int)list_frames_indexes.size(); i++) {
                int range = list_frames_indexes[i] - list_frames_indexes[i - 1];
                CameraState next_camera_state = (i == (int)list_frames_indexes.size() - 1) ? CameraState::StandBy : CameraState::Active;
                vector<cv::Rect> partial_rects = Move(list_camera_centers[i], next_camera_state, range);

                for (int j = 0; j < (int)partial_rects.size(); j++) {
                    rects.push_back(partial_rects[j]);
                }
            }
        }

        for (int i = 0; i < (int)rects.size(); i++) {
            camera_positions.push_back(VirtualCamPosition(rects[i].x, rects[i].y, rects[i].x + rects[i].width, rects[i].y + rects[i].height));
            frame_idx++;
        }

        return(camera_positions);
    }
};

#endif