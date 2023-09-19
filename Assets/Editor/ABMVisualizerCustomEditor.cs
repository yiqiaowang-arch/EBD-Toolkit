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

    void OnEnable()
    {
        compareAnimBool = new UnityEditor.AnimatedValues.AnimBool();
        visualizer = (ABMVisualizer) target;
        string initialFile = Directory.GetFiles("Data_ABM")[0];
        visualizer.file = visualizer.file == "" ? initialFile : visualizer.file;
        visualizer.fileComp = visualizer.fileComp == "" ? initialFile : visualizer.fileComp;
    }

    public override void OnInspectorGUI()
    {
        HandleAgentTaskSelection(isCompare: false);       

        visualizer.compare = GUILayout.Toggle(visualizer.compare, new GUIContent("Compare"));
        compareAnimBool.target = visualizer.compare;

        if (EditorGUILayout.BeginFadeGroup(compareAnimBool.faded))
        {
            HandleAgentTaskSelection(isCompare: true); 
        }
        EditorGUILayout.EndFadeGroup();

        visualizer.corner1 = EditorGUILayout.ObjectField(
            new GUIContent("Corner 1", "One corner of the rectangular area to visualize"),
            visualizer.corner1,
            typeof(GameObject),
            true
        ) as GameObject;

        visualizer.corner2 = EditorGUILayout.ObjectField(
            new GUIContent("Corner 2", "Other corner of the rectangular area to visualize"),
            visualizer.corner2,
            typeof(GameObject),
            true
        ) as GameObject;

        visualizer.resolution = EditorGUILayout.Slider(
            new GUIContent("Pixel Size", "Size of a single pixel. The lower, the higher the resolution of the heatmap."),
            visualizer.resolution,
            0.05f,
            1.0f
        );

        visualizer.smoothness = EditorGUILayout.Slider(
            new GUIContent("Smoothness", "Scale of the kernel density kernel. The larger, the smoother the heatmap will look, but the more detail you will lose."),
            visualizer.smoothness,
            0.1f,
            1.0f
        );

        visualizer.height = EditorGUILayout.Slider(
            new GUIContent("Y Coordinate", "Height of the cutting plane the heatmap will be displayed on"),
            visualizer.height,
            0.0f,
            5.0f
        );

        visualizer.threshold = EditorGUILayout.Slider(
            new GUIContent("Density Threshold", "Density values will be normalized in the range ]0, threshold]"),
            visualizer.threshold,
            0.001f,
            1.0f
        );
    }


    Dictionary<string, HashSet<string>> GetAgentTypesToTasks(string filename, string delim = ";")
    {
        string[] lines = File.ReadAllLines(filename);
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
        if (GUILayout.Button("Choose data file"))
        {
            string fileName = EditorUtility.OpenFilePanel("Choose data file", "Data_ABM", "csv");
            if (fileName != "")
            {
                if (isCompare)
                {
                    visualizer.fileComp = fileName;
                }
                else
                {
                    visualizer.file = fileName;
                }
            }            
        }

        if (isCompare)
        {
            GUILayout.TextField(visualizer.fileComp);
        }
        else
        {
            GUILayout.TextField(visualizer.file);
        }

        Dictionary<string, HashSet<string>> agentTypeToTask = GetAgentTypesToTasks(
            isCompare ? visualizer.fileComp : visualizer.file,
            delim: visualizer.delim
        );
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
        HandleGradient(isCompare ? "gradientComp" : "gradient");
    }

    void HandleGradient(string gradientName)
    {
        EditorGUI.BeginChangeCheck();
        SerializedObject serializedHeatmapGradient = new SerializedObject(visualizer);
        SerializedProperty heatmapGradient = serializedHeatmapGradient.FindProperty(gradientName);
        EditorGUILayout.PropertyField(heatmapGradient, true);
        if (EditorGUI.EndChangeCheck())
        {
            serializedHeatmapGradient.ApplyModifiedProperties();
        }
    }
}
