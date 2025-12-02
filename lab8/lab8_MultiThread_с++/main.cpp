#include <windows.h>
#include <vector>
#include <string>
#include <fstream>
#include <sstream>
#include <iostream>
#include <iomanip>
#include <chrono>
#include <cmath>

struct ThreadData
{
    int threadId;
    int startRow;
    int endRow;
    int N;
    double* Aflat;
    double* B;
    std::vector<double> pivotRow;
    double akk;
    double bk;
    HANDLE startEvent;
    HANDLE doneEvent;
    volatile bool terminate;
};

void printW(const std::wstring& w)
{
    DWORD written;
    WriteConsoleW(GetStdHandle(STD_OUTPUT_HANDLE),
                  w.c_str(),
                  (DWORD)w.size(),
                  &written,
                  nullptr);
}

DWORD WINAPI GaussThread(LPVOID lpParam)
{
    ThreadData* data = (ThreadData*)lpParam;
    while (!data->terminate)
    {
        WaitForSingleObject(data->startEvent, INFINITE);

        if (data->terminate) break;

        int trailing = (int)data->pivotRow.size();
        int N = data->N;
        int baseI;

        for (int i = data->startRow; i < data->endRow; ++i)
        {
            baseI = i * N;
            double a_ik = data->Aflat[baseI + (N - trailing - 1)];
            if (a_ik == 0.0) continue;

            double factor = a_ik / data->akk;
            data->Aflat[baseI + (N - trailing - 1)] = factor;

            int pj = N - trailing;
            for (int t = 0; t < trailing; ++t, ++pj)
                data->Aflat[baseI + pj] -= factor * data->pivotRow[t];

            data->B[i] -= factor * data->bk;
        }

        SetEvent(data->doneEvent);
    }
    return 0;
}

void ReadMatrixAndVector(const std::string& folder, std::vector<double>& Aflat, std::vector<double>& B, int& N)
{
    std::ifstream finA(folder + "\\A.txt");
    std::string line;
    std::vector<std::vector<double>> tempA;

    while (std::getline(finA, line))
    {
        std::istringstream ss(line);
        std::vector<double> row;
        double val;
        while (ss >> val) row.push_back(val);
        tempA.push_back(row);
    }
    N = (int)tempA.size();
    Aflat.resize(N * N);
    for (int i = 0; i < N; ++i)
        for (int j = 0; j < N; ++j)
            Aflat[i * N + j] = tempA[i][j];

    std::ifstream finB(folder + "\\B.txt");
    B.resize(N);
    for (int i = 0; i < N; ++i)
    {
        if (!(finB >> B[i]))
            throw std::runtime_error("B.txt не соответствует размерам");
    }
}

void WriteVector(const std::string& path, const std::vector<double>& X)
{
    std::ofstream fout(path);
    fout << std::setprecision(17);
    for (double v : X) fout << v << "\n";
}

int main(int argc, char* argv[])
{
    if (argc < 2)
    {
        printW(L"Ожидался аргумент — путь к папке с данными.\n");
        return 1;
    }

    std::string folder = argv[1];

    SYSTEM_INFO sysinfo;
    GetSystemInfo(&sysinfo);
    int numThreads = sysinfo.dwNumberOfProcessors;

    std::vector<double> Aflat, B;
    int N;
    ReadMatrixAndVector(folder, Aflat, B, N);

    std::vector<ThreadData> tdata(numThreads);
    std::vector<HANDLE> threads(numThreads);
    std::vector<HANDLE> startEvents(numThreads);
    std::vector<HANDLE> doneEvents(numThreads);

    int rowsPerThread = (N + numThreads - 1) / numThreads;

    for (int t = 0; t < numThreads; ++t)
    {
        int startRow = t * rowsPerThread;
        int endRow = std::min((t + 1) * rowsPerThread, N);

        startEvents[t] = CreateEvent(NULL, FALSE, FALSE, NULL);
        doneEvents[t] = CreateEvent(NULL, FALSE, FALSE, NULL);

        tdata[t] = { t, startRow, endRow, N, Aflat.data(), B.data(), {}, 0.0, 0.0, startEvents[t], doneEvents[t], false };

        threads[t] = CreateThread(NULL, 0, GaussThread, &tdata[t], 0, NULL);
    }

    auto t1 = std::chrono::high_resolution_clock::now();

    for (int k = 0; k < N; ++k)
    {
        int pivotIndex = k * N + k;
        double akk = Aflat[pivotIndex];

        int trailing = N - (k + 1);
        std::vector<double> pivotRow(trailing);
        for (int j = 0; j < trailing; ++j)
            pivotRow[j] = Aflat[k * N + (k + 1 + j)];

        double bk = B[k];

        for (int t = 0; t < numThreads; ++t)
        {
            tdata[t].pivotRow = pivotRow;
            tdata[t].akk = akk;
            tdata[t].bk = bk;
        }

        for (int t = 0; t < numThreads; ++t)
            SetEvent(startEvents[t]);

        WaitForMultipleObjects(numThreads, doneEvents.data(), TRUE, INFINITE);
    }

    for (int t = 0; t < numThreads; ++t)
        tdata[t].terminate = true;
    for (int t = 0; t < numThreads; ++t)
        SetEvent(startEvents[t]);
    WaitForMultipleObjects(numThreads, threads.data(), TRUE, INFINITE);
    for (int t = 0; t < numThreads; ++t)
    {
        CloseHandle(threads[t]);
        CloseHandle(startEvents[t]);
        CloseHandle(doneEvents[t]);
    }

    std::vector<double> X(N);
    for (int i = N - 1; i >= 0; --i)
    {
        double sum = B[i];
        int baseI = i * N;
        for (int j = i + 1; j < N; ++j)
            sum -= Aflat[baseI + j] * X[j];

        double diag = Aflat[baseI + i];
        X[i] = (std::abs(diag) < 1e-18) ? 0.0 : sum / diag;
    }

    auto t2 = std::chrono::high_resolution_clock::now();
    std::chrono::duration<double, std::milli> elapsed = t2 - t1;

    WriteVector(folder + "\\X.txt", X);

    printW(L"==============================================\n");
    printW(L"Размер матрицы: " + std::to_wstring(N) + L"x" + std::to_wstring(N) + L"\n");
    printW(L"Потоков: " + std::to_wstring(numThreads) + L"\n");
    printW(L"Время: " + std::to_wstring(elapsed.count()) + L" мс\n");
    printW(L"==============================================\n");


    return 0;
}
