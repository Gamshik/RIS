using System.Diagnostics;
using System.Threading;

namespace lab4
{
    public class MultiThreadRunner
    {
        private List<TaskInfo> _tasks;
        private Queue<TaskInfo> _taskQueue;
        private Semaphore _semaphore;
        private Semaphore _queueSemaphore;
        private Semaphore _completedTasksSemaphore;
        private int _threadCount;
        private int _completedTasks = 0;

        public MultiThreadRunner()
        {
            _tasks = new List<TaskInfo>();
            _taskQueue = new Queue<TaskInfo>();
            _threadCount = Environment.ProcessorCount;
        }

        public void LoadTasks()
        {
            _tasks.Clear();
            _taskQueue.Clear();

            var lines = File.ReadAllLines("tasks.txt");

            for (int i = 0; i < lines.Length; i++)
            {
                var parts = lines[i].Split(',');
                if (parts.Length == 2)
                {
                    string matrixFile = parts[0].Trim();
                    string vectorFile = parts[1].Trim();
                    string resultFile = $"x{i + 1}_multi.csv";

                    var task = new TaskInfo(i + 1, matrixFile, vectorFile, resultFile);
                    _tasks.Add(task);
                    _taskQueue.Enqueue(task);
                }
            }
        }

        private void WorkerThread(int threadId)
        {
            while (true)
            {
                TaskInfo task = null;

                if (_taskQueue.Count == 0)
                    break;

                try
                {
                    _queueSemaphore.WaitOne();
                    task = _taskQueue.Dequeue();
                }
                catch
                {
                    _queueSemaphore.Release();
                    throw;
                }

                if (task == null)
                    break;

                _semaphore.WaitOne();

                try
                {
                    Console.WriteLine($"[Поток #{threadId}] Взял задачу {task.MatrixAFile}");

                    Stopwatch taskTimer = Stopwatch.StartNew();

                    double[,] A = MatrixHelper.ReadMatrixFromCsv(task.MatrixAFile);
                    double[] b = MatrixHelper.ReadVectorFromCsv(task.VectorBFile);

                    double[] x = LLTSolver.Solve(A, b);

                    MatrixHelper.WriteVectorToCsv(task.ResultFile, x);

                    _completedTasksSemaphore.WaitOne();
                    _completedTasks++;
                    _completedTasksSemaphore.Release();

                    taskTimer.Stop();

                    Console.WriteLine($"[Поток #{threadId}] Завершил {task.MatrixAFile} ({taskTimer.ElapsedMilliseconds} мс)");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Поток #{threadId}] Ошибка в {task.MatrixAFile}: {ex.Message}");
                }
                finally
                {
                    _semaphore.Release();
                }
            }
        }

        public long Run()
        {
            Console.WriteLine();
            Console.WriteLine($"--------------- МНОГОПОТОЧНАЯ ВЕРСИЯ ({_threadCount} потоков) ---------------");
            Console.WriteLine();

            _completedTasks = 0;

            _queueSemaphore = new Semaphore(1, 1);

            _completedTasksSemaphore = new Semaphore(1, 1);

            _semaphore = new Semaphore(_threadCount, _threadCount);

            Stopwatch stopwatch = Stopwatch.StartNew();

            List<Thread> threads = new List<Thread>();

            for (int i = 0; i < _threadCount; i++)
            {
                int threadId = i + 1;
                Thread thread = new Thread(() => WorkerThread(threadId));
                thread.Start();
                threads.Add(thread);
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }

            stopwatch.Stop();

            Console.WriteLine();
            Console.WriteLine($"Выполнено задач: {_completedTasks}");
            Console.WriteLine($"Результаты сохранены в: x1_multi.csv ... x{_tasks.Count}_multi.csv");
            Console.WriteLine($"Общее время: {stopwatch.ElapsedMilliseconds} мс");
            Console.WriteLine();

            _semaphore.Dispose();

            return stopwatch.ElapsedMilliseconds;
        }

        public int GetTaskCount() => _taskQueue.Count;
        public int GetThreadCount() => _threadCount;
    }
}
