#include <iostream>
#include <fstream>
#include <vector>
#include <string>
#include <sstream>
#include <chrono>
#include <cmath>
#include <stdexcept>
#include <windows.h>

using namespace std;

void printW(const wstring& w)
{
    DWORD written;
    WriteConsoleW(GetStdHandle(STD_OUTPUT_HANDLE),
                  w.c_str(),
                  (DWORD)w.size(),
                  &written,
                  nullptr);
}

void ReadMatrix(const string& fileA, const string& fileB,
                    vector<vector<double>>& A, vector<double>& B)
{
    ifstream fa(fileA);
    if (!fa.is_open()) throw runtime_error("Не могу открыть A.txt");

    ifstream fb(fileB);
    if (!fb.is_open()) throw runtime_error("Не могу открыть B.txt");

    int N = 0;
    string line;
    while (getline(fa, line)) N++;
    fa.clear(); fa.seekg(0); 

    A.assign(N, vector<double>(N));
    B.assign(N, 0.0);

    for (int i = 0; i < N; i++)
        for (int j = 0; j < N; j++)
            fa >> A[i][j];

    for (int i = 0; i < N; i++)
        fb >> B[i];
}

vector<double> GaussianElimination(vector<vector<double>>& A, vector<double>& B, int N)
{
    // Прямой ход
    for (int k = 0; k < N; k++)
    {
        double pivot = A[k][k];
        if (abs(pivot) < 1e-15)
            throw runtime_error("Нулевой главный элемент в строке " + to_string(k));

        for (int j = k; j < N; j++)
            A[k][j] /= pivot;
        B[k] /= pivot;

        for (int i = k + 1; i < N; i++)
        {
            double factor = A[i][k];
            for (int j = k; j < N; j++)
                A[i][j] -= factor * A[k][j];
            B[i] -= factor * B[k];
        }
    }

    // Обратный ход
    vector<double> X(N);
    for (int i = N - 1; i >= 0; i--)
    {
        double sum = B[i];
        for (int j = i + 1; j < N; j++)
            sum -= A[i][j] * X[j];
        X[i] = sum;
    }

    return X;
}

int main(int argc, char* argv[])
{
    SetConsoleOutputCP(CP_UTF8);
    SetConsoleCP(CP_UTF8);
    setlocale(LC_ALL, "");

    if (argc < 2)
    {
        printW(L"Ожидался аргумент — путь к папке с данными.\n");
        return 0;
    }

    string folder = argv[1];
    string fileA = folder + "/A.txt";
    string fileB = folder + "/B.txt";
    string fileX = folder + "/X.txt";

    ifstream testA(fileA), testB(fileB);
    if (!testA.is_open() || !testB.is_open())
    {
        printW(L"Файлы A.txt или B.txt не найдены в указанной папке.\n");
        return 0;
    }

    vector<vector<double>> A;
    vector<double> B;


    ReadMatrix(fileA, fileB, A, B);

    auto start = chrono::high_resolution_clock::now();
    int N = B.size();
    printW(L"Размер матрицы: " + to_wstring(N) + L"x" + to_wstring(N) + L"\n");
    vector<double> X = GaussianElimination(A, B, N);

    {
        ofstream w(fileX);
        w.setf(std::ios::scientific);
        w.precision(17);

        for (int i = 0; i < N; i++)
            w << X[i] << "\n";
    }

    auto end = chrono::high_resolution_clock::now();
    long long ms = chrono::duration_cast<chrono::milliseconds>(end - start).count();

    printW(L"Время: " + to_wstring(ms) + L" мс\n");

    return 0;
}
