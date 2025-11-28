using Consumer;
using Shared;
using System.Diagnostics;

class Program
{
   static void Main(string[] args)
    {
        bool isMultiThreaded = args.Length > 0 && args[0] == "multi";
        Console.WriteLine($"[Потребитель] Мод: {(isMultiThreaded ? "многопоточнй" : "однопоточный")}. Ожидание данных...");

        using var ipc = new SharedMemoryClient(false);
        var receivedData = new List<ImageRecord>();

        while (true)
        {
            var msg = ipc.Consume();
            if (msg.IsEndOfStream) break;

            receivedData.Add(new ImageRecord
            {
                Name = msg.ImageName,
                Histogram = (float[])msg.Histogram.Clone()
            });
        }
        Console.WriteLine($"\n[Потребитель] Получено {receivedData.Count} изображений. Начато сравнение...");

        var sw = Stopwatch.StartNew();

        var results = FindTop3Similar(receivedData, isMultiThreaded);

        sw.Stop();

        Console.WriteLine("\n=== ТОП-3 НАИБОЛЕЕ СХОЖИХ ИЗОБРАЖЕНИЙ ===");
        foreach (var res in results)
        {
            Console.WriteLine($"{res.Item1} ↔ {res.Item2} : сходство = {1.0 - res.Item3:F5} (расстояние {res.Item3:F5})");
        }
        Console.WriteLine($"[Потребитель] Время: {sw.ElapsedMilliseconds} мс.");
        Console.ReadLine(); 
    }

    static List<(string, string, double)> FindTop3Similar(List<ImageRecord> images, bool multiThreaded)
    {
        var pairs = new List<(ImageRecord A, ImageRecord B)>();
        for (int i = 0; i < images.Count; i++)
        {
            for (int j = i + 1; j < images.Count; j++)
            {
                pairs.Add((images[i], images[j]));
            }
        }

        Func<(ImageRecord, ImageRecord), (string, string, double)> calcDistance = (pair) =>
        {
            double distance = HistogramIntersection(pair.Item1.Histogram, pair.Item2.Histogram);
            return (pair.Item1.Name, pair.Item2.Name, distance);
        };

        List<(string, string, double)> allDistances;

        if (multiThreaded)
        {
            allDistances = pairs.AsParallel()
                                .Select(calcDistance)
                                .OrderBy(x => x.Item3)
                                .Take(3)
                                .ToList();
        }
        else
        {
            allDistances = pairs.Select(calcDistance)
                                .OrderBy(x => x.Item3)
                                .Take(3)
                                .ToList();
        }

        return allDistances;
    }

    static double HistogramIntersection(float[] h1, float[] h2)
    {
        double sum = 0.0;
        for (int i = 0; i < h1.Length; i++)
            sum += Math.Min(h1[i], h2[i]);

        return 1.0 - sum;
    }
}