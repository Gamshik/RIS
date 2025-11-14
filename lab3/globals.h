#pragma once
#define NOMINMAX

#include <windows.h>
#include <commctrl.h>
#include <cmath>
#include <vector>
#include <string>
#include <chrono>
#include <sstream>
#include <iomanip>
#include <algorithm>
#include <random>


// --- Константы ---
constexpr double PI = 3.14159265358979323846;
constexpr int MAX_THREADS = 10;
constexpr int GRAPH_TOP = 350;
constexpr int GRAPH_LEFT = 10;
constexpr int GRAPH_WIDTH = 600;
constexpr int GRAPH_HEIGHT = 400;

constexpr int BENCH_GRAPH_TOP = 350;
constexpr int BENCH_GRAPH_LEFT = GRAPH_LEFT + GRAPH_WIDTH + 30;
constexpr int BENCH_GRAPH_WIDTH = 600;
constexpr int BENCH_GRAPH_HEIGHT = 400;


// --- Идентификаторы элементов управления ---
#define IDC_BUTTON_CALC 1
#define IDC_BUTTON_BENCH 2


// --- Структуры данных ---
struct ThreadData {
double start;
double end;
int segments;
double result;
int threadId;
};


struct BenchmarkResult {
int threadCount;
double epsilon;
double time;
double surface;
};


// --- Глобальные переменные ---
extern HWND g_hMainWnd;
extern HWND g_hEditA;
extern HWND g_hEditB;
extern HWND g_hEditEps;
extern HWND g_hEditThreads;
extern HWND g_hButtonCalc;
extern HWND g_hButtonBench;
extern HWND g_hResultText;
extern HINSTANCE g_hInst;


extern std::vector<BenchmarkResult> g_benchmarkResults;
extern double g_currentA;
extern double g_currentB;
extern HANDLE g_mutex;
extern double g_globalResult;


// --- Прототипы функций ---

LRESULT CALLBACK WndProc(HWND, UINT, WPARAM, LPARAM);

void CreateControls(HWND hwnd);
void DrawGraph(HDC hdc, RECT rect);
void DrawBenchmarkGraphs(HDC hdc, RECT rect);

double Function(double x);
double FunctionDerivative(double x); 
double CalculateSurface(double a, double b, int samples, int threadCount);
double CalculateSurfaceSingleThread(double a, double b, int samples);
int GetSamplesFromEpsilon(double epsilon);
DWORD WINAPI ThreadMonteCarloSurface(LPVOID param);

void RunBenchmarks();

