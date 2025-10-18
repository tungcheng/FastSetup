using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Techies
{
    public class FastScriptSetup
    {
        #region Menu Items

        [MenuItem("Assets/FastSetup/Generate Scripts from file", true)]
        private static bool ValidateGenerateScripts()
        {
            string path = GetSelectObjectPath();
            return File.Exists(path) && path.EndsWith(".txt");
        }

        [MenuItem("Assets/FastSetup/Generate Scripts from file")]
        private static void GenerateScripts()
        {
            string projectPath = Directory.GetCurrentDirectory();
            string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            string fullPath = Path.Combine(projectPath, assetPath);

            if (!File.Exists(fullPath))
            {
                Debug.LogError("File not found: " + fullPath);
                return;
            }

            // Get available script templates
            string[] templatePaths = GetScriptTemplates();
            if (templatePaths.Length == 0)
            {
                EditorUtility.DisplayDialog("No Templates Found",
                    "No script templates found in Assets/ScriptTemplates or the default Unity templates folder.",
                    "OK");
                return;
            }

            // Show template selection dialog
            var templateNames = templatePaths.Select(Path.GetFileNameWithoutExtension).ToArray();
            int selectedIndex = ShowTemplateSelectionDialog(templateNames);

            if (selectedIndex < 0) return; // User cancelled

            string templatePath = templatePaths[selectedIndex];
            string templateContent = File.ReadAllText(templatePath);

            // Read script paths from file
            string[] lines = File.ReadAllLines(fullPath);
            var scriptsToCreate = new List<string>();

            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;
                scriptsToCreate.Add(trimmed);
            }

            if (scriptsToCreate.Count == 0)
            {
                EditorUtility.DisplayDialog("No Scripts", "No valid script paths found in the file.", "OK");
                return;
            }

            // Generate scripts
            int created = 0;
            foreach (string scriptPath in scriptsToCreate)
            {
                if (CreateScriptFromTemplate(scriptPath, templateContent, projectPath))
                    created++;
            }

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Success",
                $"{created} script(s) created successfully out of {scriptsToCreate.Count} paths.",
                "OK");
        }

        #endregion

        #region Template Management

        private static string[] GetScriptTemplates()
        {
            var templates = new List<string>();

            // Check Assets/ScriptTemplates
            string projectPath = Directory.GetCurrentDirectory();
            string customPath = Path.Combine(projectPath, "Assets", "ScriptTemplates");
            if (Directory.Exists(customPath))
            {
                templates.AddRange(Directory.GetFiles(customPath, "*.txt", SearchOption.AllDirectories));
            }

            // Check Unity's default templates
            string editorPath = EditorApplication.applicationContentsPath;
            string unityTemplatesPath = Path.Combine(editorPath, "Resources", "ScriptTemplates");
            if (Directory.Exists(unityTemplatesPath))
            {
                templates.AddRange(Directory.GetFiles(unityTemplatesPath, "*.txt", SearchOption.TopDirectoryOnly));
            }

            return templates.ToArray();
        }

        private static int ShowTemplateSelectionDialog(string[] templateNames)
        {
            var window = ScriptableObject.CreateInstance<TemplateSelectionWindow>();
            window.templateNames = templateNames;
            window.ShowModal();
            return window.selectedIndex;
        }

        #endregion

        #region Script Generation

        private static bool CreateScriptFromTemplate(string scriptPath, string templateContent, string projectPath)
        {
            try
            {
                // Normalize path separators
                scriptPath = scriptPath.Replace("\\", "/");

                // Ensure it starts with Assets/
                if (!scriptPath.StartsWith("Assets/"))
                    scriptPath = "Assets/" + scriptPath;

                // Ensure it ends with .cs
                if (!scriptPath.EndsWith(".cs"))
                    scriptPath += ".cs";

                string fullPath = Path.Combine(projectPath, scriptPath);
                string directory = Path.GetDirectoryName(fullPath);
                string fileName = Path.GetFileNameWithoutExtension(fullPath);

                // Create directory if it doesn't exist
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Check if file already exists
                if (File.Exists(fullPath))
                {
                    Debug.LogWarning($"Script already exists: {scriptPath}");
                    return false;
                }

                // Replace template placeholders
                string scriptContent = templateContent;
                scriptContent = scriptContent.Replace("#SCRIPTNAME#", fileName);
                scriptContent = scriptContent.Replace("#NAME#", fileName);
                scriptContent = scriptContent.Replace("#NOTRIM#", "");

                // Replace namespace if needed (extract from path)
                string namespaceName = ExtractNamespaceFromPath(scriptPath);
                scriptContent = scriptContent.Replace("#NAMESPACE#", namespaceName);

                // Write file
                File.WriteAllText(fullPath, scriptContent);
                Debug.Log($"Created script: {scriptPath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create script {scriptPath}: {ex.Message}");
                return false;
            }
        }

        private static string ExtractNamespaceFromPath(string path)
        {
            // Extract namespace from path (e.g., Assets/_Project/Scripts/UI -> Project.Scripts.UI)
            var parts = path.Split('/').Skip(1).Take(path.Split('/').Length - 2);
            return string.Join(".", parts.Where(p => !string.IsNullOrEmpty(p)));
        }

        #endregion

        #region Utilities

        private static string GetSelectObjectPath()
        {
            string projectPath = Directory.GetCurrentDirectory();
            string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            string fullPath = Path.Combine(projectPath, assetPath);
            return fullPath;
        }

        #endregion
    }

    #region Template Selection Window

    public class TemplateSelectionWindow : EditorWindow
    {
        public string[] templateNames;
        public int selectedIndex = -1;
        private Vector2 scrollPosition;

        private void OnGUI()
        {
            titleContent = new GUIContent("Select Script Template");
            minSize = new Vector2(400, 300);

            GUILayout.Label("Choose a template for the scripts:", EditorStyles.boldLabel);
            GUILayout.Space(10);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            for (int i = 0; i < templateNames.Length; i++)
            {
                if (GUILayout.Button(templateNames[i], GUILayout.Height(30)))
                {
                    selectedIndex = i;
                    Close();
                }
            }

            GUILayout.EndScrollView();

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Cancel", GUILayout.Height(30)))
            {
                selectedIndex = -1;
                Close();
            }
        }
    }

    #endregion
}