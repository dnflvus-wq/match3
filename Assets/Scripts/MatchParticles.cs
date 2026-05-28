using UnityEngine;

namespace Match3
{
    public class MatchParticles : MonoBehaviour
    {
        public static MatchParticles Instance { get; private set; }

        [Header("Particle Prefabs (Phase 1 에셋)")]
        [SerializeField] private ParticleSystem sparkPrefab;
        [SerializeField] private ParticleSystem starPrefab;
        [SerializeField] private ParticleSystem glowPrefab;
        [SerializeField] private ParticleSystem ringPrefab;

        private void Awake()
        {
            Instance = this;
            LoadPrefabsIfNeeded();
        }

        private void LoadPrefabsIfNeeded()
        {
#if UNITY_EDITOR
            if (sparkPrefab == null) sparkPrefab = LoadEditorPrefab("Assets/Prefabs/Effects/MatchBurst.prefab");
            if (starPrefab == null) starPrefab = LoadEditorPrefab("Assets/Prefabs/Effects/MatchStar.prefab");
            if (glowPrefab == null) glowPrefab = LoadEditorPrefab("Assets/Prefabs/Effects/MatchGlow.prefab");
            if (ringPrefab == null) ringPrefab = LoadEditorPrefab("Assets/Prefabs/Effects/MatchRing.prefab");
#endif
        }

#if UNITY_EDITOR
        private static ParticleSystem LoadEditorPrefab(string assetPath)
        {
            var go = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            return go != null ? go.GetComponent<ParticleSystem>() : null;
        }
#endif

        public void PlayAt(Vector3 position, Color color)
        {
            PlayPrefab(sparkPrefab, position, color);
            PlayPrefab(starPrefab, position, color);
            PlayPrefab(glowPrefab, position, new Color(color.r, color.g, color.b, 0.5f));
            PlayPrefab(ringPrefab, position, new Color(color.r * 0.8f + 0.2f, color.g * 0.8f + 0.2f, color.b * 0.8f + 0.2f, 0.6f));
        }

        public void PlayBigAt(Vector3 position, Color color)
        {
            var spark = PlayPrefab(sparkPrefab, position, color);
            if (spark != null) { var m = spark.main; m.startSpeed = new ParticleSystem.MinMaxCurve(6f, 12f); m.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.4f); }

            var star = PlayPrefab(starPrefab, position, color);
            if (star != null) { var m = star.main; m.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.6f); }

            var glow = PlayPrefab(glowPrefab, position, new Color(color.r, color.g, color.b, 0.8f));
            if (glow != null) { var m = glow.main; m.startSize = 0.6f; }

            var ring = PlayPrefab(ringPrefab, position, color);
            if (ring != null) { var m = ring.main; m.startSpeed = new ParticleSystem.MinMaxCurve(5f, 10f); m.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f); }
        }

        private ParticleSystem PlayPrefab(ParticleSystem prefab, Vector3 position, Color color)
        {
            if (prefab == null) return null;
            var go = Instantiate(prefab.gameObject, position, Quaternion.identity);
            go.SetActive(true);

            var ps = go.GetComponent<ParticleSystem>();
            var main = ps.main;
            main.startColor = color;

            ps.Play();
            Destroy(go, main.duration + main.startLifetime.constantMax + 0.5f);
            return ps;
        }
    }
}
