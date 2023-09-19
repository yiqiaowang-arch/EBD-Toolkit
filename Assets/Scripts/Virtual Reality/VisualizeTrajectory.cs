using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;

public class VisualizeTrajectory : MonoBehaviour
{
    public string filePath;                         // Input file-path of walkthrough.
    public Gradient trajectoryColor;                // Color of the trajectory.
    public float trajectoryWidth = 0.2f;            // Width of the trajectory.
    public float distance;                          // Distance covered by the agent in the walkthrough.
    private LineRenderer lineRenderer;              // Renderer used to visualize trajectory.
    public Transform startLocation;
    public Transform targetLocation;
    private NavMeshPath shortestPath;
    private float elapsed = 0.0f;
    public float userDistance = 0.0f;
    public float shortestDistance = 0.0f;
    public float deviationFromShortestPath = 0.0f;

    // Start is called before the first frame update
    void Start()
    {
        // Setting up the visualization things.
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.colorGradient = trajectoryColor;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.widthMultiplier = trajectoryWidth;
        List<Vector3> positions = new List<Vector3>();
        string[] lines = File.ReadAllLines(filePath);
        for (int i = 0; i < lines.Length; i+=4) {
            positions.Add(str2Vec(lines[i]));
        }

        for (int i = 1; i < positions.Count; i++) {
            userDistance += Vector3.Distance(positions[i], positions[i - 1]);
        }
        Debug.Log("Distance of " + filePath + ": " + userDistance);
        
        lineRenderer.positionCount = positions.Count;
        lineRenderer.SetPositions(positions.ToArray());

        shortestPath = new NavMeshPath();
        NavMesh.CalculatePath(startLocation.position, targetLocation.position, NavMesh.AllAreas, shortestPath);

        // Update the way to the goal every second.
        for (int i = 0; i < shortestPath.corners.Length - 1; i++)
        {
            shortestDistance += Vector3.Distance(shortestPath.corners[i], shortestPath.corners[i + 1]);
        }

        deviationFromShortestPath = userDistance / shortestDistance;
    }

    // Update is called once per frame
    void Update()
    {
        // Update the way to the goal every second.
        for (int i = 0; i < shortestPath.corners.Length - 1; i++)
        {
            Debug.DrawLine(shortestPath.corners[i], shortestPath.corners[i + 1], Color.red);
        }
    }

     Vector3 str2Vec(string str)
    {
        str = str.Substring(1, str.Length - 2);
        string[] substrs = str.Split(',');
        return new Vector3( float.Parse(substrs[0]), 
                            float.Parse(substrs[1]), 
                            float.Parse(substrs[2]));
    }
}
