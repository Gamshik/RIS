using System.Globalization;

namespace FastGaussParallel
{
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
            int numThreads = Environment.ProcessorCount;

            if (args.Length >= 2 && int.TryParse(args[1], out int t))
                numThreads = Math.Max(1, t);

            double[] Aflat;
            double[] B;
            int N;

            ReadMatrixAndVector(folder, out Aflat, out B, out N);

            var sw = System.Diagnostics.Stopwatch.StartNew();

            // потоков не больше, чем numThreads
            ParallelOptions popt = new ParallelOptions { MaxDegreeOfParallelism = numThreads };

            for (int k = 0; k < N; k++)
            {
                int pivotIndex = k * N + k;
                double akk = Aflat[pivotIndex];

                int trailing = N - (k + 1);
                double[] pivotRow = new double[trailing];
                if (trailing > 0)
                    Array.Copy(Aflat, k * N + (k + 1), pivotRow, 0, trailing);

                double bk = B[k];

                Parallel.For(k + 1, N, popt, i =>
                {
                    int baseI = i * N;
                    double a_ik = Aflat[baseI + k];
                    if (a_ik == 0.0)
                    {
                        return;
                    }

                    double factor = a_ik / akk;

                    Aflat[baseI + k] = factor;

                    int pj = k + 1;
                    for (int t = 0; t < trailing; t++, pj++)
                    {
                        Aflat[baseI + pj] -= factor * pivotRow[t];
                    }

                    B[i] -= factor * bk;
                });
            }

            double[] X = new double[N];
            for (int i = N - 1; i >= 0; i--)
            {
                double sum = B[i];
                int baseI = i * N;
                for (int j = i + 1; j < N; j++)
                    sum -= Aflat[baseI + j] * X[j];

                double diag = Aflat[baseI + i];
                if (Math.Abs(diag) < 1e-18)
                {
                    X[i] = 0.0;
                }
                else
                {
                    X[i] = sum / diag;
                }
            }

            sw.Stop();

            WriteVector(Path.Combine(folder, "X.txt"), X);

            Console.WriteLine("==============================================");
            Console.WriteLine($"  Размер матрицы: {N}x{N}");
            Console.WriteLine($"  Потоков: {numThreads}");
            Console.WriteLine($"  Время: {sw.Elapsed.TotalMilliseconds:F3} мс");
            Console.WriteLine("==============================================");
        }

        static void ReadMatrixAndVector(string folder, out double[] Aflat, out double[] B, out int N)
        {
            string pathA = Path.Combine(folder, "A.txt");
            string pathB = Path.Combine(folder, "B.txt");
            var linesA = File.ReadAllLines(pathA);
            N = linesA.Length;
            Aflat = new double[N * N];
            for (int i = 0; i < N; i++)
            {
                var parts = linesA[i].Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != N) throw new InvalidDataException($"Строка {i} матрицы A.txt имеет {parts.Length} эл-тов, а должно {N}");
                for (int j = 0; j < N; j++)
                    Aflat[i * N + j] = double.Parse(parts[j], CultureInfo.InvariantCulture);
            }

            var linesB = File.ReadAllLines(pathB);
            if (linesB.Length != N) throw new InvalidDataException("B.txt не соответствует размерам");
            B = new double[N];
            for (int i = 0; i < N; i++)
                B[i] = double.Parse(linesB[i], CultureInfo.InvariantCulture);
        }

        static void WriteVector(string path, double[] X)
        {
            using (var sw = new StreamWriter(path))
                foreach (double v in X)
                    sw.WriteLine(v.ToString("G17", CultureInfo.InvariantCulture));
        }
    }
}
