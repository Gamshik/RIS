#include "globals.h"

double Function(double x) {
    return x * x * x * std::exp(std::sin(x));
}

double FunctionDerivative(double x) {
    double e = std::exp(std::sin(x));
    return 3.0 * x * x * e + x * x * x * e * std::cos(x);
}

DWORD WINAPI ThreadMonteCarloSurface(LPVOID param) {
    ThreadData* data = static_cast<ThreadData*>(param);

    int samples = data->segments;
    if (samples <= 0) {
        data->result = 0.0;
        return 0;
    }

    unsigned seed = static_cast<unsigned>(std::chrono::high_resolution_clock::now().time_since_epoch().count());
    seed ^= (data->threadId + 1) * 0x9e3779b9;
    std::mt19937_64 rng(seed);
    std::uniform_real_distribution<double> dist(data->start, data->end);

    double localSum = 0.0;
    for (int i = 0; i < samples; ++i) {
        double x = dist(rng);
        double fx = Function(x);
        double dfx = FunctionDerivative(x);
        double gx = 2.0 * PI * fx * std::sqrt(1.0 + dfx * dfx); 
        localSum += gx;
    }

    WaitForSingleObject(g_mutex, INFINITE);
    g_globalResult += localSum; 
    ReleaseMutex(g_mutex);

    data->result = localSum;
    return 0;
}

double CalculateSurfaceSingleThread(double a, double b, int samples) {
    if (samples <= 0) return 0.0;

    unsigned seed = static_cast<unsigned>(std::chrono::high_resolution_clock::now().time_since_epoch().count());
    std::mt19937_64 rng(seed);
    std::uniform_real_distribution<double> dist(a, b);

    double sum = 0.0;
    for (int i = 0; i < samples; ++i) {
        double x = dist(rng);
        double fx = Function(x);
        double dfx = FunctionDerivative(x);
        double gx = 2.0 * PI * fx * std::sqrt(1.0 + dfx * dfx);
        sum += gx;
    }

    double integral = (b - a) * (sum / static_cast<double>(samples));
    return integral;
}

int GetSamplesFromEpsilon(double epsilon) {
    if (epsilon >= 0.1) return 1000;
    if (epsilon >= 0.01) return 10000;
    if (epsilon >= 0.001) return 100000;
    if (epsilon >= 0.0001) return 1000000;
    return 5000000; 
}

double CalculateSurface(double a, double b, int samples, int threadCount) {
    if (threadCount == 1) {
        return CalculateSurfaceSingleThread(a, b, samples);
    }

    g_globalResult = 0.0; 
    std::vector<HANDLE> threads(threadCount);
    std::vector<ThreadData> threadData(threadCount);

    int baseSamples = samples / threadCount;
    int remainder = samples % threadCount;

    for (int i = 0; i < threadCount; ++i) {
        threadData[i].start = a;
        threadData[i].end = b;
        threadData[i].segments = baseSamples + (i < remainder ? 1 : 0); 
        threadData[i].result = 0.0;
        threadData[i].threadId = i;

        threads[i] = CreateThread(
            nullptr, 0, ThreadMonteCarloSurface,
            &threadData[i], 0, nullptr
        );

        if (threads[i] == nullptr) {
            for (int j = 0; j < i; ++j) {
                CloseHandle(threads[j]);
            }
            return 0.0;
        }
    }

    WaitForMultipleObjects(threadCount, threads.data(), TRUE, INFINITE);

    for (int i = 0; i < threadCount; ++i) {
        CloseHandle(threads[i]);
    }

    double integral = (b - a) * (g_globalResult / static_cast<double>(samples));
    return integral;
}
