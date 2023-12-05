using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.AI;
using System.Linq;
using EBD;
using System.Globalization;

using Trajectory = System.Collections.Generic.List<TrajectoryEntry>;

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
    public int numRaysPerRayCast = 100;
    public int maxNumRays = 1000;
    public int numRayCast = 0;

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
    private List<Vector3> hitPositions = new();
    private List<float> kdeValues;
    private List<int[]> hitsPerLayer;
    public bool visualizeHeatmap = false;
    public bool visualizeTrajectory = false;
    private Vector3[] particlePositions;
    public Gradient trajectoryGradient;
    public Gradient shortestPathGradient;
    public bool visualizeShortestPath = false;
    public bool inferStartLocation = true;
    public bool inferEndLocation = true;
    public Transform startLocation;
    public Transform endLocation;
    private Dictionary<string, Trajectory> trajectories = new();
    public bool reuseHeatmap = false;
    public float pathWidth = 0.1f;
    private GameObject lineRendererParent;
    private Dictionary<string, LineRenderer> lineRenderer = new();
    private GameObject shortestPathLinerendererParent;
    private LineRenderer shortestPathLinerenderer;
    public Material lineRendererMaterial;
    public Material heatmapMaterial;
    private List<string> rawDataFileNames;
    public string csvDelimiter = ",";
    public bool generateSummarizedDataFile;
    private string prec = "F3";
    public bool showTrajectoryProgressively = false;
    public float replayDuration = 10.0f;
    public bool useQuaternion = false;

    // Column names of the file to be parsed.
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
        // The total number of rays cannot be smaller than the number of rays per raycast.
        if (numRaysPerRayCast > maxNumRays)
        {
            Debug.LogError("numRaysPerRayCast must be smaller than maxNumRays.");
        }
        numRayCast = Mathf.CeilToInt((float)maxNumRays / numRaysPerRayCast);

        // If end location

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

        // Parse each file and populate the positions and direction arrays.
        foreach (string fileName in rawDataFileNames)
        {
            (List<string> columnNames, List<List<string>> data) = IO.ReadFromCSV(fileName, separator: csvDelimiter);

            // Check that all required columns are present.
            CheckColumns(columnNames);

            // Key is the trial name.
            foreach (List<string> row in data)
            {
                ParseRow(row, Path.GetFileName(fileName), ref trajectories, columnNames);
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
            foreach (KeyValuePair<string, Trajectory> entry in trajectories)
            {
                List<Vector3> currPositions = entry.Value.Select(x => x.position).ToList();
                List<float> currTimes = entry.Value.Select(x => x.time).ToList();
                lineRendererParent = new GameObject
                {
                    hideFlags = HideFlags.HideInHierarchy
                };
                lineRenderer.Add(entry.Key, lineRendererParent.AddComponent<LineRenderer>());
                Visualization.RenderTrajectory(
                    lineRenderer: lineRenderer[entry.Key],
                    positions: currPositions.ToList(),
                    timesteps: Enumerable.Range(0, currPositions.Count()).Select(i => (float)i).ToList(),
                    progress: 1.0f,
                    gradient: trajectoryGradient,
                    trajectoryWidth: pathWidth,
                    normalizeTime: true,
                    normalizePosition: false
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
                    Visualization.RenderTrajectory(
                        lineRenderer: shortestPathLinerenderer,
                        positions: navMeshPath.corners.ToList(),
                        timesteps: Enumerable.Range(0, navMeshPath.corners.Length).Select(i => (float)i).ToList(),
                        progress: 1.0f,
                        gradient: shortestPathGradient,
                        trajectoryWidth: pathWidth,
                        normalizeTime: true
                    );
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
            foreach (KeyValuePair<string, Trajectory> entry in trajectories)
            {
                List<Vector3> currPositions = entry.Value.Select(x => x.position).ToList();
                List<float> currTimes = entry.Value.Select(x => x.time).ToList();
                Visualization.RenderTrajectory(
                    lineRenderer: lineRenderer[entry.Key],
                    positions: currPositions.ToList(),
                    timesteps: Enumerable.Range(0, currPositions.Count()).Select(i => (float)i).ToList(),
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
        Trajectory trajectory,
        float radiusVertical,
        float radiusHorizontal,
        int numRaysPerRaycast,
        LayerMask layerMask,
        ref int[] hitCountPerLayer)
    {
        List<Vector3> hitPositions = new List<Vector3>();

        // For each trajectory.
        for (int j = 0; j < trajectory.Count; j++)
        {
            hitPositions.AddRange(
                VisualAttention.CastAndCollide(
                    trajectory[j].position,
                    trajectory[j].forwardDirection,
                    trajectory[j].upDirection,
                    trajectory[j].rightDirection,
                    radiusVertical,
                    radiusHorizontal,
                    numRaysPerRaycast,
                    layerMask,
                    ref hitCountPerLayer
                )
            );
        }
        return hitPositions;
    }

    private void CreateHeatMap()
    {
        // Unity generates 32 layers per default.
        hitsPerLayer = new List<int[]>();

        // Subsample the trajectory.

        // Get the total number of positions.
        int totalNumPositions = 0;
        foreach (KeyValuePair<string, Trajectory> entry in trajectories)
        {
            totalNumPositions += entry.Value.Count;
        }

        // This is not stricly uniform sampling, but to ensure that each 
        // trajectory is in the sample we will make sure that there is at least
        // one sample from each trajectory.
        Dictionary<string, int> numSamplesPerTrajectory = new();
        foreach (KeyValuePair<string, Trajectory> entry in trajectories)
        {
            float trajectoryProportion = (float)entry.Value.Count / totalNumPositions;
            int numSamples = Mathf.CeilToInt(trajectoryProportion * numRayCast);
            numSamplesPerTrajectory.Add(entry.Key, numSamples);
        }

        // Sample `maxNumRays` positions uniformly from all the trajectories.
        Dictionary<string, Trajectory> sampledTrajectories = new();
        foreach (KeyValuePair<string, Trajectory> entry in trajectories)
        {
            sampledTrajectories.Add(entry.Key, new Trajectory());
            for (int i = 0; i < numSamplesPerTrajectory[entry.Key]; i++)
            {
                int index = Random.Range(0, entry.Value.Count);
                sampledTrajectories[entry.Key].Add(trajectories[entry.Key][index]);
            }
        }

        foreach (KeyValuePair<string, Trajectory> entry in sampledTrajectories)
        {
            // Compute hit positions for each trajectory.
            int[] currHitsPerLayer = new int[32];
            hitPositions.AddRange(
                ComputeHitPositions(
                    entry.Value,
                    outerConeRadiusVertical,
                    outerConeRadiusHorizontal,
                    numRaysPerRayCast,
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
            string[] line = allLines[i].Split(csvDelimiter);
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
                processedDataFile.WriteLine(hitPositions[i].x + csvDelimiter + hitPositions[i].y + csvDelimiter + hitPositions[i].z + kdeValues[i]);
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

        foreach (KeyValuePair<string, Trajectory> entry in trajectories)
        {
            Trajectory currTrajectory = entry.Value;

            // Duration of a walkthrough is the temporal difference between the last update step and the first.
            durations.Add(entry.Key, currTrajectory.Last().time - currTrajectory.First().time);

            // Add up distances between measures time-points. Note that the resolution at which the time-points are 
            // recorded will make a difference.
            float currDistance = 0.0f;
            for (int j = 0; j < currTrajectory.Count - 1; j++)
            {
                currDistance += Vector3.Distance(currTrajectory[j].position, currTrajectory[j + 1].position);
            }
            distances.Add(entry.Key, currDistance);

            averageSpeeds.Add(entry.Key, distances[entry.Key] / durations[entry.Key]);

            Vector3 startPos = inferStartLocation ? currTrajectory.First().position : startLocation.position;
            Vector3 endPos = inferEndLocation ? currTrajectory.Last().position : endLocation.position;

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

            if (Vector3.Distance(currTrajectory.Last().position, endPos) < 2.0f)
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
        IO.WriteToCSV(outSummarizedDataFileName, columnNames, data, csvDelimiter);
    }

    private void ParseRow(
        List<string> row,
        string trialName,
        ref Dictionary<string, Trajectory> trajectories,
        List<string> columnNames
    )
    {
        if (multipleTrialsInOneFile)
        {
            trialName = row[columnNames.IndexOf(trialColumnName)];
        }

        // If we have not seen the trial before, we create a new trajectroy.
        if (!trajectories.ContainsKey(trialName))
        {
            trajectories.Add(trialName, new Trajectory());
        }

        // Construct a new trajectory entry.
        float currTime = float.Parse(row[columnNames.IndexOf(timeColumnName)], CultureInfo.InvariantCulture);
        Vector3 currPosition = new Vector3(
            float.Parse(row[columnNames.IndexOf(positionXColumnName)], CultureInfo.InvariantCulture),
            float.Parse(row[columnNames.IndexOf(positionYColumnName)], CultureInfo.InvariantCulture),
            float.Parse(row[columnNames.IndexOf(positionZColumnName)], CultureInfo.InvariantCulture)
        );
        Vector3 currForwardDirection;
        Vector3 currUpDirection;
        Vector3 currRightDirection;
        if (useQuaternion)
        {
            Quaternion currQuaternion = new(
                float.Parse(row[columnNames.IndexOf(quaternionWColumnName)], CultureInfo.InvariantCulture),
                float.Parse(row[columnNames.IndexOf(quaternionXColumnName)], CultureInfo.InvariantCulture),
                float.Parse(row[columnNames.IndexOf(quaternionYColumnName)], CultureInfo.InvariantCulture),
                float.Parse(row[columnNames.IndexOf(quaternionZColumnName)], CultureInfo.InvariantCulture)
            );
            currForwardDirection = currQuaternion * Vector3.forward;
            currUpDirection = currQuaternion * Vector3.up;
            currRightDirection = currQuaternion * Vector3.right;
        }
        else
        {
            currForwardDirection = new Vector3(
                float.Parse(row[columnNames.IndexOf(directionXColumnName)], CultureInfo.InvariantCulture),
                float.Parse(row[columnNames.IndexOf(directionYColumnName)], CultureInfo.InvariantCulture),
                float.Parse(row[columnNames.IndexOf(directionZColumnName)], CultureInfo.InvariantCulture)
            );
            currUpDirection = new Vector3(
                float.Parse(row[columnNames.IndexOf(upXColumnName)], CultureInfo.InvariantCulture),
                float.Parse(row[columnNames.IndexOf(upYColumnName)], CultureInfo.InvariantCulture),
                float.Parse(row[columnNames.IndexOf(upZColumnName)], CultureInfo.InvariantCulture)
            );
            currRightDirection = new Vector3(
                float.Parse(row[columnNames.IndexOf(rightXColumnName)], CultureInfo.InvariantCulture),
                float.Parse(row[columnNames.IndexOf(rightYColumnName)], CultureInfo.InvariantCulture),
                float.Parse(row[columnNames.IndexOf(rightZColumnName)], CultureInfo.InvariantCulture)
            );
        }

        // Add the new entry to the trajectory.
        trajectories[trialName].Add(new TrajectoryEntry
        {
            position = currPosition,
            forwardDirection = currForwardDirection,
            upDirection = currUpDirection,
            rightDirection = currRightDirection,
            time = currTime
        });
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
                throw new System.Exception($"Column {requiredColumn} not found in data file. Possible columns are: {string.Join(", ", columns)}");
            }
        }
    }
}

public struct TrajectoryEntry
{
    public Vector3 position;
    public Vector3 forwardDirection;
    public Vector3 upDirection;
    public Vector3 rightDirection;
    public float time;
}