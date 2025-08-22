using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Techies
{
    public class FastTemplateSetup
    {
        [MenuItem("Assets/FastSetup/Copy script templates folder", true)]
        private static bool ValidateCopyScriptTemplatesFolder()
        {
            return Directory.Exists(GetSelectObjectPath());
        }

        static string GetSelectObjectPath()
        {
            string projectPath = Directory.GetCurrentDirectory();
            string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            string fullPath = Path.Combine(projectPath, assetPath);
            return fullPath;
        }

        [MenuItem("Assets/FastSetup/Copy script templates folder")]
        public static void CopyScriptTemplatesFolder()
        {
            string projectPath = Directory.GetCurrentDirectory();
            string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            string sourcePath = Path.Combine(projectPath, assetPath);
            string destinationPath = Path.Combine(projectPath, "Assets", "ScriptTemplates");

            CopyFolder(sourcePath, destinationPath, true);

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog
            (
                "Success",
                "The folder has been copy to Assets/ScriptTemplates folder.\n" +
                "Unity will be closed to reload the templates.\n" + 
                "If it doesn’t, try closing and reopening the project.",
                "OK"
            );

            EditorApplication.Exit(0);
        }

        static void CopyFolder(string sourcePath, string destinationPath, bool overwrite = true)
        {
            try
            {
                // Check if source directory exists
                if (!Directory.Exists(sourcePath))
                {
                    throw new DirectoryNotFoundException($"Source directory not found: {sourcePath}");
                }

                // Create destination directory if it doesn't exist
                if (!Directory.Exists(destinationPath))
                {
                    Directory.CreateDirectory(destinationPath);
                }

                // Copy all files in the source directory
                string[] files = Directory.GetFiles(sourcePath);
                foreach (string file in files)
                {
                    string fileName = Path.GetFileName(file);
                    string destFile = Path.Combine(destinationPath, fileName);
                    File.Copy(file, destFile, overwrite);
                }

                // Recursively copy all subdirectories
                string[] directories = Directory.GetDirectories(sourcePath);
                foreach (string directory in directories)
                {
                    string dirName = Path.GetFileName(directory);
                    string destDir = Path.Combine(destinationPath, dirName);
                    CopyFolder(directory, destDir, overwrite);
                }

                Debug.Log($"Successfully copied folder from {sourcePath} to {destinationPath}");
            }
            catch (Exception ex)
            {
                Debug.Log($"Error copying folder: {ex.Message}");
                throw;
            }
        }
    }
}