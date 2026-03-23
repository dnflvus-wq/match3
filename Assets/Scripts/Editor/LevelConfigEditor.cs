using UnityEngine;
using UnityEditor;

namespace Match3.Editor
{
    [CustomEditor(typeof(LevelConfig))]
    public class LevelConfigEditor : UnityEditor.Editor
    {
        // Color palette matching ColorType enum order: Yellow, Purple, Red, Blue, Green, Pink
        private static readonly Color[] TileColors = {
            new Color(1f, 0.85f, 0.1f),   // Yellow
            new Color(0.6f, 0.2f, 0.8f),   // Purple
            new Color(0.9f, 0.15f, 0.15f), // Red
            new Color(0.2f, 0.4f, 0.9f),   // Blue
            new Color(0.2f, 0.8f, 0.3f),   // Green
            new Color(1f, 0.4f, 0.7f),     // Pink
            new Color(1f, 0.5f, 0.1f),     // Orange (future)
            new Color(0.2f, 0.8f, 0.8f),   // Cyan (future)
        };

        private static readonly string[] ColorNames = {
            "Yellow", "Purple", "Red", "Blue", "Green", "Pink", "Orange", "Cyan"
        };

        private bool showGrid = true;
        private bool showWeights = true;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var config = (LevelConfig)target;

            // --- Board ---
            EditorGUILayout.LabelField("Board Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("width"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("height"));

            EditorGUILayout.Space(5);

            // --- Rules ---
            EditorGUILayout.LabelField("Rules", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("levelType"));

            var levelType = (LevelType)serializedObject.FindProperty("levelType").enumValueIndex;
            if (levelType == LevelType.Moves)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("moveLimit"));
            else if (levelType == LevelType.Timer)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("timeLimit"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("targetScore"));

            EditorGUILayout.Space(5);

            // --- Tiles ---
            EditorGUILayout.LabelField("Tile Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("numColors"));

            EditorGUILayout.Space(5);

            // --- Color Weights Visual Editor ---
            showWeights = EditorGUILayout.Foldout(showWeights, "Color Weights (Visual)");
            if (showWeights)
            {
                DrawColorWeights(config);
            }

            EditorGUILayout.Space(5);

            // --- Grid Preview ---
            showGrid = EditorGUILayout.Foldout(showGrid, "Board Grid Preview");
            if (showGrid)
            {
                DrawGridPreview(config);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawColorWeights(LevelConfig config)
        {
            EditorGUI.indentLevel++;
            int numColors = config.numColors;

            // Ensure colorWeights array matches numColors
            if (config.colorWeights == null || config.colorWeights.Length != numColors)
            {
                Undo.RecordObject(config, "Resize Color Weights");
                var old = config.colorWeights ?? new float[0];
                config.colorWeights = new float[numColors];
                for (int i = 0; i < numColors; i++)
                    config.colorWeights[i] = i < old.Length ? old[i] : 1f;
                EditorUtility.SetDirty(config);
            }

            float totalWeight = 0f;
            for (int i = 0; i < numColors; i++)
                totalWeight += config.colorWeights[i];

            for (int i = 0; i < numColors; i++)
            {
                EditorGUILayout.BeginHorizontal();

                // Color swatch
                var prevColor = GUI.backgroundColor;
                GUI.backgroundColor = TileColors[i];
                GUILayout.Box("", GUILayout.Width(20), GUILayout.Height(18));
                GUI.backgroundColor = prevColor;

                // Label
                EditorGUILayout.LabelField(ColorNames[i], GUILayout.Width(60));

                // Slider
                EditorGUI.BeginChangeCheck();
                float newWeight = EditorGUILayout.Slider(config.colorWeights[i], 0f, 5f);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(config, "Change Weight");
                    config.colorWeights[i] = newWeight;
                    EditorUtility.SetDirty(config);
                }

                // Percentage
                float pct = totalWeight > 0 ? (config.colorWeights[i] / totalWeight * 100f) : 0f;
                EditorGUILayout.LabelField(pct.ToString("F0") + "%", GUILayout.Width(40));

                EditorGUILayout.EndHorizontal();
            }

            // Visual bar
            EditorGUILayout.Space(3);
            Rect barRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(20));
            if (totalWeight > 0)
            {
                float x = barRect.x;
                for (int i = 0; i < numColors; i++)
                {
                    float w = (config.colorWeights[i] / totalWeight) * barRect.width;
                    if (w > 0)
                    {
                        EditorGUI.DrawRect(new Rect(x, barRect.y, w, barRect.height), TileColors[i]);
                        x += w;
                    }
                }
            }

            EditorGUI.indentLevel--;
        }

        private void DrawGridPreview(LevelConfig config)
        {
            EditorGUI.indentLevel++;

            int w = Mathf.Clamp(config.width, 1, 12);
            int h = Mathf.Clamp(config.height, 1, 12);
            float cellSize = Mathf.Min(30f, (EditorGUIUtility.currentViewWidth - 80f) / w);

            EditorGUILayout.LabelField($"Board: {w} x {h}", EditorStyles.miniLabel);
            EditorGUILayout.Space(3);

            for (int y = h - 1; y >= 0; y--)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                for (int x = 0; x < w; x++)
                {
                    // Alternate checker pattern
                    Color cellColor = (x + y) % 2 == 0
                        ? new Color(0.85f, 0.85f, 0.85f)
                        : new Color(0.95f, 0.95f, 0.95f);

                    var prevBg = GUI.backgroundColor;
                    GUI.backgroundColor = cellColor;
                    GUILayout.Box($"{x},{y}", GUILayout.Width(cellSize), GUILayout.Height(cellSize));
                    GUI.backgroundColor = prevBg;
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.indentLevel--;
        }
    }
}
