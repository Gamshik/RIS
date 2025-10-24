#include "file_utils.h"
#include <fstream>
#include <iostream>
#include <cstdlib>
#include <ctime>
#include <iomanip>

void generate_data_file(const string& filename, int n) {
    ofstream file(filename);
    if (!file) { cerr << "Cannot create file\n"; return; }

    srand((unsigned)time(nullptr));
    for (int i = 0; i < n; ++i) {
        double x = (rand()%20001 - 10000)/100.0;
        double y = (rand()%20001 - 10000)/100.0;
        double z = 0.0;
        file << x << " " << y << " " << z << "\n";
    }
    cout << "Generated " << n << " points in " << filename << "\n";
}

vector<Point3D> read_points_from_file(const string& filename) {
    ifstream file(filename);
    vector<Point3D> points;
    Point3D p;
    while(file >> p.x >> p.y >> p.z) points.push_back(p);
    return points;
}

void write_results_to_file(const string& filename, const vector<TrapezoidResult>& results) {
    ofstream file(filename);
    if (!file) { cerr << "Cannot write file\n"; return; }

    file << fixed << setprecision(4);
    for (auto& res : results) {
        file << "Vertices: ";
        for (auto& v : res.vertices)
            file << "{" << v.x << ", " << v.y << ", " << v.z << "} ";
        file << "\nAngles: ";
        for (auto& a : res.angles) file << a << "° ";
        file << "\nArea: " << res.area << "\n\n";
    }
}
