using System;
using UnityEngine;

namespace PixelImmortal.Audio
{
    /// <summary>
    /// Plays sound effects through a small pool of voices. Each effect has a minimum repeat interval so a busy
    /// fight cannot turn into a wall of noise, and every play can be muted globally (saved by the caller).
    /// </summary>
    public class AudioService : MonoBehaviour
    {
        const int Voices = 8;
        const float MasterVolume = 0.6f;

        AudioSource[] voices;
        AudioClip[] clips;
        float[] lastPlayed;
        int nextVoice;

        public bool Muted { get; private set; }

        /// <summary>Sounds that actually started (not muted, not throttled). For diagnostics and tests.</summary>
        public int PlayCount { get; private set; }
        public Sfx LastSfx { get; private set; }
        public event Action MutedChanged;

        /// <summary>Per-effect minimum seconds between plays.</summary>
        static float MinInterval(Sfx s)
        {
            switch (s)
            {
                case Sfx.Hit: case Sfx.Crit: return 0.12f;
                case Sfx.Kill: return 0.07f;
                case Sfx.PlayerHurt: return 0.2f;
                case Sfx.Click: return 0.03f;
                default: return 0.05f;
            }
        }

        public void Init(bool muted)
        {
            Muted = muted;
            var values = (Sfx[])Enum.GetValues(typeof(Sfx));
            clips = new AudioClip[values.Length];
            lastPlayed = new float[values.Length];
            for (int i = 0; i < lastPlayed.Length; i++) lastPlayed[i] = -10f;
            foreach (var v in values) clips[(int)v] = ProceduralSfx.CreateClip(v);

            voices = new AudioSource[Voices];
            for (int i = 0; i < Voices; i++)
            {
                var src = new GameObject("Voice" + i).AddComponent<AudioSource>();
                src.transform.SetParent(transform, false);
                src.playOnAwake = false;
                src.spatialBlend = 0f;
                voices[i] = src;
            }
        }

        public void SetMuted(bool muted)
        {
            if (muted == Muted) return;
            Muted = muted;
            if (muted) foreach (var v in voices) v.Stop();
            MutedChanged?.Invoke();
        }

        public void Toggle() => SetMuted(!Muted);

        public void Play(Sfx sfx, float volume = 1f, float pitchJitter = 0.04f)
        {
            if (Muted || voices == null) return;
            int i = (int)sfx;
            float now = Time.unscaledTime;
            if (now - lastPlayed[i] < MinInterval(sfx)) return;
            lastPlayed[i] = now;

            var v = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            v.clip = clips[i];
            v.volume = Mathf.Clamp01(volume) * MasterVolume;
            v.pitch = 1f + UnityEngine.Random.Range(-pitchJitter, pitchJitter);
            v.Play();
            PlayCount++;
            LastSfx = sfx;
        }
    }
}
