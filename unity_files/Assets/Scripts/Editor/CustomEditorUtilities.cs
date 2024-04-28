using UnityEditor;
using System.IO;
using System.Linq;
using System;

namespace EBD
{
    public static class CustomEditorUtils
    {
        public static void ChooseRawDataFile(ref string targetFileName, string directory, string defaultFileName, bool isSave = false)
        {
            string newRawDataFileName;
            if (isSave)
            {
                newRawDataFileName = EditorUtility.SaveFilePanel("Choose raw data file", directory, defaultFileName, "csv");
            }
            else
            {
                newRawDataFileName = EditorUtility.OpenFilePanel("Choose raw data file", directory, "csv");
            }
            if (newRawDataFileName == "")
            {
                // The user has aborted the file-selection process. Revert to old file name.
                newRawDataFileName = defaultFileName;
            }
            targetFileName = newRawDataFileName;
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