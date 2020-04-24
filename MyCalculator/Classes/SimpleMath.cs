using System;
using System.Diagnostics;

namespace MyCalculator.Classes
{
    public enum EValueError
    {
        NO_ERROR,
        ZERO_DIVISION,
        NAN,
        POSITIVE_INF,
        NEGATIVE_INF,
        UNKNOWN
    }

    public static class SimpleMath
    {
        public static double Add(double x, double y)
        {
            return x + y;
        }

        public static double Subtract(double x, double y)
        {
            return x - y;
        }

        public static double Multiply(double x, double y)
        {
            return x * y;
        }

        public static double Divide(double x, double y)
        {
            //Debug.Assert(y != 0);
            return x / y;
        }

        public static double GetSquareRoot(double x)
        {
            //Debug.Assert(x >= 0);
            return Math.Sqrt(x);
        }

        public static double GetSquare(double x)
        {
            return x * x;
        }

        public static double GetInverse(double x)
        {
            //Debug.Assert(x != 0);
            return 1 / x;
        }

        public static EValueError GetError(double x)
        {
            if (double.IsNaN(x))
            {
                return EValueError.NAN;
            }
            if (double.IsPositiveInfinity(x))
            {
                return EValueError.POSITIVE_INF;
            }
            if (double.IsNegativeInfinity(x))
            {
                return EValueError.NEGATIVE_INF;
            }
            return EValueError.NO_ERROR;
        }
    }
}
