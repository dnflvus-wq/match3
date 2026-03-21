using UnityEditor;
using UnityEngine;

// Unity 컴파일 시 자동 실행 - ARM64 전용으로 강제 설정
[InitializeOnLoad]
public class FixArchitecture
{
    static FixArchitecture()
    {
        var before = PlayerSettings.Android.targetArchitectures;
        // IL2CPP + ARM64 + x86_64
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.X86_64;
        var after = PlayerSettings.Android.targetArchitectures;
        Debug.Log($"[FixArchitecture] IL2CPP+ARM64: {before} -> {after}");
    }

    [MenuItem("Build/Android ARM64 (Direct)")]
    static void BuildAndroid()
    {
        // IL2CPP로 전환 (ARM64 + x86_64 지원, 에뮬레이터 호환)
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        // ARM64 + x86_64 (실제 ARM 디바이스 + x86_64 에뮬레이터 모두 지원)
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.X86_64;

        // 방법3: Reflection으로 AndroidPlatformBuildSettings.m_TargetArchitectures 직접 설정
        try
        {
            // 활성 Build Profile 가져오기 (Unity 6 API)
            var bpType = System.Type.GetType("UnityEditor.Build.Profile.BuildProfile, UnityEditor");
            if (bpType != null)
            {
                var getActiveMethod = bpType.GetMethod("GetActiveBuildProfile", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                if (getActiveMethod != null)
                {
                    var activeProfile = getActiveMethod.Invoke(null, null);
                    if (activeProfile != null)
                    {
                        // m_PlatformBuildProfile 필드 가져오기
                        var platformField = bpType.GetField("m_PlatformBuildProfile", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        if (platformField != null)
                        {
                            var platformSettings = platformField.GetValue(activeProfile);
                            if (platformSettings != null)
                            {
                                var archField = platformSettings.GetType().GetField("m_TargetArchitectures",
                                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                                if (archField != null)
                                {
                                    archField.SetValue(platformSettings, (int)AndroidArchitecture.ARM64);
                                    Debug.Log($"[FixArchitecture] Reflection: set m_TargetArchitectures={AndroidArchitecture.ARM64}");
                                }
                                else
                                {
                                    Debug.Log($"[FixArchitecture] m_TargetArchitectures field not found. Fields: {string.Join(", ", System.Array.ConvertAll(platformSettings.GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public), f => f.Name))}");
                                }
                            }
                        }
                    }
                    else Debug.Log("[FixArchitecture] No active Build Profile");
                }
            }
        }
        catch (System.Exception e) { Debug.LogWarning("[FixArchitecture] Reflection error: " + e.Message); }

        var actual = PlayerSettings.Android.targetArchitectures;
        Debug.Log($"[FixArchitecture] Building: got={actual}({(int)actual}), scriptingBackend={PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android)}");

        // 모바일 UI 설정 (씬/prefab에 런타임 스크립트 추가)
        MobileUISetup.Run();

        var scenes = new System.Collections.Generic.List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
            if (scene.enabled) scenes.Add(scene.path);

        // 변경사항 강제 저장 → 네이티브 레이어 동기화
        AssetDatabase.SaveAssets();
        System.Threading.Thread.Sleep(500);

        var opts = new BuildPlayerOptions
        {
            scenes = scenes.ToArray(),
            locationPathName = "Build/Android/Match3.apk",
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.None
        };

        // 빌드 직전 재확인
        Debug.Log($"[FixArchitecture] Pre-build check: {PlayerSettings.Android.targetArchitectures}({(int)PlayerSettings.Android.targetArchitectures})");
        var report = BuildPipeline.BuildPlayer(opts);
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            Debug.Log("[FixArchitecture] BUILD SUCCESS: " + report.summary.outputPath);
        else
            Debug.LogError("[FixArchitecture] BUILD FAILED: " + report.summary.result);
    }
}
