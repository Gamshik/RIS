namespace lab4
{
    public static class LLTSolver
    {
        private static double[,] decompose(double[,] A)
        {
            int n = A.GetLength(0);
            double[,] L = new double[n, n];

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j <= i; j++)
                {
                    double sum = 0;

                    if (j == i) // Диагональные элементы
                    {
                        for (int k = 0; k < j; k++)
                        {
                            sum += L[j, k] * L[j, k];
                        }

                        double value = A[j, j] - sum;

                        if (value <= 0)
                        {
                            throw new InvalidOperationException(
                                $"Матрица не является положительно определённой (элемент [{j},{j}] = {value})");
                        }

                        L[j, j] = Math.Sqrt(value);
                    }
                    else // Внедиагональные элементы
                    {
                        for (int k = 0; k < j; k++)
                        {
                            sum += L[i, k] * L[j, k];
                        }

                        L[i, j] = (A[i, j] - sum) / L[j, j];
                    }
                }
            }

            return L;
        }

        private static double[] forwardSubstitution(double[,] L, double[] b)
        {
            int n = L.GetLength(0);
            double[] y = new double[n];

            for (int i = 0; i < n; i++)
            {
                double sum = 0;
                for (int j = 0; j < i; j++)
                {
                    sum += L[i, j] * y[j];
                }
                y[i] = (b[i] - sum) / L[i, i];
            }

            return y;
        }

        private static double[] backwardSubstitution(double[,] L, double[] y)
        {
            int n = L.GetLength(0);
            double[] x = new double[n];

            for (int i = n - 1; i >= 0; i--)
            {
                double sum = 0;
                for (int j = i + 1; j < n; j++)
                {
                    sum += L[j, i] * x[j]; // L^T[i,j] = L[j,i]
                }
                x[i] = (y[i] - sum) / L[i, i];
            }

            return x;
        }

        public static double[] Solve(double[,] A, double[] b)
        {
            double[,] L = decompose(A);

            double[] y = forwardSubstitution(L, b);

            double[] x = backwardSubstitution(L, y);

            return x;
        }

        public static double VerifySolution(double[,] A, double[] x, double[] b)
        {
            double[] Ax = MatrixHelper.MultiplyMatrixVector(A, x);
            double[] residual = MatrixHelper.SubtractVectors(Ax, b);
            return MatrixHelper.VectorNorm(residual);
        }
    }
}
