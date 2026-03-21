using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 모바일 Portrait 레이아웃 자동 설정 스크립트.
/// BuildAndroid()와 함께 또는 메뉴에서 수동으로 실행 가능.
/// </summary>
public class MobileUISetup
{
    private static readonly string[] LevelScenes = {
        "Assets/Scenes/Level01.unity",
        "Assets/Scenes/Level02.unity",
        "Assets/Scenes/Level03.unity",
    };

    private const string LevelSelectScene  = "Assets/Scenes/LevelSelect.unity";
    private const string GameUICanvasPrefab = "Assets/Prefabs/GameUICanvas.prefab";

    // FixArchitecture.BuildAndroid()가 호출 전에 이 메서드를 호출하도록 연결
    [MenuItem("Build/Setup Mobile UI")]
    public static void Run()
    {
        Debug.Log("[MobileUISetup] 시작...");

        SetupGameUICanvasPrefab();
        SetupLevelScenes();
        SetupLevelSelectScene();

        Debug.Log("[MobileUISetup] 완료");
    }

    // ── GameUICanvas.prefab ────────────────────────────────────────────────

    private static void SetupGameUICanvasPrefab()
    {
        Debug.Log("[MobileUISetup] GameUICanvas.prefab 수정 중...");

        using var scope = new PrefabUtility.EditPrefabContentsScope(GameUICanvasPrefab);
        var root = scope.prefabContentsRoot;

        // MobileHUDLayout 추가 (중복 방지)
        if (root.GetComponent<Match3.MobileHUDLayout>() == null)
        {
            root.AddComponent<Match3.MobileHUDLayout>();
            Debug.Log("[MobileUISetup] MobileHUDLayout 추가됨");
        }

        // SafeArea 처리용 Panel 설정
        // GameUICanvas 바로 아래에 SafeAreaPanel이 없으면 생성
        var safeAreaPanel = root.transform.Find("SafeAreaPanel");
        if (safeAreaPanel == null)
        {
            var go = new GameObject("SafeAreaPanel");
            go.transform.SetParent(root.transform, false);
            go.transform.SetAsFirstSibling();
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin       = Vector2.zero;
            rt.anchorMax       = Vector2.one;
            rt.offsetMin       = Vector2.zero;
            rt.offsetMax       = Vector2.zero;
            go.AddComponent<Match3.SafeAreaHandler>();
            Debug.Log("[MobileUISetup] SafeAreaPanel 추가됨");
        }

        Debug.Log("[MobileUISetup] GameUICanvas.prefab 수정 완료");
    }

    // ── Level 씬들 ─────────────────────────────────────────────────────────

    private static void SetupLevelScenes()
    {
        foreach (var scenePath in LevelScenes)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Debug.Log($"[MobileUISetup] 씬 처리 중: {scenePath}");

            // Main Camera에 MobileCameraSetup 추가
            var camObj = GameObject.FindWithTag("MainCamera");
            if (camObj == null)
                camObj = GameObject.Find("Main Camera");

            if (camObj != null)
            {
                if (camObj.GetComponent<Match3.MobileCameraSetup>() == null)
                {
                    camObj.AddComponent<Match3.MobileCameraSetup>();
                    Debug.Log($"[MobileUISetup] MobileCameraSetup 추가됨: {scenePath}");
                }
            }
            else
            {
                Debug.LogWarning($"[MobileUISetup] Main Camera를 찾을 수 없음: {scenePath}");
            }

            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[MobileUISetup] 씬 저장: {scenePath}");
        }
    }

    // ── LevelSelect 씬 ─────────────────────────────────────────────────────

    private static void SetupLevelSelectScene()
    {
        var scene = EditorSceneManager.OpenScene(LevelSelectScene, OpenSceneMode.Single);
        Debug.Log("[MobileUISetup] LevelSelect 씬 처리 중...");

        // LevelSelect 씬의 Canvas에도 Canvas Scaler 확인
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var canvas in canvases)
        {
            var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode         = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight  = 0.5f;
                Debug.Log($"[MobileUISetup] LevelSelect Canvas Scaler 수정: {canvas.gameObject.name}");
            }
        }

        // 레벨 버튼들을 터치 친화적 크기로 조정
        var levelSelect = Object.FindFirstObjectByType<Match3.LevelSelect>();
        if (levelSelect != null)
        {
            foreach (var btn in levelSelect.buttons)
            {
                if (btn.gameObject != null)
                {
                    var rt = btn.gameObject.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        // 최소 200x200px 보장
                        var size = rt.sizeDelta;
                        if (size.x < 200) size.x = 200;
                        if (size.y < 200) size.y = 200;
                        rt.sizeDelta = size;
                    }
                }
            }
            Debug.Log("[MobileUISetup] LevelSelect 버튼 크기 조정됨");
        }

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[MobileUISetup] LevelSelect 씬 저장 완료");
    }
}
