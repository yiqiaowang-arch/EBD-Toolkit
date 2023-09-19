using System;
using System.Collections.Generic;
using UnityEngine;

public static class EBDMath
{
    public static (T, T) MinMax<T>(T[,] array2D) where T : IComparable<T>
    {
        int dim0 = array2D.GetLength(0);
        int dim1 = array2D.GetLength(1);
        T min = array2D[0, 0];
        T max = array2D[0, 0];
        for (int i = 0; i < dim0; i++)
        {
            for (int j = 0; j < dim1; j++)
            {
                T val = array2D[i, j];
                min = val.CompareTo(min) < 0 ? val : min;
                max = val.CompareTo(max) > 0 ? val : max;
            }
        }
        return (min, max);
    }

    public static List<T> Flatten<T>(T[,] array2D)
    {
        List<T> flattened = new List<T>();
        int dim0 = array2D.GetLength(0);
        int dim1 = array2D.GetLength(1);
        for (int i = 0; i < dim0; i++)
        {
            for (int j = 0; j < dim1; j++)
            {
                flattened.Add(array2D[i, j]);
            }
        }
        return flattened;
    }

    public static float[,] UnaryOpElementWise(float[,] A, Func<float, float> fun)
    {
        int dim0 = A.GetLength(0);
        int dim1 = A.GetLength(1);
        float[,] res = new float[dim0, dim1];
        for (int i = 0; i < dim0; i++)
        {
            for (int j = 0; j < dim1; j++)
            {
                res[i, j] = fun(A[i, j]);
            }
        }
        return res;
    }

    public static float[,] BinaryOpElementWise(float[,] A, float[,] B, Func<float, float, float> fun)
    {
        int dim0 = A.GetLength(0);
        int dim1 = A.GetLength(1);
        float[,] res = new float[dim0, dim1];
        for (int i = 0; i < dim0; i++)
        {
            for (int j = 0; j < dim1; j++)
            {
                res[i, j] = fun(A[i, j], B[i, j]);
            }
        }
        return res; 
    }
}