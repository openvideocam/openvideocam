#include <string>
#include <vector>
#include <time.h>
#include "utils.h"

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

vector<wstring> split_string(wstring text, wstring delim)
{
    size_t start = 0;
    size_t end = text.find(delim);
    vector<wstring> result;
    while (end != std::string::npos)
    {
        result.push_back(text.substr(start, end - start));
        start = end + delim.length();
        end = text.find(delim, start);
    }
    result.push_back(text.substr(start));

    return result;
}

vector<int> string_vector_to_int(vector<wstring> list)
{
    vector<int> result;

    for (int i = 0; i < list.size(); i++)
    {
        result.push_back(stoi(list[i]));
    }

    return result;
}