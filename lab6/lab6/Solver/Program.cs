using Common;
using System.Diagnostics;
using System.IO.Pipes;
using System.Threading.Tasks;

const string TasksDir = "D:\\ProgrammingAndProjects\\Studies\\7sem\\RIS\\lab6\\Tasks";
const string ResultsDir = "D:\\ProgrammingAndProjects\\Studies\\7sem\\RIS\\lab6\\Results"; 
const string PipeName = "LinearSystemsPipe";

Directory.CreateDirectory(ResultsDir);

var results = new List<(int Id, int N, double SingleMs, double MultiMs)>();

Console.WriteLine("Решатель запущен. Подключаемся к Генератору через Named Pipe...");

using var pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.In, PipeOptions.Asynchronous);
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

try
{
    await pipe.ConnectAsync(cts.Token);
    Console.WriteLine("Подключено! Ожидаем задачи от Генератора...\n");
}
catch (Exception ex)
{
    Console.WriteLine($"Ошибка подключения: {ex.Message}");
    return;
}

using var reader = new StreamReader(pipe);

string? line;
while ((line = await reader.ReadLineAsync()) != null)
{
    if (!line.StartsWith("DONE|")) continue;

    var parts = line.Split('|');
    int id = int.Parse(parts[1]);
    int n = int.Parse(parts[2]);

    string aPath = Path.Combine(TasksDir, $"a{id}.csv");
    string bPath = Path.Combine(TasksDir, $"b{id}.csv");

    var system = await MatrixFileIO.ReadAsync(aPath, bPath);

    var sw = Stopwatch.StartNew();
    double[] xSingle = CholeskySolver.Solve(system);
    double singleMs = sw.Elapsed.TotalMilliseconds;

    sw.Restart();
    double[] xMulti = ParallelCholeskySolver.Solve(system);
    double multiMs = sw.Elapsed.TotalMilliseconds;

    await MatrixFileIO.WriteSolutionAsync(Path.Combine(ResultsDir, $"x{id}.csv"), xMulti, n);

    double speedup = singleMs / multiMs;
    results.Add((id, n, singleMs, multiMs));

    Console.WriteLine($"Задача {id,2} | N={n,4} | Однопот: {singleMs,8:F2} мс | Многопот: {multiMs,8:F2} мс | Ускорение: {speedup,5:F2}x");
}

Console.WriteLine("\n" + new string('=', 90));
Console.WriteLine("ЗАДАЧА   N    ОДНОПОТОЧНЫЙ    МНОГОПОТОЧНЫЙ    УСКОРЕНИЕ");
Console.WriteLine(new string('=', 90));
foreach (var r in results.OrderBy(x => x.Id))
{
    Console.WriteLine($"{r.Id,4}   {r.N,5}   {r.SingleMs,10:F2} мс    {r.MultiMs,10:F2} мс     {r.SingleMs / r.MultiMs,8:F2}x");
}

/////////////////////////////////////////////////////////////

//var tasks = new List<(int Id, int N, LinearSystem System)>();
//var singleResults = new List<(int Id, int N, double Ms)>();
//var multiResults = new List<(int Id, int N, double Ms)>();

//string? line;
//while ((line = await reader.ReadLineAsync()) != null)
//{
//    if (!line.StartsWith("DONE|")) continue;
//    var parts = line.Split('|');
//    int id = int.Parse(parts[1]);
//    int n = int.Parse(parts[2]);

//    string aPath = Path.Combine(TasksDir, $"a{id}.csv");
//    string bPath = Path.Combine(TasksDir, $"b{id}.csv");
//    while (!File.Exists(aPath) || !File.Exists(bPath)) await Task.Delay(50);

//    var system = await MatrixFileIO.ReadAsync(aPath, bPath);
//    tasks.Add((id, n, system));

//    Console.WriteLine($"Задача {id} получена | N = {n}");
//}

//if (tasks.Count == 0)
//{
//    Console.WriteLine("Нет задач. Выход.");
//    return;
//}

//Console.WriteLine($"\nПолучено {tasks.Count} задач. Запущено решение...\n");

//var swTotal = Stopwatch.StartNew();
//foreach (var t in tasks)
//{
//    var sw = Stopwatch.StartNew();
//    double[] x = CholeskySolver.Solve(t.System);
//    double ms = sw.Elapsed.TotalMilliseconds;

//    await MatrixFileIO.WriteSolutionAsync(Path.Combine(ResultsDir, $"x_single_{t.Id}.csv"), x, t.N);
//    singleResults.Add((t.Id, t.N, ms));

//    Console.WriteLine($"[Однопоточно] Задача {t.Id,2} | N={t.N,4} | {ms,8:F2} мс");
//}
//double totalSingle = swTotal.Elapsed.TotalMilliseconds;
//Console.WriteLine($"\nОднопоточно все задачи: {totalSingle:F2} мс");

//swTotal.Restart();

//var doneEvents = new ManualResetEventSlim[tasks.Count];
//for (int i = 0; i < doneEvents.Length; i++) doneEvents[i] = new ManualResetEventSlim(false);

//for (int i = 0; i < tasks.Count; i++)
//{
//    int index = i;
//    var task = tasks[i];

//    ThreadPool.QueueUserWorkItem(async _ =>
//    {
//        var sw = Stopwatch.StartNew();
//        double[] x = CholeskySolver.Solve(task.System); 
//        double ms = sw.Elapsed.TotalMilliseconds;

//        var resultPath = Path.Combine(ResultsDir, $"x_multi_{task.Id}.csv");

//        await MatrixFileIO.WriteSolutionAsync(resultPath, x, task.N);

//        lock (multiResults)
//            multiResults.Add((task.Id, task.N, ms));

//        Console.WriteLine($"[Многопоточно] Задача {task.Id,2} | N={task.N,4} | {ms,8:F2} мс");

//        doneEvents[index].Set();
//    });
//}

//for (int i = 0; i < doneEvents.Length; i++)
//{
//    doneEvents[i].Wait();
//    doneEvents[i].Dispose();
//}

//double totalMulti = swTotal.Elapsed.TotalMilliseconds;
//Console.WriteLine($"\nМногопоточно все задачи: {totalMulti:F2} мс");
//Console.WriteLine($"ОБЩЕЕ УСКОРЕНИЕ: {totalSingle / totalMulti:F2}x");