using UnityEngine;

namespace Match3
{
    public class MatchParticles : MonoBehaviour
    {
        public static MatchParticles Instance { get; private set; }

        private ParticleSystem _sparkPrefab;
        private ParticleSystem _starPrefab;
        private ParticleSystem _glowPrefab;
        private ParticleSystem _ringPrefab;

        private void Awake()
        {
            Instance = this;
            CreateSparkPrefab();
            CreateStarPrefab();
            CreateGlowPrefab();
            CreateRingPrefab();
        }

        private void CreateSparkPrefab()
        {
            var go = new GameObject("SparkParticle");
            go.transform.SetParent(transform);
            go.SetActive(false);

            _sparkPrefab = go.AddComponent<ParticleSystem>();
            var main = _sparkPrefab.main;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 9f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.25f);
            main.startColor = Color.white;
            main.maxParticles = 80;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 1.2f;

            var emission = _sparkPrefab.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 30, 45) });

            var shape = _sparkPrefab.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.1f;

            var sizeOverLifetime = _sparkPrefab.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0));

            var colorOverLifetime = _sparkPrefab.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 0.3f), new GradientColorKey(Color.yellow, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;

            var trail = _sparkPrefab.trails;
            trail.enabled = true;
            trail.ratio = 0.3f;
            trail.lifetime = 0.15f;
            trail.minVertexDistance = 0.05f;
            trail.dieWithParticles = true;
            trail.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0));

            var rotation = _sparkPrefab.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-180f, 180f);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            var mat = new Material(Shader.Find("Particles/Standard Unlit"));
            mat.SetFloat("_Mode", 1);
            renderer.material = mat;
            renderer.trailMaterial = mat;
            renderer.sortingOrder = 100;
        }

        private void CreateStarPrefab()
        {
            var go = new GameObject("StarParticle");
            go.transform.SetParent(transform);
            go.SetActive(false);

            _starPrefab = go.AddComponent<ParticleSystem>();
            var main = _starPrefab.main;
            main.duration = 1.2f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            main.startColor = Color.white;
            main.maxParticles = 40;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.6f;

            var emission = _starPrefab.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0.05f, 12, 18) });

            var shape = _starPrefab.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.15f;

            var sizeOverLifetime = _starPrefab.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            var sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0.3f);
            sizeCurve.AddKey(0.15f, 1f);
            sizeCurve.AddKey(0.5f, 0.8f);
            sizeCurve.AddKey(1f, 0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var colorOverLifetime = _starPrefab.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.9f, 0.3f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;

            var rotation = _starPrefab.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-120f, 120f);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
            renderer.sortingOrder = 101;
        }

        private void CreateGlowPrefab()
        {
            var go = new GameObject("GlowParticle");
            go.transform.SetParent(transform);
            go.SetActive(false);

            _glowPrefab = go.AddComponent<ParticleSystem>();
            var main = _glowPrefab.main;
            main.duration = 0.6f;
            main.startLifetime = 0.5f;
            main.startSpeed = 0f;
            main.startSize = 0.4f;
            main.startColor = Color.white;
            main.maxParticles = 5;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = _glowPrefab.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

            var shape = _glowPrefab.shape;
            shape.enabled = false;

            var sizeOverLifetime = _glowPrefab.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            var sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0.3f);
            sizeCurve.AddKey(0.2f, 2f);
            sizeCurve.AddKey(0.5f, 3f);
            sizeCurve.AddKey(1f, 3.5f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var colorOverLifetime = _glowPrefab.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0.7f, 0f), new GradientAlphaKey(0.3f, 0.5f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            var mat = new Material(Shader.Find("Particles/Standard Unlit"));
            mat.SetFloat("_Mode", 1);
            renderer.material = mat;
            renderer.sortingOrder = 99;
        }

        private void CreateRingPrefab()
        {
            var go = new GameObject("RingParticle");
            go.transform.SetParent(transform);
            go.SetActive(false);

            _ringPrefab = go.AddComponent<ParticleSystem>();
            var main = _ringPrefab.main;
            main.duration = 0.4f;
            main.startLifetime = 0.4f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.startColor = Color.white;
            main.maxParticles = 40;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0f;

            var emission = _ringPrefab.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 24, 36) });

            var shape = _ringPrefab.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.05f;
            shape.radiusThickness = 0f; // 원 둘레에서만 방출

            var sizeOverLifetime = _ringPrefab.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0));

            var colorOverLifetime = _ringPrefab.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            var mat = new Material(Shader.Find("Particles/Standard Unlit"));
            mat.SetFloat("_Mode", 1);
            renderer.material = mat;
            renderer.sortingOrder = 102;
        }

        public void PlayAt(Vector3 position, Color color)
        {
            PlayPrefab(_sparkPrefab, position, color);
            PlayPrefab(_starPrefab, position, color);
            PlayPrefab(_glowPrefab, position, new Color(color.r, color.g, color.b, 0.5f));
            PlayPrefab(_ringPrefab, position, new Color(color.r * 0.8f + 0.2f, color.g * 0.8f + 0.2f, color.b * 0.8f + 0.2f, 0.6f));
        }

        public void PlayBigAt(Vector3 position, Color color)
        {
            var spark = PlayPrefab(_sparkPrefab, position, color);
            if (spark != null) { var m = spark.main; m.startSpeed = new ParticleSystem.MinMaxCurve(6f, 12f); m.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.4f); }

            var star = PlayPrefab(_starPrefab, position, color);
            if (star != null) { var m = star.main; m.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.6f); }

            var glow = PlayPrefab(_glowPrefab, position, new Color(color.r, color.g, color.b, 0.8f));
            if (glow != null) { var m = glow.main; m.startSize = 0.6f; }

            var ring = PlayPrefab(_ringPrefab, position, color);
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
            Destroy(go, 2.5f);
            return ps;
        }
    }
}
