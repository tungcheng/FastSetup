using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

namespace Techies
{
    public class FastFolderSetup
    {
        [MenuItem("Assets/FastSetup/Create Folder Structure from file", true)]
        private static bool ValidateCreateFolderStructure()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            return File.Exists(path) && path.EndsWith(".txt");
        }

        [MenuItem("Assets/FastSetup/Create Folder Structure from file")]
        private static void CreateFolderStructure()
        {
            string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            string fullPath = Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length));

            if (!File.Exists(fullPath))
            {
                Debug.LogError("File not found: " + fullPath);
                return;
            }

            string[] lines = File.ReadAllLines(fullPath);
            Stack<string> folderStack = new Stack<string>();
            int previousIndent = -1;

            foreach (string rawLine in lines)
            {
                string line = rawLine.Replace("\t", "    "); // Replace tabs with 4 spaces
                if (string.IsNullOrWhiteSpace(line)) continue;

                int indent = CountIndentation(line);
                string trimmed = line.TrimStart('-', ' ').Trim();

                if (string.IsNullOrEmpty(trimmed)) continue;

                // Adjust stack based on indentation
                while (indent <= previousIndent && folderStack.Count > 0)
                {
                    folderStack.Pop();
                    previousIndent--;
                }

                string parentPath = folderStack.Count > 0 ? folderStack.Peek() : "Assets";
                string newFolderPath = Path.Combine(parentPath, trimmed);

                if (!AssetDatabase.IsValidFolder(newFolderPath))
                {
                    string folderName = Path.GetFileName(newFolderPath);
                    string parent = Path.GetDirectoryName(newFolderPath).Replace("\\", "/");
                    AssetDatabase.CreateFolder(parent, folderName);
                    Debug.Log("Created folder: " + newFolderPath);
                }

                folderStack.Push(newFolderPath);
                previousIndent = indent;
            }

            AssetDatabase.Refresh();
        }

        private static int CountIndentation(string line)
        {
            int spaceCount = 0;
            foreach (char c in line)
            {
                if (c == ' ') spaceCount++;
                else break;
            }

            // Assuming 2 spaces per indent level (based on your example)
            return spaceCount / 2;
        }
    }
}