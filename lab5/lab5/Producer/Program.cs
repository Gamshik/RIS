using Shared;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Diagnostics;

class Program
{
    static void Main(string[] args)
    {
        string folderPath = args.Length > 0 ? args[0] : "./Images";
        bool isMultiThreaded = args.Length > 1 && args[1] == "multi";

        var images = Directory.GetFiles(folderPath, "*.jpg");
        if (images.Length < 10)
        {
            Console.WriteLine("Нужно минимум 10 изображений в папке.");
            return;
        }

        Console.WriteLine($"[Поставщик] Мод: {(isMultiThreaded ? "многопоточнй" : "однопоточный")}. Найдено {images.Length} изображений.");

        using var ipc = new SharedMemoryClient(true);
        var sw = Stopwatch.StartNew();

        if (isMultiThreaded)
        {
            Parallel.ForEach(images, (imgCtx) =>
            {
                ProcessAndSend(imgCtx, ipc);
            });
        }
        else
        {
            foreach (var imgPath in images)
            {
                ProcessAndSend(imgPath, ipc);
            }
        }

        ipc.Produce(new HistogramMessage { IsEndOfStream = true });

        sw.Stop();
        Console.WriteLine($"[Поставщик] Время: {sw.ElapsedMilliseconds} мс.");
    }

    static void ProcessAndSend(string path, SharedMemoryClient ipc)
    {
        try
        {
            float[] hist = CalculateHistogram(path);

            var msg = new HistogramMessage
            {
                IsEndOfStream = false,
                ImageName = Path.GetFileName(path),
                Histogram = hist
            };

            ipc.Produce(msg);

            Console.WriteLine($"[Поставщик] Отпрввил: {Path.GetFileName(path)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Ошибка] {path}: {ex.Message}");
        }
    }

    static float[] CalculateHistogram(string path)
    {
        const int BinsPerChannel = 16;
        const int TotalBins = BinsPerChannel * BinsPerChannel * BinsPerChannel; 

        using var image = Image.Load<Rgb24>(path);
        float[] histogram = new float[TotalBins];

        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    ref Rgb24 p = ref row[x];

                    int r = p.R >> 4;
                    int g = p.G >> 4;
                    int b = p.B >> 4;

                    int index = r + (g << 4) + (b << 8); 
                    histogram[index]++;
                }
            }
        });

        float pixelCount = image.Width * image.Height;
        for (int i = 0; i < TotalBins; i++)
            histogram[i] /= pixelCount;

        return histogram;
    }
}