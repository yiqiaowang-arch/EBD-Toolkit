using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Threading.Tasks;

namespace EBD
{
    public static class KernelDensityEstimate
    {
        public static List<float> Evaluate(List<Vector3> data, List<Vector3> queryPoints, float bandwidth)
        {
            float[,] kernelValues = new float[data.Count, queryPoints.Count];
            Parallel.For(0, data.Count, i =>
            {
                for (int j = 0; j < queryPoints.Count; j++)
                {
                    kernelValues[i, j] = GaussianKernel(Vector3.Distance(data[i], queryPoints[j]), bandwidth);
                }
            });

            // Sum the kernel values for each query point.
            float[] densities = new float[queryPoints.Count];
            Parallel.For(0, queryPoints.Count, j =>
            {
                float density = 0;
                for (int i = 0; i < data.Count; i++)
                {
                    density += kernelValues[i, j];
                }
                densities[j] = density;
            });

            // Normalize the densities.
            float min = densities.Min();
            float max = densities.Max();
            float new_range = max - min;
            float[] normalizedDensities = new float[densities.Length];
            Parallel.For(0, densities.Length, i =>
            {
                normalizedDensities[i] = (densities[i] - min) / new_range;
            });
            return normalizedDensities.ToList();
        }

        private static float GaussianKernel(float dist, float bandwidth = 1.0f)
        {
            return Mathf.Exp(-0.5f * Mathf.Pow(dist / bandwidth, 2)) / (bandwidth * Mathf.Sqrt(2 * Mathf.PI));
        }
    }
}
