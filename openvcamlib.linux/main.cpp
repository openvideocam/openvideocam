#include <cstdio>
#include <sqlite3.h>
#include <tensorflow/c/c_api.h>
#include <opencv2/opencv.hpp>
#include <opencv2/core/version.hpp>
#include <../openvcamlib/db.h>
#include <../openvcamlib/base64.h>
#include <../openvcamlib/utils.h>

int main()
{
    printf("hello from %s!\n", "openvcam.linux");

    printf("You are using sqlite version: %s\n", sqlite3_libversion());

    //printf("You are using tensorflow version: %s\n", TF_Version());

    printf("You are using opencv version: %d.%d\n", CV_VERSION_MAJOR, CV_MINOR_VERSION);
    
    return 0;
}