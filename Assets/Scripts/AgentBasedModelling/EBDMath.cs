/*
DesignMind: A Toolkit for Evidence-Based, Cognitively- Informed and Human-Centered Architectural Design
Copyright (C) 2023  michal Gath-Morad, Christoph Hölscher, Raphaël Baur

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>
*/

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