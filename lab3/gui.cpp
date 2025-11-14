
#include "globals.h"

void CreateControls(HWND hwnd) {
    int yPos = 10;
    int labelWidth = 150;
    int editWidth = 100;
    int xLabel = 10;
    int xEdit = xLabel + labelWidth;

    CreateWindowW(L"STATIC", L"Start point A:",
        WS_VISIBLE | WS_CHILD, xLabel, yPos, labelWidth, 20, hwnd, nullptr, g_hInst, nullptr);
    g_hEditA = CreateWindowW(L"EDIT", L"0.5",
        WS_VISIBLE | WS_CHILD | WS_BORDER | ES_LEFT,
        xEdit, yPos, editWidth, 20, hwnd, nullptr, g_hInst, nullptr);
    yPos += 30;

    CreateWindowW(L"STATIC", L"End point B:",
        WS_VISIBLE | WS_CHILD, xLabel, yPos, labelWidth, 20, hwnd, nullptr, g_hInst, nullptr);
    g_hEditB = CreateWindowW(L"EDIT", L"3.14159",
        WS_VISIBLE | WS_CHILD | WS_BORDER | ES_LEFT,
        xEdit, yPos, editWidth, 20, hwnd, nullptr, g_hInst, nullptr);
    yPos += 30;

    CreateWindowW(L"STATIC", L"Precision (epsilon):",
        WS_VISIBLE | WS_CHILD, xLabel, yPos, labelWidth, 20, hwnd, nullptr, g_hInst, nullptr);
    g_hEditEps = CreateWindowW(L"EDIT", L"0.00001",
        WS_VISIBLE | WS_CHILD | WS_BORDER | ES_LEFT,
        xEdit, yPos, editWidth, 20, hwnd, nullptr, g_hInst, nullptr);
    yPos += 30;

    CreateWindowW(L"STATIC", L"Number of threads:",
        WS_VISIBLE | WS_CHILD, xLabel, yPos, labelWidth, 20, hwnd, nullptr, g_hInst, nullptr);
    g_hEditThreads = CreateWindowW(L"EDIT", L"4",
        WS_VISIBLE | WS_CHILD | WS_BORDER | ES_LEFT,
        xEdit, yPos, editWidth, 20, hwnd, nullptr, g_hInst, nullptr);
    yPos += 40;

    g_hButtonCalc = CreateWindowW(L"BUTTON", L"Calculate Surface",
        WS_VISIBLE | WS_CHILD | BS_PUSHBUTTON,
        xLabel, yPos, 200, 30, hwnd, (HMENU)IDC_BUTTON_CALC, g_hInst, nullptr);

    g_hButtonBench = CreateWindowW(L"BUTTON", L"Run Benchmarks",
        WS_VISIBLE | WS_CHILD | BS_PUSHBUTTON,
        xLabel + 210, yPos, 200, 30, hwnd, (HMENU)IDC_BUTTON_BENCH, g_hInst, nullptr);
    yPos += 40;

    g_hResultText = CreateWindowW(L"EDIT", L"",
        WS_VISIBLE | WS_CHILD | WS_BORDER | ES_MULTILINE | ES_READONLY | WS_VSCROLL,
        xLabel, yPos, 600, 150, hwnd, nullptr, g_hInst, nullptr);
}

void DrawGraph(HDC hdc, RECT rect) {
    // Фон
    HBRUSH bgBrush = CreateSolidBrush(RGB(255, 255, 255));
    FillRect(hdc, &rect, bgBrush);
    DeleteObject(bgBrush);

    // Рамка
    HPEN framePen = CreatePen(PS_SOLID, 1, RGB(0, 0, 0));
    SelectObject(hdc, framePen);
    Rectangle(hdc, rect.left, rect.top, rect.right, rect.bottom);

    int width = rect.right - rect.left;
    int height = rect.bottom - rect.top;
    int centerX = width / 2;
    int centerY = height / 2;

    // Сетка
    HPEN gridPen = CreatePen(PS_DOT, 1, RGB(220, 220, 220));
    SelectObject(hdc, gridPen);
    for (int i = 50; i < width; i += 50) {
        MoveToEx(hdc, rect.left + i, rect.top, nullptr);
        LineTo(hdc, rect.left + i, rect.bottom);
    }
    for (int i = 50; i < height; i += 50) {
        MoveToEx(hdc, rect.left, rect.top + i, nullptr);
        LineTo(hdc, rect.right, rect.top + i);
    }

    // Оси координат
    HPEN axisPen = CreatePen(PS_SOLID, 2, RGB(0, 0, 255));
    SelectObject(hdc, axisPen);
    MoveToEx(hdc, rect.left, rect.top + centerY, nullptr);
    LineTo(hdc, rect.right, rect.top + centerY);
    MoveToEx(hdc, rect.left + centerX, rect.top, nullptr);
    LineTo(hdc, rect.left + centerX, rect.bottom);

    // График функции
    HPEN graphPen = CreatePen(PS_SOLID, 2, RGB(255, 0, 0));
    SelectObject(hdc, graphPen);

    double xMin = g_currentA;
    double xMax = g_currentB;
    double xRange = xMax - xMin;

    double yMin = 1e10, yMax = -1e10;
    for (int i = 0; i < width; ++i) {
        double x = xMin + (i * xRange) / width;
        double y = Function(x);
        yMin = std::min(yMin, y);
        yMax = std::max(yMax, y);
    }

    double yRange = yMax - yMin;
    if (yRange < 1e-10) yRange = 1.0;

    bool firstPoint = true;
    for (int i = 0; i < width; ++i) {
        double x = xMin + (i * xRange) / width;
        double y = Function(x);
        int screenX = rect.left + i;
        int screenY = rect.top + height - static_cast<int>(((y - yMin) / yRange) * height * 0.9) - height * 0.05;

        if (screenY < rect.top) screenY = rect.top;
        if (screenY > rect.bottom) screenY = rect.bottom;

        if (firstPoint) {
            MoveToEx(hdc, screenX, screenY, nullptr);
            firstPoint = false;
        } else {
            LineTo(hdc, screenX, screenY);
        }
    }

    // Заголовок
    SetTextColor(hdc, RGB(0, 0, 0));
    SetBkMode(hdc, TRANSPARENT);
    HFONT hFont = CreateFontW(16, 0, 0, 0, FW_BOLD, FALSE, FALSE, FALSE,
        DEFAULT_CHARSET, OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS,
        DEFAULT_QUALITY, DEFAULT_PITCH | FF_SWISS, L"Arial");
    SelectObject(hdc, hFont);
    TextOutW(hdc, rect.left + 10, rect.top + 10, L"f(x) = x^3 * e^{sin(x)}", 23);

    DeleteObject(hFont);
    DeleteObject(framePen);
    DeleteObject(gridPen);
    DeleteObject(axisPen);
    DeleteObject(graphPen);
}

void DrawBenchmarkGraphs(HDC hdc, RECT rect) {
    if (g_benchmarkResults.empty()) return;

    // Фон
    HBRUSH bgBrush = CreateSolidBrush(RGB(255, 255, 255));
    FillRect(hdc, &rect, bgBrush);
    DeleteObject(bgBrush);

    // Рамка
    Rectangle(hdc, rect.left, rect.top, rect.right, rect.bottom);

    int halfWidth = (rect.right - rect.left) / 2;
    RECT leftRect = {rect.left + 10, rect.top + 10, rect.left + halfWidth - 10, rect.bottom - 10};
    RECT rightRect = {rect.left + halfWidth + 10, rect.top + 10, rect.right - 10, rect.bottom - 10};

    // График 1: Время vs Точность (1 и 3 потока)
    std::vector<BenchmarkResult> epsResults;
    for (const auto& r : g_benchmarkResults) {
        if ((r.threadCount == 1 || r.threadCount == 3) && r.epsilon >= 0.00001) {
            epsResults.push_back(r);
        }
    }

    if (!epsResults.empty()) {
        std::vector<double> epsValues;
        for (const auto& r : epsResults) epsValues.push_back(r.epsilon);
        std::sort(epsValues.begin(), epsValues.end(), std::greater<double>());
        epsValues.erase(std::unique(epsValues.begin(), epsValues.end()), epsValues.end());

        std::vector<double> times1(epsValues.size(), 0.0);
        std::vector<double> times3(epsValues.size(), 0.0);
        for (size_t i = 0; i < epsValues.size(); ++i) {
            double eps = epsValues[i];
            for (const auto& r : epsResults) {
                if (std::abs(r.epsilon - eps) < 1e-15) {
                    if (r.threadCount == 1) times1[i] = r.time;
                    if (r.threadCount == 3) times3[i] = r.time;
                }
            }
        }

        SetTextColor(hdc, RGB(0, 0, 0));
        SetBkMode(hdc, TRANSPARENT);
        TextOutW(hdc, leftRect.left, leftRect.top, L"Time vs Precision", 17);

        int plotLeft = leftRect.left + 60;
        int plotTop = leftRect.top + 30;
        int plotRight = leftRect.right - 30;
        int plotBottom = leftRect.bottom - 40;

        HBRUSH bg = CreateSolidBrush(RGB(250,250,250));
        RECT plotArea = {plotLeft, plotTop, plotRight, plotBottom};
        FillRect(hdc, &plotArea, bg);
        DeleteObject(bg);
        Rectangle(hdc, plotLeft, plotTop, plotRight, plotBottom);

        int plotW = plotRight - plotLeft;
        int plotH = plotBottom - plotTop;

        double maxTime = 0.0;
        for (double t : times1) maxTime = std::max(maxTime, t);
        for (double t : times3) maxTime = std::max(maxTime, t);
        if (maxTime <= 0.0) maxTime = 1.0;

        int n = static_cast<int>(epsValues.size());
        for (int i = 0; i < n; ++i) {
            int x = plotLeft + (i * plotW) / std::max(1, n - 1);
            MoveToEx(hdc, x, plotBottom, nullptr);
            LineTo(hdc, x, plotBottom + 5);
            wchar_t buf[64];
            swprintf_s(buf, L"%.0e", epsValues[i]);
            TextOutW(hdc, x - 20, plotBottom + 8, buf, static_cast<int>(wcslen(buf)));
        }

        int stepsY = 5;
        for (int i = 0; i <= stepsY; ++i) {
            int y = plotBottom - (i * plotH) / stepsY;
            MoveToEx(hdc, plotLeft - 5, y, nullptr);
            LineTo(hdc, plotLeft, y);
            double val = (i * maxTime) / stepsY;
            wchar_t buf[64];
            swprintf_s(buf, L"%.3f", val);
            TextOutW(hdc, plotLeft - 55, y - 8, buf, static_cast<int>(wcslen(buf)));
        }

        // Подписи осей
        TextOutW(hdc, (plotLeft + plotRight)/2 - 30, plotBottom + 30, L"Precision (ε)", 14);
        TextOutW(hdc, plotLeft - 50, plotTop - 20, L"Time (s)", 8);

        HPEN pen1 = CreatePen(PS_SOLID, 2, RGB(0, 0, 200));
        HPEN pen3 = CreatePen(PS_SOLID, 2, RGB(200, 0, 0));

        SelectObject(hdc, pen1);
        for (int i = 0; i < n; ++i) {
            int x = plotLeft + (i * plotW) / std::max(1, n - 1);
            int y = plotBottom - static_cast<int>((times1[i] / maxTime) * plotH);
            if (i == 0) MoveToEx(hdc, x, y, nullptr);
            else LineTo(hdc, x, y);
            Ellipse(hdc, x - 3, y - 3, x + 3, y + 3);
        }

        SelectObject(hdc, pen3);
        for (int i = 0; i < n; ++i) {
            int x = plotLeft + (i * plotW) / std::max(1, n - 1);
            int y = plotBottom - static_cast<int>((times3[i] / maxTime) * plotH);
            if (i == 0) MoveToEx(hdc, x, y, nullptr);
            else LineTo(hdc, x, y);
            Ellipse(hdc, x - 3, y - 3, x + 3, y + 3);
        }

        RECT legend = {plotRight - 130, plotTop + 10, plotRight - 10, plotTop + 60};
        HBRUSH lbg = CreateSolidBrush(RGB(245,245,245));
        FillRect(hdc, &legend, lbg);
        DeleteObject(lbg);
        Rectangle(hdc, legend.left, legend.top, legend.right, legend.bottom);

        HPEN legendPen1 = CreatePen(PS_SOLID, 3, RGB(0,0,200));
        HPEN legendPen3 = CreatePen(PS_SOLID, 3, RGB(200,0,0));
        SelectObject(hdc, legendPen1);
        MoveToEx(hdc, legend.left + 10, legend.top + 15, nullptr);
        LineTo(hdc, legend.left + 40, legend.top + 15);
        SelectObject(hdc, legendPen3);
        MoveToEx(hdc, legend.left + 10, legend.top + 35, nullptr);
        LineTo(hdc, legend.left + 40, legend.top + 35);
        SetTextColor(hdc, RGB(0,0,0));
        TextOutW(hdc, legend.left + 45, legend.top + 7, L"1 thread", 8);
        TextOutW(hdc, legend.left + 45, legend.top + 27, L"3 threads", 9);

        DeleteObject(pen1);
        DeleteObject(pen3);
        DeleteObject(legendPen1);
        DeleteObject(legendPen3);
    }

    // График 2: Время vs Количество потоков
    std::vector<BenchmarkResult> threadResults;
    int skip = 2;

    for (const auto& r : g_benchmarkResults) {
        if (std::abs(r.epsilon - 0.00001) < 1e-12 &&
            r.threadCount >= 1 && r.threadCount <= 10)
        {
            if (skip > 0) {
                skip--;
                continue;
            }
            threadResults.push_back(r);
        }
    }

    if (!threadResults.empty()) {
        TextOutW(hdc, rightRect.left, rightRect.top, L"Time vs Threads", 15);

        double maxTime = 0.0;
        for (const auto& r : threadResults) {
            maxTime = std::max(maxTime, r.time);
        }

        int plotLeft = rightRect.left + 60;
        int plotTop = rightRect.top + 30;
        int plotRight = rightRect.right - 30;
        int plotBottom = rightRect.bottom - 40;

        HBRUSH bg = CreateSolidBrush(RGB(250,250,250));
        RECT plotArea = {plotLeft, plotTop, plotRight, plotBottom};
        FillRect(hdc, &plotArea, bg);
        DeleteObject(bg);
        Rectangle(hdc, plotLeft, plotTop, plotRight, plotBottom);

        int plotW = plotRight - plotLeft;
        int plotH = plotBottom - plotTop;

        if (maxTime > 0) {
            HPEN threadPen = CreatePen(PS_SOLID, 2, RGB(0, 150, 0));
            SelectObject(hdc, threadPen);

            size_t n = threadResults.size();
            for (size_t i = 0; i < n; ++i) {
                int x = plotLeft + (i * plotW) / std::max<size_t>(1, n - 1);
                int y = plotBottom - static_cast<int>((threadResults[i].time / maxTime) * plotH);
                if (i == 0) MoveToEx(hdc, x, y, nullptr);
                else LineTo(hdc, x, y);
                Ellipse(hdc, x - 3, y - 3, x + 3, y + 3);
            }

            // Оси и подписи
            for (size_t i = 0; i < n; ++i) {
                int x = plotLeft + (i * plotW) / std::max<size_t>(1, n - 1);
                MoveToEx(hdc, x, plotBottom, nullptr);
                LineTo(hdc, x, plotBottom + 5);
                wchar_t buf[16];
                swprintf_s(buf, L"%d", threadResults[i].threadCount);
                TextOutW(hdc, x - 10, plotBottom + 8, buf, static_cast<int>(wcslen(buf)));
            }

            int stepsY = 5;
            for (int i = 0; i <= stepsY; ++i) {
                int y = plotBottom - (i * plotH) / stepsY;
                MoveToEx(hdc, plotLeft - 5, y, nullptr);
                LineTo(hdc, plotLeft, y);
                double val = (i * maxTime) / stepsY;
                wchar_t buf[32];
                swprintf_s(buf, L"%.3f", val);
                TextOutW(hdc, plotLeft - 55, y - 8, buf, static_cast<int>(wcslen(buf)));
            }

            // Подписи осей
            TextOutW(hdc, (plotLeft + plotRight)/2 - 40, plotBottom + 30, L"Threads count", 13);
            TextOutW(hdc, plotLeft - 50, plotTop - 20, L"Time (s)", 8);

            // Легенда
            RECT legend = {plotRight - 120, plotTop + 10, plotRight - 10, plotTop + 40};
            HBRUSH lbg = CreateSolidBrush(RGB(245,245,245));
            FillRect(hdc, &legend, lbg);
            DeleteObject(lbg);
            Rectangle(hdc, legend.left, legend.top, legend.right, legend.bottom);
            MoveToEx(hdc, legend.left + 10, legend.top + 15, nullptr);
            LineTo(hdc, legend.left + 40, legend.top + 15);
            TextOutW(hdc, legend.left + 45, legend.top + 7, L"ε = 1e-5", 9);

            DeleteObject(threadPen);
        }
    } 
}
