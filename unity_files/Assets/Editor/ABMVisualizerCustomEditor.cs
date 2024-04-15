using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(ABMVisualizer))]
public class ABMVisualizerCustomEditor : Editor
{
    ABMVisualizer visualizer;
    UnityEditor.AnimatedValues.AnimBool compareAnimBool;
    Dictionary<string, HashSet<string>> agentTypeToTask;
    Dictionary<string, HashSet<string>> agentTypeToTaskCompare;

    void OnEnable()
    {
        compareAnimBool = new UnityEditor.AnimatedValues.AnimBool();
        visualizer = (ABMVisualizer) target;

        // If the file to read from is either unitialized or the path does not
        // exist anymore, just set the first file in the Data_ABM directory.
        // Else, use the existing file.
        visualizer.fileName = GetValidInitialFileName(visualizer.fileName);
        visualizer.fileNameCompare = GetValidInitialFileName(visualizer.fileNameCompare);
        agentTypeToTask = GetAgentTypesToTasks(
            visualizer.fileName,
            delim: visualizer.delim
        );
        agentTypeToTaskCompare = GetAgentTypesToTasks(
            visualizer.fileNameCompare,
            delim: visualizer.delim
        );
    }

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();

        HandleAgentTaskSelection(isCompare: false); 
        SerializedObject serializedGradient = HandleGradient("gradient");     

        visualizer.compare = GUILayout.Toggle(visualizer.compare, new GUIContent("Compare"));
        compareAnimBool.target = visualizer.compare;

        SerializedObject serializedGradientCompare = null;
        if (EditorGUILayout.BeginFadeGroup(compareAnimBool.faded))
        {
            HandleAgentTaskSelection(isCompare: true);
            serializedGradientCompare = HandleGradient("gradientCompare");
        }
        EditorGUILayout.EndFadeGroup();

        GameObject corner1 = EditorGUILayout.ObjectField(
            new GUIContent("Corner 1", "One corner of the rectangular area to visualize"),
            visualizer.corner1,
            typeof(GameObject),
            true
        ) as GameObject;

        GameObject corner2 = EditorGUILayout.ObjectField(
            new GUIContent("Corner 2", "Other corner of the rectangular area to visualize"),
            visualizer.corner2,
            typeof(GameObject),
            true
        ) as GameObject;

        float resolution = EditorGUILayout.Slider(
            new GUIContent("Pixel Size", "Size of a single pixel. The lower, the higher the resolution of the heatmap."),
            visualizer.resolution,
            0.1f,
            1.0f
        );

        float smoothness = EditorGUILayout.Slider(
            new GUIContent("Smoothness", "Scale of the kernel density kernel. The larger, the smoother the heatmap will look, but the more detail you will lose."),
            visualizer.smoothness,
            0.1f,
            1.0f
        );

        float height = EditorGUILayout.Slider(
            new GUIContent("Y Coordinate", "Height of the cutting plane the heatmap will be displayed on"),
            visualizer.height,
            0.0f,
            5.0f
        );

        float threshold = EditorGUILayout.Slider(
            new GUIContent("Density Threshold", "Density values will be normalized in the range ]0, threshold]"),
            visualizer.threshold,
            0.001f,
            1.0f
        );

        // Set new properties only if there was a change.
        if (EditorGUI.EndChangeCheck())
        {
            Debug.Log("here");
            visualizer.corner1 = corner1;
            visualizer.corner2 = corner2;
            visualizer.resolution = resolution;
            visualizer.smoothness = smoothness;
            visualizer.height = height;
            visualizer.threshold = threshold;
            serializedGradient.ApplyModifiedProperties();
            if (serializedGradientCompare != null) {
                serializedGradientCompare.ApplyModifiedProperties();
            }
        }
    }


    Dictionary<string, HashSet<string>> GetAgentTypesToTasks(string fileName, string delim = ";")
    {
        string[] lines = File.ReadAllLines(fileName);
        Dictionary<string, HashSet<string>> dict = new Dictionary<string, HashSet<string>>();
        foreach (string line in lines.Skip(1))
        {
            string[] split = line.Split(delim);
            string agentType = split[0].Trim();
            string task = split[1].Trim();
            if (!dict.ContainsKey(agentType))
            {
                dict.Add(agentType, new HashSet<string>(new string[] {task}));
            }
            dict[agentType].Add(task);
        }
        return dict;
    }

    List<string> HandlePopup(ref int flags, string[] options, string name)
    {
        flags = EditorGUILayout.MaskField(name, flags, options);
        List<string> selectedOptions = new List<string>();
        for (int i = 0; i < options.Length; i++)
        {
            if ((flags & (1 << i)) == (1 << i) ) selectedOptions.Add(options[i]);
        }
        return selectedOptions;
    }

    void HandleAgentTaskSelection(bool isCompare)
    {
        EditorGUI.BeginChangeCheck();
        if (GUILayout.Button("Choose data file"))
        {
            string fileName = EditorUtility.OpenFilePanel("Choose data file", "Data_ABM", "csv");
            if (fileName != "")
            {
                if (isCompare)
                {
                    visualizer.fileNameCompare = fileName;
                }
                else
                {
                    visualizer.fileName = fileName;
                }
            }         
        }

        if (isCompare)
        {
            GUILayout.TextField(visualizer.fileNameCompare);
        }
        else
        {
            GUILayout.TextField(visualizer.fileName);
        }

        if (EditorGUI.EndChangeCheck())
        {
            Dictionary<string, HashSet<string>> agentTypeToTask = GetAgentTypesToTasks(
                isCompare ? visualizer.fileNameCompare : visualizer.fileName,
                delim: visualizer.delim
            );
        }
        string[] agentOptions = agentTypeToTask.Keys.ToArray();
        List<string> selectedAgents;
        if (isCompare)
        {
            selectedAgents = HandlePopup(ref visualizer.agentFlagsComp, agentOptions, "Agents");
            visualizer.agentTypeFilterComp = selectedAgents;
        }
        else
        {
            selectedAgents = HandlePopup(ref visualizer.agentFlags, agentOptions, "Agents");
            visualizer.agentTypeFilter = selectedAgents;
        }

        if (selectedAgents.Count > 0)
        {
            HashSet<string> taskOptionsSet = new HashSet<string>();
            foreach (string selectedAgent in selectedAgents)
            {
                taskOptionsSet.UnionWith(agentTypeToTask[selectedAgent]);
            }
            string[] taskOptions = taskOptionsSet.ToArray();
            if (isCompare)
            {
                visualizer.taskFilterComp = HandlePopup(ref visualizer.taskFlagsComp, taskOptions, "Tasks");
            }
            else
            {
                visualizer.taskFilter = HandlePopup(ref visualizer.taskFlagsComp, taskOptions, "Tasks");
            }
        }
    }

    SerializedObject HandleGradient(string gradientName)
    {
        SerializedObject serializedHeatmapGradient = new SerializedObject(visualizer);
        SerializedProperty heatmapGradient = serializedHeatmapGradient.FindProperty(gradientName);
        EditorGUILayout.PropertyField(heatmapGradient, true);
        return serializedHeatmapGradient;
    }

    private string GetValidInitialFileName(string currentFileName)
    {
        if (File.Exists(currentFileName))
        {
            return currentFileName;
        }
        string[] generatedFileNames = Directory.GetFiles("Data_ABM");
        if (generatedFileNames.Length == 0)
        {
            throw new System.Exception("You are attempting to visualize ABM data, but no ABM data has been generated. Please run an ABM simulation first.");
        }
        else
        {
            return generatedFileNames[0];
        }
    }
}