/*
DesignMind: A Toolkit for Evidence-Based, Cognitively- Informed and Human-Centered Architectural Design
Copyright (C) 2023 Michal Gath-Morad, Christoph Hölscher, Raphaël Baur

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

using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.AI;
using System.Linq;
using EBD;
using System.Globalization;

public class ProcessWalkthrough : MonoBehaviour
{
    // Public variables.
    public LayerMask layerMask;
    public Gradient heatmapGradient;
    public bool generateData = true;

    // IO-related public variables.
    public string inPathWalkthrough;
    public string inPathHeatmapData;
    public string outDirHeatmap;
    public string outFileNameHeatmap;
    public string outDirStatistic;
    public string outFileNameStatistic;

    // Public variables concerned with the raycast.
    public float horizontalViewAngle = 90.0f;
    public float verticalViewAngle = 60.0f;
    public int raysPerRaycast = 100;

    // Private variables concerned with the raycast.
    private float outerConeRadiusHorizontal;
    private float outerConeRadiusVertical;

    // Visualization-related public variables.
    public float particleSize = 1.0f;
    public float kernelSize = 1.0f;

    public bool useAllFilesInDirectory = false;
    public string rawDataDirectory = "VR_Data/Default";
    public string rawDataFileName = "VR_Data/Default/default.csv";
    public string outProcessedDataFileName;
    public string outSummarizedDataFileName;
    public string inProcessedDataFileName;
    public string inSummarizedDataFileName;
    private List<Vector3> hitPositions = new List<Vector3>();
    private List<float> kdeValues;
    private List<int[]> hitsPerLayer;
    public bool visualizeHeatmap = false;
    public bool visualizeTrajectory = false;
    private Vector3[] particlePositions;
    public Gradient trajectoryGradient;
    public Gradient shortestPathGradient;
    public bool visualizeShortestPath = false;
    public bool inferStartLocation = true;
    public Transform startLocation;
    public Transform endLocation;
    private Dictionary<string, List<Vector3>> trajectoryPositions = new();
    private Dictionary<string, List<Vector3>> trajectoryForwardDirections = new();
    private Dictionary<string, List<Vector3>> trajectoryUpDirections = new();
    private Dictionary<string, List<Vector3>> trajectoryRightDirections = new();
    private Dictionary<string, List<float>> trajectoryTimes = new();
    public bool reuseHeatmap = false;
    public float pathWidth = 0.1f;
    private GameObject lineRendererParent;
    private LineRenderer lineRenderer;
    private GameObject shortestPathLinerendererParent;
    private LineRenderer shortestPathLinerenderer;
    private int numFiles;
    public Material lineRendererMaterial;
    public Material heatmapMaterial;
    private List<string> rawDataFileNames;
    private string csvSep = ";";
    public bool generateSummarizedDataFile;
    private string prec = "F3";
    public bool showTrajectoryProgressively = false;
    public float replayDuration = 10.0f;
    public bool useQuaternion = false;
    public string positionXColumnName = "PositionX";
    public string positionYColumnName = "PositionY";
    public string positionZColumnName = "PositionZ";
    public string directionXColumnName = "DirectionX";
    public string directionYColumnName = "DirectionY";
    public string directionZColumnName = "DirectionZ";
    public string upXColumnName = "UpX";
    public string upYColumnName = "UpY";
    public string upZColumnName = "UpZ";
    public string rightXColumnName = "RightX";
    public string rightYColumnName = "RightY";
    public string rightZColumnName = "RightZ";
    public string timeColumnName = "Time";
    public string quaternionWColumnName = "QuaternionW";
    public string quaternionXColumnName = "QuaternionX";
    public string quaternionYColumnName = "QuaternionY";
    public string quaternionZColumnName = "QuaternionZ";
    public bool multipleTrialsInOneFile = false;
    public string trialColumnName = "Trial";
    void Start()
    {
        if (lineRendererMaterial == null)
        {
            lineRendererMaterial = new Material(Shader.Find("Sprites/Default"));  // Default material for linerenderer.
        }
        if (heatmapMaterial == null)
        {
            heatmapMaterial = new Material(Shader.Find("Particles/Priority Additive (Soft)")); // Default material for heatmap.
        }
        // Set material of particle system.
        gameObject.GetComponent<ParticleSystemRenderer>().material = heatmapMaterial;
        outerConeRadiusHorizontal = Mathf.Tan(horizontalViewAngle / 2.0f * Mathf.Deg2Rad);
        outerConeRadiusVertical = Mathf.Tan(verticalViewAngle / 2.0f * Mathf.Deg2Rad);
        hitsPerLayer = new List<int[]>();

        // Create a list of filenames for the raw data files to be read. If <useAllFilesInDirectory> is false, then this
        // list will consist of only one file. Otherwise all files in that directory will be added.
        rawDataFileNames = new List<string>();
        if (useAllFilesInDirectory)
        {
            // Read in all files in the directory.
            rawDataFileNames = new List<string>(Directory.GetFiles(rawDataDirectory, "*.csv"));
        }
        else
        {
            // Only get single file.
            rawDataFileNames.Add(rawDataFileName);
        }

        numFiles = rawDataFileNames.Count;

        // Parse each file and populate the positions and direction arrays.
        foreach (string fileName in rawDataFileNames)
        {
            (List<string> columnNames, List<List<string>> data) = IO.ReadFromCSV(fileName);

            // Check that all required columns are present.
            CheckColumns(columnNames);

            // Key is the trial name.
            foreach (List<string> row in data)
            {
                ParseRow(
                    row,
                    Path.GetFileName(fileName), // This will be overwritten by participant id if `multipleTrialsInOneFile` is true.
                    ref trajectoryTimes,
                    ref trajectoryPositions,
                    ref trajectoryForwardDirections,
                    ref trajectoryUpDirections,
                    ref trajectoryRightDirections,
                    columnNames
                );
            }
        }

        if (visualizeHeatmap)
        {
            if (reuseHeatmap)
            {
                LoadHeatMap();
            }
            else
            {
                CreateHeatMap();
                WriteProcessedDataFile();
            }
            ParticleSystem particleSystem = GetComponent<ParticleSystem>();
            Visualization.SetupParticleSystem(particleSystem, hitPositions, kdeValues, heatmapGradient, particleSize);
        }
        if (visualizeTrajectory)
        {
            foreach (KeyValuePair<string, List<Vector3>> entry in trajectoryPositions)
            {
                Vector3[] currPositions = entry.Value.ToArray();
                float[] currTimes = trajectoryTimes[entry.Key].ToArray();
                lineRendererParent = new GameObject
                {
                    hideFlags = HideFlags.HideInHierarchy
                };
                lineRenderer = lineRendererParent.AddComponent<LineRenderer>();
                Visualization.RenderTrajectory(
                    lineRenderer: lineRenderer,
                    positions: currPositions.ToList(),
                    timesteps: currTimes.ToList(),
                    progress: 1.0f,
                    gradient: trajectoryGradient,
                    trajectoryWidth: pathWidth,
                    normalizeTime: true
                );
                if (visualizeShortestPath)
                {
                    Vector3 startPos = inferStartLocation ? currPositions[0] : startLocation.position;
                    Vector3 endPos = endLocation.position;

                    // startPos and endPos do not necessarily lie on the NavMesh. Finding path between them might fail.
                    NavMesh.SamplePosition(startPos, out NavMeshHit startHit, 100.0f, NavMesh.AllAreas);  // Hardcoded to 100 units of maximal distance.
                    startPos = startHit.position;
                    NavMesh.SamplePosition(endPos, out NavMeshHit endHit, 100.0f, NavMesh.AllAreas);
                    endPos = endHit.position;

                    // Creating linerenderer for shortest path.
                    shortestPathLinerendererParent = new GameObject
                    {
                        hideFlags = HideFlags.HideInHierarchy
                    };
                    shortestPathLinerenderer = shortestPathLinerendererParent.AddComponent<LineRenderer>();

                    // Create shortest path.
                    NavMeshPath navMeshPath = new NavMeshPath();
                    NavMesh.CalculatePath(startPos, endPos, NavMesh.AllAreas, navMeshPath);
                    /*
                    Visualization.RenderTrajectory(
                        lineRenderer: shortestPathLinerenderer,
                        positions: navMeshPath.corners.ToList(),
                        timesteps: Enumerable.Range(0, navMeshPath.corners.Length).Select(i => (float)i).ToList(),
                        progress: 1.0f,
                        gradient: shortestPathGradient,
                        trajectoryWidth: pathWidth,
                        normalizeTime: true
                    );
                    */
                }
            }
        }

        if (generateSummarizedDataFile)
        {
            WriteSummarizedDataFile();
        }
    }

    void Update()
    {
        if (visualizeTrajectory & showTrajectoryProgressively)
        {
            foreach (KeyValuePair<string, List<Vector3>> entry in trajectoryPositions)
            {
                Vector3[] currPositions = entry.Value.ToArray();
                float[] currTimes = trajectoryTimes[entry.Key].ToArray();
                Visualization.RenderTrajectory(
                    lineRenderer: lineRenderer,
                    positions: currPositions.ToList(),
                    timesteps: currTimes.ToList(),
                    progress: Time.realtimeSinceStartup % replayDuration / replayDuration,
                    gradient: trajectoryGradient,
                    trajectoryWidth: pathWidth,
                    normalizeTime: true
                );
            }
        }
    }

    public string CreateDerivedDataFileName(string rawDataDirectory, string rawDataFileName, string type)
    {
        if (useAllFilesInDirectory)
        {
            string[] splitRawDataDirectory = rawDataDirectory.Split('/');
            return "all_files_in_" + splitRawDataDirectory[splitRawDataDirectory.Length - 1] + "_" + type + ".csv";
        }
        string[] splitRawDir = rawDataDirectory.Split('/');
        return splitRawDir[splitRawDir.Length - 1] + "_" + Path.GetFileNameWithoutExtension(rawDataFileName) + "_" + type + Path.GetExtension(rawDataFileName);
    }

    private List<Vector3> ComputeHitPositions(
        List<Vector3> positions,
        List<Vector3> forwardDirections,
        List<Vector3> upDirections,
        List<Vector3> rightDirections,
        float radiusVertical,
        float radiusHorizontal,
        int numRaysPerRaycast,
        LayerMask layerMask,
        ref int[] hitCountPerLayer)
    {
        List<Vector3> hitPositions = new List<Vector3>();

        // For each trajectory.
        for (int j = 0; j < positions.Count; j++)
        {
            hitPositions.AddRange(
                VisualAttention.CastAndCollide(
                    positions[j],
                    forwardDirections[j],
                    upDirections[j],
                    rightDirections[j],
                    radiusVertical,
                    radiusHorizontal,
                    numRaysPerRaycast,
                    layerMask,
                    ref hitCountPerLayer));
        }
        return hitPositions;
    }

    private void CreateHeatMap()
    {
        // Unity generates 32 layers per default.
        hitsPerLayer = new List<int[]>();

        foreach (KeyValuePair<string, List<Vector3>> entry in trajectoryPositions)
        {
            // Compute hit positions for each trajectory.
            int[] currHitsPerLayer = new int[32];
            hitPositions.AddRange(
                ComputeHitPositions(
                    entry.Value,
                    trajectoryForwardDirections[entry.Key],
                    trajectoryUpDirections[entry.Key],
                    trajectoryRightDirections[entry.Key],
                    outerConeRadiusVertical,
                    outerConeRadiusHorizontal,
                    raysPerRaycast,
                    layerMask,
                    ref currHitsPerLayer
                    )
                );
            hitsPerLayer.Add(currHitsPerLayer);
        }

        kdeValues = Visualization.KernelDensityEstimate(hitPositions, kernelSize);
        particlePositions = hitPositions.ToArray();
    }

    private void LoadHeatMap()
    {
        // Reading in the heatmap-data from prior processing and creating arrays for positions and colors \in [0, 1].
        string[] allLines = File.ReadAllLines(inProcessedDataFileName);
        particlePositions = new Vector3[allLines.Length];
        kdeValues = new List<float>();
        for (int i = 0; i < allLines.Length; i++)
        {
            string[] line = allLines[i].Split(csvSep);
            particlePositions[i] = new Vector3(float.Parse(line[0]), float.Parse(line[1]), float.Parse(line[2]));
            kdeValues[i] = float.Parse(line[3]);
        }
    }

    private void WriteProcessedDataFile()
    {
        using (StreamWriter processedDataFile = new StreamWriter(outProcessedDataFileName))
        {
            for (int i = 0; i < hitPositions.Count; i++)
            {
                processedDataFile.WriteLine(hitPositions[i].x + csvSep + hitPositions[i].y + csvSep + hitPositions[i].z + kdeValues[i]);
            }
        }
    }

    private void WriteSummarizedDataFile()
    {

        // Variables to be written out. One entry per trial id (or file name).
        Dictionary<string, float> durations = new();
        Dictionary<string, float> distances = new();
        Dictionary<string, float> averageSpeeds = new();
        Dictionary<string, float> shortestPathDistances = new();
        Dictionary<string, float> surplusShortestPaths = new();
        Dictionary<string, float> ratioShortestPaths = new();
        Dictionary<string, int> successfuls = new();
        Dictionary<string, List<float>> viewPercentages = new();

        foreach (KeyValuePair<string, List<Vector3>> entry in trajectoryPositions)
        {
            // Duration of a walkthrough is the temporal difference between the last update step and the first.
            durations.Add(entry.Key, trajectoryTimes[entry.Key][trajectoryTimes[entry.Key].Count - 1] - trajectoryTimes[entry.Key][0]);

            // Add up distances between measures time-points. Note that the resolution at which the time-points are 
            // recorded will make a difference.
            float currDistance = 0.0f;
            for (int j = 0; j < trajectoryPositions[entry.Key].Count - 1; j++)
            {
                currDistance += Vector3.Distance(trajectoryPositions[entry.Key][j], trajectoryPositions[entry.Key][j + 1]);
            }
            distances.Add(entry.Key, currDistance);

            averageSpeeds.Add(entry.Key, distances[entry.Key] / durations[entry.Key]);

            Vector3 startPos = inferStartLocation ? entry.Value[0] : startLocation.position;
            Vector3 endPos = endLocation.position;

            // startPos and endPos do not necessarily lie on the NavMesh. Finding path between them might fail.
            NavMesh.SamplePosition(startPos, out NavMeshHit startHit, 100.0f, NavMesh.AllAreas);  // Hardcoded to 100 units of maximal distance.
            startPos = startHit.position;
            NavMesh.SamplePosition(endPos, out NavMeshHit endHit, 100.0f, NavMesh.AllAreas);
            endPos = endHit.position;

            // Create shortest path.
            NavMeshPath navMeshPath = new NavMeshPath();
            NavMesh.CalculatePath(startPos, endPos, NavMesh.AllAreas, navMeshPath);

            float currShortestPathDistance = 0.0f;
            for (int j = 0; j < navMeshPath.corners.Length - 1; j++)
            {
                currShortestPathDistance += Vector3.Distance(navMeshPath.corners[j], navMeshPath.corners[j + 1]);
            }

            shortestPathDistances.Add(entry.Key, currShortestPathDistance);

            surplusShortestPaths.Add(entry.Key, distances[entry.Key] - shortestPathDistances[entry.Key]);

            ratioShortestPaths.Add(entry.Key, distances[entry.Key] / shortestPathDistances[entry.Key]);

            if (Vector3.Distance(trajectoryPositions[entry.Key][trajectoryPositions[entry.Key].Count - 1], endLocation.position) < 2.0f)
            {
                successfuls.Add(entry.Key, 1);
            }
            else
            {
                successfuls.Add(entry.Key, 0);
            }
        }

        List<string> columnNames = new() {
                "TrialID",
                "Duration",
                "Distance",
                "AverageSpeed",
                "ShortestPathDistance",
                "SurplusShortestPath",
                "RatioShortestPath",
                "Successful"
            };
        for (int i = 0; i < hitsPerLayer[0].Length; i++)
        {
            columnNames.Add(LayerMask.LayerToName(i));
        }

        List<List<string>> data = new();
        foreach (KeyValuePair<string, float> entry in durations)
        {
            List<string> row = new() {
                    entry.Key,
                    durations[entry.Key].ToString(prec, CultureInfo.InvariantCulture),
                    distances[entry.Key].ToString(prec, CultureInfo.InvariantCulture),
                    averageSpeeds[entry.Key].ToString(prec, CultureInfo.InvariantCulture),
                    shortestPathDistances[entry.Key].ToString(prec, CultureInfo.InvariantCulture),
                    surplusShortestPaths[entry.Key].ToString(prec, CultureInfo.InvariantCulture),
                    ratioShortestPaths[entry.Key].ToString(prec, CultureInfo.InvariantCulture),
                    successfuls[entry.Key].ToString(prec, CultureInfo.InvariantCulture)
                };
            for (int j = 0; j < hitsPerLayer[0].Length; j++)
            {
                row.Add(hitsPerLayer[0][j].ToString(prec, CultureInfo.InvariantCulture));
            }
            data.Add(row);
        }
        IO.WriteToCSV(outSummarizedDataFileName, columnNames, data, csvSep);
    }

    private void ParseRow(
        List<string> row,
        string trialName,
        ref Dictionary<string, List<float>> times,
        ref Dictionary<string, List<Vector3>> positions,
        ref Dictionary<string, List<Vector3>> forwardDirections,
        ref Dictionary<string, List<Vector3>> upDirections,
        ref Dictionary<string, List<Vector3>> rightDirections,
        List<string> columnNames
    )
    {
        if (multipleTrialsInOneFile)
        {
            trialName = row[columnNames.IndexOf(trialColumnName)];
        }
        if (!times.ContainsKey(trialName))
        {
            times.Add(trialName, new List<float>());
            positions.Add(trialName, new List<Vector3>());
            forwardDirections.Add(trialName, new List<Vector3>());
            upDirections.Add(trialName, new List<Vector3>());
            rightDirections.Add(trialName, new List<Vector3>());
        }
        times[trialName].Add(float.Parse(row[columnNames.IndexOf(timeColumnName)], CultureInfo.InvariantCulture));
        positions[trialName].Add(new Vector3(
            float.Parse(row[columnNames.IndexOf(positionXColumnName)], CultureInfo.InvariantCulture),
            float.Parse(row[columnNames.IndexOf(positionYColumnName)], CultureInfo.InvariantCulture),
            float.Parse(row[columnNames.IndexOf(positionZColumnName)], CultureInfo.InvariantCulture)
        ));
        if (useQuaternion)
        {
            Quaternion currQuaternion = new(
                float.Parse(row[columnNames.IndexOf(quaternionWColumnName)], CultureInfo.InvariantCulture),
                float.Parse(row[columnNames.IndexOf(quaternionXColumnName)], CultureInfo.InvariantCulture),
                float.Parse(row[columnNames.IndexOf(quaternionYColumnName)], CultureInfo.InvariantCulture),
                float.Parse(row[columnNames.IndexOf(quaternionZColumnName)], CultureInfo.InvariantCulture)
            );
            forwardDirections[trialName].Add(currQuaternion * Vector3.forward);
            upDirections[trialName].Add(currQuaternion * Vector3.up);
            rightDirections[trialName].Add(currQuaternion * Vector3.right);
        }
        else
        {
            forwardDirections[trialName].Add(new Vector3(
                float.Parse(row[columnNames.IndexOf(directionXColumnName)], CultureInfo.InvariantCulture),
                float.Parse(row[columnNames.IndexOf(directionYColumnName)], CultureInfo.InvariantCulture),
                float.Parse(row[columnNames.IndexOf(directionZColumnName)], CultureInfo.InvariantCulture)
            ));
            upDirections[trialName].Add(new Vector3(
                float.Parse(row[columnNames.IndexOf(upXColumnName)], CultureInfo.InvariantCulture),
                float.Parse(row[columnNames.IndexOf(upYColumnName)], CultureInfo.InvariantCulture),
                float.Parse(row[columnNames.IndexOf(upZColumnName)], CultureInfo.InvariantCulture)
            ));
            rightDirections[trialName].Add(new Vector3(
                float.Parse(row[columnNames.IndexOf(rightXColumnName)], CultureInfo.InvariantCulture),
                float.Parse(row[columnNames.IndexOf(rightYColumnName)], CultureInfo.InvariantCulture),
                float.Parse(row[columnNames.IndexOf(rightZColumnName)], CultureInfo.InvariantCulture)
            ));
        }
    }

    private void CheckColumns(List<string> columns)
    {
        List<string> requiredColumns = new() {
            positionXColumnName,
            positionYColumnName,
            positionZColumnName,
            timeColumnName,
        };

        if (useQuaternion)
        {
            requiredColumns.AddRange(new List<string> {
                quaternionWColumnName,
                quaternionXColumnName,
                quaternionYColumnName,
                quaternionZColumnName
            });
        }
        else
        {
            requiredColumns.AddRange(new List<string> {
                directionXColumnName,
                directionYColumnName,
                directionZColumnName,
                upXColumnName,
                upYColumnName,
                upZColumnName,
                rightXColumnName,
                rightYColumnName,
                rightZColumnName
            });
        }

        if (multipleTrialsInOneFile)
        {
            requiredColumns.Add(trialColumnName);
        }

        foreach (string requiredColumn in requiredColumns)
        {
            if (!columns.Contains(requiredColumn))
            {
                throw new System.Exception("Column " + requiredColumn + " not found in data file.");
            }
        }
    }
}