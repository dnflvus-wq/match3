using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelGenerator
{
    [MenuItem("Tools/Generate Levels 04-10")]
    public static void GenerateLevels()
    {
        string sourcePath = "Assets/Scenes/Level01.unity";

        // 레벨 설정: 이동수, 목표점수, 1성, 2성, 3성
        int[][] levelConfigs = {
            new[] { 22, 1200, 600, 1200, 2400 },   // Level04
            new[] { 20, 1500, 750, 1500, 3000 },   // Level05
            new[] { 20, 1800, 900, 1800, 3600 },   // Level06
            new[] { 18, 2000, 1000, 2000, 4000 },  // Level07
            new[] { 18, 2200, 1100, 2200, 4400 },  // Level08
            new[] { 16, 2500, 1250, 2500, 5000 },  // Level09
            new[] { 15, 3000, 1500, 3000, 6000 },  // Level10
        };

        for (int i = 0; i < levelConfigs.Length; i++)
        {
            int levelNum = i + 4;
            string destPath = $"Assets/Scenes/Level{levelNum:D2}.unity";

            if (System.IO.File.Exists(destPath))
            {
                Debug.Log($"Level{levelNum:D2} already exists, skipping");
                continue;
            }

            AssetDatabase.CopyAsset(sourcePath, destPath);
            Debug.Log($"Created {destPath}");

            // 씬을 열어서 파라미터 수정
            var scene = EditorSceneManager.OpenScene(destPath, OpenSceneMode.Single);

            // LevelMoves 컴포넌트 찾기
            var levelMoves = Object.FindObjectOfType<Match3.LevelMoves>();
            if (levelMoves != null)
            {
                levelMoves.numMoves = levelConfigs[i][0];
                levelMoves.targetScore = levelConfigs[i][1];
                levelMoves.score1Star = levelConfigs[i][2];
                levelMoves.score2Star = levelConfigs[i][3];
                levelMoves.score3Star = levelConfigs[i][4];
                EditorUtility.SetDirty(levelMoves);
            }

            EditorSceneManager.SaveScene(scene);
            Debug.Log($"Level{levelNum:D2}: moves={levelConfigs[i][0]}, target={levelConfigs[i][1]}");
        }

        // Build Settings에 씬 추가
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        for (int levelNum = 4; levelNum <= 10; levelNum++)
        {
            string path = $"Assets/Scenes/Level{levelNum:D2}.unity";
            bool exists = false;
            foreach (var s in scenes) { if (s.path == path) { exists = true; break; } }
            if (!exists)
            {
                scenes.Add(new EditorBuildSettingsScene(path, true));
                Debug.Log($"Added {path} to Build Settings");
            }
        }
        EditorBuildSettings.scenes = scenes.ToArray();

        // 원래 씬으로 복귀
        EditorSceneManager.OpenScene(sourcePath);
        Debug.Log("Level generation complete! 7 new levels created.");
    }
}
