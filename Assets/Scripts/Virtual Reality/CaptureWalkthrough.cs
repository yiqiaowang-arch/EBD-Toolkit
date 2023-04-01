/*
DesignMind: A Toolkit for Evidence-Based, Cognitively- Informed and Human-Centered Architectural Design
Copyright (C) 2023  michal Gath-Morad, Christoph Hölscher, Raphaël Baur

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

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;
using UnityEditor;

public class CaptureWalkthrough : MonoBehaviour
{
    public float sampleInterval = 0.1f;             // How many seconds have to pass until new sample is taken.
    public string fileName;                         // Name of file the samples get written to.
    public bool useCustomSubDirectory = false;
    public string directory = "RawData/Default";
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
    private const char csvSep = ';';  

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

        fileName = MakeFileNameUnique(fileName);

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
            WriteRawDataFile();
            EditorApplication.ExitPlaymode();
        }
    }

    private string MakeFileNameUnique(string path)
    {
        // If the file does not exist yet, we can just return the input path.
        if (!File.Exists(path))
        {
            return path;
        }

        string[] splitPath = path.Split('/');
        string pathWithoutFileName = "";
        for (int i = 0; i < splitPath.Length - 1; i++) {
            pathWithoutFileName += splitPath[i] + "/";
        }
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
        string extension = Path.GetExtension(path);
        int wildCard = 0;
        while (File.Exists(pathWithoutFileName + fileNameWithoutExtension + "_" + wildCard.ToString() + extension))
        {
            wildCard++;
        }
        return pathWithoutFileName + fileNameWithoutExtension + "_" + wildCard.ToString() + extension;
    }

    public void WriteRawDataFile()
    {
        using (StreamWriter openFile = new StreamWriter(fileName)) 
        {
            for (int i = 0; i < positions.Count; i++) {

                // Write out time.
                string line = times[i].ToString("F3") + csvSep;

                // Write out coordinates of position.
                Vector3 currPos = positions[i];
                line += currPos.x.ToString("F3") + csvSep;
                line += currPos.y.ToString("F3") + csvSep;
                line += currPos.z.ToString("F3") + csvSep;

                // Write out coordinates of forward direction.
                Vector3 currDir = directions[i];
                line += currDir.x.ToString("F3") + csvSep;
                line += currDir.y.ToString("F3") + csvSep;
                line += currDir.z.ToString("F3") + csvSep;

                // Write out coordinates of up direction.
                Vector3 currUp = ups[i];
                line += currUp.x.ToString("F3") + csvSep;
                line += currUp.y.ToString("F3") + csvSep;
                line += currUp.z.ToString("F3") + csvSep;

                // Write out coordinates of right direction.
                Vector3 currRight = rights[i];
                line += currRight.x.ToString("F3") + csvSep;
                line += currRight.y.ToString("F3") + csvSep;
                line += currRight.z.ToString("F3");

                openFile.WriteLine(line);
            }
        }
    }

    // Need to define this as well in case the trial is ended before the player can reach the end.
    void OnDestroy()
    {
        if (this.enabled)
        {
            WriteRawDataFile();
        }
    }
}
