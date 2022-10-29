using UnityEngine;
using UnityEditor;
using System.IO;

[CustomEditor(typeof(CaptureWalkthrough))]
public class CaptureWalkthroughCustomEditor : Editor
{
    CaptureWalkthrough capture;
    public override void OnInspectorGUI()
    {
        CaptureWalkthrough capture = (CaptureWalkthrough) target;

        EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Choose subdirectory"))
        {
            string directoryName = EditorUtility.OpenFolderPanel("Choose subdirectory", "RawData", "Default");

            // If the user cancels the action, an empty string will be returned. In that case we do not want to make 
            // any modifications.
            if (directoryName != "")
            {
                capture.directory = directoryName;
            }
        }

        string[] splitDirectories = capture.directory.Split('/');
        GUILayout.Label(splitDirectories[splitDirectories.Length - 2] + '/' + splitDirectories[splitDirectories.Length -1]);

        EditorGUILayout.EndHorizontal();

        if (EditorGUI.EndChangeCheck())
        {
            // Retrigger file name choice if directory was changed.
            string fileName = capture.directory + "/" + Path.GetFileName(capture.fileName);
            
            // If the user cancels the action, an empty string will be returned. In that case we do not want to make 
            // any modifications.
            if (fileName != "")
            {
                capture.fileName = fileName;
            }
        }

        EditorGUILayout.Space();

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Choose (base) file name")) 
        {
            string fileName = EditorUtility.SaveFilePanel("Select file name", capture.directory, "capture", "csv");
            
            // If the user cancels the action, an empty string will be returned. In that case we do not want to make 
            // any modifications.
            if (fileName != "")
            {
                capture.fileName = fileName;
            }
        }
        GUILayout.Label(Path.GetFileName(capture.fileName));

        GUILayout.EndHorizontal();

        EditorGUILayout.Space();

        if (GUILayout.Button("Delete file"))
        {
            string fileNameToDelete = EditorUtility.OpenFilePanel("Delete file", "RawData", "csv");
            if (fileNameToDelete != "")
            {
                FileUtil.DeleteFileOrDirectory(fileNameToDelete);
            }
        }

        EditorGUILayout.Space();

        capture.sampleInterval = EditorGUILayout.Slider("Sample Interval", capture.sampleInterval, 0.05f, 1.0f);

        capture.target = EditorGUILayout.ObjectField(
            new GUIContent("Target", "The Game Object that the player needs to find"),
            capture.target,
            typeof(Transform),
            true
        ) as Transform;

        capture.targetProximity = EditorGUILayout.Slider(
            new GUIContent("Target proximity", "How close has the player have to get such that the task is solved"), 
            capture.targetProximity, 
            0.5f, 
            2.0f
        );

        // Stop gamemode if user is close enough to target.
        if (Vector3.Distance(capture.gameObject.transform.position, capture.target.position) < capture.targetProximity)
        {
            capture.WriteRawDataFile();
            EditorApplication.isPlaying = false;
        }
    }
}
