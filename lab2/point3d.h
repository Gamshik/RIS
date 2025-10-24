#pragma once
#include <array>
#include <vector>
#include <atomic>

struct Point3D {
    double x, y, z;
};

struct TrapezoidResult {
    std::array<Point3D, 4> vertices;
    double area;
    std::array<double, 4> angles;
};

struct ThreadData {
    const std::vector<Point3D>* points;
    std::vector<TrapezoidResult>* shared_results;
    std::atomic<bool>* ready_flags;
    int thread_id;
    int num_threads;
};
