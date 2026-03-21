using UnityEngine;

namespace Match3
{
    public class MatchParticles : MonoBehaviour
    {
        public static MatchParticles Instance { get; private set; }

        private ParticleSystem _sparkPrefab;

        private void Awake()
        {
            Instance = this;
            CreateSparkPrefab();
        }

        private void CreateSparkPrefab()
        {
            // 프리팹 대신 코드로 파티클 시스템 생성
            var go = new GameObject("SparkParticle");
            go.transform.SetParent(transform);
            go.SetActive(false);

            _sparkPrefab = go.AddComponent<ParticleSystem>();
            var main = _sparkPrefab.main;
            main.duration = 0.5f;
            main.startLifetime = 0.4f;
            main.startSpeed = 3f;
            main.startSize = 0.15f;
            main.startColor = Color.white;
            main.maxParticles = 20;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = _sparkPrefab.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });

            var shape = _sparkPrefab.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.2f;

            var sizeOverLifetime = _sparkPrefab.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0));

            var colorOverLifetime = _sparkPrefab.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;

            // 렌더러
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
            renderer.material.color = Color.white;
        }

        public void PlayAt(Vector3 position, Color color)
        {
            var go = Instantiate(_sparkPrefab.gameObject, position, Quaternion.identity);
            go.SetActive(true);

            var ps = go.GetComponent<ParticleSystem>();
            var main = ps.main;
            main.startColor = color;

            ps.Play();

            Destroy(go, 1f);
        }
    }
}
