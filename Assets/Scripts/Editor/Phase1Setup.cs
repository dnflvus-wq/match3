using UnityEngine;
using UnityEditor;

namespace Match3.Editor
{
    public static class Phase1Setup
    {
        [MenuItem("Match3/Phase1 Setup - All")]
        public static void RunAll()
        {
            SetupTilePrefabs();
            SetupParticlePrefabs();
            SetupUIPrefabs();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Phase1] All setup complete!");
        }

        [MenuItem("Match3/Phase1 - 1. Tile Prefabs")]
        public static void SetupTilePrefabs()
        {
            EnsureFolder("Assets/Prefabs/Tiles");

            // fish_1~6 = Yellow, Purple, Red, Blue, Green, Pink
            var spriteMap = new (string tileName, string spritePath)[] {
                ("Tile_Yellow", "Assets/Textures/Sprites/fish_1.png"),
                ("Tile_Purple", "Assets/Textures/Sprites/fish_2.png"),
                ("Tile_Red",    "Assets/Textures/Sprites/fish_3.png"),
                ("Tile_Blue",   "Assets/Textures/Sprites/fish_4.png"),
                ("Tile_Green",  "Assets/Textures/Sprites/fish_5.png"),
                ("Tile_Pink",   "Assets/Textures/Sprites/fish_6.png"),
            };

            // 1. Create BaseTile.prefab
            string basePath = "Assets/Prefabs/Tiles/BaseTile.prefab";
            GameObject baseGo = new GameObject("BaseTile");
            var sr = baseGo.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 1;
            var col = baseGo.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.9f, 0.9f);
            baseGo.AddComponent<TileView>();

            GameObject basePrefab = PrefabUtility.SaveAsPrefabAsset(baseGo, basePath);
            Object.DestroyImmediate(baseGo);
            Debug.Log("[Phase1] Created BaseTile.prefab");

            // 2. Create Prefab Variants (6 colors with sprites)
            foreach (var (tileName, spritePath) in spriteMap)
            {
                string variantPath = "Assets/Prefabs/Tiles/" + tileName + ".prefab";
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
                instance.name = tileName;

                var renderer = instance.GetComponent<SpriteRenderer>();
                if (renderer != null && sprite != null)
                    renderer.sprite = sprite;

                PrefabUtility.SaveAsPrefabAsset(instance, variantPath);
                Object.DestroyImmediate(instance);
                Debug.Log("[Phase1] Variant: " + tileName + " sprite=" + (sprite != null ? sprite.name : "NULL"));
            }

            // Orange/Cyan (future colors, no sprite yet)
            foreach (var tileName in new[] { "Tile_Orange", "Tile_Cyan" })
            {
                string variantPath = "Assets/Prefabs/Tiles/" + tileName + ".prefab";
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
                instance.name = tileName;
                PrefabUtility.SaveAsPrefabAsset(instance, variantPath);
                Object.DestroyImmediate(instance);
                Debug.Log("[Phase1] Variant (no sprite): " + tileName);
            }

            AssetDatabase.SaveAssets();
        }

        [MenuItem("Match3/Phase1 - 2. Particle Prefabs")]
        public static void SetupParticlePrefabs()
        {
            // MatchBurst - sparkle explosion
            ConfigureParticle("Assets/Prefabs/Effects/MatchBurst.prefab",
                duration: 0.5f, startSpeed: 3f, startSize: 0.15f,
                gravity: 1.2f, burstCount: 20, maxP: 30,
                shape: ParticleSystemShapeType.Sphere, radius: 0.1f,
                color: new Color(1f, 0.9f, 0.3f));

            // MatchStar - floating stars
            ConfigureParticle("Assets/Prefabs/Effects/MatchStar.prefab",
                duration: 0.8f, startSpeed: 2f, startSize: 0.4f,
                gravity: 0.3f, burstCount: 15, maxP: 20,
                shape: ParticleSystemShapeType.Sphere, radius: 0.2f,
                color: new Color(1f, 1f, 0.5f));

            // MatchGlow - single big glow
            ConfigureParticle("Assets/Prefabs/Effects/MatchGlow.prefab",
                duration: 0.3f, startSpeed: 0f, startSize: 1.5f,
                gravity: 0f, burstCount: 1, maxP: 3,
                shape: ParticleSystemShapeType.Sphere, radius: 0.01f,
                color: new Color(1f, 1f, 1f, 0.8f));

            // MatchRing - ring burst
            ConfigureParticle("Assets/Prefabs/Effects/MatchRing.prefab",
                duration: 0.4f, startSpeed: 4f, startSize: 0.1f,
                gravity: 0f, burstCount: 30, maxP: 36,
                shape: ParticleSystemShapeType.Circle, radius: 0.05f,
                color: new Color(0.8f, 0.9f, 1f));

            AssetDatabase.SaveAssets();
            Debug.Log("[Phase1] Particle prefabs configured!");
        }

        static void ConfigureParticle(string path, float duration, float startSpeed,
            float startSize, float gravity, int burstCount, int maxP,
            ParticleSystemShapeType shape, float radius, Color color)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) { Debug.LogError("Not found: " + path); return; }

            var root = PrefabUtility.LoadPrefabContents(path);
            var ps = root.GetComponent<ParticleSystem>();
            if (ps == null) { PrefabUtility.UnloadPrefabContents(root); return; }

            var main = ps.main;
            main.duration = duration;
            main.loop = false;
            main.startSpeed = startSpeed;
            main.startSize = startSize;
            main.gravityModifier = gravity;
            main.maxParticles = maxP;
            main.startColor = color;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burstCount) });

            var sh = ps.shape;
            sh.enabled = true;
            sh.shapeType = shape;
            sh.radius = radius;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = new ParticleSystem.MinMaxGradient(grad);

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            var renderer = root.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = 100;
                renderer.material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        [MenuItem("Match3/Phase1 - 3. UI Prefabs")]
        public static void SetupUIPrefabs()
        {
            // LevelButton - assign level_btn sprite
            {
                string path = "Assets/Prefabs/UI/LevelButton.prefab";
                var root = PrefabUtility.LoadPrefabContents(path);
                var img = root.GetComponent<UnityEngine.UI.Image>();
                if (img != null)
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/level_btn.png");
                    if (sprite != null) img.sprite = sprite;
                    img.preserveAspect = true;
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
                Debug.Log("[Phase1] LevelButton: sprite assigned");
            }

            // HudPanel - add image + text children
            {
                string path = "Assets/Prefabs/UI/HudPanel.prefab";
                var root = PrefabUtility.LoadPrefabContents(path);

                var img = root.GetComponent<UnityEngine.UI.Image>();
                if (img == null) img = root.AddComponent<UnityEngine.UI.Image>();
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/hud_panel.png");
                if (sprite != null) img.sprite = sprite;
                img.type = UnityEngine.UI.Image.Type.Sliced;

                AddTextChild(root.transform, "ScoreText", "0", 48);
                AddTextChild(root.transform, "MovesText", "20", 48);
                AddTextChild(root.transform, "TargetText", "15000", 36);

                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
                Debug.Log("[Phase1] HudPanel: image + texts assigned");
            }

            // GameOverPanel - overlay + buttons
            {
                string path = "Assets/Prefabs/UI/GameOverPanel.prefab";
                var root = PrefabUtility.LoadPrefabContents(path);

                var img = root.GetComponent<UnityEngine.UI.Image>();
                if (img != null) img.color = new Color(0f, 0f, 0f, 0.7f);

                AddTextChild(root.transform, "ResultText", "Level Clear!", 64);
                AddTextChild(root.transform, "FinalScoreText", "Score: 0", 48);
                AddButtonChild(root.transform, "ReplayButton", "Replay", new Vector2(-100, -100));
                AddButtonChild(root.transform, "HomeButton", "Home", new Vector2(100, -100));

                // Stars container
                if (root.transform.Find("Stars") == null)
                {
                    var starsGo = new GameObject("Stars", typeof(RectTransform));
                    starsGo.transform.SetParent(root.transform, false);
                    var rt = starsGo.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(0, 50);
                    rt.sizeDelta = new Vector2(300, 80);
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
                Debug.Log("[Phase1] GameOverPanel: configured");
            }

            // BoosterButton - icon + count
            {
                string path = "Assets/Prefabs/UI/BoosterButton.prefab";
                var root = PrefabUtility.LoadPrefabContents(path);

                var img = root.GetComponent<UnityEngine.UI.Image>();
                if (img != null) img.color = new Color(0.2f, 0.7f, 0.3f, 0.85f);

                if (root.transform.Find("Icon") == null)
                {
                    var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                    iconGo.transform.SetParent(root.transform, false);
                    var rt = iconGo.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.1f, 0.2f);
                    rt.anchorMax = new Vector2(0.6f, 0.8f);
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
                Debug.Log("[Phase1] BoosterButton: configured");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[Phase1] All UI prefabs configured!");
        }

        static void AddTextChild(Transform parent, string name, string text, int fontSize)
        {
            if (parent.Find(name) != null) return;
            var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<UnityEngine.UI.Text>();
            t.text = text;
            t.alignment = TextAnchor.MiddleCenter;
            t.fontSize = fontSize;
            t.color = Color.white;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static void AddButtonChild(Transform parent, string name, string label, Vector2 pos)
        {
            if (parent.Find(name) != null) return;
            var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(180, 60);
            AddTextChild(go.transform, "Text", label, 32);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
