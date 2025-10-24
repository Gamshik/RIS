using System.Text;

namespace PlanProc
{
    public static class MathUtils
    {
        private static readonly Random _random = new Random();

        public static void GenerateExampleScenario()
        {
            Console.WriteLine("Генерация примеров входных файлов (P1-P2: 50x50, P3-P10: 10x10) для демонстрации GS...");

            var mainContent = new StringBuilder();
            mainContent.AppendLine("# Сценарий для демонстрации преимуществ Guaranteed Scheduling (GS)");
            mainContent.AppendLine("q=10");

            mainContent.AppendLine("P1: A1 b1 t=0");
            mainContent.AppendLine("P2: A2 b2 t=1");

            for (int i = 3; i <= 10; i++)
            {
                mainContent.AppendLine($"P{i}: A{i} b{i} t=2");
            }

            File.WriteAllText("main.txt", mainContent.ToString());

            GenerateSymmetricMatrix(1, 50);
            GenerateSymmetricMatrix(2, 50);
            for (int i = 3; i <= 10; i++)
            {
                GenerateSymmetricMatrix(i, 10);
            }
        }

        private static void GenerateSymmetricMatrix(int index, int size)
        {
            // Генерация B
            var B = new double[size, size];
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    B[i, j] = _random.NextDouble() * 2 - 1;
                }
            }

            // A = B * B^T
            var A = new double[size, size];
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++) 
                {
                    double sum = 0;
                    for (int k = 0; k < size; k++)
                    {
                        sum += B[i, k] * B[j, k];
                    }
                    A[i, j] = sum;
                    if (i != j) A[j, i] = sum; 
                }
            }

            double diagonalBoost = size * 10; 
            for (int i = 0; i < size; i++)
            {
                double offDiagSum = 0;
                for (int j = 0; j < size; j++)
                {
                    if (i != j) offDiagSum += Math.Abs(A[i, j]);
                }
                A[i, i] += offDiagSum + diagonalBoost; 
            }

            var matrixBuilder = new StringBuilder();
            var vectorBuilder = new StringBuilder();
            for (int i = 0; i < size; i++)
            {
                var row = Enumerable.Range(0, size).Select(j => Math.Round(A[i, j], 2)).Select(d => d.ToString("F2"));
                matrixBuilder.AppendLine(string.Join(" ", row));
                vectorBuilder.AppendLine((_random.NextDouble() * 100).ToString("F2"));
            }

            File.WriteAllText($"A{index}.txt", matrixBuilder.ToString());
            File.WriteAllText($"b{index}.txt", vectorBuilder.ToString());
        }

        public static double[,] ReadMatrixFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Ошибка: файл '{filePath}' не найден.");
                return null;
            }

            try
            {
                var lines = File.ReadAllLines(filePath).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
                if (lines.Length == 0) return new double[0, 0];

                var data = lines.Select(line => line.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(double.Parse).ToArray()).ToList();

                int rows = data.Count;
                int cols = data[0].Length;

                if (data.Any(row => row.Length != cols))
                {
                    Console.WriteLine($"Ошибка: строки в файле '{filePath}' имеют разную длину.");
                    return null;
                }

                var matrix = new double[rows, cols];
                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < cols; j++)
                    {
                        matrix[i, j] = data[i][j];
                    }
                }

                return matrix;
            }
            catch (Exception ex) when (ex is FormatException || ex is OverflowException)
            {
                Console.WriteLine($"Ошибка формата данных в файле '{filePath}': {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка чтения файла '{filePath}': {ex.Message}");
                return null;
            }
        }

        public static bool VerifySolution(double[,] A, double[] x, double[,] b)
        {
            if (A == null || x == null || b == null) return false;

            int n = A.GetLength(0);
            if (A.GetLength(1) != n || b.GetLength(0) != n || b.GetLength(1) != 1 || x.Length != n)
            {
                return false;
            }

            const double epsilon = 1e-6;
            for (int i = 0; i < n; i++)
            {
                double sum = 0;
                for (int j = 0; j < n; j++)
                {
                    sum += A[i, j] * x[j];
                }

                if (Math.Abs(sum - b[i, 0]) > epsilon)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsSymmetric(double[,] matrix)
        {
            if (matrix == null) return false;

            int n = matrix.GetLength(0);
            if (n != matrix.GetLength(1)) return false;

            const double tolerance = 1e-9;
            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    if (Math.Abs(matrix[i, j] - matrix[j, i]) > tolerance)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static double[] SolveCholesky(double[,] A, double[,] b)
        {
            if (A == null || b == null) return null;

            int n = A.GetLength(0);
            if (n != A.GetLength(1) || n != b.GetLength(0) || b.GetLength(1) != 1)
            {
                return null; 
            }

            if (!IsSymmetric(A))
            {
                return null; 
            }

            var L = new double[n, n];

            // Cholesky разложение: A = L * L^T
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j <= i; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < j; k++)
                    {
                        sum += L[i, k] * L[j, k];
                    }

                    if (i == j)
                    {
                        double diag = A[i, i] - sum;
                        if (diag <= 1e-9) return null; // Не положительно-определенная
                        L[i, j] = Math.Sqrt(diag);
                    }
                    else
                    {
                        if (Math.Abs(L[j, j]) < 1e-9) return null; // Деление на ноль
                        L[i, j] = (A[i, j] - sum) / L[j, j];
                    }
                }
            }

            // Прямой ход: L y = b
            var y = new double[n];
            for (int i = 0; i < n; i++)
            {
                double sum = 0;
                for (int j = 0; j < i; j++)
                {
                    sum += L[i, j] * y[j];
                }
                if (Math.Abs(L[i, i]) < 1e-6) return null;
                y[i] = (b[i, 0] - sum) / L[i, i];
            }

            // Обратный ход: L^T x = y
            var x = new double[n];
            for (int i = n - 1; i >= 0; i--)
            {
                double sum = 0;
                for (int j = i + 1; j < n; j++)
                {
                    sum += L[j, i] * x[j];
                }
                if (Math.Abs(L[i, i]) < 1e-6) return null;
                x[i] = (y[i] - sum) / L[i, i];
            }

            return x;
        }

        public static int GetMatrixSize(string filePath)
        {
            if (!File.Exists(filePath)) return 0;
            try
            {
                var lines = File.ReadAllLines(filePath);
                return lines.Length; // n = количество строк
            }
            catch
            {
                return 0;
            }
        }
    }
}