// AssetDirStat.cs
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace JulienNoe.Tools.AssetDirStat
{
    // Main EditorWindow class for analyzing disk usage of project assets
    public class AssetDirStatEditor : EditorWindow
    {
        // Internal class to store info about each asset
        private class AssetInfo
        {
            public string path;
            public string extension;
            public long size;
        }

        // Dictionary mapping extensions to lists of corresponding assets
        private Dictionary<string, List<AssetInfo>> assetsByType = new();

        // Stores a color for each extension (used in UI)
        private Dictionary<string, Color> typeColors = new();

        // Stores total size per extension
        private Dictionary<string, long> totalSizeByType = new();

        // Currently selected extension
        private string selectedExtension = null;

        // Scroll positions for each panel
        private Vector2 scrollPosLeft;
        private Vector2 scrollPosRight;

        // Help panel toggle
        private bool showHelp = false;

        // Total disk size of all assets found
        private long globalTotalSize = 0;

        // Property to disable tool during play mode
        private bool isPlayMode => EditorApplication.isPlayingOrWillChangePlaymode;

        // Adds menu item to Unity under Tools > Analysis > Asset Disk Stat
        [MenuItem("Tools/Julien Noe/Asset Disk Stat")]
        public static void ShowWindow()
        {
            var window = GetWindow<AssetDirStatEditor>("AssetDirStat");
            window.minSize = new Vector2(600, 400);
        }

        // Automatically scan assets when tool is opened in edit mode
        private void OnEnable()
        {
            if (!isPlayMode)
            {
                ScanAssets();
            }
        }

        // Main GUI drawing function
        private void OnGUI()
        {
            // Freeze UI in play mode
            if (isPlayMode)
            {
                EditorGUILayout.HelpBox("AssetDiskStat is disabled in Play Mode.", MessageType.Warning);
                return;
            }

            DrawHelpButton();

            if (showHelp)
            {
                EditorGUILayout.HelpBox(
                    "AssetDiskStat scans the entire Assets/ folder, groups files by type (e.g., .png, .mat), and displays a visual breakdown.\n\n" +
                    "Click on a colored block to list the matching assets.\n" +
                    "Click on an asset to ping it in the project.\n" +
                    "Use the Scan button below to refresh the analysis.",
                    MessageType.Info);
            }

            DrawScanButton();
            DrawClearButton();

            EditorGUILayout.BeginHorizontal();
            DrawTypePanel();
            DrawAssetListPanel();
            EditorGUILayout.EndHorizontal();
        }

        // Top Help button (blue)
        private void DrawHelpButton()
        {
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("Help", GUILayout.Height(24)))
            {
                showHelp = !showHelp;
            }
            GUI.backgroundColor = Color.white;
        }

        // Green Scan button that triggers a new analysis
        private void DrawScanButton()
        {
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Scan", GUILayout.Height(32)))
            {
                ScanAssets();
            }
            GUI.backgroundColor = Color.white;
        }

        // Orange Clear button to reset data
        private void DrawClearButton()
        {
            GUI.backgroundColor = new Color(1f, 0.5f, 0f); // orange
            if (GUILayout.Button("Clear", GUILayout.Height(24)))
            {
                ClearData();
            }
            GUI.backgroundColor = Color.white;
        }

        // Left panel: colored blocks grouped by file extension
        private void DrawTypePanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(200));
            EditorGUILayout.LabelField("Asset Types", EditorStyles.boldLabel);

            scrollPosLeft = EditorGUILayout.BeginScrollView(scrollPosLeft, false, false);

            float totalCount = assetsByType.Sum(kvp => kvp.Value.Count);

            foreach (var kvp in assetsByType.OrderByDescending(e => e.Value.Count))
            {
                string ext = kvp.Key;
                int count = kvp.Value.Count;
                float ratio = count / Mathf.Max(totalCount, 1);
                long typeSize = totalSizeByType.ContainsKey(ext) ? totalSizeByType[ext] : 0;

                float height = Mathf.Clamp(20f + (80f * ratio), 20f, 100f);
                GUI.backgroundColor = typeColors.ContainsKey(ext) ? typeColors[ext] : Color.gray;

                string label = $"{ext} ({count}) - {EditorUtility.FormatBytes(typeSize)}";
                GUIStyle style = new(GUI.skin.button)
                {
                    wordWrap = false,
                    alignment = TextAnchor.MiddleLeft
                };

                // Click to filter list on the right
                if (GUILayout.Button(label, style, GUILayout.Height(height), GUILayout.Width(190)))
                {
                    selectedExtension = ext;
                }
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Total Size:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(EditorUtility.FormatBytes(globalTotalSize), EditorStyles.helpBox);

            EditorGUILayout.EndVertical();
        }

        // Right panel: list of assets for selected type
        private void DrawAssetListPanel()
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Assets", EditorStyles.boldLabel);

            scrollPosRight = EditorGUILayout.BeginScrollView(scrollPosRight, true, true);

            if (!string.IsNullOrEmpty(selectedExtension) && assetsByType.ContainsKey(selectedExtension))
            {
                foreach (var asset in assetsByType[selectedExtension].OrderByDescending(a => a.size))
                {
                    string sizeLabel = EditorUtility.FormatBytes(asset.size);
                    string label = $"{asset.path} ({sizeLabel})";

                    if (GUILayout.Button(label, GUILayout.Height(22)))
                    {
                        var obj = AssetDatabase.LoadAssetAtPath<Object>(asset.path);
                        Selection.activeObject = obj;
                        EditorGUIUtility.PingObject(obj);
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Select a type to see assets.", MessageType.Info);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // Full scan of the Assets folder, updates all structures
        private void ScanAssets()
        {
            ClearData();

            string[] allPaths = Directory.GetFiles("Assets", "*.*", SearchOption.AllDirectories)
                                        .Where(p => !p.EndsWith(".meta")).ToArray();

            foreach (string path in allPaths)
            {
                string ext = Path.GetExtension(path).ToLower();
                FileInfo fi = new(path);

                if (!assetsByType.ContainsKey(ext))
                    assetsByType[ext] = new List<AssetInfo>();

                assetsByType[ext].Add(new AssetInfo
                {
                    path = path.Replace("\\", "/"),
                    extension = ext,
                    size = fi.Length
                });

                if (!totalSizeByType.ContainsKey(ext))
                    totalSizeByType[ext] = 0;

                totalSizeByType[ext] += fi.Length;
                globalTotalSize += fi.Length;
            }

            // Assign distinct colors
            System.Random rng = new();
            foreach (var key in assetsByType.Keys)
            {
                typeColors[key] = RandomColor(rng);
            }
        }

        // Reset all dictionaries and counters
        private void ClearData()
        {
            assetsByType.Clear();
            typeColors.Clear();
            totalSizeByType.Clear();
            globalTotalSize = 0;
            selectedExtension = null;
        }

        // Generates a bright readable random color
        private Color RandomColor(System.Random rng)
        {
            return new Color(
                (float)rng.NextDouble() * 0.5f + 0.5f,
                (float)rng.NextDouble() * 0.5f + 0.5f,
                (float)rng.NextDouble() * 0.5f + 0.5f);
        }
    }
}
