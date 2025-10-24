namespace PlanProc
{
    public class ProcessScheduler
    {
        private readonly List<Process> _incomingProcesses;
        private readonly string _algorithm;
        private readonly List<Process> _readyProcesses;
        private readonly List<Process> _terminatedProcesses;
        private readonly Dictionary<string, double> _statistics;

        private int _currentTime;

        public ProcessScheduler(List<Process> processes, string algorithm)
        {
            _incomingProcesses = processes.OrderBy(p => p.ArrivalTime).ToList();
            _algorithm = algorithm.ToUpperInvariant();
            _readyProcesses = new List<Process>();
            _terminatedProcesses = new List<Process>();
            _statistics = new Dictionary<string, double>();
            _currentTime = 0;
        }

        public Dictionary<string, double> Statistics => _statistics;

        public void Run()
        {
            Console.WriteLine($"\n--- Запуск симуляции с алгоритмом '{_algorithm}' ---");

            while (_readyProcesses.Any() || _incomingProcesses.Any() || IsSystemBusy())
            {
                EnqueueArrivedProcesses();

                if (!SelectAndExecuteNextProcess())
                {
                    // Нет готовых процессов — простаиваем (idle time)
                    _currentTime++;
                    Thread.Sleep(1);
                }
            }

            CalculateStatistics();
            WriteResults();
        }

        private bool IsSystemBusy()
        {
            return false;
        }

        private void EnqueueArrivedProcesses()
        {
            while (_incomingProcesses.Any() && _incomingProcesses[0].ArrivalTime <= _currentTime)
            {
                var process = _incomingProcesses[0];
                _readyProcesses.Add(process);
                _incomingProcesses.RemoveAt(0);
                Console.WriteLine($"Время {_currentTime}: Процесс {process.Name} прибыл в очередь.");
            }
        }

        private bool SelectAndExecuteNextProcess()
        {
            if (!_readyProcesses.Any()) return false;

            Process selectedProcess = SelectProcess();
            _readyProcesses.Remove(selectedProcess);

            if (selectedProcess.StartTime == 0)
            {
                selectedProcess.StartTime = _currentTime;
            }

            Console.WriteLine($"Время {_currentTime}: Процесс {selectedProcess.Name} начал выполнение ({_algorithm}).");

            bool isCompleted = ExecuteProcess(selectedProcess);
            int burstTime = selectedProcess.TotalWorkUnits;
            _currentTime += burstTime;
            selectedProcess.TerminationTime = _currentTime;
            selectedProcess.UsedCpuTime += burstTime;

            _terminatedProcesses.Add(selectedProcess);

            Console.WriteLine($"Время {_currentTime}: Процесс {selectedProcess.Name} завершил выполнение (burst: {burstTime}).");
            return true;
        }

        private Process SelectProcess()
        {
            if (_algorithm == "FCFS")
            {
                return _readyProcesses.OrderBy(p => p.ArrivalTime).First();
            }
            else if (_algorithm == "GS")
            {
                return _readyProcesses
                    .OrderBy(CalculateRatio) 
                    .ThenBy(p => p.BurstTime) 
                    .First();
            }
            else
            {
                throw new NotSupportedException($"Алгоритм '{_algorithm}' не поддерживается.");
            }
        }

        private double CalculateRatio(Process p)
        {
            double elapsed = Math.Max(_currentTime - p.ArrivalTime, 0);
            if (elapsed <= 0) return 0;
            double share = _readyProcesses.Count > 0 ? 1.0 / _readyProcesses.Count : 0;
            double entitled = elapsed * share;
            double ratio = p.UsedCpuTime / (entitled + 1e-9);
            return ratio;
        }

        private bool ExecuteProcess(Process process)
        {
            try
            {
                var matrixData = MathUtils.ReadMatrixFromFile(process.MatrixFile);
                var vectorData = MathUtils.ReadMatrixFromFile(process.VectorFile);
                if (matrixData == null || vectorData == null)
                {
                    process.ErrorMessage = $"Ошибка загрузки данных для процесса {process.Name}.";
                    return false;
                }

                int matrixSize = matrixData.GetLength(0);
                process.TotalWorkUnits = matrixSize;

                double[] solution = MathUtils.SolveCholesky(matrixData, vectorData);
                if (solution == null)
                {
                    process.ErrorMessage = "Ошибка: Матрица не симметричная, не положительно определенная или вырожденная.";
                    return false;
                }

                process.Solution = solution;
                return true;
            }
            catch (Exception ex)
            {
                process.ErrorMessage = $"Произошла непредвиденная ошибка: {ex.Message}";
                return false;
            }
        }

        private void CalculateStatistics()
        {
            double totalWaitingTime = 0;
            double totalTurnaroundTime = 0;
            int totalProcesses = _terminatedProcesses.Count;

            foreach (var p in _terminatedProcesses)
            {
                double turnaround = p.TerminationTime - p.ArrivalTime;
                double waiting = turnaround - p.TotalWorkUnits;

                totalWaitingTime += waiting;
                totalTurnaroundTime += turnaround;
            }

            _statistics["total_time"] = _currentTime;
            _statistics["avg_waiting_time"] = totalProcesses > 0 ? totalWaitingTime / totalProcesses : 0;
            _statistics["avg_turnaround_time"] = totalProcesses > 0 ? totalTurnaroundTime / totalProcesses : 0;
            _statistics["throughput"] = totalProcesses > 0 ? totalProcesses / (double)_currentTime : 0;
        }

        private void WriteResults()
        {
            foreach (var p in _terminatedProcesses)
            {
                string outputFilename = $"{p.Name}_{_algorithm}.txt";
                using var writer = new StreamWriter(outputFilename);
                if (!string.IsNullOrEmpty(p.ErrorMessage))
                {
                    writer.WriteLine($"Ошибка при выполнении процесса: {p.ErrorMessage}");
                }
                else
                {
                    writer.WriteLine($"Решение для СЛАУ процесса {p.Name}:");
                    foreach (var value in p.Solution)
                    {
                        writer.WriteLine(value.ToString("F6"));
                    }

                    bool isCorrect = MathUtils.VerifySolution(
                        MathUtils.ReadMatrixFromFile(p.MatrixFile),
                        p.Solution,
                        MathUtils.ReadMatrixFromFile(p.VectorFile));

                    writer.WriteLine("\n--- Верификация решения ---");
                    writer.WriteLine($"Решение является {(isCorrect ? "корректным" : "НЕКОРРЕКТНЫМ")}.");
                }

                Console.WriteLine($"Результаты для {p.Name} сохранены в '{outputFilename}'.");
            }

            string statsFilename = $"статистика_{_algorithm}.txt";
            using var statsWriter = new StreamWriter(statsFilename);
            statsWriter.WriteLine($"Статистика для алгоритма планирования: {_algorithm}");
            statsWriter.WriteLine("==================================================");
            statsWriter.WriteLine($"Общее время выполнения: {_statistics["total_time"]} единиц");
            statsWriter.WriteLine($"Среднее время ожидания: {_statistics["avg_waiting_time"]:F2}");
            statsWriter.WriteLine($"Среднее время оборота: {_statistics["avg_   _time"]:F2}");
            statsWriter.WriteLine($"Пропускная способность: {_statistics["throughput"]:F4} процессов/единица");

            statsWriter.WriteLine("\nДетали по процессам:");
            foreach (var p in _terminatedProcesses)
            {
                statsWriter.WriteLine($"  Процесс {p.Name}:");
                statsWriter.WriteLine($"    Время прибытия: {p.ArrivalTime}");
                statsWriter.WriteLine($"    Время старта: {p.StartTime}");
                statsWriter.WriteLine($"    Время завершения: {p.TerminationTime}");
                statsWriter.WriteLine($"    Burst time: {p.TotalWorkUnits}");
                statsWriter.WriteLine($"    Оборот: {p.TerminationTime - p.ArrivalTime}");
                statsWriter.WriteLine($"    Ожидание: {(p.TerminationTime - p.ArrivalTime) - p.TotalWorkUnits}");
                if (!string.IsNullOrEmpty(p.ErrorMessage)) statsWriter.WriteLine($"    Ошибка: {p.ErrorMessage}");
            }

            Console.WriteLine($"Статистика сохранена в '{statsFilename}'.");
        }
    }
}