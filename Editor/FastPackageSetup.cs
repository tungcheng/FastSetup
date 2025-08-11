using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace Techies
{
    public class FastPackageSetup
    {
        [MenuItem("Assets/FastSetup/Import Packages from file", true)]
        private static bool ValidateImportFromText()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            return File.Exists(path) && path.EndsWith(".txt");
        }

        [MenuItem("Assets/FastSetup/Overwrite Packages manifest file", true)]
        private static bool ValidateOverwritePackagesManifest()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            return File.Exists(path) && path.EndsWith(".json");
        }

        class RegistryInfo
        {
            public string name;
            public string url;
        }

        [MenuItem("Assets/FastSetup/Import Packages from file")]
        public static void ImportFromText()
        {
            string projectPath = Directory.GetCurrentDirectory();
            string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            string fullPath = Path.Combine(projectPath, assetPath);

            if (!File.Exists(fullPath))
            {
                Debug.LogError("File not found: " + fullPath);
                return;
            }

            string[] lines = File.ReadAllLines(fullPath);
            string manifestPath = Path.Combine(projectPath, "Packages/manifest.json");

            var manifest = JObject.Parse(File.ReadAllText(manifestPath));
            JObject dependencies = manifest["dependencies"] as JObject ?? new JObject();
            JArray scopedRegistries = manifest["scopedRegistries"] as JArray ?? new JArray();

            var registries = new Dictionary<string, RegistryInfo>();
            var scopesByRegistry = new Dictionary<string, HashSet<string>>();

            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;

                string[] parts = line.Split(' ');
                if (parts.Length < 3)
                {
                    Debug.LogWarning($"Invalid line: {line}");
                    continue;
                }

                switch (parts[0])
                {
                    case "registry":
                        {
                            if (parts.Length != 3)
                            {
                                Debug.LogWarning($"Invalid registry line: {line}");
                                continue;
                            }

                            string name = parts[1];
                            string url = parts[2];
                            registries[name] = new RegistryInfo { name = name, url = url };
                            break;
                        }

                    case "git":
                        {
                            if (parts.Length != 3)
                            {
                                Debug.LogWarning($"Invalid git line: {line}");
                                continue;
                            }

                            string packageName = parts[1];
                            string gitUrl = parts[2];
                            dependencies[packageName] = gitUrl;
                            break;
                        }

                    default:
                        {
                            // Positional registry package line
                            if (parts.Length != 4)
                            {
                                Debug.LogWarning($"Invalid package line: {line}");
                                continue;
                            }

                            string registryName = parts[0];
                            string scope = parts[1];
                            string packageName = parts[2];
                            string version = parts[3];

                            if (!registries.TryGetValue(registryName, out var reg))
                            {
                                Debug.LogWarning($"Registry '{registryName}' not defined.");
                                continue;
                            }

                            dependencies[packageName] = version;

                            if (!scopesByRegistry.ContainsKey(registryName))
                                scopesByRegistry[registryName] = new HashSet<string>();

                            scopesByRegistry[registryName].Add(scope);
                            break;
                        }
                }
            }

            // Add/merge scoped registries
            foreach (var kvp in scopesByRegistry)
            {
                var reg = registries[kvp.Key];
                var newScopes = kvp.Value;

                var existing = scopedRegistries.FirstOrDefault(r =>
                    r["name"]?.ToString() == reg.name &&
                    r["url"]?.ToString() == reg.url) as JObject;

                var registryObj = existing ?? new JObject
                {
                    ["name"] = reg.name,
                    ["url"] = reg.url,
                    ["scopes"] = new JArray(newScopes)
                };

                var scopes = registryObj["scopes"] as JArray;
                var scopeSet = new HashSet<string>(scopes.Select(s => s.ToString()));
                foreach (var s in newScopes)
                {
                    if (!scopeSet.Contains(s))
                        scopes.Add(s);
                }

                if (existing == null)
                {
                    scopedRegistries.Add(registryObj);
                }
            }

            manifest["scopedRegistries"] = scopedRegistries;
            manifest["dependencies"] = dependencies;
            File.WriteAllText(manifestPath, manifest.ToString());

            EditorUtility.DisplayDialog
            (
                "Success",
                "manifest.json has been edited successfully.\n" +
                "Unity will re-resolve packages automatically. If it doesn’t, try closing and reopening the project.",
                "OK"
            );

            Client.Resolve();
        }

        [MenuItem("Assets/FastSetup/Overwrite Packages manifest file")]
        public static void OverwritePackagesManifest()
        {
            if (!EditorUtility.DisplayDialog("Overwrite Packages manifest.json?", "Press Yes button will overwrite content of file Packages/manifest.json", "Yes", "No"))
                return;

            string projectPath = Directory.GetCurrentDirectory();
            string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            string fullPath = Path.Combine(projectPath, assetPath);

            if (!File.Exists(fullPath))
            {
                Debug.LogError("File not found: " + fullPath);
                return;
            }

            string manifestPath = Path.Combine(projectPath, "Packages/manifest.json");
            string json = File.ReadAllText(fullPath);

            try
            {
                // Validate JSON format using JSON.NET (will throw on malformed JSON)
                JToken parsed = JToken.Parse(json);

                if (parsed.Type != JTokenType.Object)
                    throw new JsonException("Root JSON must be an object.");

                JObject root = (JObject)parsed;
                if (root["dependencies"] is not JObject)
                    throw new JsonException("Expected a 'dependencies' object in the root.");

                string pretty = root.ToString(Formatting.Indented);
                File.WriteAllText(manifestPath, pretty);

                EditorUtility.DisplayDialog
                (
                    "Success",
                    "manifest.json has been overwritten successfully.\n" +
                    "Unity will re-resolve packages automatically. If it doesn’t, try closing and reopening the project.",
                    "OK"
                );

                Client.Resolve();
            }
            catch (JsonReaderException jex)
            {
                EditorUtility.DisplayDialog(
                    "Invalid JSON",
                    $"The selected file is not valid JSON.\n\n{jex.Message}",
                    "OK"
                );
            }
            catch (JsonException jex)
            {
                EditorUtility.DisplayDialog(
                    "JSON Validation Failed",
                    $"The JSON format isn't compatible with a Unity manifest.\n\n{jex.Message}",
                    "OK"
                );
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    $"Failed to overwrite manifest.json.\n\n{ex.Message}",
                    "OK"
                );
            }
        }
    }
}