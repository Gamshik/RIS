#include "point3d.h"
#include "math_utils.h"
#include "file_utils.h"
#include "thread_utils.h"
#include <iostream>
#include <vector>

using namespace std;

int main() {
    const string filename = "points";

    int N_POINTS = 0;
    while(N_POINTS <= 0) {
        cout << "Enter number of points: ";
        cin >> N_POINTS;
    }

    generate_data_file(filename, N_POINTS);
    vector<Point3D> points = read_points_from_file(filename);
    if(points.empty()) return 1;

    int choice = 0;
    while(choice != 3) {
        cout << "\nSelect mode:\n1. Single-thread\n2. Multi-thread\n3. Exit\n";
        cin >> choice;

        switch(choice) {
            case 1: run_single_threaded(points); break;
            case 2: {
                int num_threads = 0;
                while(num_threads<1||num_threads>4) {
                    cout << "Enter number of threads (1-4): ";
                    cin >> num_threads;
                }
                run_multi_thread(points,num_threads);
                break;
            }
            default: cout << "Invalid choice\n";
        }
    }

    return 0;
}
