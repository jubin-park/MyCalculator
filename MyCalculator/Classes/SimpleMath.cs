using System;

namespace MyCalculator.Classes
{
    public enum EOperator
    {
        NONE,
        ADDITION,
        SUBTRACTION,
        MULTIPLICATION,
        DIVISION,
        EQUAL,
    }

    public enum EFunction
    {
        NONE,
        SQUARE_ROOT,
        SQUARE,
        INVERSE,
        NEGATE,
        PERCENT,
    }

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
        public static double Add(double x, double y, out EValueError err)
        {
            double z = x + y;
            err = GetErrorFromNumber(z);
            return z;
        }

        public static double Subtract(double x, double y, out EValueError err)
        {
            double z = x - y;
            err = GetErrorFromNumber(z);
            return z;
        }

        public static double Multiply(double x, double y, out EValueError err)
        {
            double z = x * y;
            err = GetErrorFromNumber(z);
            return z;
        }

        public static double Divide(double x, double y, out EValueError err)
        {
            double z = x / y;
            if (y == 0)
            {
                err = EValueError.ZERO_DIVISION;
            }
            else
            {
                err = GetErrorFromNumber(z);
            }
            return z;
        }

        public static double GetSquareRoot(double x, out EValueError err)
        {
            double z = Math.Sqrt(x);
            err = GetErrorFromNumber(z);
            return z;
        }

        public static double GetSquare(double x, out EValueError err)
        {
            double z = x * x;
            err = GetErrorFromNumber(z);
            return z;
        }

        public static double GetInverse(double x, out EValueError err)
        {
            double z = 1 / x;
            if (x == 0)
            {
                err = EValueError.ZERO_DIVISION;
            }
            else
            {
                err = GetErrorFromNumber(z);
            }
            return z;
        }

        public static EValueError GetErrorFromNumber(double x)
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
