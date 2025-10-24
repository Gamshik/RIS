using PlanProc;

namespace SchedulerSimulator
{
    public class Program
    {
        public static void Main(string[] args)
        {
            MathUtils.GenerateExampleScenario();

            var processesInfo = JobFileParser.Parse("main.txt");
            if (processesInfo == null || !processesInfo.Any())
            {
                Console.WriteLine("Ошибка: не удалось загрузить или распарсить задания. Программа завершена.");
                return;
            }

            var fcfsProcesses = processesInfo.Select(info => new Process
            {
                Name = info.Name,
                MatrixFile = info.MatrixFile,
                VectorFile = info.VectorFile,
                ArrivalTime = info.ArrivalTime,
                BurstTime = MathUtils.GetMatrixSize(info.MatrixFile)
            }).ToList();

            var fcfsScheduler = new ProcessScheduler(fcfsProcesses.ToList(), "FCFS"); 
            fcfsScheduler.Run();

            var gsProcesses = processesInfo.Select(info => new Process
            {
                Name = info.Name,
                MatrixFile = info.MatrixFile,
                VectorFile = info.VectorFile,
                ArrivalTime = info.ArrivalTime,
                BurstTime = MathUtils.GetMatrixSize(info.MatrixFile)
            }).ToList();

            var gsScheduler = new ProcessScheduler(gsProcesses.ToList(), "GS");
            gsScheduler.Run();

            PrintComparison(fcfsScheduler, gsScheduler);
        }

        private static void PrintComparison(ProcessScheduler fcfsScheduler, ProcessScheduler gsScheduler)
        {
            Console.WriteLine("\n--- Сравнение и выводы ---");

            Console.WriteLine("Алгоритм FCFS:");
            PrintStats(fcfsScheduler);

            Console.WriteLine("\nАлгоритм GS:");
            PrintStats(gsScheduler);
        }

        private static void PrintStats(ProcessScheduler scheduler)
        {
            var stats = scheduler.Statistics;
            Console.WriteLine($"  Общее время выполнения: {stats["total_time"]}");
            Console.WriteLine($"  Среднее время ожидания: {stats["avg_waiting_time"]:F2}");
            Console.WriteLine($"  Среднее время оборота: {stats["avg_turnaround_time"]:F2}");
        }
    }
}