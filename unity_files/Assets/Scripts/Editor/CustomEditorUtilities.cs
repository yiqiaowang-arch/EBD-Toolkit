using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditorInternal;
using System.Linq;
using System;
using EBD;

namespace EBD
{
    public static class CustomEditorUtils
    {
        public static void ChooseRawDataFile(ref string directory, string defaultFileName)
        {
            string newRawDataFileName = EditorUtility.OpenFilePanel("Choose raw data file", directory, "csv");
            if (newRawDataFileName == "")
            {
                // The user has aborted the file-selection process. Revert to old file name.
                newRawDataFileName = defaultFileName;
            }
            directory = newRawDataFileName;
        }

        // This assumes that the Unity project is called `unity_files`
        public static string GetPathFromProjectRoot(string fullPath)
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
}