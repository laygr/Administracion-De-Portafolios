using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Optimizacion
{
    public static class VectorOp
    {
        public static double[] createWith(int n, double initialValue)
        {
            double[] result = new double[n];
            for (int i = 0; i < n; i++)
            {
                result[i] = initialValue;
            }
            return result;
        }
        public static double[] DotProduct(double[] a, double[] b)
        {
            int n = a.Length;
            double[] result = new double[n];
            for(int i = 0; i < n; i++)
            {
                result[i] = a[i] * b[i];
            }
            return result;
        }
        public static double[] DotDivision(double[] a, double[] b)
        {
            int n = a.Length;
            double[] result = new double[n];
            for (int i = 0; i < n; i++)
            {
                result[i] = a[i] / b[i];
            }
            return result;
        }
        public static double sumproduct(double[] a, double[] b)
        {
            return DotProduct(a, b).Sum();
        }
        public static double[] multiplication(double[] a, double b)
        {
            double[] result = (double[])a.Clone();
            for (int i = 0; i < a.GetLength(0); i++)
            {
                result[i] *= b;
            }
            return result;
        }
        public static double[] division(double[]a, double b)
        {
            int n = a.Length;
            double[] result = new double[n];
            for(int i = 0; i < n; i++)
            {
                result[i] = a[i] / b;
            }
            return result;
        }

        public static double[] normalize(double[] a)
        {
            var sum = a.Sum();
            if (sum == 0)
            {
                return createWith(a.Length, 0);
            }
            else
            {
                return division(a, a.Sum());
            }
        }
        public static double[] normalize(double[] a, double normalizeTo)
        {
            return multiplication(normalize(a), normalizeTo);
        }

        public static double[] DotAddition(double[] a, double[] b)
        {
            double[] result = (double[])a.Clone();
            for (int i = 0; i < a.GetLength(0); i++)
            {
                result[i] += b[i];
            }
            return result;
        }

        public static double[] Addition(double[] a, double b)
        {
            double[] result = (double[])a.Clone();
            for (int i = 0; i < a.GetLength(0); i++)
            {
                result[i] += b;
            }
            return result;
        }

        public static double[] DotSubtraction(double[] a, double[] b)
        {
            double[] result = (double[])a.Clone();
            for(int i = 0; i < a.GetLength(0); i++)
            {
                result[i] -= b[i];
            }
            return result;
        }

        public static double SquareError(double[] a, double[] b)
        {
            double acum = 0;
            for(int i = 0; i < a.Length; i++)
            {
                acum += Math.Pow(a[i] - b[i], 2);
            }
            return acum;
        }

        public static double[] difference(double[] a, double[] b)
        {
            double[] result = (double[])a.Clone();
            for (int i = 0; i < a.GetLength(0); i++)
            {
                result[i] = Math.Abs(result[i] - b[i]);
            }
            return result;
        }
    }
    public static class MatrixOp
    {
        public static double[,] diagonal(double[,] matrix)
        {
            int n = matrix.GetLength(0);
            int m = matrix.GetLength(1);
            double[,] result = new double[n, m];
            for(int r = 0; r < n; r++)
            {
                for(int c = 0; c < m; c++)
                {
                    if (r == c)
                    {
                        result[r, c] = matrix[r, c];
                    }
                    else
                    {
                        result[r, c] = 0;
                    }
                }
            }
            return result;
        }
        public static double[] columnFromMatrix(double[,] m, int c)
        {
            int n = m.GetLength(0);
            double[] result = new double[n];
            for(int i = 0; i < n; i++)
            {
                result[i] = m[i, c];
            }
            return result;
        }

        public static double[,] matrixFromRow(double[] row)
        {
            int n = row.GetLength(0);
            double[,] result = new double[1, n];
            for (int i = 0; i < n; i++)
            {
                result[0, i] = row[i];
            }
            return result;
        }
        public static double[,] matrixFromColumn(double[] column)
        {
            int n = column.GetLength(0);
            double[,] result = new double[n, 1];
            for (int i = 0; i < n; i++)
            {
                result[i, 0] = column[i];
            }
            return result;
        }
        public static double[,] mmult(double[,] a, double[,] b)
        {
            int m = a.GetLength(0);
            int n = b.GetLength(1);
            int k = a.GetLength(1);

            double[,] c = new double[m, n];

            alglib.rmatrixgemm(m, n, k, 1, a, 0, 0, 0, b, 0, 0, 0, 0, ref c, 0, 0);
            return c;
        }
        public static double sumproduct(double[,] a, double[,] b)
        {
            int n = a.GetLength(1);
            double result = 0;
            for (int i = 0; i < n; i++)
            {
                result += a[0, i] * b[0, i];
            }
            return result;
        }
        public static double[,] transpose(double[,] a)
        {
            int m = a.GetLength(0);
            int n = a.GetLength(1);
            double[,] result = new double[n, m];
            alglib.rmatrixtranspose(m, n, a, 0, 0, ref result, 0, 0);
            return result;
        }
        public static double[,] invert(double[,] a)
        {
            double[,] result = (double[,])a.Clone();
            int info;
            alglib.matinvreport rep;
            alglib.rmatrixinverse(ref result, out info, out rep);
            return result;
        }
        public static double[,] mmultbyscalar(double[,] a, double f)
        {
            double[,] result = (double[,])a.Clone();
            for (int r = 0; r < a.GetLength(0); r++)
            {
                for (int c = 0; c < a.GetLength(1); c++)
                {
                    result[r, c] *= f;
                }
            }
            return result;
        }

        public static double[,] mmultbyscalar(double f, double[,] a)
        {
            return mmultbyscalar(a, f);
        }

        public static double[,] dividebyscalar(double[,]a, double f)
        {
            double[,] result = (double[,])a.Clone();
            for (int r = 0; r < a.GetLength(0); r++)
            {
                for (int c = 0; c < a.GetLength(1); c++)
                {
                    result[r, c] /= f;
                }
            }
            return result;
        }

        public static double[,] pointwiseDivision(double[,]a, double[,] b)
        {
            double[,] result = (double[,])a.Clone();
            for (int r = 0; r < a.GetLength(0); r++)
            {
                for (int c = 0; c < a.GetLength(1); c++)
                {
                    result[r, c] /= b[r,c];
                }
            }
            return result;
        }
        public static double[,] addition(double[,] a, double[,] b)
        {
            double[,] result = (double[,])a.Clone();
            for (int r = 0; r < a.GetLength(0); r++)
            {
                for (int c = 0; c < a.GetLength(1); c++)
                {
                    result[r, c] += b[r, c];
                }
            }
            return result;
        }

        public static double[,] addScalar(double[,] a, double b)
        {
            double[,] result = (double[,])a.Clone();
            for (int r = 0; r < a.GetLength(0); r++)
            {
                for (int c = 0; c < a.GetLength(1); c++)
                {
                    result[r, c] += b;
                }
            }
            return result;
        }
        public static double[,] addScalar(double b, double[,]a)
        {
            return addScalar(a, b);
        }
        public static double[,] varcovar(double[,] mat)
        {
            int n = mat.GetLength(0);
            int m = mat.GetLength(1);

            double[,] result = new double[n, m];
            alglib.covm(mat, out result);
            return result;
        }
    }
}
