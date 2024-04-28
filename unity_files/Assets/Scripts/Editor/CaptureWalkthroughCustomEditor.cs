using UnityEngine;
using UnityEditor;
using System.IO;
using EBD;

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

        if (GUILayout.Button("Choose directory"))
        {
            string newRawDirectoryName = EditorUtility.OpenFolderPanel("Choose directory to write data to", "", DefaultPaths.RawDataPath);

            // If the user cancels the action, an empty string will be returned. In that case we do not want to make 
            // any modifications.
            if (newRawDirectoryName != "" && newRawDirectoryName != capture.dataDirectory)
            {
                capture.dataDirectory = newRawDirectoryName;
            }
        }

        GUILayout.Label(CustomEditorUtils.GetPathFromProjectRoot(capture.dataDirectory));

        EditorGUILayout.EndHorizontal();

        if (EditorGUI.EndChangeCheck())
        {
            // Retrigger file name choice if directory was changed.
            string fileName = capture.dataDirectory + "/" + Path.GetFileName(capture.fileName);
            
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
            string fileName = EditorUtility.SaveFilePanel("Select file name", capture.dataDirectory, "capture", "csv");
            
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

        capture.sampleInterval = EditorGUILayout.Slider("Sample Interval", capture.sampleInterval, 0.05f, 1.0f);

        capture.target = EditorGUILayout.ObjectField(
            new GUIContent("Target", "The Game Object that the player needs to find"),
            capture.target,
            typeof(Transform),
            true
        ) as Transform;

        capture.targetProximity = EditorGUILayout.Slider(
            new GUIContent("Target proximity", "How close has the player have to get to the target"), 
            capture.targetProximity, 
            0.5f, 
            2.0f
        );
    }
}