using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.AI;

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
    private float interSubconeDisplacement;
    private float intraSubconeDisplacementAngle;

    // Visualization-related public variables.
    public float particleSize = 1.0f;
    public float h = 1.0f;

    public bool useAllFilesInDirectory = false;
    public string rawDataDirectory = "VR_Data/Default";
    public string rawDataFileName = "VR_Data/Default/default.csv";
    public string outProcessedDataFileName;
    public string outSummarizedDataFileName;
    public string inProcessedDataFileName;
    public string inSummarizedDataFileName;
    private List<Vector3> hits = new List<Vector3>();
    private float[] colors;
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
    private List<Vector3[]> trajectoryPositions;
    private List<Vector3[]> trajectoryForwardDirections;
    private List<Vector3[]> trajectoryUpDirections;
    private List<Vector3[]> trajectoryRightDirections;
    private List<float[]> trajectoryTimes;
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
    private char csvSep = ';';
    public bool generateSummarizedDataFile;
    private string prec = "F3";

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
        outerConeRadiusHorizontal = Mathf.Tan((horizontalViewAngle / 2.0f) * Mathf.Deg2Rad);
        outerConeRadiusVertical = Mathf.Tan((verticalViewAngle / 2.0f) * Mathf.Deg2Rad);
        hitsPerLayer = new List<int[]>();
        trajectoryTimes = new List<float[]>();
        trajectoryPositions = new List<Vector3[]>();
        trajectoryForwardDirections = new List<Vector3[]>();
        trajectoryUpDirections = new List<Vector3[]>();
        trajectoryRightDirections = new List<Vector3[]>();

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

        // TODO: Remove after debug.
        foreach (string fileName in rawDataFileNames)
            Debug.Log(fileName);

        // Parse each file and populate the positions and direction arrays.
        foreach (string fileName in rawDataFileNames)
        {
            (float[], Vector3[], Vector3[], Vector3[], Vector3[]) parsedData = ReadRawDataFile(fileName);
            trajectoryTimes.Add(parsedData.Item1);
            trajectoryPositions.Add(parsedData.Item2);
            trajectoryForwardDirections.Add(parsedData.Item3);
            trajectoryUpDirections.Add(parsedData.Item4);
            trajectoryRightDirections.Add(parsedData.Item5);
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
            CreateParticles();
        }
        if (visualizeTrajectory)
        {
            foreach (Vector3[] currPositions in trajectoryPositions)
            {
                lineRendererParent = new GameObject();
                lineRendererParent.hideFlags = HideFlags.HideInHierarchy;
                lineRenderer = lineRendererParent.AddComponent<LineRenderer>();
                VisualizeTrajectory(lineRenderer, new List<Vector3>(currPositions), trajectoryGradient, pathWidth);
                if (visualizeShortestPath)
                {
                    Vector3 startPos = inferStartLocation ? currPositions[0] : startLocation.position;
                    Vector3 endPos = endLocation.position;

                    // startPos and endPos do not necessarily lie on the NavMesh. Finding path between them might fail.
                    NavMeshHit startHit;
                    NavMesh.SamplePosition(startPos, out startHit, 100.0f, NavMesh.AllAreas);  // Hardcoded to 100 units of maximal distance.
                    startPos = startHit.position;
                    NavMeshHit endHit;
                    NavMesh.SamplePosition(endPos, out endHit, 100.0f, NavMesh.AllAreas);
                    endPos = endHit.position;

                    // Creating linerenderer for shortest path.
                    shortestPathLinerendererParent = new GameObject();
                    shortestPathLinerendererParent.hideFlags = HideFlags.HideInHierarchy;
                    shortestPathLinerenderer = shortestPathLinerendererParent.AddComponent<LineRenderer>();

                    // Create shortest path.
                    NavMeshPath navMeshPath = new NavMeshPath();
                    NavMesh.CalculatePath(startPos, endPos, NavMesh.AllAreas, navMeshPath);
                    VisualizeTrajectory(shortestPathLinerenderer, new List<Vector3>(navMeshPath.corners), shortestPathGradient, pathWidth);
                }
            }
        }

        if (generateSummarizedDataFile)
        {
            WriteSummarizedDataFile();
        }
    }

    /* Converts string-representation of vector (in format of Vector3.ToString()) to Vector3.
     * @param str       string representation of vector.
     * @out             Vector3 representation of input string.
     */
    Vector3 str2Vec(string str)
    {
        str = str.Substring(1, str.Length - 2);
        string[] substrs = str.Split(csvSep);
        return new Vector3( float.Parse(substrs[0]),
                            float.Parse(substrs[1]),
                            float.Parse(substrs[2]));
    }

    /* Returns a set points corresponding to collisions of rays from a cone-shaped raycast.
     * @param viewPoint         Vector corresponding to the view-point.
     * @param forward           Vector corresponging to the direction of sight.
     * @param vertical          Vector corresponding to the vertical axis of the cone.
     * @param horizontal        Vector corresponding to the horizontal axis of the cone.
     * @return                  List of vectors corresponding to the collisions of the cone-raycast with the environment.
     */
    private List<Vector3> CastAndCollide(Vector3 viewPoint, Vector3 forward, Vector3 vertical, Vector3 horizontal, ref int[] hitsPerLayer)
    {
        Vector3 hitPos = Vector3.zero;
        List<Vector3> results = new List<Vector3>();
        for (int i = 0; i < raysPerRaycast; i++)
        {
            Vector3 p = viewPoint
                        + forward
                        + vertical * Random.value * outerConeRadiusVertical * Mathf.Sin(2.0f * Mathf.PI * Random.value)
                        + horizontal * Random.value * outerConeRadiusHorizontal * Mathf.Sin(2.0f * Mathf.PI * Random.value);
            if (Collision(viewPoint, p - viewPoint, ref hitPos, ref hitsPerLayer))
            {
                results.Add(hitPos);
            }
        }

        return results;
    }

    bool Collision(Vector3 start, Vector3 dir, ref Vector3 hitPos, ref int[] hitsPerLayer)
    {
        // If a hit occurs, this will hold all the information about it.
        RaycastHit hit;

        // Casting the ray and checking for collision.
        if (!Physics.Raycast(start, dir, out hit))
        {
            return false;
        }


        // The MeshCollider the ray hit. NULL-check.
        MeshCollider meshCollider = hit.collider as MeshCollider;
        if (meshCollider == null || meshCollider.sharedMesh == null)
        {
            return false;
        }

        // Check if it is in the desired layer.
        if (!(layerMask == (layerMask | (1 << meshCollider.gameObject.layer))))
        {
            return false;
        }
        hitsPerLayer[meshCollider.gameObject.layer] += 1;
        hitPos = hit.point;
        return true;
    }

    /* Makes sure filename is unique and output directory exists.
     * @param dirName       Name of directory.
     * @param fileName      Proposed name of file.
     * @param format        Format of file.
     * @out                 Unique file-name.
     */
    string makeFileNameUnique(string dirName, string fileName, string format)
    {
        // Create directory if does not exist.
        Directory.CreateDirectory(dirName);

        // This is the path the file will be written to.
        string path = dirName + Path.DirectorySeparatorChar + fileName + "." + format;

        // Check if specified file exists yet and if user wants to overwrite.
        if (File.Exists(path))
        {
            /* In this case we need to make the filename unique.
             * We will achiece that by:
             * foldername + sep + filename + . + format -> foldername + sep + filename + _x + . format
             * x will be increased in case of multiple overwrites.
             */

            // Check if there was a previous overwrite and get highest identifier.
            int id = 0;
            while (File.Exists(dirName + Path.DirectorySeparatorChar + fileName + "_" + id.ToString() + "." + format))
            {
                id++;
            }

            // Now we have found a unique identifier and create the new name.
            path = dirName + Path.DirectorySeparatorChar + fileName + "_" + id.ToString() + "." + format;
        }
        return path;
    }

    void CreateParticles()
    {
        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[particlePositions.Length];
        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].position = particlePositions[i];
            particles[i].velocity = Vector3.zero;
            particles[i].size = particleSize;
            particles[i].color = heatmapGradient.Evaluate(colors[i]);
        }
        ParticleSystem partSys = GetComponent<ParticleSystem>();
        partSys.SetParticles(particles, particles.Length);
    }

    void Update()
    {
        if (visualizeTrajectory)
        {
            /*
            VisualizeTrajectory(lineRenderer, new List<Vector3>(trajectoryPositions), trajectoryGradient, pathWidth);
            */
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

    private (float[], Vector3[], Vector3[], Vector3[], Vector3[]) ReadRawDataFile(string rawDataFileName)
    {
        // Reading in the data from a walkthough.
        string[] data = File.ReadAllLines(rawDataFileName);

        Vector3[] positions = new Vector3[data.Length];
        Vector3[] forwardDirections = new Vector3[data.Length];
        Vector3[] upDirections = new Vector3[data.Length];
        Vector3[] rightDirections = new Vector3[data.Length];
        float[] times = new float[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            // Split string at comma.
            try {
              string[] splitLine = data[i].Split(csvSep);
              times[i] = float.Parse(splitLine[0]);
              positions[i] = new Vector3(float.Parse(splitLine[1]), float.Parse(splitLine[2]), float.Parse(splitLine[3]));
              forwardDirections[i] = new Vector3(float.Parse(splitLine[4]), float.Parse(splitLine[5]), float.Parse(splitLine[6]));
              upDirections[i] = new Vector3(float.Parse(splitLine[7]), float.Parse(splitLine[8]), float.Parse(splitLine[9]));
              rightDirections[i] = new Vector3(float.Parse(splitLine[10]), float.Parse(splitLine[11]), float.Parse(splitLine[12]));
            }
            catch {
              continue;
            }

        }
        return (times, positions, forwardDirections, upDirections, rightDirections);
    }

    private void CreateHeatMap()
    {

        // Will hold all the positions where the rays hit.
        hits = new List<Vector3>();

        // Unity generates 32 layers per default.
        hitsPerLayer = new List<int[]>();

        for (int i = 0; i < numFiles; i++)
        {
            Vector3[] currPositions = trajectoryPositions[i];
            Vector3[] currForwardDirections = trajectoryForwardDirections[i];
            Vector3[] currUpDirections = trajectoryUpDirections[i];
            Vector3[] currRightDirections = trajectoryRightDirections[i];
            int[] currHitsPerLayer = new int[32];  // Unity has 32 layers by default.
            for (int j = 0; j < currPositions.Length; j++)
            {
                hits.AddRange(
                    CastAndCollide(
                        currPositions[j],
                        currForwardDirections[j],
                        currUpDirections[j],
                        currRightDirections[j],
                        ref currHitsPerLayer
                    )
                );
            }
            hitsPerLayer.Add(currHitsPerLayer);
        }

        int n = hits.Count;

        // Calculate the distances between each hit.
        List<float> distances = new List<float>();
        for (int i = 0; i < n; i++)
        {
            for (int j = i + 1; j < n; j++)
            {
                distances.Add(Vector3.Distance(hits[j], hits[i]));
            }
        }

        float[] avgDistances = new float[hits.Count];
        for (int i = 0; i < n; i++)
        {
            float avg = 0;
            int offset = 0;
            int C = 0;
            for (int j = 1; j <= i; j++)
            {
                avg += Mathf.Exp(-distances[offset + i - j] / h);
                offset += n - j;
                C++;
            }
            for (int j = 0; j < n - i - 1; j++)
            {
                avg += Mathf.Exp(-distances[offset + j] / h);
                C++;
            }
            avgDistances[i] = avg / (C * h);
        }
        float min = float.MaxValue;
        float max = 0.0f;
        for (int i = 0; i < n; i++)
        {
            min = avgDistances[i] < min ? avgDistances[i] : min;
            max = avgDistances[i] > max ? avgDistances[i] : max;
        }

        float range = max - min;

        colors = new float[hits.Count];
        for (int i = 0; i < hits.Count; i++)
        {
            colors[i] = (avgDistances[i] - min) / range;
        }
        particlePositions = hits.ToArray();
    }

    private void LoadHeatMap()
    {
        // Reading in the heatmap-data from prior processing and creating arrays for positions and colors \in [0, 1].
        string[] allLines = File.ReadAllLines(inProcessedDataFileName);
        particlePositions = new Vector3[allLines.Length];
        colors = new float[allLines.Length];
        for (int i = 0; i < allLines.Length; i++)
        {
            string[] line = allLines[i].Split(csvSep);
            particlePositions[i] = new Vector3(float.Parse(line[0]), float.Parse(line[1]), float.Parse(line[2]));
            colors[i] = float.Parse(line[3]);
        }
    }

    private void WriteProcessedDataFile()
    {
        using (StreamWriter processedDataFile = new StreamWriter(outProcessedDataFileName))
        {
            for (int i = 0; i < hits.Count; i++)
            {
                processedDataFile.WriteLine(hits[i].x + csvSep + hits[i].y +csvSep+ hits[i].z + colors[i]);
            }
        }
    }

    private void WriteSummarizedDataFile()
    {

        // Variables to be written out.
        List<float> durations = new List<float>();
        List<float> distances = new List<float>();
        List<float> averageSpeeds = new List<float>();
        List<float> shortestPathDistances = new List<float>();
        List<float> surplusShortestPaths = new List<float>();
        List<float> ratioShortestPaths = new List<float>();
        List<int> successfuls = new List<int>();
        List<List<float>> viewPercentages = new List<List<float>>();

        for (int i = 0; i < numFiles; i++)
        {
            // Duration of a walkthrough is the temporal difference between the last update step and the first.
            durations.Add(trajectoryTimes[i][trajectoryTimes[i].Length - 1] - trajectoryTimes[i][0]);
            Debug.Log($"Adding time: {trajectoryTimes[i][trajectoryTimes[i].Length - 1] - trajectoryTimes[i][0]}");
        }

        // Distances of user trajectory.
        for (int i = 0; i < numFiles; i++)
        {
            // Add up distances between measures time-points. Note that the resolution at which the time-points are
            // recorded will make a difference.
            float currDistance = 0.0f;
            for (int j = 0; j < trajectoryPositions[i].Length - 1; j++)
            {
                currDistance += Vector3.Distance(trajectoryPositions[i][j], trajectoryPositions[i][j + 1]);
            }
            distances.Add(currDistance);
            Debug.Log($"Adding distance: {currDistance}");
        }

        // Average speeds.
        for (int i = 0; i < numFiles; i++)
        {
            averageSpeeds.Add(distances[i] / durations[i]);
            Debug.Log($"Average speed: {distances[i] / durations[i]}");
        }



        // Shortest path distances.
        for (int i = 0; i < numFiles; i++)
        {
            Vector3[] currPositions = trajectoryPositions[i];
            Vector3 startPos = inferStartLocation ? currPositions[0] : startLocation.position;
            Vector3 endPos = endLocation.position;

            // startPos and endPos do not necessarily lie on the NavMesh. Finding path between them might fail.
            NavMeshHit startHit;
            NavMesh.SamplePosition(startPos, out startHit, 100.0f, NavMesh.AllAreas);  // Hardcoded to 100 units of maximal distance.
            startPos = startHit.position;
            NavMeshHit endHit;
            NavMesh.SamplePosition(endPos, out endHit, 100.0f, NavMesh.AllAreas);
            endPos = endHit.position;

            // Create shortest path.
            NavMeshPath navMeshPath = new NavMeshPath();
            NavMesh.CalculatePath(startPos, endPos, NavMesh.AllAreas, navMeshPath);

            float currDistance = 0.0f;
            for (int j = 0; j < navMeshPath.corners.Length - 1; j++)
            {
                currDistance += Vector3.Distance(navMeshPath.corners[j], navMeshPath.corners[j + 1]);
            }

            shortestPathDistances.Add(currDistance);
        }


        // Surplus distance to shortest path.
        for (int i = 0; i < numFiles; i++)
        {
            surplusShortestPaths.Add(distances[i] - shortestPathDistances[i]);
        }


        // Ratio between user trajectory length and shortest path.
        for (int i = 0; i < numFiles; i++)
        {
            ratioShortestPaths.Add(distances[i] / shortestPathDistances[i]);
        }



        // Whether the run was successful.
        for (int i = 0; i < numFiles; i++)
        {
            if (Vector3.Distance(trajectoryPositions[i][trajectoryPositions[i].Length - 1], endLocation.position) < 2.0f)
            {
                successfuls.Add(1);
            }
            else
            {
                successfuls.Add(0);
            }
        }

        // Percentages of visibility.
        for (int i = 0; i < numFiles; i++)
        {
            int[] currHitsPerLayer = hitsPerLayer[i];

            // Determine the total number of hits.
            int totalHits = 0;
            for (int j = 0; j < currHitsPerLayer.Length; j++)
            {
                Debug.Log(currHitsPerLayer[j]);
                totalHits += currHitsPerLayer[j];
            }

            List<float> currHitPercentages = new List<float>();
            for (int j = 0; j < currHitsPerLayer.Length; j++)
            {
                currHitPercentages.Add((float) currHitsPerLayer[j] / totalHits);
            }
            viewPercentages.Add(currHitPercentages);
        }


        bool isHead = true;  // Indicates whether the current line is a header.
        using (StreamWriter summaryDataFile = new StreamWriter(outSummarizedDataFileName))
        {
            if (isHead)
            {
                // Generate header.
                string header = "RawDataFileName" + csvSep;
                header += "Duration" + csvSep;
                header += "Distance" + csvSep;
                header += "AverageSpeed" + csvSep;
                header += "ShortestPathDistance" + csvSep;
                header += "SurplusShortestPath" + csvSep;
                header += "RatioShortestPath" + csvSep;
                header += "Successful" + csvSep;

                // For each layer, generate a header.
                for (int i = 0; i < hitsPerLayer[0].Length - 1; i++)
                {
                    header += LayerMask.LayerToName(i) + csvSep;
                }

                // Last element should be followed by comma, thus breaking off.
                header += LayerMask.LayerToName(hitsPerLayer[0].Length - 1);
                isHead = false;  // Set head to false, such that head will not be generated in the following iterations.
                summaryDataFile.WriteLine(header);
            }

            // Write data for each file.
            for (int i = 0; i < numFiles; i++)
            {
                // Generate normal line.
                string line = "";
                line += Path.GetFileNameWithoutExtension(rawDataFileNames[i]) + csvSep;
                line += durations[i].ToString(prec) + csvSep;
                line += distances[i].ToString(prec) + csvSep;
                line += averageSpeeds[i].ToString(prec) + csvSep;
                line += shortestPathDistances[i].ToString(prec) + csvSep;
                line += surplusShortestPaths[i].ToString(prec) + csvSep;
                line += ratioShortestPaths[i].ToString(prec) + csvSep;
                line += successfuls[i].ToString(prec) + csvSep;
                for (int j = 0; j < viewPercentages[i].Count - 1; j++)
                {
                    line += viewPercentages[i][j].ToString(prec) + csvSep;
                }
                line += viewPercentages[i][viewPercentages[i].Count - 1].ToString(prec);
                summaryDataFile.WriteLine(line);
            }
        }
    }

    private void VisualizeTrajectory(LineRenderer lineRenderer, List<Vector3> positions, Gradient gradient, float trajectoryWidth)
    {
        lineRenderer.colorGradient = gradient;
        lineRenderer.material = lineRendererMaterial;
        lineRenderer.widthMultiplier = trajectoryWidth;
        lineRenderer.positionCount = positions.Count;
        lineRenderer.SetPositions(positions.ToArray());
    }

    private Mesh CreateTrajectoryMesh(Vector3[] positions, Vector3[] upDirections, Vector3[] rightDirections, int resolution, float thickness)
    {
        int numPos = positions.Length;
        float radIncrement = 2 * Mathf.PI / resolution;

        // For each position in the trajectory, we create a surrounding ring of <resolution> vertices.
        Vector3[] vertices = new Vector3[numPos * resolution];
        Vector3[] normals = new Vector3[numPos * resolution];
        Color[] colors = new Color[numPos * resolution];

        // Calculate maximal distance between timesteps.
        float maxDist = 0.0f;
        for (int i = 0; i < numPos - 1; i++)
        {
            float currDist = Vector3.Distance(positions[i + 1], positions[i]);
            maxDist = currDist > maxDist ? currDist : maxDist;
        }

        Color currColor = Color.black; // Initialize as black.

        for (int i = 0; i < numPos; i++)
        {
            Vector3 currPos = positions[i];
            Vector3 currRight = rightDirections[i];
            Vector3 currUp = upDirections[i];

            if (i >= 1 && i < numPos - 1)
            {
                // Averaged direction of segments adjacent to current position.
                Vector3 lastDir = positions[i] - positions[i - 1];
                Vector3 nextDir = positions[i + 1] - positions[i];
                Vector3 ringPlaneNormal = (lastDir + nextDir) / 2;

                // Project right vector and up vector onto this new plane.
                currRight = Vector3.Normalize(Vector3.ProjectOnPlane(currRight, ringPlaneNormal));
                currUp = Vector3.Normalize(Vector3.ProjectOnPlane(currUp, ringPlaneNormal));
            }

            if (i < numPos - 1)
            {
                currColor = trajectoryGradient.Evaluate(Vector3.Distance(positions[i + 1], positions[i]) / maxDist);
            }

            // The surrounding ring lies on the plane spanned by the up and right vectors.
            for (int j = 0; j < resolution; j++)
            {
                Vector3 vertex = positions[i] + thickness * Mathf.Cos(radIncrement * j) * currRight + thickness * Mathf.Sin(radIncrement * j) * currUp;
                normals[i * resolution + j] = Mathf.Cos(radIncrement * j) * currRight + Mathf.Sin(radIncrement * j) * currUp;
                Debug.DrawRay(vertex, 0.1f * normals[i * resolution + j], Color.blue, 120.0f);
                vertices[i * resolution + j] = vertex;
                colors[i * resolution + j] = currColor;
            }
        }

        // The forward triangles:
        int[] triangles = new int[(numPos - 1) * (resolution - 1) * 2 * 3];
        int triIdx = 0;
        for (int i = 0; i < numPos - 1; i++)
        {
            for (int j = 0; j < resolution - 1; j++)
            {
                triangles[triIdx * 3] = i * resolution + j;
                triangles[triIdx * 3 + 1] = i * resolution + j + 1;
                triangles[triIdx * 3 + 2] = (i + 1) * resolution + j;
                triIdx++;
            }
        }

        // The backward triangles:
        for (int i = 1; i < numPos; i++)
        {
            for (int j = 1; j < resolution; j++)
            {
                triangles[triIdx * 3] = i * resolution + j;
                triangles[triIdx * 3 + 1] = i * resolution + j - 1;
                triangles[triIdx * 3 + 2] = (i - 1) * resolution + j;
                triIdx++;
            }
        }

        Mesh mesh = new Mesh();
        GameObject go = new GameObject();
        go.AddComponent<MeshFilter>();
        go.GetComponent<MeshFilter>().mesh = mesh;
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.normals = normals;
        mesh.colors = colors;
        go.AddComponent<MeshRenderer>();
        return mesh;
    }
}
