using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;

public class NewVisualizer : MonoBehaviour
{
    public List<string> agentTypeFilter;
    public List<string> taskFilter;
    public int bootstrappingSamples = 100;
    public string file;
    public bool compare = false;
    public string comparisonFile;
    public List<string> comparisonAgentTypeFilter;
    public List<string> comparisonTaskFilter;
    public GameObject corner1;
    public GameObject corner2;
    public float smoothness = 1.0f;
    public Gradient gradient;
    public Gradient comparisonGradient;
    public float resolution;
    public float height;
    public float particleSize;
    public float threshold = 1.0f;
    public string delim = ";";

    private List<Vector3> positionsFlat;
    private List<Vector3> comparisonPositionsFlat;
    private List<List<Vector3>> trajectories;
    private List<List<Vector3>> compareTrajectories;
    private Vector3[,] grid;
    private float[,] vals;
    private float[,] compareVals;

    // Start is called before the first frame update
    void Start()
    {
        if (threshold <= 0.0)
        {
            throw new Exception("Threshold needs to be strictly larger than 0");
        }
        if (smoothness <= 0.0)
        {
            throw new Exception("Smoothness needs to be strictly larger than 0");
        }

        // Removing leading and trailing whitespaces.
        for (int i = 0; i < agentTypeFilter.Count; i++) {
            agentTypeFilter[i] = agentTypeFilter[i].Trim();
        }

        // Removing leading and trailing whitespaces.
        for (int i = 0; i < taskFilter.Count; i++) {
            taskFilter[i] = taskFilter[i].Trim();
        }

        // Removing leading and trailing whitespaces.
        for (int i = 0; i < comparisonAgentTypeFilter.Count; i++) {
            agentTypeFilter[i] = comparisonAgentTypeFilter[i].Trim();
        }

        // Removing leading and trailing whitespaces.
        for (int i = 0; i < comparisonTaskFilter.Count; i++) {
            comparisonTaskFilter[i] = comparisonTaskFilter[i].Trim();
        }

        // Initialize the positions.
        positionsFlat = new List<Vector3>();
        trajectories = new List<List<Vector3>>();
        if (compare) {
            comparisonPositionsFlat = new List<Vector3>();
            compareTrajectories = new List<List<Vector3>>();
        }

        List<Vector3> positions = ReadData(file);

        // Read in lines.
        string[] lines = File.ReadAllLines(file);
        string[] compareLines = File.ReadAllLines(comparisonFile);

        // Determine first column that contains positions.
        int posStartIdx = 0;
        for (; posStartIdx < lines[0].Split(';').Length; posStartIdx++) {
            if (lines[0].Split(';')[posStartIdx].Contains("pos")) {
                break;
            }
        }
        
        // Select lines and generate trajectories.
        for (int l = 1; l < lines.Length; l++) {
            string line = lines[l];
            string[] split = line.Split(';');

            // Initialize trajectory.
            List<Vector3> trajectory = new List<Vector3>();

            // Only select trajectory if it satisfies the filter.
            Debug.Log(split[0] + "_" + agentTypeFilter[0]);
            if (agentTypeFilter.Contains(split[0]) && taskFilter.Contains(split[1])) {
                trajectory = CreateTrajectory(split, posStartIdx);
            }
            trajectories.Add(trajectory);
            positionsFlat.AddRange(trajectory);
        }

        if (compare) {
            for (int l = 0; l < compareLines.Length; l++) {
                string line = compareLines[l];
                string[] split = line.Split(';');

                // Initialize trajectory.
                List<Vector3> compTrajectory = new List<Vector3>();

                // Only select trajectory if it satisfies the filter.
                if (comparisonAgentTypeFilter.Contains(split[0]) && comparisonTaskFilter.Contains(split[1])) {
                    compTrajectory = CreateTrajectory(split, posStartIdx);
                }

                compareTrajectories.Add(compTrajectory);
                comparisonPositionsFlat.AddRange(compTrajectory);
            }
        }

        Debug.Log(positionsFlat.Count);

        // Generate grid. Get dimensions.
        float x_min = Mathf.Min(corner1.transform.position.x, corner2.transform.position.x);
        float x_max = Mathf.Max(corner1.transform.position.x, corner2.transform.position.x);
        float z_min = Mathf.Min(corner1.transform.position.z, corner2.transform.position.z);
        float z_max = Mathf.Max(corner1.transform.position.z, corner2.transform.position.z);
        float x_range = x_max - x_min;
        float z_range = z_max - z_min;
        int x_dim = (int) Mathf.Floor(x_range / resolution);
        int z_dim = (int) Mathf.Floor(z_range / resolution);
        grid = new Vector3[x_dim,z_dim];
        vals = new float[x_dim,z_dim];
        compareVals = new float[x_dim,z_dim];

        // Fill with positions.
        float min_val = float.PositiveInfinity;
        float max_val = float.NegativeInfinity;
        float min_val_comp = float.PositiveInfinity;
        float max_val_comp = float.NegativeInfinity; 
        for (int i = 0; i < x_dim; i++) {
            for (int j = 0; j < z_dim; j++) {
                grid[i,j] = new Vector3(x_min + i * resolution, height, z_min + j * resolution);
                vals[i,j] = bootStrap(grid[i,j]);
                max_val = vals[i,j] > max_val ? vals[i,j] : max_val;
                min_val = vals[i,j] < min_val ? vals[i,j] : min_val;
                if (compare) {
                    compareVals[i,j] = compareBootStrap(grid[i,j]);
                    min_val_comp = compareVals[i,j] < min_val_comp ? compareVals[i,j] : min_val_comp;
                    max_val_comp = compareVals[i,j] > max_val_comp ? compareVals[i,j] : max_val_comp;
                }
            }
        }

        // Thresholding.
        max_val = min_val + (max_val - min_val) * threshold;
        max_val_comp = min_val_comp  + (max_val_comp - min_val_comp) * threshold;


        Vector3[] grid_flat = new Vector3[x_dim * z_dim];
        float[] vals_flat = new float[x_dim * z_dim];
        float[] vals_flat_comp = new float[x_dim * z_dim];
        float range = max_val - min_val;
        float range_comp = max_val_comp - min_val_comp;
        for (int i = 0; i < x_dim; i++) {
            for (int j = 0; j < z_dim; j++) {
                grid_flat[i * z_dim + j] = grid[i,j];
                float threshed_val = Mathf.Clamp(vals[i,j], vals[i,j], max_val);
                vals_flat[i * z_dim + j] = (threshed_val - min_val) / range;
                if (compare) {
                    float threshed_val_comp = Mathf.Clamp(compareVals[i,j], compareVals[i,j], max_val_comp);
                    vals_flat_comp[i * z_dim + j] = (threshed_val_comp - min_val_comp) / range_comp;
                }
            }
        }

        float[] diff_vals = new float[x_dim * z_dim];
        for (int i = 0; i < diff_vals.Length; i++) {
            diff_vals[i] = vals_flat[i] - vals_flat_comp[i];
        }

        if (compare) {
            float min = float.MaxValue;
            float max = float.MinValue;
            foreach (float val in diff_vals) {
                min = val < min ? val : min;
                max = val > max ? val : max;
            }
            for (int i = 0; i < diff_vals.Length; i++) {
                diff_vals[i] = diff_vals[i] < 0.0f ? 0.5f - 0.5f * (diff_vals[i] / min) : 0.5f + 0.5f * (diff_vals[i] / max);
            }
            createParticles(grid_flat, diff_vals, particleSize, comparisonGradient);
        } else {
            createParticles(grid_flat, vals_flat, particleSize, gradient);
        }
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

    private float bootStrap(Vector3 refPos) {

        float res = 0.0f;

        // Check if size of bootstrappingsamples is is >= all samples.
        // In this case we use all the samples.
        if (bootstrappingSamples >= positionsFlat.Count) {
            for (int i = 0; i < positionsFlat.Count; i++) {
                res += Mathf.Exp(-Vector3.Distance(refPos, positionsFlat[i]) / smoothness);
            }
            res /= positionsFlat.Count * smoothness;
        } else {
            for (int i = 0; i < bootstrappingSamples; i++) {
                res += Mathf.Exp(-Vector3.Distance(refPos, positionsFlat[UnityEngine.Random.Range(0, positionsFlat.Count)]) / smoothness);
            }
            res /= bootstrappingSamples * smoothness;
        }
        return res;
    }

    private float compareBootStrap(Vector3 refPos) {
        float res = 0.0f;

        // Check if size of bootstrappingsamples is is >= all samples.
        // In this case we use all the samples.
        if (bootstrappingSamples >= comparisonPositionsFlat.Count) {
            for (int i = 0; i < comparisonPositionsFlat.Count; i++) {
                res += Mathf.Exp(-Vector3.Distance(refPos, comparisonPositionsFlat[i]) / smoothness);
            }
            res /= comparisonPositionsFlat.Count * smoothness;
        } else {
            for (int i = 0; i < bootstrappingSamples; i++) {
                res += Mathf.Exp(-Vector3.Distance(refPos, comparisonPositionsFlat[UnityEngine.Random.Range(0, comparisonPositionsFlat.Count)]) / smoothness);
            }
            res /= bootstrappingSamples * smoothness;
        }
        return res;
    }

    void createParticles(Vector3[] positions, float[] colors, float size, Gradient gradient) {
        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[positions.Length];
        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].position = positions[i];
            particles[i].velocity = Vector3.zero;
            particles[i].size = particleSize;
            particles[i].color = gradient.Evaluate(colors[i]);
        }
        ParticleSystem partSys = GetComponent<ParticleSystem>();
        partSys.SetParticles(particles, particles.Length);
    }

    private void ReadData(string fileName)
    {
        string[] lines = File.ReadAllLines(file);

        // Determine first column that contains position-data.
        int posStartIdx = 0;
        string[] splitHeader = lines[0].Split(delim);
        for (; posStartIdx < splitHeader.Length; posStartIdx++) {
            if (splitHeader[posStartIdx].Contains("pos")) {
                break;
            }
        }

        // Select lines and generate trajectories.
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
            trajectories.Add(trajectory);
            positionsFlat.AddRange(trajectory);
        }
    }
}
