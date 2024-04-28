using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using System.Globalization;
using EBD;

public class CaptureWalkthrough : MonoBehaviour
{
    public float sampleInterval = 0.1f;             // How many seconds have to pass until new sample is taken.
    public string fileName;                         // Name of file the samples get written to.
    public bool useCustomSubDirectory = false;
    public string dataDirectory;
    public float targetProximity = 1.0f;
    public Transform target;                        // The target to be found.
    public GameObject view;                         // The actual view.
    private List<float> times;                      // Time point at which values were recorded.
    private List<Vector3> positions;                // List of all positions.
    private List<Vector3> directions;               // List of all directions.
    private List<Vector3> ups;                      // Up axis at each sample.
    private List<Vector3> rights;                   // Right axis at each sample.
    private List<float> yAngle;                     // Azimuth.
    private List<float> xAngle;                     // Elevation.
    private List<float> time;                       // Time.
    private float lastSample;                       // The time the last sample was taken.  
    private const string csvSep = ";";

    // Start is called before the first frame update
    void Start()
    {
        if (!Directory.Exists("RawData/Default"))
        {
            Directory.CreateDirectory("RawData/Default");
        }
        // Checking that camera is present.
        bool hasCamera = false;
        for (int i = 0; i < gameObject.transform.childCount; i++)
        {
            if (gameObject.transform.GetChild(i).GetComponent<Camera>() != null)
            {
                view = gameObject.transform.GetChild(i).gameObject;
                hasCamera = true;
            }
        }
        if (!hasCamera)
        {
            throw new System.Exception("The Player Object this component is attached to has no Camera object child. Please use a valid Player.");
        }

        // Initialize the containers of our data.
        times = new List<float>();
        positions = new List<Vector3>();
        directions = new List<Vector3>();
        ups = new List<Vector3>();
        rights = new List<Vector3>();
        yAngle = new List<float>();
        xAngle = new List<float>();
        time = new List<float>();

        fileName = IO.GenerateUniqueFilename(dataDirectory, fileName);

        Debug.Log("Writing raw data to " + fileName);

        // Set the time of the last sample to the moment the game starts.
        lastSample = Time.realtimeSinceStartup;
    }

    // Update is called once per frame
    void Update()
    {
        // Check if we have exceeded the desired interval.
        float currTime = Time.realtimeSinceStartup;
        if (currTime - lastSample > sampleInterval)
        {
            // We have exceed the desired interval and set the current time to be the last time we sampled.
            lastSample = currTime;

            // Sample the current position and direction.
            times.Add(Time.realtimeSinceStartup);
            positions.Add(view.transform.position);
            directions.Add(view.transform.forward);
            ups.Add(view.transform.up);
            rights.Add(view.transform.right);
            yAngle.Add(view.transform.rotation.eulerAngles.y);
            xAngle.Add(view.transform.rotation.eulerAngles.x);
            time.Add(currTime);
        }

        if (Vector3.Distance(gameObject.transform.position, target.transform.position) < targetProximity)
        {
            // Prepare data for CSV.
            (List<string> columnNames, List<List<string>> data) = PrepareDataForCSV();

            // Write data.
            IO.WriteCSV(fileName, columnNames, data, csvSep);
            EditorApplication.ExitPlaymode();
        }
    }

    private (List<string>, List<List<string>>) PrepareDataForCSV()
    {
        // Generate column names.
        List<string> columnNames = new() {
            "Time",
            "PositionX",
            "PositionY",
            "PositionZ",
            "DirectionX",
            "DirectionY",
            "DirectionZ",
            "UpX",
            "UpY",
            "UpZ",
            "RightX",
            "RightY",
            "RightZ",
        };

        // Generate data matrix from the lists.
        List<List<string>> data = new List<List<string>>();
        for (int i = 0; i < positions.Count; i++)
        {
            List<string> row = new List<string>() {
                times[i].ToString("F3", CultureInfo.InvariantCulture),
                positions[i].x.ToString("F3", CultureInfo.InvariantCulture),
                positions[i].y.ToString("F3", CultureInfo.InvariantCulture),
                positions[i].z.ToString("F3", CultureInfo.InvariantCulture),
                directions[i].x.ToString("F3", CultureInfo.InvariantCulture),
                directions[i].y.ToString("F3", CultureInfo.InvariantCulture),
                directions[i].z.ToString("F3", CultureInfo.InvariantCulture),
                ups[i].x.ToString("F3", CultureInfo.InvariantCulture),
                ups[i].y.ToString("F3", CultureInfo.InvariantCulture),
                ups[i].z.ToString("F3", CultureInfo.InvariantCulture),
                rights[i].x.ToString("F3", CultureInfo.InvariantCulture),
                rights[i].y.ToString("F3", CultureInfo.InvariantCulture),
                rights[i].z.ToString("F3", CultureInfo.InvariantCulture),
            };
            data.Add(row);
        }

        return (columnNames, data);
    }

    // Need to define this as well in case the trial is ended before the player can reach the end.
    void OnDestroy()
    {
        if (this.enabled)
        {
            // Prepare data for CSV.
            (List<string> columnNames, List<List<string>> data) = PrepareDataForCSV();

            // Write data.
            IO.WriteCSV(fileName, columnNames, data, csvSep);
        }
    }
}
