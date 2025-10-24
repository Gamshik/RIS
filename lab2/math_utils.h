#pragma once
#include "point3d.h"
#include <vector>
#include <array>

constexpr double EPSILON = 1e-9;
constexpr double PI = 3.14159265358979323846;

using Vector3D = Point3D;

Vector3D operator-(const Point3D& a, const Point3D& b);
double dot_product(const Vector3D& v1, const Vector3D& v2);
Vector3D cross_product(const Vector3D& v1, const Vector3D& v2);
double magnitude(const Vector3D& v);
bool are_parallel(const Vector3D& v1, const Vector3D& v2);
double clamp(double v, double lo = -1.0, double hi = 1.0);
double calculate_angle(const Point3D& p_prev, const Point3D& p_curr, const Point3D& p_next);
bool are_coplanar(const Point3D& p1, const Point3D& p2, const Point3D& p3, const Point3D& p4);
void process_combination(const std::array<Point3D, 4>& points, std::vector<TrapezoidResult>& local_results);
