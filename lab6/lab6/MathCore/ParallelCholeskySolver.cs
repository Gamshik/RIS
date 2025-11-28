namespace Common
{
    public static class ParallelCholeskySolver
    {
        public static double[] Solve(LinearSystem system)
        {
            int n = system.N;
            var L = new double[n, n];
            CholeskyDecomposeParallel(system.A, L, n);

            var y = ForwardSubstitution(L, system.B, n);
            var x = BackwardSubstitution(L, y, n);
            return x;
        }

        private static void CholeskyDecomposeParallel(double[,] A, double[,] L, int n)
        {
            var rowReadyEvents = new ManualResetEventSlim[n];
            for (int i = 0; i < n; i++)
                rowReadyEvents[i] = new ManualResetEventSlim(false);

            for (int row = 0; row < n; row++)
            {
                int r = row;
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    for (int prev = 0; prev < r; prev++)
                        rowReadyEvents[prev].Wait();

                    double sum = 0;
                    for (int k = 0; k < r; k++)
                        sum += L[r, k] * L[r, k];
                    L[r, r] = Math.Sqrt(A[r, r] - sum);

                    for (int i = r + 1; i < n; i++)
                    {
                        double sum2 = 0;
                        for (int k = 0; k < r; k++)
                            sum2 += L[i, k] * L[r, k];
                        L[i, r] = (A[i, r] - sum2) / L[r, r];
                    }

                    rowReadyEvents[r].Set();
                });
            }

            for (int i = 0; i < n; i++)
                rowReadyEvents[i].Wait();

            foreach (var ev in rowReadyEvents)
                ev.Dispose();
        }

        private static double[] ForwardSubstitution(double[,] L, double[] b, int n)
        {
            var y = new double[n];
            for (int i = 0; i < n; i++)
            {
                double sum = 0;
                for (int j = 0; j < i; j++) sum += L[i, j] * y[j];
                y[i] = (b[i] - sum) / L[i, i];
            }
            return y;
        }

        private static double[] BackwardSubstitution(double[,] L, double[] y, int n)
        {
            var x = new double[n];
            for (int i = n - 1; i >= 0; i--)
            {
                double sum = 0;
                for (int j = i + 1; j < n; j++) sum += L[j, i] * x[j];
                x[i] = (y[i] - sum) / L[i, i];
            }
            return x;
        }
    }
}
