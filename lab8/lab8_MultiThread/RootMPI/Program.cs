using MPI;
using System.Globalization;

namespace RootMPI
{
    class RootMPI
    {
        static void Main(string[] args)
        {
            using (new MPI.Environment(ref args))
            {
                Intracommunicator comm = Communicator.world;
                int rank = comm.Rank;
                int size = comm.Size;

                if (args.Length < 1)
                {
                    if (rank == 0) Console.WriteLine("Ожидался аргумент - путь к папке с данными.");
                    return;
                }

                string folder = args[0];

                double[][] ALocal = null;
                int[] myColumns = null;
                double[] B = null;
                int N = 0;
                int localCols = 0;
                double[][] A = null;

                if (rank == 0)
                {
                    Console.WriteLine("[ROOT] Чтение матрицы...");
                    A = ReadMatrix(Path.Combine(folder, "A.txt"));
                    B = ReadVector(Path.Combine(folder, "B.txt"));
                    N = A.Length;
                    Console.WriteLine($"[ROOT] размер матрицы = {N}х{N}, процессов = {size}");
                }

                double startTime = MPI.Environment.Time;

                int[] nArray = new int[1];
                if (rank == 0) nArray[0] = N;
                comm.Broadcast(ref nArray, 0);
                N = nArray[0];

                if (rank != 0) B = new double[N];
                comm.Broadcast(ref B, 0);

                if (rank == 0)
                {
                    for (int p = 0; p < size; p++)
                    {
                        List<int> cols = [];

                        for (int j = p; j < N; j += size)
                            cols.Add(j);

                        int colCount = cols.Count;

                        if (p == 0)
                        {
                            localCols = colCount;
                            myColumns = [.. cols];
                            ALocal = new double[N][];
                            for (int i = 0; i < N; i++)
                            {
                                ALocal[i] = new double[localCols];
                                for (int locJ = 0; locJ < localCols; locJ++)
                                    ALocal[i][locJ] = A[i][myColumns[locJ]];
                            }
                        }
                        else
                        {
                            comm.Send(colCount, p, (int)MessageTag.RootToWorker_ColumnCount);
                            comm.Send(cols.ToArray(), p, (int)MessageTag.RootToWorker_ColumnIndices);

                            double[] columnData = new double[colCount * N];
                            for (int i = 0; i < N; i++)
                                for (int locJ = 0; locJ < colCount; locJ++)
                                    columnData[i * colCount + locJ] = A[i][cols[locJ]];
                            comm.Send(columnData, p, (int)MessageTag.RootToWorker_ColumnData);
                        }
                    }

                    Console.WriteLine($"[ROOT] Столбцы распределены. Моё кол-во столбцов - {localCols}");
                }
                else
                {
                    localCols = comm.Receive<int>(0, (int)MessageTag.RootToWorker_ColumnCount);
                    myColumns = new int[localCols];
                    comm.Receive(0, (int)MessageTag.RootToWorker_ColumnIndices, ref myColumns);

                    double[] columnData = new double[localCols * N];
                    comm.Receive(0, (int)MessageTag.RootToWorker_ColumnData, ref columnData);

                    ALocal = new double[N][];
                    for (int i = 0; i < N; i++)
                    {
                        ALocal[i] = new double[localCols];
                        for (int locJ = 0; locJ < localCols; locJ++)
                            ALocal[i][locJ] = columnData[i * localCols + locJ];
                    }

                    Console.WriteLine($"[WORKER {rank}] Получено {localCols} столбцов");
                }

                comm.Barrier();

                for (int k = 0; k < N; k++)
                {
                    int columnOwner = k % size;

                    // опорная строка
                    double[] pivotRow = new double[N];
                    // диагональный эл-т
                    double akk = 0.0;
                    // правая часть уравнения
                    double bk = 0.0;

                    if (rank == columnOwner)
                    {
                        int localK = -1;

                        for (int locJ = 0; locJ < localCols; locJ++)
                            if (myColumns[locJ] == k)
                            {
                                localK = locJ;
                                break;
                            }

                        if (localK == -1)
                        {
                            Console.WriteLine($"[ERROR RANK {rank}] Столбец не найден!");
                            return;
                        }

                        for (int locJ = 0; locJ < localCols; locJ++)
                            pivotRow[myColumns[locJ]] = ALocal[k][locJ];

                        for (int p = 0; p < size; p++)
                        {
                            if (p == rank) continue;

                            int otherColCount = comm.Receive<int>(p, (int)MessageTag.PivotColCount);
                            int[] otherCols = new int[otherColCount];
                            comm.Receive(p, (int)MessageTag.PivotColIndices, ref otherCols);
                            double[] otherValues = new double[otherColCount];
                            comm.Receive(p, (int)MessageTag.PivotRowValues, ref otherValues);

                            for (int t = 0; t < otherColCount; t++)
                                pivotRow[otherCols[t]] = otherValues[t];
                        }

                        akk = pivotRow[k];
                        bk = B[k];
                    }
                    else
                    {
                        comm.Send(localCols, columnOwner, (int)MessageTag.PivotColCount);
                        comm.Send(myColumns, columnOwner, (int)MessageTag.PivotColIndices);

                        double[] myRowK = new double[localCols];
                        for (int locJ = 0; locJ < localCols; locJ++)
                            myRowK[locJ] = ALocal[k][locJ];

                        comm.Send(myRowK, columnOwner, (int)MessageTag.PivotRowValues);
                    }

                    comm.Broadcast(ref pivotRow, columnOwner);
                    double[] akkArray = [0.0];
                    if (rank == columnOwner) akkArray[0] = akk;
                    comm.Broadcast(ref akkArray, columnOwner);
                    akk = akkArray[0];

                    double[] bkArray = [0.0];
                    if (rank == columnOwner) bkArray[0] = bk;
                    comm.Broadcast(ref bkArray, columnOwner);
                    bk = bkArray[0];

                    for (int i = k + 1; i < N; i++)
                    {
                        double factor = pivotRow[k] / akk;

                        for (int locJ = 0; locJ < localCols; locJ++)
                            ALocal[i][locJ] -= factor * pivotRow[myColumns[locJ]];
                    }

                    for (int i = k + 1; i < N; i++)
                    {
                        double factor = pivotRow[k] / akk;
                        B[i] -= factor * bk;
                    }
                }

                comm.Barrier();

                double[][] A_full = null;

                if (rank == 0)
                {
                    A_full = new double[N][];
                    for (int i = 0; i < N; i++)
                    {
                        A_full[i] = new double[N];
                        // Свои столбцы
                        for (int locJ = 0; locJ < localCols; locJ++)
                            A_full[i][myColumns[locJ]] = ALocal[i][locJ];
                    }

                    // Получаем от воркеров
                    for (int p = 1; p < size; p++)
                    {
                        int colCount = comm.Receive<int>(p, (int)MessageTag.WorkerToRoot_ResultColCount);
                        int[] cols = new int[colCount];
                        comm.Receive(p, (int)MessageTag.WorkerToRoot_ResultColIndices, ref cols);
                        double[] columnData = new double[colCount * N];
                        comm.Receive(p, (int)MessageTag.WorkerToRoot_ResultColumnData, ref columnData);

                        for (int i = 0; i < N; i++)
                            for (int locJ = 0; locJ < colCount; locJ++)
                                A_full[i][cols[locJ]] = columnData[i * colCount + locJ];
                    }
                }
                else
                {
                    comm.Send(localCols, 0, (int)MessageTag.WorkerToRoot_ResultColCount);
                    comm.Send(myColumns, 0, (int)MessageTag.WorkerToRoot_ResultColIndices);
                    double[] columnData = new double[localCols * N];
                    for (int i = 0; i < N; i++)
                        for (int locJ = 0; locJ < localCols; locJ++)
                            columnData[i * localCols + locJ] = ALocal[i][locJ];
                    comm.Send(columnData, 0, (int)MessageTag.WorkerToRoot_ResultColumnData);
                }

                comm.Barrier();

                if (rank == 0)
                {
                    double[] X = new double[N];

                    for (int i = N - 1; i >= 0; i--)
                    {
                        double sum = B[i];
                        for (int j = i + 1; j < N; j++)
                            sum -= A_full[i][j] * X[j];

                        X[i] = sum / A_full[i][i];
                    }


                    string outPath = Path.Combine(folder, "X.txt");
                    WriteVector(outPath, X);

                    double endTime = MPI.Environment.Time;
                    double totalTime = endTime - startTime; 
                    double totalTimeMs = totalTime * 1000.0;

                    Console.WriteLine("==============================================");
                    Console.WriteLine($"  Размер матрицы: {N}x{N}");
                    Console.WriteLine($"  Процессов: {size}");
                    Console.WriteLine($"  Время: {totalTimeMs:F3} мс");
                    Console.WriteLine("==============================================");
                }

                comm.Barrier();
            }
        }

        static double[][] ReadMatrix(string path)
        {
            var lines = File.ReadAllLines(path);
            int N = lines.Length;
            double[][] A = new double[N][];
            for (int i = 0; i < N; i++)
            {
                var parts = lines[i].Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                A[i] = new double[N];
                for (int j = 0; j < N; j++)
                    A[i][j] = double.Parse(parts[j], CultureInfo.InvariantCulture);
            }
            return A;
        }

        static double[] ReadVector(string path)
        {
            var lines = File.ReadAllLines(path);
            int N = lines.Length;
            double[] B = new double[N];
            for (int i = 0; i < N; i++)
                B[i] = double.Parse(lines[i], CultureInfo.InvariantCulture);
            return B;
        }

        static void WriteVector(string path, double[] X)
        {
            using (var sw = new StreamWriter(path))
                foreach (double x in X)
                    sw.WriteLine(x.ToString("G17", CultureInfo.InvariantCulture));
        }
    }
}
