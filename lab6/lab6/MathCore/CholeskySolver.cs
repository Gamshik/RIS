namespace Common
{
    public static class CholeskySolver
    {
        public static double[] Solve(LinearSystem system)
        {
            int n = system.N;
            var L = new double[n, n];
            CholeskyDecompose(system.A, L, n);

            var y = ForwardSubstitution(L, system.B, n);
            var x = BackwardSubstitution(L, y, n);
            return x;
        }

        private static void CholeskyDecompose(double[,] A, double[,] L, int n)
        {
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j <= i; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < j; k++)
                        sum += L[i, k] * L[j, k];

                    if (i == j)
                        L[i, i] = Math.Sqrt(A[i, i] - sum);
                    else
                        L[i, j] = (A[i, j] - sum) / L[j, j];
                }
            }
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
