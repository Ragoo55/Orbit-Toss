using UnityEngine;

namespace OrbitToss
{
    /// <summary>
    /// Sound cues and optional background music. All clips are generated procedurally at start-up (soft sine tones),
    /// so the prototype needs no audio files. To use real sounds later, assign AudioClips in the Inspector - any
    /// assigned clip replaces the generated placeholder.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Header("Optional replacement clips (leave empty to use generated placeholders)")]
        [SerializeField] private AudioClip grabClip;
        [SerializeField] private AudioClip throwClip;
        [SerializeField] private AudioClip knockClip;
        [SerializeField] private AudioClip perfectClip;
        [SerializeField] private AudioClip goodClip;
        [SerializeField] private AudioClip tryAgainClip;
        [SerializeField] private AudioClip uiClickClip;
        [SerializeField] private AudioClip musicClip;

        [Header("References")]
        [SerializeField] private SettingsManager settings;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource musicSource;

        private const int SampleRate = 44100;

        private void Awake()
        {
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;

            if (grabClip == null) grabClip = Tone("Grab", new[] { 660f }, 0.08f, 0.35f);
            if (throwClip == null) throwClip = Sweep("Throw", 320f, 720f, 0.18f, 0.3f);
            if (knockClip == null) knockClip = Knock("Knock", 0.09f);
            if (perfectClip == null) perfectClip = Arpeggio("Perfect", new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.12f, 0.35f);
            if (goodClip == null) goodClip = Arpeggio("Good", new[] { 523.25f, 659.25f }, 0.14f, 0.35f);
            if (tryAgainClip == null) tryAgainClip = Arpeggio("TryAgain", new[] { 392f, 329.63f }, 0.16f, 0.3f);
            if (uiClickClip == null) uiClickClip = Tone("Click", new[] { 900f }, 0.04f, 0.25f);
            if (musicClip == null) musicClip = AmbientPad("SpaceAmbience", 16f);
            musicSource.clip = musicClip;
        }

        private void OnEnable() { if (settings != null) settings.Changed += ApplySettings; }
        private void OnDisable() { if (settings != null) settings.Changed -= ApplySettings; }
        private void Start() { ApplySettings(); }

        private void ApplySettings()
        {
            float master = settings != null ? settings.MasterVolume : 0.8f;
            float music = settings != null ? settings.MusicVolume : 0.4f;
            bool musicOn = settings == null || settings.MusicEnabled;

            AudioListener.volume = master;
            musicSource.volume = music;
            if (musicOn && music > 0f && !musicSource.isPlaying) musicSource.Play();
            if ((!musicOn || music <= 0f) && musicSource.isPlaying) musicSource.Stop();
        }

        private float Sfx => settings != null ? settings.SfxVolume : 0.8f;

        public void PlayGrab() => Play(grabClip, 1f);
        public void PlayThrow() => Play(throwClip, 1f);
        public void PlayUiClick() => Play(uiClickClip, 1f);

        public void PlayFeedback(FeedbackKind kind)
        {
            switch (kind)
            {
                case FeedbackKind.Perfect: Play(perfectClip, 1f); break;
                case FeedbackKind.Good: Play(goodClip, 1f); break;
                default: Play(tryAgainClip, 1f); break;
            }
        }

        /// <summary>Spatial knock at the impact point, louder for harder hits.</summary>
        public void PlayKnock(Vector3 position, float impactSpeed)
        {
            float v = Mathf.Clamp01(impactSpeed / 4f) * Sfx;
            if (v < 0.03f || knockClip == null) return;
            AudioSource.PlayClipAtPoint(knockClip, position, v);
        }

        private void Play(AudioClip clip, float scale)
        {
            if (clip == null) return;
            sfxSource.PlayOneShot(clip, Sfx * scale);
        }

        // ---------------------------------------------------------------- procedural placeholder clips

        private static AudioClip Tone(string clipName, float[] freqs, float duration, float amplitude)
        {
            int n = Mathf.CeilToInt(SampleRate * duration);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float env = Envelope(t, duration, 0.005f);
                float s = 0f;
                foreach (float f in freqs) s += Mathf.Sin(2f * Mathf.PI * f * t);
                data[i] = s / freqs.Length * env * amplitude;
            }
            return Make(clipName, data);
        }

        private static AudioClip Sweep(string clipName, float f0, float f1, float duration, float amplitude)
        {
            int n = Mathf.CeilToInt(SampleRate * duration);
            var data = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float f = Mathf.Lerp(f0, f1, t / duration);
                phase += 2f * Mathf.PI * f / SampleRate;
                data[i] = Mathf.Sin(phase) * Envelope(t, duration, 0.02f) * amplitude;
            }
            return Make(clipName, data);
        }

        private static AudioClip Knock(string clipName, float duration)
        {
            int n = Mathf.CeilToInt(SampleRate * duration);
            var data = new float[n];
            var rng = new System.Random(7);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float decay = Mathf.Exp(-t * 60f);
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0) * 0.3f;
                data[i] = (Mathf.Sin(2f * Mathf.PI * 180f * t) * 0.7f + noise) * decay;
            }
            return Make(clipName, data);
        }

        private static AudioClip Arpeggio(string clipName, float[] notes, float noteLength, float amplitude)
        {
            int perNote = Mathf.CeilToInt(SampleRate * noteLength);
            int tail = SampleRate / 4;
            var data = new float[perNote * notes.Length + tail];
            for (int k = 0; k < notes.Length; k++)
            {
                int start = k * perNote;
                float length = noteLength + 0.25f;
                for (int i = 0; i < Mathf.CeilToInt(SampleRate * length) && start + i < data.Length; i++)
                {
                    float t = (float)i / SampleRate;
                    data[start + i] += Mathf.Sin(2f * Mathf.PI * notes[k] * t) * Envelope(t, length, 0.01f) * amplitude * 0.6f;
                }
            }
            return Make(clipName, data);
        }

        /// <summary>A slow, calm chord loop (no rhythm, no sudden changes).</summary>
        private static AudioClip AmbientPad(string clipName, float duration)
        {
            int n = Mathf.CeilToInt(SampleRate * duration);
            var data = new float[n];
            float[][] chords =
            {
                new[] { 130.81f, 196f, 261.63f, 329.63f },   // C
                new[] { 110f, 164.81f, 220f, 261.63f },      // Am
                new[] { 87.31f, 130.81f, 174.61f, 220f },    // F
                new[] { 98f, 146.83f, 196f, 246.94f }        // G
            };
            float chordLength = duration / chords.Length;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                int c = Mathf.Min((int)(t / chordLength), chords.Length - 1);
                float local = t - c * chordLength;
                float env = Mathf.Clamp01(local / 1.5f) * Mathf.Clamp01((chordLength - local) / 1.5f);
                float s = 0f;
                foreach (float f in chords[c]) s += Mathf.Sin(2f * Mathf.PI * f * t);
                data[i] = s / chords[c].Length * env * 0.12f;
            }
            return Make(clipName, data);
        }

        private static float Envelope(float t, float duration, float attack)
        {
            float a = Mathf.Clamp01(t / attack);
            float r = Mathf.Clamp01((duration - t) / (duration * 0.6f));
            return a * r;
        }

        private static AudioClip Make(string clipName, float[] data)
        {
            AudioClip clip = AudioClip.Create(clipName, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
