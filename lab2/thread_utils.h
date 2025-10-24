#pragma once
#include "point3d.h"
#include <vector>
#include <atomic>
#include <windows.h>

using namespace std;

DWORD WINAPI worker_with_flags(LPVOID lpParam);
void run_single_threaded(const std::vector<Point3D>& points);
void run_multi_thread(const std::vector<Point3D>& points, int num_threads);
