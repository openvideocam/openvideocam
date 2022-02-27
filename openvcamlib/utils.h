#ifndef _UTILS_H_
#define _UTILS_H_

#include <string>

using namespace std;

wstring get_current_time();
vector<wstring> split_string(wstring text, wstring delim);
vector<int> string_vector_to_int(vector<wstring> list);

#endif
