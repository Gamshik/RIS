#include "math_utils.h"
#include <cmath>
#include <iostream>
#include <chrono>
#include <iomanip>

Vector3D operator-(const Point3D& a, const Point3D& b) {
    return {a.x - b.x, a.y - b.y, a.z - b.z};
}

double dot_product(const Vector3D& v1, const Vector3D& v2) {
    return v1.x*v2.x + v1.y*v2.y + v1.z*v2.z;
}

Vector3D cross_product(const Vector3D& v1, const Vector3D& v2) {
    return {
        v1.y*v2.z - v1.z*v2.y,
        v1.z*v2.x - v1.x*v2.z,
        v1.x*v2.y - v1.y*v2.x
    };
}

double magnitude(const Vector3D& v) {
    return sqrt(dot_product(v, v));
}

bool are_parallel(const Vector3D& v1, const Vector3D& v2) {
    return magnitude(cross_product(v1, v2)) < EPSILON;
}

double clamp(double v, double lo, double hi) {
    if (v < lo) return lo;
    if (v > hi) return hi;
    return v;
}

double calculate_angle(const Point3D& p_prev, const Point3D& p_curr, const Point3D& p_next) {
    Vector3D v1 = p_prev - p_curr;
    Vector3D v2 = p_next - p_curr;
    double dot = dot_product(v1, v2);
    double mag1 = magnitude(v1);
    double mag2 = magnitude(v2);
    if (mag1 < EPSILON || mag2 < EPSILON) return 0.0;
    double cosv = dot / (mag1 * mag2);
    cosv = clamp(cosv, -1.0, 1.0);
    return acos(cosv) * 180.0 / PI;
}

bool are_coplanar(const Point3D& p1, const Point3D& p2, const Point3D& p3, const Point3D& p4) {
    Vector3D v1 = p2 - p1;
    Vector3D v2 = p3 - p1;
    Vector3D v3 = p4 - p1;
    double triple = dot_product(v1, cross_product(v2, v3));
    double scale = magnitude(v1) * magnitude(cross_product(v2, v3));
    if (scale < EPSILON) return true;
    return fabs(triple) <= 1e-9 * scale;
}

void process_combination(const std::array<Point3D, 4>& points, std::vector<TrapezoidResult>& local_results) {
    const Point3D& p1 = points[0];
    const Point3D& p2 = points[1];
    const Point3D& p3 = points[2];
    const Point3D& p4 = points[3];

    if (!are_coplanar(p1, p2, p3, p4)) return;

    Vector3D v12 = p2 - p1;
    Vector3D v34 = p4 - p3;
    Vector3D v13 = p3 - p1;
    Vector3D v24 = p4 - p2;
    Vector3D v14 = p4 - p1;
    Vector3D v23 = p3 - p2;

    std::array<Point3D, 4> ordered_vertices;
    bool is_trapezoid = false;

    if (are_parallel(v12, v34) && magnitude(v13) > EPSILON && magnitude(v24) > EPSILON) {
        ordered_vertices = {p1, p2, p4, p3};
        is_trapezoid = true;
    } else if (are_parallel(v13, v24) && magnitude(v12) > EPSILON && magnitude(v34) > EPSILON) {
        ordered_vertices = {p1, p3, p4, p2};
        is_trapezoid = true;
    } else if (are_parallel(v14, v23) && magnitude(v12) > EPSILON && magnitude(v34) > EPSILON) {
        ordered_vertices = {p1, p4, p3, p2};
        is_trapezoid = true;
    }

    if (!is_trapezoid) return;

    TrapezoidResult res;
    res.vertices = ordered_vertices;

    Vector3D d1 = ordered_vertices[2] - ordered_vertices[0];
    Vector3D d2 = ordered_vertices[1] - ordered_vertices[0];
    Vector3D d3 = ordered_vertices[3] - ordered_vertices[0];

    res.area = 0.5*magnitude(cross_product(d2, d1)) + 0.5*magnitude(cross_product(d1, d3));

    res.angles[0] = calculate_angle(ordered_vertices[3], ordered_vertices[0], ordered_vertices[1]);
    res.angles[1] = calculate_angle(ordered_vertices[0], ordered_vertices[1], ordered_vertices[2]);
    res.angles[2] = calculate_angle(ordered_vertices[1], ordered_vertices[2], ordered_vertices[3]);
    res.angles[3] = calculate_angle(ordered_vertices[2], ordered_vertices[3], ordered_vertices[0]);

    double angle_sum = res.angles[0] + res.angles[1] + res.angles[2] + res.angles[3];
    if (res.area > EPSILON && fabs(angle_sum - 360.0) < 1.0) {
        local_results.push_back(res);
    }
}