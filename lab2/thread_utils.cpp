#include "thread_utils.h"
#include "math_utils.h"
#include "file_utils.h"
#include <windows.h>
#include <vector>
#include <iostream>
#include <chrono>

DWORD WINAPI worker_with_flags(LPVOID lpParam) {
    ThreadData* data = (ThreadData*)lpParam;
    const vector<Point3D>& points = *data->points;
    vector<TrapezoidResult>& shared_results = *data->shared_results;
    atomic<bool>* ready_flags = data->ready_flags;
    int thread_id = data->thread_id;
    int num_threads = data->num_threads;
    int n = (int)points.size();

    for (int i = thread_id; i < n-3; i += num_threads) {
        vector<TrapezoidResult> local_results;
        for (int j = i+1; j<n-2; ++j)
            for (int k=j+1; k<n-1; ++k)
                for (int l=k+1; l<n; ++l)
                    process_combination({points[i], points[j], points[k], points[l]}, local_results);

        ready_flags[thread_id].store(true);
        for (int other=0; other<num_threads; ++other) 
            if (other!=thread_id) 
                while(ready_flags[other].load()) 
                    Sleep(1);

        shared_results.insert(shared_results.end(), local_results.begin(), local_results.end());
        ready_flags[thread_id].store(false);
    }

    return 0;
}

void run_single_threaded(const vector<Point3D>& points) {
    auto start = chrono::high_resolution_clock::now();
    
    vector<TrapezoidResult> results;
    size_t n = points.size();

    for (size_t i = 0; i < n; ++i)
        for (size_t j = i + 1; j < n; ++j)
            for (size_t k = j + 1; k < n; ++k)
                for (size_t l = k + 1; l < n; ++l)
                    process_combination({points[i], points[j], points[k], points[l]}, results);
            

    auto end = chrono::high_resolution_clock::now();
    chrono::duration<double> duration = end - start;

    write_results_to_file("single_thread", results);
    
    cout << "Trapezoids found: " << results.size() << endl;
    cout << "Execution time: " << duration.count() << " seconds" << endl;
}


void run_multi_thread(const vector<Point3D>& points, int num_threads) {
    auto start = chrono::high_resolution_clock::now();

    vector<TrapezoidResult> shared_results;
    vector<HANDLE> threads(num_threads);
    vector<ThreadData> thread_data(num_threads);
    vector<atomic<bool>> ready_flags(num_threads);

    for (int i = 0; i < num_threads; ++i) ready_flags[i].store(false);

    for (int i = 0; i < num_threads; ++i) {
        thread_data[i].points = &points;
        thread_data[i].shared_results = &shared_results;
        thread_data[i].ready_flags = ready_flags.data();
        thread_data[i].thread_id = i;
        thread_data[i].num_threads = num_threads;

        threads[i] = CreateThread(
            nullptr,
            0,
            worker_with_flags,
            &thread_data[i],
            0,
            nullptr
        );
    }

    WaitForMultipleObjects(num_threads, threads.data(), TRUE, INFINITE);

    for (int i = 0; i < num_threads; ++i) CloseHandle(threads[i]);

    auto end = chrono::high_resolution_clock::now();
    chrono::duration<double> duration = end - start;

    write_results_to_file("multi_thread", shared_results);
    
    cout << "Trapezoids found: " << shared_results.size() << "\n";
    cout << "Execution time: " << duration.count() << " seconds\n";
}