using System.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class HeatMapper : MonoBehaviour
{
    public string path;                             // Path to file we read the walkthrough-data from.
    public float horizontalAngle = 90.0f;           // Constraint of field of view along horizontal axis.
    public float verticalAngle = 90.0f;             // Constraint of field of view along vertical axis.
    public float radiusAngle = 45.0f;               // Angle rays are displaced round a single radius.
    public int layers = 3;                          // How many radii do we want.
    public Gradient gradient;                       // Gradient to visualize heatMap.
    public string outDir;                           // Directory the output gets saved in.
    public string outFileName;                      // Filename of the output file.
    public string outFormat;                        // File format of the output file.
    public bool forceWrite;                         // Should you overwrite file if exists.
    public LayerMask layerMask;                     // Define which layers should be taken into account.

    // Data-structure that maps between GameObject and array of #hits for each vertex.
    private Dictionary<GameObject, float[]> go2vertexHits;
    private Dictionary<GameObject, float[]> go2vertexHitsRaw;
    private Dictionary<int, float> layer2hits;      // Maps between layers and hits per layer.
    private System.IO.StreamWriter file;            // Stream to write output.
    private Vector3[] positions;                    // Positions of the walkthrough.
    private Vector3[] directions;                   // Directions of the walkthrough.
    private Vector3[] ups;                          // Up-directions of the walkthrough.
    private Vector3[] rights;                       // Right-directions of the walkthrough.
    private Vector3 maxPos = Vector3.zero;
    private System.IO.StreamWriter hitFile;

    /* Converts string-representation of vector (in format of Vector3.ToString()) to Vector3.
     * @param str       string representation of vector.
     * @out             Vector3 representation of input string.
     */
    Vector3 str2Vec(string str)
    {
        str = str.Substring(1, str.Length - 2);
        string[] substrs = str.Split(',');
        return new Vector3( float.Parse(substrs[0]), 
                            float.Parse(substrs[1]), 
                            float.Parse(substrs[2]));
    }

    void addOrInsert(Dictionary<int, float> dict, int key, float value)
    {
        float current;
        if (dict.TryGetValue(key, out current))
        {
            dict[key] = current + value;
        }
        else
        {
            dict.Add(key, value);
        }
    }

    /* For a ray returns array of rays around this ray corresponding to cone-shaped raycast.
     * @param p         point where view-direction vector starts.
     * @param d         directions of the view.
     * @param r         radius of circle the cone has after one unit.
     * @param l         number of layers that we want to have in the cone (0 just means one ray in the original view-direction)
     * @param a         angle the rays are displaced around a single such layered-cone.
     * @return          array of directions corresponding to the directions of the different rays in the raycast.
     */
    Vector3[] coneRaycast(Vector3 p, Vector3 d, Vector3 up, Vector3 right, float r, int l, float a)
    {
        int raysPerLayer = (int) Mathf.Floor(360 / a);
        float radiusPerLayer = l == 0 ? r : r / (float) l;

        // Array of ray-directions we will return.
        Vector3[] result = new Vector3[1 + l * raysPerLayer];
        int resIdx = 0;

        // For each layer.
        for (int i = 0; i <= layers; i++)
        {
            // At layer 0 we just return the original view-direction (otherwise we get raysPerLayer - 1 duplicate rays on the cone axis).
            if (i == 0)
            {
                result[resIdx] = d;
                // Debug.DrawRay(p, len * d, Color.blue, 120.0f);
                resIdx++;
                continue;
            }

            // For each ray in a layer.
            for (int j = 0; j < raysPerLayer; j++)
            {
                Vector3 yDev = Mathf.Sin(j * a * Mathf.Deg2Rad) * up * radiusPerLayer * i;
                Vector3 xDev = Mathf.Cos(j * a * Mathf.Deg2Rad) * right * radiusPerLayer * i;
                Vector3 pOnRad = p + d + xDev + yDev;
                result[resIdx] = pOnRad - p;
                // Debug.DrawRay(p, len * (pOnRad - p), Color.red, 120.0f);
                resIdx++;
            }
        }
        
        return result;
    }

    /* For each ray, checks if there is collision and updates the ID-#hitsPerVertex data-structure accordingly.
     * @param p         point where ray starts.
     * @param d         direction of ray.
     * @paran mapping   dictionary that maps between IDs and the array which counts the number of hits per vertex.
     * @return          true if there was a collision, false otherwise.
     */
    bool vertHitUpdater(Vector3 p, Vector3 d, Dictionary<GameObject, float[]> vertMapping, Dictionary<GameObject, float[]> vertMappingRaw, Dictionary<int, float> layerMapping, int type)
    {
        // If a hit occurs, this will hold all the information about it.
        RaycastHit hit;

        // Casting the ray and checking for collision.
        if (!Physics.Raycast(p, d, out hit))
            return false;

        
        // The MeshCollider the ray hit. NULL-check.
        MeshCollider meshCollider = hit.collider as MeshCollider;
        if (meshCollider == null || meshCollider.sharedMesh == null)
            return false;

        // Check if it is in the desired layer.
        if (!(layerMask == (layerMask | (1 << meshCollider.gameObject.layer))))
            return false;

        // Since meshes might not be unique, we need to find the ID of the parent GameObject. This is guaranteed to be unique.
        GameObject parentObject = meshCollider.gameObject;

        // Extracting the mesh from the collider.
        Mesh mesh = meshCollider.sharedMesh;
        int[] triangles = mesh.triangles;
        
        // Getting the indices of the vertices participating in triangle that was hit.
        int[] idcs = {  triangles[hit.triangleIndex * 3 + 0],
                        triangles[hit.triangleIndex * 3 + 1],
                        triangles[hit.triangleIndex * 3 + 2]};

        
        Vector3[] vertices = mesh.vertices;
        Vector3[] vtxAtTri = {  vertices[triangles[hit.triangleIndex * 3 + 0]],
                                vertices[triangles[hit.triangleIndex * 3 + 1]],
                                vertices[triangles[hit.triangleIndex * 3 + 2]]};

        // Calculating the area of the current triangle to normalize.
        Vector3 supportVec1 = vtxAtTri[1] - vtxAtTri[0];
        Vector3 supportVec2 = vtxAtTri[2] - vtxAtTri[0];
        float area = Vector3.Cross(supportVec1, supportVec2).magnitude / 2.0f;
        
        /* Check if the Mesh is already a key: 
         * - if yes then increase the hits in the corresponding entries of the value.
         * - else make new entry.
         */
        float[] hitArray;
        if (vertMapping.TryGetValue(parentObject, out hitArray))
        {
            float acc = 0;
            float[] hitRaw = vertMappingRaw[parentObject];
            for (int i = 0; i < hitRaw.Length; i++)
            {
                acc += hitRaw[i];
            }
            // Updating the entries.
            for (int i = 0; i < 3; i++)
            {
                hitArray[idcs[i]] += 1.0f / area;
            }
            for (int i = 0; i < 3; i++)
            {
                hitRaw[idcs[i]] += 1.0f;
            }
            vertMapping[parentObject] = hitArray;
            vertMappingRaw[parentObject] = hitRaw;
            addOrInsert(layer2hits, parentObject.layer, acc + 3);
        }
        else
        {
            hitArray = new float[mesh.vertices.Length];
            float[] hitRaw = new float[mesh.vertices.Length];
            for (int i = 0; i < 3; i++)
            {
                hitArray[idcs[i]] += 1.0f / area;
            }
            for (int i = 0; i < 3; i++)
            {
                hitRaw[idcs[i]] += 1.0f;
            }
            vertMapping.Add(parentObject, hitArray);
            vertMappingRaw.Add(parentObject, hitRaw);
            addOrInsert(layer2hits, parentObject.layer, 3);
        }
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
        path = dirName + Path.DirectorySeparatorChar + fileName + "." + format;
        
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

    // Start is called before the first frame update
    void Start()
    {
        hitFile = new StreamWriter("shittyFile.csv");
        // The radius of the circle after 1 unit.
        float radius = Mathf.Tan(horizontalAngle / 2.0f) / 2.0f;

        // Get the raw data.
        string[] data = File.ReadAllLines(path);

        // Convert into vectors.
        positions = new Vector3[data.Length / 4];
        directions = new Vector3[data.Length / 4];
        ups = new Vector3[data.Length / 4];
        rights = new Vector3[data.Length / 4];
        for (int i = 0; i < data.Length / 4; i++)
        {
            positions[i] = str2Vec(data[4 * i]);
            directions[i] = str2Vec(data[4 * i + 1]);
            ups[i] = str2Vec(data[4 * i + 2]);
            rights[i] = str2Vec(data[4 * i + 3]);
        }

        // Begin to count hits.
        go2vertexHits = new Dictionary<GameObject, float[]>();
        go2vertexHitsRaw = new Dictionary<GameObject, float[]>();
        layer2hits = new Dictionary<int, float>();
        for (int i = 0; i < positions.Length; i++)
        {
            // Cone-shaped raycast.
            Vector3[] rays = coneRaycast(positions[i], directions[i], ups[i], rights[i], radius, layers, radiusAngle);
            
            // For each ray in the raycast count the hits.
            for (int j = 0; j < rays.Length; j++)
            {
                vertHitUpdater(positions[i], rays[j], go2vertexHits, go2vertexHitsRaw, layer2hits, j == 0 ? 0 : 1);
            }
        }

        // Find the maximum amount of hits over all vertices.
        float max = 0;
        float avg = 0;
        float norm = 0;
        foreach (KeyValuePair<GameObject, float[]> kvPair in go2vertexHits)
        {
            GameObject go = kvPair.Key;
            float[] hits = kvPair.Value;
            for (int i = 0; i < hits.Length; i++)
            {
                hitFile.WriteLine(hits[i].ToString());
                avg += hits[i];
                norm++;
                max = hits[i] > max ? hits[i] : max;
                maxPos = hits[i] > max ? go.GetComponent<MeshFilter>().mesh.vertices[i] : maxPos;
            }  
        }
        avg /= norm;
        Debug.Log(avg);
        // Assign vertex color.
        foreach (KeyValuePair<GameObject, float[]> kvPair in go2vertexHits)
        {
            GameObject go = kvPair.Key;
            float[] hits = kvPair.Value;
            Color[] colors = new Color[go.GetComponent<MeshFilter>().mesh.vertices.Length];
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] / max == 1.0f)
                {
                    Debug.Log(max);
                    Debug.Log("YES");
                    Debug.DrawLine(go.GetComponent<MeshFilter>().mesh.vertices[i], go.GetComponent<MeshFilter>().mesh.vertices[i] + 10.0f * Vector3.up, Color.red, 120.0f);
                    colors[i] = Color.red;
                    continue;
                }
                colors[i] = gradient.Evaluate(hits[i] / (avg * 10));
                // colors[i] = new Color((float) i % 2, (float) i % 3, (float) i % 5);
            }
            go.GetComponent<MeshFilter>().mesh.colors = colors;
        }

        // Write the mapping between layer and hits to file.
        // Create directory if does not exist.
        Directory.CreateDirectory(outDir);
        string outPath = forceWrite ? outDir + Path.DirectorySeparatorChar + outFileName + "." + outFormat : makeFileNameUnique(outDir, outFileName, outFormat);
        file = new StreamWriter(path);
        float acc = 0;
        foreach (KeyValuePair<int, float> kvPair in layer2hits)
        {
            acc += kvPair.Value;
        }
        foreach (KeyValuePair<int, float> kvPair in layer2hits)
        {
            file.WriteLine(LayerMask.LayerToName(kvPair.Key) + "," + kvPair.Value / acc);
        }
        file.Close();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(maxPos, 120.0f);
    }
}
