using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildScript
{
    [MenuItem("Build/Android ARM64 (Direct)")]
    public static void BuildAndroid()
    {
        // 씬 목록
        var sceneList = new System.Collections.Generic.List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
                sceneList.Add(scene.path);
        }
        if (sceneList.Count == 0)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets" }))
                sceneList.Add(AssetDatabase.GUIDToAssetPath(guid));
        }

        // Build Profile 우회 - 직접 빌드
        var options = new BuildPlayerOptions();
        options.scenes = sceneList.ToArray();
        options.locationPathName = "Build/Match3.apk";
        options.target = BuildTarget.Android;
        options.targetGroup = BuildTargetGroup.Android;
        options.options = BuildOptions.None;

        Debug.Log("[BuildScript] 씬 수: " + sceneList.Count);
        foreach (var s in sceneList) Debug.Log("[BuildScript] 씬: " + s);

        var report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result == BuildResult.Succeeded)
            Debug.Log("[BuildScript] BUILD SUCCESS: " + report.summary.outputPath);
        else
        {
            Debug.LogError("[BuildScript] BUILD FAILED: " + report.summary.result);
            foreach (var step in report.steps)
            {
                foreach (var msg in step.messages)
                {
                    if (msg.type == LogType.Error || msg.type == LogType.Warning)
                        Debug.LogError(step.name + ": " + msg.content);
                }
            }
        }
    }
}
