#pragma once
#define WIN32_LEAN_AND_MEAN
#include <cstddef>   
#ifdef byte
#undef byte
#endif
#include <windows.h>
#include "point3d.h"
#include <vector>
#include <atomic>

using namespace std;

DWORD WINAPI worker_with_flags(LPVOID lpParam);
void run_single_threaded(const std::vector<Point3D>& points);
void run_multi_thread(const std::vector<Point3D>& points, int num_threads);
