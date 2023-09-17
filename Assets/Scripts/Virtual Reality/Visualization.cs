using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Linq;

public class Visualization
{

    public static void RenderTrajectory(LineRenderer lineRenderer, List<Vector3> positions, Gradient gradient, float trajectoryWidth)
    {
        lineRenderer.colorGradient = gradient;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));  // Default material for linerenderer.
        lineRenderer.widthMultiplier = trajectoryWidth;
        lineRenderer.positionCount = positions.Count;
        lineRenderer.SetPositions(positions.ToArray());
    }

    public static List<float> KernelDensityEstimate(List<Vector3> positions, float sigma)
    {
        int n = positions.Count;

        // Calculate the distances between each hit (parallel).
        float[] distances = new float[n * (n - 1) / 2];
        int k = 0;

        // Precomputing indices into flattened distances_parallel array.
        int[] rowIndices = new int[n * (n - 1) / 2];
        int[] colIndices = new int[n * (n - 1) / 2];
        for (int i = 0; i < n; i++)
        {
            for (int j = i + 1; j < n; j++)
            {
                rowIndices[k] = i;
                colIndices[k] = j;
                k++;
            }
        }

        // Computing distances in parallel.
        Parallel.For(0, distances.Length, K =>
        {
            distances[K] = Vector3.SqrMagnitude(positions[rowIndices[K]] - positions[colIndices[K]]);
        });

        // Calculate KDE with precomputation.
        // Compute partial row-wise and column-wise sums.
        float[] rowSums = new float[n];
        float[] colSums = new float[n];
        Parallel.For(0, distances.Length, K =>
        {
            float val = Mathf.Exp(-distances[K] / sigma);
            rowSums[rowIndices[K]] += val;
            colSums[colIndices[K]] += val;
        });

        // Combine partial sums to yield final result.
        List<float> heatmapValues = new List<float>();
        for (int i = 0; i < n; i++)
        {
            heatmapValues.Add(rowSums[i] + colSums[i]);
        }

        // Normalize results.
        float min = heatmapValues.Min();
        float max = heatmapValues.Max();
        float new_range = max - min;
        Parallel.For(0, n, i =>
        {
            heatmapValues[i] = (heatmapValues[i] - min) / new_range;
        });

        return heatmapValues;
    }

    public static void SetupParticleSystem(
        ParticleSystem particleSystem,
        List<Vector3> particlePositions,
        List<float> heatmapValues,
        Gradient heatmapGradient,
        float particleSize)
    {
        var partSysMain = particleSystem.main;
        partSysMain.maxParticles = particlePositions.Count;
        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[particlePositions.Count];
        particleSystem.GetParticles(particles, particles.Length, 0);
        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].position = particlePositions[i];
            particles[i].velocity = Vector3.zero;
            particles[i].startSize = particleSize;
            particles[i].startColor = heatmapGradient.Evaluate(heatmapValues[i]);
        }
        particleSystem.SetParticles(particles, particles.Length, 0);
    }

    public static void AddTMProLabel(
        string titleText,
        float fontSize,
        Vector3 constantDisplacement,
        Vector3 displacement)
    {
        GameObject titleTextGO = new GameObject("LabelText");
        titleTextGO.hideFlags = HideFlags.HideInHierarchy;
        TMPro.TextMeshPro textMesh = titleTextGO.AddComponent<TMPro.TextMeshPro>();
        textMesh.text = titleText;
        textMesh.fontSize = fontSize;
        textMesh.transform.position = constantDisplacement + displacement;
        textMesh.transform.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
        textMesh.enableWordWrapping = false;
        textMesh.color = Color.black;
        textMesh.fontStyle = TMPro.FontStyles.Bold;
    }
}
