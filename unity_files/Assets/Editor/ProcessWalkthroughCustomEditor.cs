﻿using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditorInternal;
using System.Linq;
using System;

[CustomEditor(typeof(ProcessWalkthrough))]
public class ProcessWalkthroughCustomEditor : Editor
{
    // This directory contains trajectory data (e.g., created by CaptureWalkthough.cs)
    string defaultRawDataPath = Path.Combine("Data", "VirtualWalkthrough", "Raw");

    // This directory contains processed data (e.g, the value of point-clouds)
    string defaultProcessedDataPath = Path.Combine("Data", "VirtualWalkthrough", "Processed");

    // This directory contains statistics about the trajectory.
    string defaultFinalDataPath = Path.Combine("Data", "VirtualWalkthrough", "Final");
    UnityEditor.AnimatedValues.AnimBool visualizeTrajectoryAnimBool;
    UnityEditor.AnimatedValues.AnimBool visualizeHeatmapAnimBool;
    UnityEditor.AnimatedValues.AnimBool visualizeShortestPathBool;
    UnityEditor.AnimatedValues.AnimBool useQuaternionAnimBool;
    ProcessWalkthrough processor;
    readonly int buttonWidth = 210;

    private void OnEnable()
    {
        processor = (ProcessWalkthrough)target;
        visualizeTrajectoryAnimBool = new UnityEditor.AnimatedValues.AnimBool(processor.visualizeTrajectory);
        visualizeHeatmapAnimBool = new UnityEditor.AnimatedValues.AnimBool(processor.visualizeHeatmap);
        visualizeShortestPathBool = new UnityEditor.AnimatedValues.AnimBool(processor.visualizeShortestPath);
        useQuaternionAnimBool = new UnityEditor.AnimatedValues.AnimBool(processor.useQuaternion);
        useQuaternionAnimBool.valueChanged.AddListener(Repaint);
    }
    public override void OnInspectorGUI()
    {

        EditorGUILayout.Space();

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                                                                                            //
        // File IO                                                                                                    //
        //                                                                                                            //
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        HorizontalSeperator();

        EditorGUILayout.Space();

        GUILayout.Label("File Input/Output", EditorStyles.boldLabel);

        EditorGUILayout.Space();
        
        GUILayout.BeginHorizontal();

        // If a new directory is chosen, we need to force the user to choose a new file name as well.
        if (GUILayout.Button("Choose raw data directory", GUILayout.Width(buttonWidth)))
        {
            string newRawDirectoryName = EditorUtility.OpenFolderPanel("Choose directory containing raw data", "", defaultRawDataPath);

            // Only apply changes if the user has actually chosen a new directory (the returned name is not the empty
            // string) and if the new directory is not the same as the old one.
            if (newRawDirectoryName != "" && newRawDirectoryName != processor.rawDataDirectory)
            {
                processor.rawDataDirectory = newRawDirectoryName;

                // If the not all files in the directory are used, we need to choose a new file name.
                if (!processor.useAllFilesInDirectory)
                {
                    ChooseRawDataFile(Directory.GetFiles(processor.rawDataDirectory)[0]);
                }
            }
        }

        GUILayout.Label(GetPathFromProjectRoot(processor.rawDataDirectory));

        GUILayout.EndHorizontal();

        // If user goes from using all files in the directory back to just using a single file, a prompt appears to 
        // choose a new file.
        EditorGUI.BeginChangeCheck();
        processor.useAllFilesInDirectory = GUILayout.Toggle(processor.useAllFilesInDirectory, "Use all files in raw directory");
        if (EditorGUI.EndChangeCheck())
        {
            if (!processor.useAllFilesInDirectory)
            {
                // The toggle was previously on, but is now switched off. In this case we need to choose a specific file.
                ChooseRawDataFile(Directory.GetFiles(processor.rawDataDirectory)[0]);
            }
        }

        GUILayout.BeginHorizontal();

        // If not all files are chosen, a specific file need to be indicated.
        EditorGUI.BeginDisabledGroup(processor.useAllFilesInDirectory);
        if (GUILayout.Button("Choose raw data file", GUILayout.Width(buttonWidth)))
        {
            ChooseRawDataFile(processor.rawDataFileName);
        }
        SetDerivedDirNames();
        GUILayout.Label(Path.GetFileName(processor.rawDataFileName));
        EditorGUI.EndDisabledGroup();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        GUILayout.Label("Processed data file name: " + GetPathFromProjectRoot(processor.outProcessedDataFileName));

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        GUILayout.Label("Summarized data file name: " + GetPathFromProjectRoot(processor.outSummarizedDataFileName));

        GUILayout.EndHorizontal();

        if (GUILayout.Button("Delete file", GUILayout.Width(buttonWidth)))
        {
            string fileNameToDelete = EditorUtility.OpenFilePanel("Delete file", defaultRawDataPath, "csv");

            if (fileNameToDelete != "")
            {
                FileUtil.DeleteFileOrDirectory(fileNameToDelete);
            }
        }

        GUILayout.Label("Column Names", EditorStyles.boldLabel);
        EditorGUI.indentLevel += 2;
        processor.timeColumnName = EditorGUILayout.TextField("Time", processor.timeColumnName);
        processor.positionXColumnName = EditorGUILayout.TextField("Position X", processor.positionXColumnName);
        processor.positionYColumnName = EditorGUILayout.TextField("Position Y", processor.positionYColumnName);
        processor.positionZColumnName = EditorGUILayout.TextField("Position Z", processor.positionZColumnName);
        processor.useQuaternion = EditorGUILayout.Toggle("Use Quaternion", processor.useQuaternion);
        useQuaternionAnimBool.target = processor.useQuaternion;
        if (EditorGUILayout.BeginFadeGroup(useQuaternionAnimBool.faded))
        {
            processor.quaternionWColumnName = EditorGUILayout.TextField("Rotation X", processor.quaternionWColumnName);
            processor.quaternionXColumnName = EditorGUILayout.TextField("Rotation Y", processor.quaternionXColumnName);
            processor.quaternionYColumnName = EditorGUILayout.TextField("Rotation Z", processor.quaternionYColumnName);
            processor.quaternionZColumnName = EditorGUILayout.TextField("Rotation W", processor.quaternionZColumnName);
        }
        EditorGUILayout.EndFadeGroup();

        if (EditorGUILayout.BeginFadeGroup(1.0f - useQuaternionAnimBool.faded))
        {
            processor.directionXColumnName = EditorGUILayout.TextField("Direction X", processor.directionXColumnName);
            processor.directionYColumnName = EditorGUILayout.TextField("Direction Y", processor.directionYColumnName);
            processor.directionZColumnName = EditorGUILayout.TextField("Direction Z", processor.directionZColumnName);
            processor.upXColumnName = EditorGUILayout.TextField("Up X", processor.upXColumnName);
            processor.upYColumnName = EditorGUILayout.TextField("Up Y", processor.upYColumnName);
            processor.upZColumnName = EditorGUILayout.TextField("Up Z", processor.upZColumnName);
            processor.rightXColumnName = EditorGUILayout.TextField("Right X", processor.rightXColumnName);
            processor.rightYColumnName = EditorGUILayout.TextField("Right Y", processor.rightYColumnName);
            processor.rightZColumnName = EditorGUILayout.TextField("Right Z", processor.rightZColumnName);
        }
        EditorGUILayout.EndFadeGroup();
        processor.multipleTrialsInOneFile = EditorGUILayout.Toggle("Multiple Trials in One File", processor.multipleTrialsInOneFile);
        EditorGUI.BeginDisabledGroup(!processor.multipleTrialsInOneFile);
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("keyColumns"), true);
            serializedObject.ApplyModifiedProperties();
        }
        EditorGUI.EndDisabledGroup();
        EditorGUI.indentLevel -= 2;

        processor.csvDelimiter = EditorGUILayout.TextField("CSV Delimiter", processor.csvDelimiter);

        EditorGUILayout.Space();

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///                                                                                                           //
        /// Data Processing                                                                                           //
        ///                                                                                                           //
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        HorizontalSeperator();

        EditorGUILayout.Space();

        GUILayout.Label("Data Processing", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("filters"), true);
        serializedObject.ApplyModifiedProperties();

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                                                                                            //
        // Visualization                                                                                              //
        //                                                                                                            //
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        HorizontalSeperator();

        EditorGUILayout.Space();

        GUILayout.Label("Visualizations", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        processor.visualizeHeatmap = GUILayout.Toggle(processor.visualizeHeatmap, " Heatmap");
        visualizeHeatmapAnimBool.target = processor.visualizeHeatmap;
        if (EditorGUILayout.BeginFadeGroup(visualizeHeatmapAnimBool.faded))
        {
            EditorGUI.indentLevel += 2;
            processor.reuseHeatmap = EditorGUILayout.ToggleLeft("Use processed data file", processor.reuseHeatmap);
            EditorGUI.BeginDisabledGroup(processor.reuseHeatmap);
            processor.numRaysPerRayCast = EditorGUILayout.IntSlider("Rays per Raycast", processor.numRaysPerRayCast, 1, 200);
            processor.maxNumRays = EditorGUILayout.IntSlider("Max Rays", processor.maxNumRays, 1, 1000000);
            processor.particleSize = EditorGUILayout.Slider("Particle Size", processor.particleSize, 0.1f, 5.0f);
            processor.kernelSize = EditorGUILayout.Slider("Kernel Size", processor.kernelSize, 0.1f, 10.0f);

            EditorGUI.BeginChangeCheck();
            SerializedObject serializedHeatmapGradient = new SerializedObject(target);
            SerializedProperty heatmapGradient = serializedHeatmapGradient.FindProperty("heatmapGradient");
            EditorGUILayout.PropertyField(heatmapGradient, true);
            if (EditorGUI.EndChangeCheck())
            {
                serializedHeatmapGradient.ApplyModifiedProperties();
            }
            LayerMask newMask = EditorGUILayout.MaskField("Heatmap Layers", InternalEditorUtility.LayerMaskToConcatenatedLayersMask(processor.layerMask), InternalEditorUtility.layers);
            processor.layerMask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(newMask);
            EditorGUI.EndDisabledGroup();
            processor.heatmapMaterial = EditorGUILayout.ObjectField("Heatmap Material", processor.heatmapMaterial, typeof(Material), true) as Material;
            EditorGUI.indentLevel -= 2;
        }
        EditorGUILayout.EndFadeGroup();

        EditorGUILayout.Space();

        EditorGUI.BeginDisabledGroup(!processor.generateData);
        processor.visualizeTrajectory = GUILayout.Toggle(processor.visualizeTrajectory, new GUIContent("Trajectory"));
        visualizeTrajectoryAnimBool.target = processor.visualizeTrajectory;
        if (EditorGUILayout.BeginFadeGroup(visualizeTrajectoryAnimBool.faded))
        {
            EditorGUI.indentLevel += 2;

            // Gradient of the trajectory.
            EditorGUI.BeginChangeCheck();
            SerializedObject serializedGradient = new(target);
            SerializedProperty colorGradient = serializedGradient.FindProperty("trajectoryGradient");
            EditorGUILayout.PropertyField(colorGradient, true);
            if (EditorGUI.EndChangeCheck())
            {
                serializedGradient.ApplyModifiedProperties();
            }

            // Width of the trajectory.
            processor.pathWidth = EditorGUILayout.Slider("Trajectory Width", processor.pathWidth, 0.01f, 1.0f);

            // Material of the line renderer.
            processor.lineRendererMaterial = EditorGUILayout.ObjectField("Trajectory Material", processor.lineRendererMaterial, typeof(Material), true) as Material;

            // Set showTrajectoryProgressively field.
            processor.showTrajectoryProgressively = EditorGUILayout.ToggleLeft("Show trajectory progressively", processor.showTrajectoryProgressively);

            EditorGUI.indentLevel += 2;
            EditorGUI.BeginDisabledGroup(!processor.showTrajectoryProgressively);
            {
                // Set trajectoryProgressionSpeed field.
                processor.replayDuration = EditorGUILayout.Slider("Replay speed", processor.replayDuration, 0.01f, 60.0f);
            }
            EditorGUI.EndDisabledGroup();
            EditorGUI.indentLevel -= 2;

            // Should shortest path be visualized?
            processor.visualizeShortestPath = EditorGUILayout.ToggleLeft("Visualize Shortest Path", processor.visualizeShortestPath);
            visualizeShortestPathBool.target = processor.visualizeShortestPath;
            if (EditorGUILayout.BeginFadeGroup(visualizeShortestPathBool.faded))
            {
                EditorGUI.indentLevel += 2;

                // Should the start location of the shortest path inferred automatically ot chosen manually.
                processor.inferStartLocation = EditorGUILayout.ToggleLeft(
                    new GUIContent("Infer start location", "Check this if you want the script to automatically infer where the agent has started."),
                    processor.inferStartLocation
                );

                // Should the end location of the shortest path inferred automatically ot chosen manually.
                processor.inferEndLocation = EditorGUILayout.ToggleLeft(
                    new GUIContent("Infer end location", "Check this if you want the script to automatically infer where the agent has ended."),
                    processor.inferEndLocation
                );

                EditorGUI.BeginDisabledGroup(processor.inferStartLocation);
                {
                    processor.startLocation = EditorGUILayout.ObjectField(
                        new GUIContent("Start", "The gameobject that corresponds to the start"),
                        processor.startLocation, typeof(Transform), true
                    ) as Transform;
                }
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(processor.inferEndLocation);
                {
                    processor.endLocation = EditorGUILayout.ObjectField(
                        new GUIContent("End", "The gameobject that corresponds to the target"),
                        processor.endLocation, typeof(Transform), true
                    ) as Transform;
                }
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginChangeCheck();
                SerializedObject serializedGradient1 = new SerializedObject(target);
                SerializedProperty shortestPathGradient = serializedGradient1.FindProperty("shortestPathGradient");
                EditorGUILayout.PropertyField(shortestPathGradient, true);
                if (EditorGUI.EndChangeCheck())
                {
                    serializedGradient1.ApplyModifiedProperties();
                }

                EditorGUI.indentLevel -= 2;
            }
            EditorGUI.indentLevel -= 2;
            EditorGUILayout.EndFadeGroup();
        }
        EditorGUILayout.EndFadeGroup();
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                                                                                            //
        // Summary                                                                                                    //
        //                                                                                                            //
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        HorizontalSeperator();

        EditorGUILayout.Space();

        EditorGUI.BeginDisabledGroup(!processor.visualizeHeatmap);
        processor.generateSummarizedDataFile = EditorGUILayout.Toggle(
            new GUIContent("Generate Summary", "Enable \"Visualize Heatmap\" to generate summary"),
            processor.generateSummarizedDataFile
        );
        EditorGUI.EndDisabledGroup();
    }

    private void HorizontalSeperator()
    {
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
    }

    string CreateDerivedFileName(string rawFileName, string type, bool useAllFilesInDirectory)
    {
        string rawDirPath = Path.GetDirectoryName(rawFileName);
        string[] rawDirComponents = rawDirPath.Split(Path.DirectorySeparatorChar);
        if (useAllFilesInDirectory)
        {
            return "all_files_in_" + rawDirComponents.Last() + "_" + type + ".csv";
        }
        return Path.GetFileNameWithoutExtension(rawFileName) + "_" + type + ".csv";
    }

    void SetDerivedDirNames()
    {
        Directory.CreateDirectory(defaultProcessedDataPath);
        Directory.CreateDirectory(defaultFinalDataPath);
        processor.outProcessedDataFileName = Path.Combine(
            defaultProcessedDataPath,
            CreateDerivedFileName(
                processor.rawDataFileName,
                "processed",
                processor.useAllFilesInDirectory
            )
        );
        processor.outSummarizedDataFileName = Path.Combine(
            defaultFinalDataPath,
            CreateDerivedFileName(
                processor.rawDataFileName,
                "final",
                processor.useAllFilesInDirectory
            )
        );
    }

    void ChooseRawDataFile(string defaultFileName)
    {
        string newRawDataFileName = EditorUtility.OpenFilePanel("Choose raw data file", processor.rawDataDirectory, "csv");
        if (newRawDataFileName == "")
        {
            // The user has aborted the file-selection process. Revert to old file name.
            newRawDataFileName = defaultFileName;
        }
        processor.rawDataFileName = newRawDataFileName;
    }

    // This assumes that the Unity project is called `unity_files`
    string GetPathFromProjectRoot(string fullPath)
    {
        string[] pathComponents = fullPath.Split(Path.DirectorySeparatorChar);

        // If the pathComponents contain `unity_files`, we want to return the path from the first occurence of `unity_files`.
        if (pathComponents.Contains("unity_files"))
        {
            int unityFilesIndex = Array.IndexOf(pathComponents, "unity_files");
            return Path.Combine(pathComponents.Skip(unityFilesIndex).ToArray());
        }

        // Else the path is outside the unity project, and it's safer to just print the full path.
        return fullPath;
    }
}