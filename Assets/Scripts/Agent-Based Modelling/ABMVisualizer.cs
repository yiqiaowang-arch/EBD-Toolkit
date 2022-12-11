using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;

public class ABMVisualizer : MonoBehaviour
{
    public List<string> agentTypeFilter;
    public List<string> taskFilter;
    public int agentFlags;
    public int taskFlags;
    public string fileName;
    public bool compare = false;
    public string fileNameCompare;
    public List<string> agentTypeFilterComp;
    public List<string> taskFilterComp;
    public int agentFlagsComp;
    public int taskFlagsComp;
    public GameObject corner1;
    public GameObject corner2;
    public float smoothness = 1.0f;
    public Gradient gradient;
    public Gradient gradientCompare;
    public float resolution;
    public float height;
    public float threshold = 1.0f;
    public string delim = ";";
    private float[,,,] _distances; // [x_dim, y_dim, z_dim, n_points]

    // Start is called before the first frame update
    void Start()
    {
        // Removing leading and trailing whitespaces.
        for (int i = 0; i < agentTypeFilter.Count; i++) {
            agentTypeFilter[i] = agentTypeFilter[i].Trim();
        }

        // Removing leading and trailing whitespaces.
        for (int i = 0; i < taskFilter.Count; i++) {
            taskFilter[i] = taskFilter[i].Trim();
        }

        // Removing leading and trailing whitespaces.
        for (int i = 0; i < agentTypeFilterComp.Count; i++) {
            agentTypeFilterComp[i] = agentTypeFilterComp[i].Trim();
        }

        // Removing leading and trailing whitespaces.
        for (int i = 0; i < taskFilterComp.Count; i++) {
            taskFilterComp[i] = taskFilterComp[i].Trim();
        }

        List<Vector3> positions = ReadData(fileName, agentTypeFilter, taskFilter);

        Vector3 minCorner = Vector3.zero;
        Vector3 maxCorner = Vector3.zero;
        Vector3[,] grid = CreateGrid(out minCorner, out maxCorner);
        int xDim = grid.GetLength(0);
        int zDim = grid.GetLength(1);

        // Compute grid-positions.
        for (int i = 0; i < xDim; i++)
        {
            for (int j = 0; j < zDim; j++)
            {
                grid[i, j] = new Vector3(
                    minCorner.x + i * resolution,
                    height,
                    minCorner.z + j * resolution
                );
            }
        }

        float startComputation = Time.realtimeSinceStartup;
        float[,] densities = ComputeDensityMap(positions, grid);
        float endComputation = Time.realtimeSinceStartup;
        Debug.Log("Density Computation: " + (endComputation - startComputation));

        float[,] gradientVals = new float[xDim, zDim];
        if (compare)
        {
            // Reading in comparison data.
            List<Vector3> positionsComp = ReadData(fileNameCompare, agentTypeFilterComp, taskFilterComp);

            // Computing (unnormalized) density values of comparison data.
            float[,] densitiesComp = ComputeDensityMap(positionsComp, grid);

            // Calculating the deltas between the density maps.
            Func<float, float, float> sub = (a, b) => a - b;
            float[,] densityDeltas = EBDMath.BinaryOpElementWise(densities, densitiesComp, sub);

            // Normalize such that either min(diffs) = -1.0 or max(diffs) = 1.0
            (float minDelta, float maxDelta) = EBDMath.MinMax(densityDeltas);
            float absMax = Mathf.Max(Mathf.Abs(minDelta), Mathf.Abs(maxDelta));
            Func<float, float> normalize = (x) => x / absMax;
            float[,] normalized = EBDMath.UnaryOpElementWise(densityDeltas, normalize);

            // Threshold values if required.
            if (threshold < 1.0f)
            {
                Func<float, float> thresh = (x) => Mathf.Abs(x) > threshold ? Mathf.Sign(x) * 1.0f : x / threshold;
                normalized = EBDMath.UnaryOpElementWise(normalized, thresh);
            }

            // Scale and translate such that is in [0, 1].
            Func<float, float> scaleTrans = (x) => x / 2.0f + 0.5f;
            gradientVals = EBDMath.UnaryOpElementWise(normalized, scaleTrans);
        }
        else
        {
            // Normalize values.
            (float minDens, float maxDens) = EBDMath.MinMax(densities);
            float range = maxDens - minDens;
            Func<float, float> normalize = (x) => (x / range);
            gradientVals = EBDMath.UnaryOpElementWise(densities, normalize);
            (float minVal, float maxVal) = EBDMath.MinMax(gradientVals);

            // Threshold values if required.
            if (threshold < 1.0)
            {
                Func<float, float> thresh = (x) => x > threshold ? 1.0f : x / threshold;
                gradientVals = EBDMath.UnaryOpElementWise(gradientVals, thresh);
            }
        }

        Vector3[] gridFlat = EBDMath.Flatten(grid).ToArray();
        float[] gradientValsFlat = EBDMath.Flatten(gradientVals).ToArray();

        (float min, float max) = EBDMath.MinMax(gradientVals);
        float startRender = Time.realtimeSinceStartup;
        CreateParticles(
            gridFlat,
            gradientValsFlat,
            resolution,
            compare ? gradientCompare : gradient
        );
        float endRender = Time.realtimeSinceStartup;
        Debug.Log("Time rendering: " + (endRender - startRender));
    }

    private List<Vector3> CreateTrajectory(string[] str, int startIdx)
    {
        // Determine the largest multiple of 3 that fits.
        int posCount = (str.Length - startIdx) / 3;

        // Build the trajectory.
        List<Vector3> res = new List<Vector3>();
        for (int i = 0; i < posCount; i++) 
        {
            // Index offset by the initial, unnecessary columns.
            int I = startIdx + 3 * i;
            if (str[I] == "NaN") 
            {
                break;
            }
            res.Add(
                new Vector3(
                    float.Parse(str[I], CultureInfo.InvariantCulture),
                    float.Parse(str[I + 1], CultureInfo.InvariantCulture),
                    float.Parse(str[I + 2], CultureInfo.InvariantCulture)
                )
            );
        }
        return res;
    }

    private float ComputeDensity(Vector3 pos, List<Vector3> positions) {
        float res = 0.0f;
        for (int i = 0; i < positions.Count; i++) {
            res += Mathf.Exp(-Vector3.Distance(pos, positions[i]) / smoothness);
        }
        res /= positions.Count * smoothness;
        return res;
    }

    void CreateParticles(Vector3[] positions, float[] colors, float size, Gradient gradient) {
        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[positions.Length];
        Parallel.For(0, particles.Length, i => {
            particles[i].position = positions[i];
            particles[i].velocity = Vector3.zero;
            particles[i].size = size;
            particles[i].color = gradient.Evaluate(colors[i]);
        });
        ParticleSystem partSys = GetComponent<ParticleSystem>();
        partSys.SetParticles(particles, particles.Length);
    }

    private List<Vector3> ReadData(
        string fileName,
        List<string> agentTypeFilter,
        List<string> taskFilter
    )
    {
        string[] lines = File.ReadAllLines(fileName);

        // Determine first column that contains position-data.
        int posStartIdx = 0;
        string[] splitHeader = lines[0].Split(delim);
        for (; posStartIdx < splitHeader.Length; posStartIdx++) {
            if (splitHeader[posStartIdx].Contains("pos")) {
                break;
            }
        }

        // Select lines and generate trajectories.
        List<Vector3> positions = new List<Vector3>();
        foreach (string line in lines)
        {
            string[] split = line.Split(delim);

            // Initialize trajectory.
            List<Vector3> trajectory = new List<Vector3>();

            // Only select trajectory if it satisfies the filter.
            string agentType = split[0];
            string task = split[1];
            if (agentTypeFilter.Contains(agentType) && taskFilter.Contains(task)) {
                trajectory = CreateTrajectory(split, posStartIdx);
            }
            positions.AddRange(trajectory);
        }
        
        return positions;
    }

    Vector3[,] CreateGrid(out Vector3 min, out Vector3 max)
    {
        min.x = Mathf.Min(corner1.transform.position.x, corner2.transform.position.x);
        max.x = Mathf.Max(corner1.transform.position.x, corner2.transform.position.x);
        min.y = Mathf.Min(corner1.transform.position.y, corner2.transform.position.y);
        max.y = Mathf.Max(corner1.transform.position.y, corner2.transform.position.y);
        min.z = Mathf.Min(corner1.transform.position.z, corner2.transform.position.z);
        max.z = Mathf.Max(corner1.transform.position.z, corner2.transform.position.z);
        float xRange = max.x - min.x;
        float zRange = max.z - min.z;
        int xDim = (int) Mathf.Floor(xRange / resolution);
        int zDim = (int) Mathf.Floor(zRange / resolution);
        return new Vector3[xDim, zDim];
    }

    float[,] ComputeDensityMap(List<Vector3> positions, Vector3[,] grid)
    {   
        int xDim = grid.GetLength(0);
        int zDim = grid.GetLength(1);
        
        float[,] densities = new float[xDim, zDim];

        // Compute density values.
        Parallel.For(0, xDim * zDim, k => {
            int i = k / zDim;
            int j = k % zDim;
            densities[i,j] = ComputeDensity(grid[i,j], positions);
        });

        return densities;
    }

    void Update() {
    }
}
