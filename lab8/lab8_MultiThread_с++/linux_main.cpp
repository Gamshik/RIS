#include <pthread.h>
#include <unistd.h>   
#include <vector>
#include <string>
#include <fstream>
#include <sstream>
#include <iostream>
#include <iomanip>
#include <chrono>
#include <cmath>
#include <atomic>

struct WorkerInfo {
    int id;
    int startRow;
    int endRow;
};

static std::vector<double> Aflat;
static std::vector<double> B;
static int N;
static int numThreads;

static std::vector<WorkerInfo> workers;
static std::vector<pthread_t> threads;

static pthread_mutex_t mutex_shared = PTHREAD_MUTEX_INITIALIZER;
static pthread_cond_t cvStart = PTHREAD_COND_INITIALIZER;
static pthread_cond_t cvDone  = PTHREAD_COND_INITIALIZER;

static int current_k = -1;
static std::vector<double> shared_pivotRow;
static double shared_akk = 0.0;
static double shared_bk = 0.0;
static int finishedCount = 0;
static bool terminateFlag = false;

void ReadMatrixAndVector(const std::string& folder, std::vector<double>& Af, std::vector<double>& Bv, int& n)
{
    std::ifstream finA(folder + "/A.txt");
    if (!finA) throw std::runtime_error("Cannot open A.txt");
    std::string line;
    std::vector<std::vector<double>> tmp;
    while (std::getline(finA, line))
    {
        std::istringstream ss(line);
        std::vector<double> row;
        double v;
        while (ss >> v) row.push_back(v);
        if (!row.empty()) tmp.push_back(std::move(row));
    }
    n = (int)tmp.size();
    if (n == 0) throw std::runtime_error("Empty A.txt");
    Af.assign(n * n, 0.0);
    for (int i = 0; i < n; ++i) {
        if ((int)tmp[i].size() != n)
            throw std::runtime_error("A.txt: inconsistent row size");
        for (int j = 0; j < n; ++j) Af[i * n + j] = tmp[i][j];
    }

    std::ifstream finB(folder + "/B.txt");
    if (!finB) throw std::runtime_error("Cannot open B.txt");
    Bv.assign(n, 0.0);
    for (int i = 0; i < n; ++i) {
        if (!(finB >> Bv[i])) throw std::runtime_error("B.txt not match size");
    }
}

void WriteVector(const std::string& path, const std::vector<double>& X)
{
    std::ofstream fout(path);
    fout << std::setprecision(17);
    for (double v : X) fout << v << "\n";
}

void* WorkerRoutine(void* arg)
{
    WorkerInfo* wi = (WorkerInfo*)arg;

    while (true)
    {
        pthread_mutex_lock(&mutex_shared);
        while (current_k == -1 && !terminateFlag) {
            pthread_cond_wait(&cvStart, &mutex_shared);
        }

        if (terminateFlag) {
            pthread_mutex_unlock(&mutex_shared);
            break;
        }

        int k = current_k;
        double akk = shared_akk;
        double bk = shared_bk;
        std::vector<double> localPivot = shared_pivotRow;
        pthread_mutex_unlock(&mutex_shared);

        int rowBegin = std::max(wi->startRow, k + 1);
        int rowEnd = wi->endRow;
        if (rowBegin < rowEnd) {
            int trailing = (int)localPivot.size();
            for (int i = rowBegin; i < rowEnd; ++i) {
                int baseI = i * N;
                double a_ik = Aflat[baseI + k];
                if (a_ik == 0.0) continue;
                double factor = a_ik / akk;
                Aflat[baseI + k] = factor;

                int pj = k + 1;
                for (int t = 0; t < trailing; ++t, ++pj)
                    Aflat[baseI + pj] -= factor * localPivot[t];
                B[i] -= factor * bk;
            }
        }

        pthread_mutex_lock(&mutex_shared);
        ++finishedCount;
        if (finishedCount >= numThreads) {
            pthread_cond_signal(&cvDone);
        }
        pthread_mutex_unlock(&mutex_shared);
    }

    return nullptr;
}

int main(int argc, char* argv[])
{
    if (argc < 2) {
        std::cerr << "Ожидался аргумент — путь к папке с данными\n";
        return 1;
    }
    std::string folder = argv[1];

    numThreads = (int)sysconf(_SC_NPROCESSORS_ONLN);
    if (numThreads <= 0) numThreads = 1;

    ReadMatrixAndVector(folder, Aflat, B, N);

    workers.resize(numThreads);
    threads.resize(numThreads);
    int rowsPer = (N + numThreads - 1) / numThreads;
    for (int t = 0; t < numThreads; ++t) {
        int s = t * rowsPer;
        int e = std::min((t + 1) * rowsPer, N);
        workers[t].id = t;
        workers[t].startRow = s;
        workers[t].endRow = e;
    }

    for (int t = 0; t < numThreads; ++t) {
        if (pthread_create(&threads[t], nullptr, WorkerRoutine, &workers[t]) != 0) {
            std::cerr << "Ошибка при создании потока " << t << "\n";
            return 2;
        }
    }

    auto t1 = std::chrono::high_resolution_clock::now();

    for (int k = 0; k < N; ++k) {
        int pivotIndex = k * N + k;
        double akk = Aflat[pivotIndex];

        int trailing = N - (k + 1);
        std::vector<double> pivotRow(trailing);
        for (int j = 0; j < trailing; ++j)
            pivotRow[j] = Aflat[k * N + (k + 1 + j)];
        double bk = B[k];

        pthread_mutex_lock(&mutex_shared);
        shared_pivotRow = std::move(pivotRow); 
        shared_akk = akk;
        shared_bk = bk;
        finishedCount = 0;
        current_k = k;
        pthread_cond_broadcast(&cvStart);
        
        while (finishedCount < numThreads) {
            pthread_cond_wait(&cvDone, &mutex_shared);
        }
        
        current_k = -1;
        pthread_mutex_unlock(&mutex_shared);
    }

    pthread_mutex_lock(&mutex_shared);
    terminateFlag = true;
    pthread_cond_broadcast(&cvStart);
    pthread_mutex_unlock(&mutex_shared);

    for (int t = 0; t < numThreads; ++t) {
        pthread_join(threads[t], nullptr);
    }

    std::vector<double> X(N);
    for (int i = N - 1; i >= 0; --i) {
        double sum = B[i];
        int baseI = i * N;
        for (int j = i + 1; j < N; ++j)
            sum -= Aflat[baseI + j] * X[j];
        double diag = Aflat[baseI + i];
        X[i] = (std::abs(diag) < 1e-18) ? 0.0 : sum / diag;
    }

    auto t2 = std::chrono::high_resolution_clock::now();
    std::chrono::duration<double, std::milli> elapsed = t2 - t1;

    WriteVector(folder + "/X.txt", X);

    std::cout << "==============================================\n";
    std::cout << "Размер матрицы: " << N << "x" << N << "\n";
    std::cout << "Потоков: " << numThreads << "\n";
    std::cout << "Время: " << elapsed.count() << " мс\n";
    std::cout << "==============================================\n";

    return 0;
}
