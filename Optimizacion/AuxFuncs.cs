using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Optimizacion
{
    public static class VectorOp
    {
        public static double sumproduct(double[] a, double[] b)
        {
            int n = a.GetLength(0);
            double result = 0;
            for (int i = 0; i < n; i++)
            {
                result += a[i] * b[i];
            }
            return result;
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
            double[] result = (double[])a.Clone();
            for(int i = 0; i < a.GetLength(0); i++)
            {
                result[i] /= b;
            }
            return result;
        }

        public static double[] normalize(double[] a)
        {
            return division(a, a.Sum());
        }
        
        public static double[] subtraction(double[] a, double[] b)
        {
            double[] result = (double[])a.Clone();
            for(int i = 0; i < a.GetLength(0); i++)
            {
                result[i] -= b[i];
            }
            return result;
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
        public static double[,] matrixFrowRow(double[] row)
        {
            int n = row.GetLength(0);
            double[,] result = new double[1, n];
            for (int i = 0; i < n; i++)
            {
                result[0, i] = row[i];
            }
            return result;
        }
        public static double[,] matrixFrowColumn(double[] column)
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
            System.Console.WriteLine("{0}", alglib.ap.format(result, 4)); // EXPECTED: [[0.666666,-0.333333],[-0.333333,0.666666]]
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
