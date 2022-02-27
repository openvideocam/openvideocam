using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace openvcamlibnet
{
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate int ProcessingStatusHandler(int video_idx, int frame_idx, int frame_count);

    public class UnmanagedOpenVCamLib
    {
        [DllImport("openvcamlib.dll", EntryPoint = "StartCreateVideoMetaData", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        internal static extern int StartCreateVideoMetaData(string video_id_list, string metadata_file, string tensorflow_saved_model_dir, ProcessingStatusHandler processing_status_callback);

        [DllImport("openvcamlib.dll", EntryPoint = "StartCreateVirtualCamData", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        internal static extern int StartCreateVirtualCamData(string metadata_file, double confidence_level, string video_resolution, string logo_filename, int output_video_detection_margin, int camera_reaction_time, int minimum_time_from_rest_to_ball, ProcessingStatusHandler processing_status_callback);

        [DllImport("openvcamlib.dll", EntryPoint = "StartCreateOutputVideo", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        internal static extern int StartCreateOutputVideo(string metadata_file, bool display_video, ProcessingStatusHandler processing_status_callback);

        [DllImport("openvcamlib.dll", EntryPoint = "GetOperationStatus", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        internal static extern int GetOperationStatus(int thread_idx);

        [DllImport("openvcamlib.dll", EntryPoint = "StopOperation", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        internal static extern int StopOperation(int thread_idx);

        [DllImport("openvcamlib.dll", EntryPoint = "WaitForOperationToFinish", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        internal static extern int WaitForOperationToFinish(int thread_idx);

        [DllImport("openvcamlib.dll", EntryPoint = "GetSnapshotFromVideo", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        internal static extern int GetSnapshotFromVideo(string video_file, int width, int height, IntPtr buffer, ref int size);

        [DllImport("openvcamlib.dll", EntryPoint = "GetVideoDetails", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        internal static extern int GetVideoDetails(string video_file, ref int frame_count, ref double fps, ref int width, ref int height, ref int size);
 
        [DllImport("openvcamlib.dll", EntryPoint = "GetImageAsBase64", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        internal static extern int GetImageAsBase64(string file, StringBuilder base64_buffer, int buffer_size);
    }

    public class OpenVCamLib
    {
        private const int MAX_BUFFER = 10000000;

        public static int StartCreateVideoMetaData(List<int> video_ids_list, string metadata_file, string tensorflow_saved_model_dir, ProcessingStatusHandler processing_status_callback)
        {
            string comma_separated_video_list = String.Join(",", video_ids_list);
            return UnmanagedOpenVCamLib.StartCreateVideoMetaData(comma_separated_video_list, metadata_file, tensorflow_saved_model_dir, processing_status_callback);
        }

        public static int StartCreateVirtualCamData(string metadata_file, double confidence_level, string video_resolution, string logo_filename, int output_video_detection_margin, int camera_reaction_time, int minimum_time_from_rest_to_ball, ProcessingStatusHandler processing_status_callback)
        {
            return UnmanagedOpenVCamLib.StartCreateVirtualCamData(metadata_file, confidence_level, video_resolution, logo_filename, output_video_detection_margin, camera_reaction_time, minimum_time_from_rest_to_ball, processing_status_callback);
        }

        public static int StartCreateOutputVideo(string metadata_file, ProcessingStatusHandler processing_status_callback)
        {
            return UnmanagedOpenVCamLib.StartCreateOutputVideo(metadata_file, false, processing_status_callback);
        }

        public static int GetOperationStatus(int thread_idx)
        {
            return UnmanagedOpenVCamLib.GetOperationStatus(thread_idx);
        }

        public static int StopOperation(int thread_idx)
        {
            return UnmanagedOpenVCamLib.StopOperation(thread_idx);
        }

        public static int WaitForOperationToFinish(int thread_idx)
        {
            return UnmanagedOpenVCamLib.WaitForOperationToFinish(thread_idx);
        }

        public static Image GetSnapshotFromVideo(string video_file)
        {
            return GetSnapshotFromVideo(video_file, 0, 0);
        }

        public static Image GetSnapshotFromVideo(string video_file, int width, int height)
        {
            byte[] array = new byte[MAX_BUFFER];
            int size = Marshal.SizeOf(array[0]) * array.Length;
            IntPtr buffer = Marshal.AllocHGlobal(size);
            int buffer_size = array.Length;
            if (UnmanagedOpenVCamLib.GetSnapshotFromVideo(video_file, width, height, buffer, ref buffer_size) > 0)
            {
                Marshal.Copy(buffer, array, 0, MAX_BUFFER);
                Marshal.FreeHGlobal(buffer);

                MemoryStream ms = new MemoryStream(array, 0, buffer_size);
                ms.Write(array, 0, buffer_size);
                return Image.FromStream(ms);
            }
            else
            {
                return null;
            }
        }

        public static int GetVideoDetails(string video_file, ref int frame_count, ref double fps, ref int width, ref int height, ref int size)
        {
            return UnmanagedOpenVCamLib.GetVideoDetails(video_file, ref frame_count, ref fps, ref width, ref height, ref size);
        }

        public static string GetImageAsBase64(string file)
        {
            int buffer_size = 200000;
            StringBuilder base64_buffer = new StringBuilder(buffer_size);

            if (UnmanagedOpenVCamLib.GetImageAsBase64(file, base64_buffer, buffer_size) == 1)
                return base64_buffer.ToString();
            else
                return "";
        }
    }
}
