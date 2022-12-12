using System;
using System.Collections.Generic;
using UnityEngine;

public static class EBDMath
{
    public static (T, T) MinMax<T>(T[,,] A) where T : IComparable<T>
    {
        int dim0 = A.GetLength(0);
        int dim1 = A.GetLength(1);
        int dim2 = A.GetLength(2);
        T min = A[0, 0, 0];
        T max = A[0, 0, 0];
        for (int i = 0; i < dim0; i++)
        {
            for (int j = 0; j < dim1; j++)
            {
                for (int k = 0; k < dim2; k++)
                {
                    T val = A[i, j, k];
                    min = val.CompareTo(min) < 0 ? val : min;
                    max = val.CompareTo(max) > 0 ? val : max;
                }
            }
        }
        return (min, max);
    }

    public static List<T> Flatten<T>(T[,,] A)
    {
        List<T> flattened = new List<T>();
        int dim0 = A.GetLength(0);
        int dim1 = A.GetLength(1);
        int dim2 = A.GetLength(2);
        for (int i = 0; i < dim0; i++)
        {
            for (int j = 0; j < dim1; j++)
            {
                for (int k = 0; k < dim2; k++)
                {
                    flattened.Add(A[i, j, k]);
                }
            }
        }
        return flattened;
    }

    public static float[,,] UnaryOpElementWise(float[,,] A, Func<float, float> fun)
    {
        int dim0 = A.GetLength(0);
        int dim1 = A.GetLength(1);
        int dim2 = A.GetLength(2);
        float[,,] res = new float[dim0, dim1, dim2];
        for (int i = 0; i < dim0; i++)
        {
            for (int j = 0; j < dim1; j++)
            {
                for (int k = 0; k < dim2; k++)
                {
                    res[i, j, k] = fun(A[i, j, k]);
                }
            }
        }
        return res;
    }

    public static float[,,] BinaryOpElementWise(float[,,] A, float[,,] B, Func<float, float, float> fun)
    {
        int dim0 = A.GetLength(0);
        int dim1 = A.GetLength(1);
        int dim2 = A.GetLength(2);
        float[,,] res = new float[dim0, dim1, dim2];
        for (int i = 0; i < dim0; i++)
        {
            for (int j = 0; j < dim1; j++)
            {
                for (int k = 0; k < dim2; k++) {
                    res[i, j, k] = fun(A[i, j, k], B[i, j, k]);
                }
            }
        }
        return res; 
    }
}