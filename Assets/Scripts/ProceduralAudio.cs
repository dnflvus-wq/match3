using UnityEngine;

namespace Match3
{
    public static class ProceduralAudio
    {
        public static AudioClip GenerateMatch()
        {
            // 밝은 "딩!" 소리
            return GenerateTone(880f, 0.12f, 0.6f, fadeOut: true);
        }

        public static AudioClip GenerateSwap()
        {
            // 짧은 "슉" 소리 (노이즈 기반)
            int sampleRate = 44100;
            float duration = 0.08f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = 1f - (t / duration);
                float freq = Mathf.Lerp(600f, 1200f, t / duration);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.3f;
            }

            return CreateClip("swap", data, sampleRate);
        }

        public static AudioClip GenerateFail()
        {
            // 낮은 "뿅" 소리
            int sampleRate = 44100;
            float duration = 0.2f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = 1f - (t / duration);
                float freq = Mathf.Lerp(300f, 150f, t / duration);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.4f;
            }

            return CreateClip("fail", data, sampleRate);
        }

        public static AudioClip GenerateCombo()
        {
            // 상승 아르페지오
            int sampleRate = 44100;
            float duration = 0.3f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];

            float[] notes = { 523f, 659f, 784f, 1047f }; // C5, E5, G5, C6

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                int noteIdx = Mathf.Min((int)(t / duration * notes.Length), notes.Length - 1);
                float envelope = 1f - (t / duration) * 0.5f;
                data[i] = Mathf.Sin(2f * Mathf.PI * notes[noteIdx] * t) * envelope * 0.4f;
            }

            return CreateClip("combo", data, sampleRate);
        }

        public static AudioClip GenerateSpecial()
        {
            // 화려한 "차밍" 소리
            int sampleRate = 44100;
            float duration = 0.25f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Max(0, 1f - t / duration);
                float freq = 1200f + Mathf.Sin(t * 40f) * 200f;
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.35f;
            }

            return CreateClip("special", data, sampleRate);
        }

        public static AudioClip GenerateWin()
        {
            // 팡파레 (상승 3화음)
            int sampleRate = 44100;
            float duration = 0.8f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];

            float[][] chords = {
                new[] { 523f, 659f, 784f },  // C major
                new[] { 587f, 740f, 880f },  // D major
                new[] { 659f, 831f, 988f },  // E major
            };

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                int chordIdx = Mathf.Min((int)(t / duration * chords.Length), chords.Length - 1);
                float envelope = Mathf.Max(0, 1f - t / duration * 0.3f);
                float sample = 0;
                foreach (float freq in chords[chordIdx])
                {
                    sample += Mathf.Sin(2f * Mathf.PI * freq * t);
                }
                data[i] = sample / 3f * envelope * 0.4f;
            }

            return CreateClip("win", data, sampleRate);
        }

        public static AudioClip GenerateLose()
        {
            // 슬픈 하강음
            int sampleRate = 44100;
            float duration = 0.5f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Max(0, 1f - t / duration);
                float freq = Mathf.Lerp(400f, 150f, t / duration);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.35f;
            }

            return CreateClip("lose", data, sampleRate);
        }

        private static AudioClip GenerateTone(float freq, float duration, float volume, bool fadeOut = false)
        {
            int sampleRate = 44100;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = fadeOut ? (1f - t / duration) : 1f;
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * volume;
            }

            return CreateClip("tone_" + freq, data, sampleRate);
        }

        private static AudioClip CreateClip(string name, float[] data, int sampleRate)
        {
            AudioClip clip = AudioClip.Create(name, data.Length, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
