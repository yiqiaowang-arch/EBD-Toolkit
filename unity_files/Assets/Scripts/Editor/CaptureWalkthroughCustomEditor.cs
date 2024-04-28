using UnityEngine;
using UnityEditor;
using System.IO;
using EBD;

[CustomEditor(typeof(CaptureWalkthrough))]
public class CaptureWalkthroughCustomEditor : Editor
{
    CaptureWalkthrough capture;

    public void OnEnable()
    {
        capture = (CaptureWalkthrough) target;
    }
    
    public override void OnInspectorGUI()
    {
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Choose directory"))
        {
            string newRawDirectoryName = EditorUtility.OpenFolderPanel("Choose directory to write data to", "", DefaultPaths.RawDataPath);

            // If the user cancels the action, an empty string will be returned. In that case we do not want to make 
            // any modifications.
            if (newRawDirectoryName != "" && newRawDirectoryName != capture.dataDirectory)
            {
                capture.dataDirectory = newRawDirectoryName;

                // Choose a new file name based on the new directory.
                CustomEditorUtils.ChooseRawDataFile(
                    ref capture.fileName,
                    capture.dataDirectory,
                    Path.GetFileName(capture.fileName),
                    true
                );
            }
        }

        GUILayout.Label(CustomEditorUtils.GetPathFromProjectRoot(capture.dataDirectory));

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Choose file name")) 
        {
            CustomEditorUtils.ChooseRawDataFile(
                ref capture.fileName,
                capture.dataDirectory,
                Path.GetFileName(capture.fileName),
                true
            );
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