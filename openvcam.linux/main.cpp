#include <cstdio>
#include <codecvt>
#include <bits/locale_conv.h>
#include <string>
#include <iostream>
#include <../openvcamlib/openvcamlib.h>

#define TRUE 1
#define FALSE 0

using namespace std;

wstring get_current_time()
{
    wchar_t str_time[128];
    time_t td;
    struct tm tm;
    time(&td);
    tm = *localtime(&td);
    wcsftime(str_time, 128, L"%X", &tm);
    return(str_time);
}

int UpdateStatus(int video_idx, int frame_idx, int frame_count)
{
    if ((frame_idx % 30) == 0)
        wcout << get_current_time() << ": Processed video " << video_idx << ", frame " << frame_idx << " of " << frame_count << " frames" << endl;

    return 0;
}

int main(int argc, char** argv)
{
    bool display_window = false;
    bool create_video_only = false;
    wstring input_video_ids = L"";
    wstring tensor_flow_saved_model = L"";
    wstring meta_data = L"";

    wstring start_time = get_current_time();

    for (int i = 1; i < argc; i++)
    {
        wstring param = std::wstring_convert<std::codecvt_utf8<wchar_t>>().from_bytes(argv[i]);

        if (param.find(L"/i:") != string::npos) {
            input_video_ids = param.substr(3);
        }
        else if (param.find(L"/m:") != string::npos) {
            meta_data = param.substr(3);
        }
        else if (param.find(L"/t:") != string::npos) {
            tensor_flow_saved_model = param.substr(3);
        }
        else if (param.find(L"/d") != string::npos) {
            display_window = true;
        }
        else if (param.find(L"/o") != string::npos) {
            create_video_only = true;
        }
    }

    if ((input_video_ids != L"") && (meta_data != L"") && (tensor_flow_saved_model != L""))
    {
        wcout << endl;
        wcout << L"---------------------------------------------------------";
        wcout << L"---------------------------------------------------------";
        wcout << L"Calling meta data creator with the following parameters  " << endl;
        wcout << L"Input video ids: " << input_video_ids << endl;
        wcout << L"Model          : " << tensor_flow_saved_model << endl;
        wcout << L"Meta data file : " << meta_data << endl;
        wcout << L"---------------------------------------------------------";
        wcout << L"---------------------------------------------------------";
        wcout << endl;
        wstring x = input_video_ids;
        int idx = StartCreateVideoMetaData(input_video_ids.c_str(), meta_data.c_str(), tensor_flow_saved_model.c_str(), UpdateStatus);
        if (idx != 0) {
            wcout << endl << L"Creating meta data failed" << endl;
            return -1;
        }
        else {
            wcout << endl << L"Process has started, press any key to stop" << endl;
            std::cin.get();
            StopOperation(idx);
            wcout << L"-----------------------------" << endl;
            wcout << L"-----------------------------" << endl;
            wcout << L"Process started : " << start_time << endl;
            wcout << L"Process finished: " << get_current_time() << endl;
            wcout << L"-----------------------------" << endl;
            wcout << L"-----------------------------" << endl;
            return 0;
        }
    }
    else if ((input_video_ids == L"") && (meta_data != L""))
    {
        int idx;

        wcout << endl;
        wcout << L"---------------------------------------------------------";
        wcout << L"---------------------------------------------------------";
        wcout << L"Calling virtual camera creation with the following parameters  " << endl;
        wcout << L"Meta data file     : " << meta_data << endl;
        wcout << L"---------------------------------------------------------";
        wcout << L"---------------------------------------------------------";
        wcout << endl;

        if (!create_video_only)
        {
            idx = StartCreateVirtualCamData(meta_data.c_str(), NULL);
            if (idx != 0) {
                wcout << endl << L"Creating virtual camera data failed" << endl;
                return -1;
            }
            else {
                wcout << endl << L"Creating virtual camera data has started, press any key to stop" << endl;
            }

            WaitForOperationToFinish(idx);

            wcout << endl << L"Creating virtual camera data finished" << endl;
        }
        else
        {
            wcout << endl << L"Skipping virtual camera data generation" << endl;
        }

        wcout << endl << L"Creating output video" << endl;

        idx = StartCreateOutputVideo(meta_data.c_str(), (display_window ? TRUE : FALSE), UpdateStatus);
        if (idx != 0) {
            wcout << endl << L"Creating output video failed" << endl;
            return -1;
        }

        std::cin.get();
        StopOperation(idx);

        wcout << L"-----------------------------" << endl;
        wcout << L"-----------------------------" << endl;
        wcout << L"Process started : " << start_time << endl;
        wcout << L"Process finished: " << get_current_time() << endl;
        wcout << L"-----------------------------" << endl;
        wcout << L"-----------------------------" << endl;
        return 0;
    }
    else
    {
        wcout << endl << L"Missing parameters" << endl;
        wcout << L"" << endl;
        wcout << L"Usage:" << endl;
        wcout << L"openvcam /i:[input video ids] /m:[meta data] /t:[tensorflow saved model] /d /o" << endl;
        wcout << L"" << endl;
        wcout << L"Use /d parameter to display video output when creating it" << endl;
        wcout << L"" << endl;
        return -1;
    }
}