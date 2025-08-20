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

            JObject GetRegistry(string name)
            {
                var existing = scopedRegistries.FirstOrDefault(r => r["name"]?.ToString().ToLower() == name) as JObject;
                var registry = existing ?? new JObject
                {
                    ["name"] = name,
                    ["url"] = "undefined",
                    ["scopes"] = new JArray()
                };
                if (existing == null)
                    scopedRegistries.Add(registry);
                return registry;
            }

            JObject AddRegistry(string name, string url)
            {
                var registry = GetRegistry(name);
                registry["url"] = url;
                return registry;
            }

            JObject AddScope(string name, string scope)
            {
                var registry = GetRegistry(name);
                
                var scopes = registry["scopes"].ToObject<List<string>>();
                if (!scopes.Contains(scope))
                {
                    scopes.Add(scope);
                    registry["scopes"] = JArray.FromObject(scopes);
                }
                return registry;
            }

            string GetScope(string part)
            {
                return part.Split('@')[0];
            }

            var listPackageToAdd = new List<string>();

            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;

                string[] parts = line.Split(' ');
                switch (parts[0])
                {
                    case "git":
                        {
                            listPackageToAdd.Add(parts[1]);
                            continue;
                        }
                    case "openupm":
                        {
                            AddRegistry("openupm", "https://package.openupm.com");
                            AddScope("openupm", GetScope(parts[2]));
                            listPackageToAdd.Add(parts[2]);
                            continue;
                        }
                    case "npm":
                        {
                            AddRegistry("npm", "https://registry.npmjs.org");
                            AddScope("npm", GetScope(parts[2]));
                            listPackageToAdd.Add(parts[2]);
                            continue;
                        }
                    case "registry":
                        {
                            AddRegistry(parts[1], parts[2]);
                            continue;
                        }
                    default:
                        {
                            AddScope(parts[0], GetScope(parts[2]));
                            listPackageToAdd.Add(parts[2]);
                            continue;
                        }
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

            Client.AddAndRemove(listPackageToAdd.ToArray(), null);
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