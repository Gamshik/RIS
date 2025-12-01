using System.Diagnostics;
using System.Globalization;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Ожидался аргумент - путь к папке с данными.");
            return;
        }

        string folder = args[0];

        string fileA = Path.Combine(folder, "A.txt");
        string fileB = Path.Combine(folder, "B.txt");
        string fileX = Path.Combine(folder, "X.txt");

        if (!File.Exists(fileA) || !File.Exists(fileB))
        {
            Console.WriteLine("Файлы A.txt или B.txt не найдены в указанной папке.");
            return;
        }

        double[,] A;
        double[] B;

        Stopwatch time = Stopwatch.StartNew();
        ReadMatrix(fileA, fileB, out A, out B);

        int N = B.Length;

        double[] X = GaussianElimination(A, B, N);

        using (var w = new StreamWriter(fileX))
        {
            for (int i = 0; i < N; i++)
                w.WriteLine(X[i].ToString("G17", CultureInfo.InvariantCulture));
        }

        time.Stop();

        Console.WriteLine($"Размер матрицы: {N}x{N}");
        Console.WriteLine($"Время: {time.ElapsedMilliseconds} мс");
    }

    static void ReadMatrix(string fileA, string fileB, out double[,] A, out double[] B)
    {
        string[] aLines = File.ReadAllLines(fileA);
        string[] bLines = File.ReadAllLines(fileB);

        int N = aLines.Length;

        A = new double[N, N];
        B = new double[N];

        for (int i = 0; i < N; i++)
        {
            string[] parts = aLines[i].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int j = 0; j < N; j++)
                A[i, j] = double.Parse(parts[j], CultureInfo.InvariantCulture);
        }

        for (int i = 0; i < N; i++)
            B[i] = double.Parse(bLines[i], CultureInfo.InvariantCulture);
    }

    static double[] GaussianElimination(double[,] A, double[] B, int N)
    {
        // Прямой ход
        for (int k = 0; k < N; k++)
        {
            double pivot = A[k, k];
            if (Math.Abs(pivot) < 1e-15)
                throw new Exception($"Нулевой главный элемент в строке {k}");

            // Нормируем строку
            for (int j = k; j < N; j++)
                A[k, j] /= pivot;
            B[k] /= pivot;

            // Вычитание
            for (int i = k + 1; i < N; i++)
            {
                double factor = A[i, k];
                for (int j = k; j < N; j++)
                    A[i, j] -= factor * A[k, j];
                B[i] -= factor * B[k];
            }
        }

        // Обратный ход
        double[] X = new double[N];
        for (int i = N - 1; i >= 0; i--)
        {
            double sum = B[i];
            for (int j = i + 1; j < N; j++)
                sum -= A[i, j] * X[j];

            X[i] = sum;
        }

        return X;
    }
}
