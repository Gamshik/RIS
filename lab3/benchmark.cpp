#include "globals.h"

void RunBenchmarks() {
    g_benchmarkResults.clear();

    double a = g_currentA;
    double b = g_currentB;

    // Тест 1: Зависимость от точности (1 и 3 потока)
    std::vector<double> epsilons = {0.1, 0.01, 0.001, 0.0001, 0.00001};

    SetWindowTextW(g_hResultText, L"Running benchmarks...\r\n");
    UpdateWindow(g_hMainWnd);

    for (double eps : epsilons) {
        int samples = GetSamplesFromEpsilon(eps);

        // 1 поток
        auto start = std::chrono::high_resolution_clock::now();
        double surf = CalculateSurface(a, b, samples, 1);
        auto end = std::chrono::high_resolution_clock::now();
        double time = std::chrono::duration<double>(end - start).count();
        g_benchmarkResults.push_back({1, eps, time, surf});

        // 3 потока
        start = std::chrono::high_resolution_clock::now();
        surf = CalculateSurface(a, b, samples, 3);
        end = std::chrono::high_resolution_clock::now();
        time = std::chrono::duration<double>(end - start).count();
        g_benchmarkResults.push_back({3, eps, time, surf});
    }

    // Тест 2: Зависимость от количества потоков (eps = 0.00001)
    double fixedEps = 0.00001;
    int fixedSamples = GetSamplesFromEpsilon(fixedEps);

    for (int threads = 1; threads <= 10; ++threads) {
        auto start = std::chrono::high_resolution_clock::now();
        double surf = CalculateSurface(a, b, fixedSamples, threads);
        auto end = std::chrono::high_resolution_clock::now();
        double time = std::chrono::duration<double>(end - start).count();
        g_benchmarkResults.push_back({threads, fixedEps, time, surf});
    }

    InvalidateRect(g_hMainWnd, nullptr, TRUE);
}
