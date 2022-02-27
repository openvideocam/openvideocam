using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using openvcamlibnet;

namespace OpenVCam.DataAccess
{
    //https://docs.microsoft.com/en-us/windows/uwp/data-access/sqlite-databases

    public static class VideoConstants
    {
        public const int VMD_VERSION = 1;
    }

    public class VirtualCamPosition
    {
        private int m_x0;
        private int m_y0;
        private int m_x1;
        private int m_y1;

        public VirtualCamPosition()
        {
            m_x0 = 0;
            m_y0 = 0;
            m_x1 = 0;
            m_y1 = 0;
        }

        public VirtualCamPosition(int x0, int y0, int x1, int y1)
        {
            m_x0 = x0;
            m_y0 = y0;
            m_x1 = x1;
            m_y1 = y1;
        }

        public int X0 { get { return m_x0; } set { m_x0 = value; } }
        public int Y0 { get { return m_y0; } set { m_y0 = value; } }
        public int X1 { get { return m_x1; } set { m_x1 = value; } }
        public int Y1 { get { return m_y1; } set { m_y1 = value; } }
    };

    public class VideoFrameObject
    {
        private int m_object_id;
        private double m_confidence;
        private int m_x;
        private int m_y;
        private int m_width;
        private int m_height;

        public VideoFrameObject(int object_id, double confidence, int x, int y, int width, int height)
        {
            m_object_id = object_id;
            m_confidence = confidence;
            m_x = x;
            m_y = y;
            m_width = width;
            m_height = height;
        }

        public int ObjectId { get { return m_object_id; } set { m_object_id = value; } }
        public double Confidence { get { return m_confidence; } set { m_confidence = value; } }
        public int X { get { return m_x; } set { m_x = value; } }
        public int Y { get { return m_y; } set { m_y = value; } }
        public int Width { get { return m_width; } set { m_width = value; } }
        public int Height { get { return m_height; } set { m_height = value; } }
    }

    public class VideoFrame
    {
        private int m_frame_idx;
        private List<VideoFrameObject> m_objects;
        private List<VirtualCamPosition> m_vcam_positions;

        public VideoFrame(int frame_id)
        {
            m_frame_idx = frame_id;
            m_objects = new List<VideoFrameObject>();
            m_vcam_positions = new List<VirtualCamPosition>();
        }

        public int FrameIdx { get { return m_frame_idx; } set { m_frame_idx = value; } }
        public List<VideoFrameObject> Objects { get { return m_objects; } }
        public List<VirtualCamPosition> VCamPositions { get { return m_vcam_positions; } }
    };

    public class GameSummary
    {
        private VideoMetaData m_parent;
        private DateTime m_date;
        private string m_location;
        private string m_team_1;
        private int m_team_1_score;
        private string m_team_2;
        private int m_team_2_score;
        private int m_field_size;
        private int m_game_length;
        
        public GameSummary(VideoMetaData parent)
        {
            m_parent = parent;
            m_date = DateTime.Now;
            m_location = "";
            m_team_1 = "";
            m_team_1_score = 0;
            m_team_2 = "";
            m_team_2_score = 0;
            m_field_size = 0;
            m_game_length = 0;
        }

        public GameSummary(VideoMetaData parent, GameSummary o)
        {
            m_parent = parent;
            m_date = o.m_date;
            m_location = o.m_location;
            m_team_1 = o.m_team_1;
            m_team_1_score = o.m_team_1_score;
            m_team_2 = o.m_team_2;
            m_team_2_score = o.m_team_2_score;
            m_field_size = o.m_field_size;
            m_game_length = o.m_game_length;
        }

        public GameSummary(
            DateTime date,
            string location,
            string team_1,
            int team_1_score,
            string team_2,
            int team_2_score,
            int field_size,
            int game_length)
        {
            m_parent = null;
            m_date = date;
            m_location = location;
            m_team_1 = team_1;
            m_team_1_score = team_1_score;
            m_team_2 = team_2;
            m_team_2_score = team_2_score;
            m_field_size = field_size;
            m_game_length = game_length;
        }

        public DateTime Date { get { return m_date; } set { m_date = value; m_parent.SetChangedFlag(); } }
        public string Location { get { return m_location; } set { m_location = value; m_parent.SetChangedFlag(); } }
        public string Team1 { get { return m_team_1; } set { m_team_1 = value; m_parent.SetChangedFlag(); } }
        public int Team1Score { get { return m_team_1_score; } set { m_team_1_score = value; m_parent.SetChangedFlag(); } }
        public string Team2 { get { return m_team_2; } set { m_team_2 = value; m_parent.SetChangedFlag(); } }
        public int Team2Score { get { return m_team_2_score; } set { m_team_2_score = value; m_parent.SetChangedFlag(); } }
        public int FieldSize { get { return m_field_size; } set { m_field_size = value; m_parent.SetChangedFlag(); } }
        public int GameLength { get { return m_game_length; } set { m_game_length = value; m_parent.SetChangedFlag(); } }
    }

    public enum GameHalf: int
    {
        Undefined = 0,
        FirstHalf = 1,
        SecondHalf = 2
    }

    public class Video
    {
        private VideoMetaData m_parent;
        private int m_id;
        private string m_filename;
        private int m_frame_count;
        private List<VideoFrame> m_frames;
        private string m_field_location;
        private int m_camera_position;
        private int m_selection_start;
        private int m_selection_end;
        private GameHalf m_game_half;
        private int m_size;
        private double m_fps;
        private int m_frame_width;
        private int m_frame_height;
        private string m_team1_field_position;
        private Image m_snapshot;        

        internal void SetId(int id)
        {
            m_id = id;
        }
        internal void SetParent(VideoMetaData parent)
        {
            m_parent = parent;
        }

        internal Video(int id, string filename, int frame_count, string field_location, int camera_position, int selection_start, int selection_end, GameHalf game_half, int size, double fps, int frame_width, int frame_height, string team1_field_position) : this(filename, frame_count, field_location, camera_position, selection_start, selection_end, game_half, size, fps, frame_width, frame_height, team1_field_position)
        {
            m_id = id;
        }

        public Video(string filename) : this(filename, 0, "", -1, -1, -1, GameHalf.Undefined, -1, -1, -1, -1, "L")
        {
            OpenVCamLib.GetVideoDetails(filename, ref m_frame_count, ref m_fps, ref m_frame_width, ref m_frame_height, ref m_size);
        }
        public Video(string filename, int frame_count, string field_location, int camera_position, int selection_start, int selection_end, GameHalf game_half, int size, double fps, int frame_width, int frame_height, string team1_field_position)
        {
            m_id = -1;
            m_filename = filename;
            m_frame_count = frame_count;
            m_frames = new List<VideoFrame>();
            m_snapshot = OpenVCamLib.GetSnapshotFromVideo(filename, 480, 270);
            m_field_location = field_location;
            m_camera_position = camera_position;
            m_selection_start = selection_start;
            m_selection_end = selection_end;
            m_game_half = game_half;
            m_size = size;
            m_fps = fps;
            m_frame_width = frame_width;
            m_frame_height = frame_height;
            m_team1_field_position = team1_field_position;
        }

        public Image GetFullSnapshot()
        {
            return (OpenVCamLib.GetSnapshotFromVideo(m_filename, m_frame_width, m_frame_height));
        }

        public int Id { get { return m_id; } }
        public string FileName { get { return m_filename; } set { m_filename = value; m_parent.SetChangedFlag(); } }
        public int FrameCount { get { return m_frame_count; } set { m_frame_count = value; m_parent.SetChangedFlag(); } }
        public int CameraPosition { get { return m_camera_position; } set { m_camera_position = value; m_parent.SetChangedFlag(); } }
        public int SelectionStart { get { return m_selection_start; } set { m_selection_start = value; m_parent.SetChangedFlag(); } }
        public int SelectionEnd { get { return m_selection_end; } set { m_selection_end = value; m_parent.SetChangedFlag(); } }
        public GameHalf GameHalf { get { return m_game_half; } set { m_game_half = value; m_parent.SetChangedFlag(); } }
        public int Size { get { return m_size; } set { m_size = value; m_parent.SetChangedFlag(); } }
        public double Fps { get { return m_fps; } set { m_fps = value; m_parent.SetChangedFlag(); } }
        public int FrameWidth { get { return m_frame_width; } set { m_frame_width = value; m_parent.SetChangedFlag(); } }
        public int FrameHeight { get { return m_frame_height; } set { m_frame_height = value; m_parent.SetChangedFlag(); } }
        public string Team1FieldPosition { get { return m_team1_field_position; } set { m_team1_field_position = value; m_parent.SetChangedFlag(); } }
        public List<VideoFrame> Frames { get { return m_frames; } }
        public string FieldLocation {  get { return m_field_location; } set { m_field_location = value; m_parent.SetChangedFlag(); } }
        public Image Snapshot { get { return m_snapshot; } }
        public string Name
        {
            get
            {
                FileInfo info = new FileInfo(m_filename);
                return info.Name;
            }
        }
        public int Length
        {
            get
            {
                if ((m_frame_count > 0) && (m_fps > 0))
                    return (Convert.ToInt32(m_frame_count / m_fps));
                else
                    return (0);
            }
        }
    }

    public class OutputVideo
    {
        private VideoMetaData m_parent;
        private string m_file_name;
        private string m_target_video_codec;
        private int m_target_video_frame_width;
        private int m_target_video_frame_height;
        private double m_target_video_confidence_level;
        private int m_detection_margin;
        private int m_camera_reaction;
        private int m_reaction_from_resting;
        private string m_logo_image;

        public OutputVideo(VideoMetaData parent)
        {
            m_parent = parent;
            m_file_name = "";
            m_target_video_codec = "";
            m_target_video_frame_width = 1920;
            m_target_video_frame_height = 1080;
            m_target_video_confidence_level = 0.5;
            m_detection_margin = 100;
            m_camera_reaction = 3;
            m_reaction_from_resting = 6;
            m_logo_image = "";            
        }

        public OutputVideo(VideoMetaData parent, OutputVideo o)
        {
            m_parent = parent;
            m_file_name = o.m_file_name;
            m_target_video_frame_width = o.m_target_video_frame_width;
            m_target_video_frame_height = o.m_target_video_frame_height;
            m_target_video_confidence_level = o.m_target_video_confidence_level;
            m_detection_margin = o.m_detection_margin;
            m_camera_reaction = o.m_camera_reaction;
            m_reaction_from_resting = o.m_reaction_from_resting;
            m_logo_image = o.m_logo_image;
        }

        public OutputVideo(
            string file_name,
            string target_video_codec,
            int target_video_frame_width,
            int target_video_frame_height,
            double target_video_confidence_level,
            int detection_margin,
            int camera_reaction,
            int reaction_from_resting,
            string logo_image)
        {
            m_parent = null;
            m_file_name = file_name;
            m_target_video_frame_width = target_video_frame_width;
            m_target_video_frame_height = target_video_frame_height;
            m_target_video_confidence_level = target_video_confidence_level;
            m_detection_margin = detection_margin;
            m_camera_reaction = camera_reaction;
            m_reaction_from_resting = reaction_from_resting;
            m_logo_image = logo_image;
            m_target_video_codec = target_video_codec;
        }

        public string FileName { get { return m_file_name; } set { m_file_name = value; m_parent.SetChangedFlag();  } }
        public string TargetVideoCodec { get { return m_target_video_codec; } set { m_target_video_codec = value; m_parent.SetChangedFlag(); } }
        public int TargetFrameWidth { get { return m_target_video_frame_width; } set { m_target_video_frame_width = value; m_parent.SetChangedFlag(); } }
        public int TargetFrameHeight { get { return m_target_video_frame_height; } set { m_target_video_frame_height = value; m_parent.SetChangedFlag(); } }
        public double ConfidenceLevel { get { return m_target_video_confidence_level; } set { m_target_video_confidence_level = value; m_parent.SetChangedFlag(); } }
        public int DetectionMargin { get { return m_detection_margin; } set { m_detection_margin = value; m_parent.SetChangedFlag(); } }
        public int CameraReaction { get { return m_camera_reaction; } set { m_camera_reaction = value; m_parent.SetChangedFlag(); } }
        public int ReactionFromResting { get { return m_reaction_from_resting; } set { m_reaction_from_resting = value; m_parent.SetChangedFlag(); } }
        public string LogoImage { get { return m_logo_image; } set { m_logo_image = value; m_parent.SetChangedFlag(); } }
        public Image LogoImageAsImage
        {
            get
            {
                if (string.IsNullOrEmpty(m_logo_image))
                    return null;

                byte[] logo_img_bytes = Convert.FromBase64String(m_logo_image);
                MemoryStream logo_img_ms = new MemoryStream(logo_img_bytes, 0, logo_img_bytes.Length);
                logo_img_ms.Write(logo_img_bytes, 0, logo_img_bytes.Length);
                return Image.FromStream(logo_img_ms);
            }
        }        
    }

    public class VideoMetaData
    {
        private int m_schema_version;
        private GameSummary m_game_summary;              
        private ObservableCollection<Video> m_videos;
        private OutputVideo m_output_video;

        private bool m_changed;

        private void VideoCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                Video new_video = (Video)e.NewItems[0];
                new_video.SetParent(this);
                m_changed = true;
                if (new_video.Id == -1)
                {
                    int new_id = 1;
                    while (true)
                    {
                        bool found = false;
                        foreach (Video v in m_videos)
                        {
                            if (v.Id == new_id)
                            {
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        {
                            new_video.SetId(new_id);
                            break;
                        }
                        new_id++;
                    }
                }
            }
            else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                m_changed = true;
            }
        }

        internal void SetChangedFlag() { m_changed = true;  }

        internal void ResetChangedFlag() { m_changed = false; }

        public VideoMetaData()
        {
            m_schema_version = VideoConstants.VMD_VERSION;
            m_game_summary = new GameSummary(this);
            m_output_video = new OutputVideo(this);
            m_videos = new ObservableCollection<Video>();
            m_videos.CollectionChanged += VideoCollectionChanged;
            m_changed = false;
        }

        public VideoMetaData(int schema_version,
            GameSummary summary,
            OutputVideo output_video)
        {
            m_schema_version = schema_version;
            m_game_summary = new GameSummary(this, summary);
            m_output_video = new OutputVideo(this, output_video);
            m_videos = new ObservableCollection<Video>();
            m_videos.CollectionChanged += VideoCollectionChanged;
            m_changed = false;
        }

        public int SchemaVersion { get { return m_schema_version; } }
        public GameSummary Summary { get { return m_game_summary; } }        
        public OutputVideo Output { get { return m_output_video; } }
        public ObservableCollection<Video> Videos { get { return m_videos; } }        
        public bool Changed { get { return m_changed; } }
    }

    public class VideoMetaDataModel
    {
        private const int MAX_LOGO_IMAGE_SIZE = 200000;

        private string m_filename;
        private string m_tmp_filename;
        private VideoMetaData m_vmd;

        private VideoMetaData LoadMetaDataFromDisk()
        {
            VideoMetaData result = null;

            //Create a SQLite connection
            using (SqliteConnection db = new SqliteConnection($"Filename={m_tmp_filename}"))
            {
                //Open database
                db.Open();

                int schema_version = -1;
                GameSummary summary = null;
                OutputVideo output_video = null;

                //Get header data from TB_HEADER table
                string header_stmt = "SELECT SCHEMA_VERSION FROM TB_HEADER;";
                using (SqliteCommand cmd = new SqliteCommand(header_stmt, db))
                {
                    SqliteDataReader header_recordset = cmd.ExecuteReader();
                    if (header_recordset.Read())
                    {
                        schema_version = header_recordset.GetInt32(0);
                    }
                    else
                    {
                        throw new Exception("VideoMetaData file is invalid");
                    }
                }                

                //Get summary data from TB_GAME_SUMMARY table
                string game_summary_stmt = "SELECT DATE, LOCATION, TEAM_1, TEAM_1_SCORE, TEAM_2, TEAM_2_SCORE, FIELD_SIZE, GAME_LENGTH FROM TB_GAME_SUMMARY;";
                using (SqliteCommand cmd = new SqliteCommand(game_summary_stmt, db))
                {
                    SqliteDataReader game_summary_recordset = cmd.ExecuteReader();
                    if (game_summary_recordset.Read())
                    {
                        summary = new GameSummary(
                            game_summary_recordset.GetDateTime(0),
                            game_summary_recordset.GetString(1),
                            game_summary_recordset.GetString(2),
                            game_summary_recordset.GetInt32(3),
                            game_summary_recordset.GetString(4),
                            game_summary_recordset.GetInt32(5),
                            game_summary_recordset.GetInt32(6),
                            game_summary_recordset.GetInt32(7)
                            );
                    }
                    else
                    {
                        throw new Exception("VideoMetaData file is invalid");
                    }
                }

                //Get output data from TB_OUTPUT table
                string output_summary_stmt = "SELECT VIDEO_FILENAME, TARGET_CODEC, TARGET_WIDTH, TARGET_HEIGHT, TARGET_CONFIDENCE_LEVEL, DETECTION_MARGIN, CAMERA_REACTION, REACTION_RESTING, LOGO_IMAGE FROM TB_OUTPUT;";
                using (SqliteCommand cmd = new SqliteCommand(output_summary_stmt, db))
                {
                    SqliteDataReader output_summary_recordset = cmd.ExecuteReader();
                    if (output_summary_recordset.Read())
                    {
                        output_video = new OutputVideo(
                            output_summary_recordset.GetString(0),
                            output_summary_recordset.GetString(1),
                            output_summary_recordset.GetInt32(2),
                            output_summary_recordset.GetInt32(3),
                            output_summary_recordset.GetDouble(4),
                            output_summary_recordset.GetInt32(5),
                            output_summary_recordset.GetInt32(6),
                            output_summary_recordset.GetInt32(7),
                            (output_summary_recordset.IsDBNull(8) ? "" : output_summary_recordset.GetString(8))
                            );
                    }
                    else
                    {
                        throw new Exception("VideoMetaData file is invalid");
                    }
                }

                result = new VideoMetaData(
                            schema_version,
                            summary,
                            output_video);

                //List to store the ID of each video being loaded
                List<int> video_ids = new List<int>();

                //Get the video list from TB_VIDEOS
                string videos_stmt = "SELECT ID, NAME, FRAME_COUNT, FIELD_LOCATION, CAMERA_POSITION, SELECTION_START, SELECTION_END, GAME_HALF, SIZE, FPS, FRAME_WIDTH, FRAME_HEIGHT, TEAM1_FIELD_POSITION FROM TB_VIDEOS;";
                using (SqliteCommand cmd = new SqliteCommand(videos_stmt, db))
                {
                    SqliteDataReader videos_recordset = cmd.ExecuteReader();
                    while (videos_recordset.Read())
                    {
                        video_ids.Add(videos_recordset.GetInt32(0));

                        result.Videos.Add(new Video(
                            videos_recordset.GetInt32(0),
                            Path.Combine(new string[] { Path.GetDirectoryName(m_tmp_filename), videos_recordset.GetString(1) }),
                            videos_recordset.GetInt32(2),
                            videos_recordset.GetString(3),
                            videos_recordset.GetInt32(4),
                            videos_recordset.GetInt32(5),
                            videos_recordset.GetInt32(6),
                            (GameHalf)videos_recordset.GetInt32(7),
                            videos_recordset.GetInt32(8),
                            videos_recordset.GetDouble(9),
                            videos_recordset.GetInt32(10),
                            videos_recordset.GetInt32(11),
                            videos_recordset.GetString(12)));
                    }
                }

                //Get the list of video frames for each video from TB_VIDEO_FRAMES
                string frames_stmt = "SELECT FRAME_IDX FROM TB_VIDEO_FRAMES WHERE VIDEO_ID = @video_id;";
                using (SqliteCommand cmd = new SqliteCommand(frames_stmt, db))
                {
                    for (int i = 0; i < video_ids.Count; i++)
                    {
                        cmd.Parameters.AddWithValue("@video_id", video_ids[i]);
                        cmd.Prepare();

                        using (SqliteDataReader frames_recordset = cmd.ExecuteReader())
                        {
                            while (frames_recordset.Read())
                            {
                                result.Videos[i].Frames.Add(new VideoFrame(frames_recordset.GetInt32(0)));
                            }
                        }

                        cmd.Parameters.Clear();
                    }
                }

                //Get the list of objects for each frame in each video from TB_VIDEO_FRAME_OBJECTS
                string video_frames_stmt = "SELECT OBJECT_ID, CONFIDENCE, X, Y, WIDTH, HEIGHT FROM TB_VIDEO_FRAME_OBJECTS WHERE VIDEO_ID = @video_id AND FRAME_IDX = @frame_idx ;";
                using (SqliteCommand cmd = new SqliteCommand(video_frames_stmt, db))
                {
                    for (int i = 0; i < video_ids.Count; i++)
                    {
                        foreach (VideoFrame frame in result.Videos[i].Frames)
                        {
                            cmd.Parameters.AddWithValue("@video_id", video_ids[i]);
                            cmd.Parameters.AddWithValue("@frame_idx", frame.FrameIdx);
                            cmd.Prepare();

                            using (SqliteDataReader objects_recordset = cmd.ExecuteReader())
                            {
                                while (objects_recordset.Read())
                                {
                                    frame.Objects.Add(new VideoFrameObject(
                                        objects_recordset.GetInt32(0),
                                        objects_recordset.GetDouble(1),
                                        objects_recordset.GetInt32(2),
                                        objects_recordset.GetInt32(3),
                                        objects_recordset.GetInt32(4),
                                        objects_recordset.GetInt32(5)));
                                }
                            }

                            cmd.Parameters.Clear();
                        }
                    }
                }

                string vcam_stmt = "SELECT X0, Y0, X1, Y1 FROM TB_VIRTUAL_CAMERA_POSITIONS WHERE VIDEO_ID = @video_id AND FRAME_IDX = @frame_idx ;";
                using (SqliteCommand cmd = new SqliteCommand(vcam_stmt, db))
                {
                    for (int i = 0; i < video_ids.Count; i++)
                    {
                        foreach (VideoFrame frame in result.Videos[i].Frames)
                        {
                            cmd.Parameters.AddWithValue("@video_id", video_ids[i]);
                            cmd.Parameters.AddWithValue("@frame_idx", frame.FrameIdx);
                            cmd.Prepare();

                            using (SqliteDataReader vcam_objects_recordset = cmd.ExecuteReader())
                            {
                                while (vcam_objects_recordset.Read())
                                {
                                    frame.VCamPositions.Add(new VirtualCamPosition(
                                        vcam_objects_recordset.GetInt32(0),
                                        vcam_objects_recordset.GetInt32(1),
                                        vcam_objects_recordset.GetInt32(2),
                                        vcam_objects_recordset.GetInt32(3)));
                                }
                            }

                            cmd.Parameters.Clear();
                        }
                    }
                }
            }

            result.ResetChangedFlag();

            //Return the newly created VideoMetaData object if we have one or just return an empty one
            return result;
        }

        public void FlushMetaDataToDisk()
        {
            using (SqliteConnection db = new SqliteConnection($"Filename={m_tmp_filename}"))
            {
                db.Open();

                //Begin database transaction to write data back to database
                SqliteTransaction transaction = db.BeginTransaction();

                //Make sure logo size is not greater than 200K
                if (m_vmd.Output.LogoImage.Length > MAX_LOGO_IMAGE_SIZE)
                {
                    throw new Exception("Logo image too big");
                }

                //----------------------------------------------------------------
                //Remove existing database objects (if any) and create fresh objects
                List<string> statements = new List<string>();
                statements.Add("DROP INDEX IF EXISTS IDX_VIDEO_FRAMES");
                statements.Add("DROP INDEX IF EXISTS IDX_FRAME_OBJECTS");
                statements.Add("DROP INDEX IF EXISTS IDX_VIRTUAL_CAMERA_POSITIONS");
                statements.Add("DROP TABLE IF EXISTS TB_OUTPUT;");
                statements.Add("DROP TABLE IF EXISTS TB_VIRTUAL_CAMERA_POSITIONS;");
                statements.Add("DROP TABLE IF EXISTS TB_VIDEO_FRAME_OBJECTS;");
                statements.Add("DROP TABLE IF EXISTS TB_VIDEO_FRAMES;");
                statements.Add("DROP TABLE IF EXISTS TB_VIDEOS;");
                statements.Add("DROP TABLE IF EXISTS TB_GAME_SUMMARY;");
                statements.Add("DROP TABLE IF EXISTS TB_HEADER;");
                statements.Add("CREATE TABLE TB_HEADER (SCHEMA_VERSION INT);");
                statements.Add("CREATE TABLE TB_GAME_SUMMARY (DATE DATETIME, LOCATION NVARCHAR(2048), TEAM_1 NVARCHAR(2048), TEAM_1_SCORE INT, TEAM_2 NVARCHAR(2048), TEAM_2_SCORE INT, FIELD_SIZE INT, GAME_LENGTH INT);");
                statements.Add("CREATE TABLE TB_VIDEOS (ID INT PRIMARY KEY, NAME NVARCHAR(2048), FRAME_COUNT INT, FIELD_LOCATION NVARCHAR(2048), CAMERA_POSITION INT, SELECTION_START INT, SELECTION_END INT, SIZE INT, FPS FLOAT, FRAME_WIDTH INT, FRAME_HEIGHT INT, GAME_HALF INT, TEAM1_FIELD_POSITION CHAR);");
                statements.Add("CREATE TABLE TB_VIDEO_FRAMES (VIDEO_ID INT, FRAME_IDX INT, PRIMARY KEY(VIDEO_ID, FRAME_IDX), FOREIGN KEY(VIDEO_ID) REFERENCES TB_VIDEOS(ID));");
                statements.Add("CREATE TABLE TB_VIDEO_FRAME_OBJECTS (VIDEO_ID INT, FRAME_IDX INT, OBJECT_ID INT, CONFIDENCE FLOAT, X INT, Y INT, WIDTH INT, HEIGHT INT, FOREIGN KEY(VIDEO_ID, FRAME_IDX) REFERENCES TB_VIDEO_FRAMES(VIDEO_ID, FRAME_IDX));");
                statements.Add("CREATE TABLE TB_VIRTUAL_CAMERA_POSITIONS (VIDEO_ID INT, FRAME_IDX INT, X0 INT, Y0 INT, X1 INT, Y1 INT, FOREIGN KEY(VIDEO_ID, FRAME_IDX) REFERENCES TB_VIDEO_FRAMES(VIDEO_ID, FRAME_IDX));");
                statements.Add("CREATE TABLE TB_OUTPUT (VIDEO_FILENAME NVARCHAR(2048), TARGET_CODEC NVARCHAR(4), TARGET_WIDTH INT, TARGET_HEIGHT INT, TARGET_CONFIDENCE_LEVEL FLOAT, DETECTION_MARGIN INT, CAMERA_REACTION INT, REACTION_RESTING INT, LOGO_IMAGE TEXT);");
                statements.Add("CREATE INDEX IDX_VIDEO_FRAMES ON TB_VIDEO_FRAMES(VIDEO_ID, FRAME_IDX);");
                statements.Add("CREATE INDEX IDX_FRAME_OBJECTS ON TB_VIDEO_FRAME_OBJECTS(VIDEO_ID, FRAME_IDX);");
                statements.Add("CREATE INDEX IDX_VIRTUAL_CAMERA_POSITIONS ON TB_VIRTUAL_CAMERA_POSITIONS(VIDEO_ID, FRAME_IDX);");

                foreach (string stmt in statements)
                {
                    SqliteCommand cmd = new SqliteCommand(stmt, db);
                    cmd.Transaction = transaction;
                    cmd.ExecuteNonQuery();
                }

                //----------------------------------------------------------------
                //Insert HEADER data to TB_HEADER
                string header_stmt = String.Format("INSERT INTO TB_HEADER VALUES ({0});", m_vmd.SchemaVersion);

                using (SqliteCommand cmd = new SqliteCommand(header_stmt, db, transaction))
                {
                    if (cmd.ExecuteNonQuery() <= 0)
                    {
                        throw new Exception("Error inserting data");
                    }
                }

                //----------------------------------------------------------------
                //Insert SUMMARY data to TB_GAME_SUMMARY
                string summary_stmt = String.Format("INSERT INTO TB_GAME_SUMMARY VALUES ('{0}', '{1}', '{2}', {3}, '{4}', {5}, {6}, {7});",
                    m_vmd.Summary.Date.ToString("yyyy-MM-dd HH:mm:ss"),
                    m_vmd.Summary.Location,
                    m_vmd.Summary.Team1,
                    m_vmd.Summary.Team1Score,
                    m_vmd.Summary.Team2,
                    m_vmd.Summary.Team2Score,
                    m_vmd.Summary.FieldSize,
                    m_vmd.Summary.GameLength);

                using (SqliteCommand cmd = new SqliteCommand(summary_stmt, db, transaction))
                {
                    if (cmd.ExecuteNonQuery() <= 0)
                    {
                        throw new Exception("Error inserting data");
                    }
                }

                //----------------------------------------------------------------
                //Insert OUTPUT data to TB_OUTPUT
                string output_stmt = String.Format("INSERT INTO TB_OUTPUT VALUES ('{0}', '{1}', {2}, {3}, {4}, {5}, {6}, {7}, '{8}');",    
                    m_vmd.Output.FileName,
                    m_vmd.Output.TargetVideoCodec,
                    m_vmd.Output.TargetFrameWidth,
                    m_vmd.Output.TargetFrameHeight,
                    m_vmd.Output.ConfidenceLevel,
                    m_vmd.Output.DetectionMargin,
                    m_vmd.Output.CameraReaction,
                    m_vmd.Output.ReactionFromResting,
                    (string.IsNullOrEmpty(m_vmd.Output.LogoImage)) ? "" : m_vmd.Output.LogoImage);

                using (SqliteCommand cmd = new SqliteCommand(output_stmt, db, transaction))
                {
                    if (cmd.ExecuteNonQuery() <= 0)
                    {
                        throw new Exception("Logo image too big");
                    }
                }

                //----------------------------------------------------------------
                //Insert all videos and dependent objects
                foreach (Video video in m_vmd.Videos)
                {
                    //----------------------------------------------------------------
                    //Insert video
                    string video_stmt = String.Format("INSERT INTO TB_VIDEOS VALUES ({0}, '{1}', {2}, '{3}', {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, '{12}');", 
                        video.Id,
                        Path.GetRelativePath(Path.GetDirectoryName(m_tmp_filename), video.FileName), 
                        video.FrameCount, 
                        video.FieldLocation, 
                        video.CameraPosition,
                        video.SelectionStart,
                        video.SelectionEnd,
                        video.Size,
                        video.Fps,
                        video.FrameWidth,
                        video.FrameHeight,                        
                        Convert.ToInt32(video.GameHalf),
                        video.Team1FieldPosition);

                    using (SqliteCommand cmd = new SqliteCommand(video_stmt, db, transaction))
                    {
                        if (cmd.ExecuteNonQuery() <= 0)
                        {
                            throw new Exception("Failed adding video to file");
                        }
                    }

                    //----------------------------------------------------------------
                    //Insert video frames for all videos
                    foreach (VideoFrame frame in video.Frames)
                    {
                        //Insert video frame
                        string frame_stmt = String.Format("INSERT INTO TB_VIDEO_FRAMES VALUES({0}, {1});", video.Id, frame.FrameIdx);

                        using (SqliteCommand cmd = new SqliteCommand(frame_stmt, db, transaction))
                        {
                            if (cmd.ExecuteNonQuery() <= 0)
                            {
                                throw new Exception("Failed adding video frame to file");
                            }
                        }

                        //Insert all objects inside frame
                        foreach (VideoFrameObject obj in frame.Objects)
                        {
                            string frame_obj_stmt = String.Format("INSERT INTO TB_VIDEO_FRAME_OBJECTS VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7});",
                                video.Id,
                                frame.FrameIdx,
                                obj.ObjectId,
                                obj.Confidence,
                                obj.X,
                                obj.Y,
                                obj.Width,
                                obj.Height);

                            using (SqliteCommand cmd = new SqliteCommand(frame_obj_stmt, db, transaction))
                            {
                                if (cmd.ExecuteNonQuery() <= 0)
                                {
                                    throw new Exception("Failed adding video frame object to file");
                                }
                            }
                        }

                        //Insert all VCAM positions for frame
                        foreach (VirtualCamPosition vcam_position in frame.VCamPositions)
                        {
                            string vcam_position_stmt = String.Format("INSERT INTO TB_VIRTUAL_CAMERA_POSITIONS VALUES ({0}, {1}, {2}, {3}, {4}, {5});",
                                video.Id,
                                frame.FrameIdx,
                                vcam_position.X0,
                                vcam_position.Y0,
                                vcam_position.X1,
                                vcam_position.Y1);

                            using (SqliteCommand cmd = new SqliteCommand(vcam_position_stmt, db, transaction))
                            {
                                if (cmd.ExecuteNonQuery() <= 0)
                                {
                                    throw new Exception("Failed adding video frame vcam position to file");
                                }
                            }
                        }
                    }
                }
                transaction.Commit();
            }
        }

        public bool HasFileName { get { return (m_filename == null) ? false : true; } }

        public string FileName { get { return (m_filename == null) ? "" : m_filename; } }

        public string TempFileName { get { return (m_tmp_filename == null) ? "" : m_tmp_filename; } }

        public void Reaload()
        {
            m_vmd = LoadMetaDataFromDisk();
        }

        public VideoMetaDataModel(string filename = null)
        {
            if (filename == null)
            {
                //We are creating a new VMD, so we just create the m_vmd in memory
                m_filename = null;
                m_tmp_filename = Path.GetTempFileName();
                m_vmd = new VideoMetaData();
            }
            else
            {
                //We are opening an existing VMD, so we copy the VMD to a temporary ".~" file and finally load it to our m_vmd in memory
                m_filename = filename;
                m_tmp_filename = filename + ".~";

                FileInfo tmp_file = new FileInfo(m_tmp_filename);
                if (tmp_file.Exists)
                    tmp_file.Delete();
                FileInfo file = new FileInfo(m_filename);
                file.CopyTo(m_tmp_filename);

                m_vmd = LoadMetaDataFromDisk();
            }
        }

        ~VideoMetaDataModel()
        {
            if (m_tmp_filename != null)
                Close();
        }

        public void Save(string filename)
        {
            m_filename = filename;
            Save();
        }

        public void Save()
        {
            if (m_filename == null)
                throw new Exception("VideoMetaDataManager.Save() failed. No filename given.");

            //Before anything else, flush current data in memory to disk
            FlushMetaDataToDisk();

            //Now copy the temporary file to the actual filename provided
            FileInfo file = new FileInfo(m_tmp_filename); 
            file.CopyTo(m_filename, true);

            //If tmp_filename is a general temp file created by the constructor that does not receive a filename
            //let's copy it to a new temp file m_filename + ".~" and delete the current temp fione
            if (m_tmp_filename != (m_filename + ".~"))
            {
                file.CopyTo(m_filename + ".~");
                file.Delete();
                m_tmp_filename = m_filename + ".~";

                //WE need to flush again as the relative paths to the videos must be redefined with the new path
                FlushMetaDataToDisk();
            }
        }

        public void Close()
        {
            FileInfo file = new FileInfo(m_tmp_filename);
            file.Delete();

            m_filename = null;
            m_tmp_filename = null;
            m_vmd = null;
        }

        public VideoMetaData MetaData { get { return m_vmd; } }
    }
}
