#include <mpi.h>
#include <windows.h>
#include <iostream>
#include <fstream>
#include <sstream>
#include <string>
#include <stdexcept>
#include <chrono>
#include <locale>
#include <vector>

using namespace std;

void logToFile(const wstring& message) {
    const string logPath = "log.txt";
    int size_needed = WideCharToMultiByte(CP_UTF8, 0, message.c_str(), (int)message.size(), nullptr, 0, nullptr, nullptr);
    string utf8Str(size_needed, 0);
    WideCharToMultiByte(CP_UTF8, 0, message.c_str(), (int)message.size(), &utf8Str[0], size_needed, nullptr, nullptr);
    ofstream logFile(logPath, ios::app);
    if (!logFile.is_open()) return;
    logFile << utf8Str << endl;
    logFile.close();
}

void ReadMatrix(const std::string& path, double**& A, int& N)
{
    ifstream fin(path);
    if (!fin) throw std::runtime_error("Cannot open " + path);

    string line;
    vector<vector<double>> rows;

    while (std::getline(fin, line))
    {
        istringstream ss(line);
        vector<double> row;
        double v;

        while (ss >> v) row.push_back(v);

        if (!row.empty())
            rows.push_back(std::move(row));
    }

    N = (int)rows.size();
    if (N == 0) throw std::runtime_error("Matrix file is empty");

    for (int i = 0; i < N; i++)
        if ((int)rows[i].size() != N)
            throw std::runtime_error("Matrix is not square");

    A = new double*[N];
    for (int i = 0; i < N; i++)
        A[i] = new double[N];

    
    for (int i = 0; i < N; i++)
        for (int j = 0; j < N; j++)
            A[i][j] = rows[i][j];
}

void ReadVector(const std::string& path, double*& B, int N)
{
    std::ifstream fin(path);
    if (!fin) throw std::runtime_error("Cannot open " + path);

    B = new double[N];
    for (int i = 0; i < N; i++)
        if (!(fin >> B[i]))
            throw std::runtime_error("Vector file does not match matrix size");
}


void WriteVector(const string& path, double* X, int N) {
    ofstream fout(path);
    fout.precision(17);
    fout << scientific;
    for (int i = 0; i < N; i++) fout << X[i] << "\n";
}

void LocalGaussianElimination(double** ALocal, double* B, int* myColumns, int localCols, int N, int rank, int size, MPI_Comm comm) {
    for (int k = 0; k < N; k++) {
        int columnOwner = k % size;
        double* pivotRow = new double[N];
        double akk = 0.0;
        double bk = 0.0;

        if (rank == columnOwner) {
            int localK = -1;
            for (int j = 0; j < localCols; j++) if (myColumns[j] == k) { localK = j; break; }
            if (localK == -1) throw runtime_error("Столбец не найден у владельца");
            for (int j = 0; j < localCols; j++) pivotRow[myColumns[j]] = ALocal[k][j];
            akk = pivotRow[k];
            bk = B[k];
        }

        MPI_Bcast(pivotRow, N, MPI_DOUBLE, columnOwner, comm);
        MPI_Bcast(&akk, 1, MPI_DOUBLE, columnOwner, comm);
        MPI_Bcast(&bk, 1, MPI_DOUBLE, columnOwner, comm);

        for (int i = k + 1; i < N; i++) {
            double factor = pivotRow[k] / akk;
            for (int j = 0; j < localCols; j++)
                ALocal[i][j] -= factor * pivotRow[myColumns[j]];
            B[i] -= factor * bk;
        }
        delete[] pivotRow;
        MPI_Barrier(comm);
    }
}

int main(int argc, char* argv[]) {
    SetConsoleOutputCP(CP_UTF8);
    SetConsoleCP(CP_UTF8);
    setlocale(LC_ALL, "");

    MPI_Init(&argc, &argv);
    int rank, size;
    MPI_Comm_rank(MPI_COMM_WORLD, &rank);
    MPI_Comm_size(MPI_COMM_WORLD, &size);

    if (argc < 2) {
        if (rank == 0) logToFile(L"Ожидался аргумент — путь к папке с данными.\n");
        MPI_Finalize();
        return 0;
    }

    string folder = argv[1];
    string fileA = folder + "/A.txt";
    string fileB = folder + "/B.txt";
    string fileX = folder + "/X.txt";

    double** A = nullptr;
    double* B = nullptr;
    int N = 0;

    if (rank == 0) {
        logToFile(L"[ROOT] Чтение матрицы...");
        ReadMatrix(fileA, A, N);
        B = new double[N];
        ReadVector(fileB, B, N);
        logToFile(L"[ROOT] Матрица считана");
    }

    double startTime = MPI_Wtime();

    MPI_Bcast(&N, 1, MPI_INT, 0, MPI_COMM_WORLD);
    if (rank != 0) B = new double[N];
    MPI_Bcast(B, N, MPI_DOUBLE, 0, MPI_COMM_WORLD);

    // Распределение столбцов
    int localCols = (N + size - 1 - rank) / size;
    int* myColumns = new int[localCols];
    for (int j = 0, idx = 0; j < N; j++) if (j % size == rank) myColumns[idx++] = j;

    double** ALocal = new double*[N];
    for (int i = 0; i < N; i++) ALocal[i] = new double[localCols];

    if (rank == 0) {
        for (int i = 0; i < N; i++)
            for (int j = 0; j < localCols; j++)
                ALocal[i][j] = A[i][myColumns[j]];

        for (int p = 1; p < size; p++) {
            int pCols = (N + size - 1 - p) / size;
            int* cols = new int[pCols];
            for (int j = 0, idx = 0; j < N; j++) if (j % size == p) cols[idx++] = j;

            double* columnData = new double[N * pCols];
            for (int i = 0; i < N; i++)
                for (int j = 0; j < pCols; j++)
                    columnData[i * pCols + j] = A[i][cols[j]];

            MPI_Send(&pCols, 1, MPI_INT, p, 0, MPI_COMM_WORLD);
            MPI_Send(cols, pCols, MPI_INT, p, 1, MPI_COMM_WORLD);
            MPI_Send(columnData, N * pCols, MPI_DOUBLE, p, 2, MPI_COMM_WORLD);

            delete[] cols;
            delete[] columnData;
        }

            logToFile(L"[ROOT] Столбцы распределены. Моё кол-во столбцов - " + to_wstring(localCols) + L" столбцов");

    } else {
        int colCount;
        MPI_Recv(&colCount, 1, MPI_INT, 0, 0, MPI_COMM_WORLD, MPI_STATUS_IGNORE);
        MPI_Recv(myColumns, colCount, MPI_INT, 0, 1, MPI_COMM_WORLD, MPI_STATUS_IGNORE);

        double* columnData = new double[N * colCount];
        MPI_Recv(columnData, N * colCount, MPI_DOUBLE, 0, 2, MPI_COMM_WORLD, MPI_STATUS_IGNORE);

        for (int i = 0; i < N; i++)
            for (int j = 0; j < colCount; j++)
                ALocal[i][j] = columnData[i * colCount + j];

        logToFile(L"[WORKER " + to_wstring(rank) + L"] Получено " + to_wstring(colCount) + L" столбцов");

        delete[] columnData;
    }

    MPI_Barrier(MPI_COMM_WORLD);

    LocalGaussianElimination(ALocal, B, myColumns, localCols, N, rank, size, MPI_COMM_WORLD);

    MPI_Barrier(MPI_COMM_WORLD);

    if (rank == 0) {
        double* X = new double[N];
        for (int i = N - 1; i >= 0; i--) {
            double sum = B[i];
            for (int j = i + 1; j < N; j++)
                sum -= ALocal[i % N][j % localCols] * X[j]; 
            X[i] = sum / ALocal[i % N][i % localCols];
        }
        WriteVector(fileX, X, N);
        delete[] X;

        double endTime = MPI_Wtime();
        logToFile(L"==============================================");
        logToFile(L"  Размер матрицы: " + to_wstring(N) + L"x" + to_wstring(N));
        logToFile(L"  Процессов: " + to_wstring(size));
        logToFile(L"  Время: " + to_wstring((long long)((endTime - startTime) * 1000)) + L" мс");
        logToFile(L"==============================================");
    }

    // Очистка памяти
    for (int i = 0; i < N; i++) delete[] ALocal[i];
    delete[] ALocal;
    delete[] myColumns;
    delete[] B;
    if (rank == 0) for (int i = 0; i < N; i++) delete[] A[i];
    delete[] A;

    MPI_Finalize();
    return 0;
}
