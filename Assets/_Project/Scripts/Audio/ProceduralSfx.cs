using System;
using UnityEngine;

namespace PixelImmortal.Audio
{
    public enum Sfx
    {
        Hit, Crit, Kill, PlayerHurt, Death, Revive,
        CastSword, CastFire, CastIce, CastShield, CastBlades,
        BossWin, BossLose, MapChange,
        Click, Buy, Claim, Breakthrough, Drop, Pull
    }

    /// <summary>
    /// Placeholder sound effects synthesized from simple waveforms (no audio files). Pure functions that return
    /// mono samples in [-1, 1], so they are testable and can be replaced by real clips later behind the same enum.
    /// </summary>
    public static class ProceduralSfx
    {
        public const int SampleRate = 22050;

        enum Wave { Sine, Square, Triangle, Noise }

        public static float[] Samples(Sfx sfx)
        {
            switch (sfx)
            {
                case Sfx.Hit: return Mix(Noise(0.05f, 0.35f, 3f, 1), Tone(190f, 130f, 0.06f, Wave.Sine, 0.3f, 2f));
                case Sfx.Crit: return Mix(Noise(0.07f, 0.4f, 3f, 1), Tone(900f, 1500f, 0.1f, Wave.Triangle, 0.3f, 1.5f), Tone(180f, 120f, 0.08f, Wave.Sine, 0.3f, 2f));
                case Sfx.Kill: return Tone(520f, 140f, 0.13f, Wave.Square, 0.2f, 1.5f);
                case Sfx.PlayerHurt: return Mix(Tone(130f, 70f, 0.14f, Wave.Sine, 0.45f, 2f), Noise(0.1f, 0.25f, 3f, 2));
                case Sfx.Death: return Tone(420f, 50f, 0.55f, Wave.Triangle, 0.4f, 1.2f);
                case Sfx.Revive: return Tone(200f, 820f, 0.32f, Wave.Triangle, 0.35f, 1f);

                case Sfx.CastSword: return Mix(Noise(0.14f, 0.3f, 1.5f, 0, true), Tone(1200f, 2400f, 0.12f, Wave.Triangle, 0.25f, 1.5f));
                case Sfx.CastFire: return Mix(Noise(0.22f, 0.32f, 1.2f, 3), Tone(180f, 520f, 0.22f, Wave.Sine, 0.35f, 1f));
                case Sfx.CastIce: return Mix(Tone(1318f, 1318f, 0.3f, Wave.Sine, 0.3f, 2f), Tone(1760f, 1760f, 0.3f, Wave.Sine, 0.22f, 2.4f, 0.06f));
                case Sfx.CastShield: return Mix(Tone(440f, 440f, 0.32f, Wave.Sine, 0.3f, 1f, 0f, 0.08f), Tone(660f, 660f, 0.32f, Wave.Sine, 0.22f, 1f, 0f, 0.08f));
                case Sfx.CastBlades:
                    return Arp(new[] { 1500f, 1900f, 2300f, 1700f, 2500f, 2100f }, 0.035f, 0.07f, Wave.Triangle, 0.28f);

                case Sfx.BossWin: return Arp(new[] { 523f, 659f, 784f, 1046f }, 0.11f, 0.2f, Wave.Triangle, 0.4f);
                case Sfx.BossLose: return Mix(Tone(330f, 90f, 0.6f, Wave.Triangle, 0.4f, 1f), Tone(247f, 70f, 0.6f, Wave.Sine, 0.3f, 1f, 0.1f));
                case Sfx.MapChange: return Tone(150f, 620f, 0.55f, Wave.Sine, 0.3f, 0.8f, 0f, 0.15f);

                case Sfx.Click: return Tone(880f, 760f, 0.03f, Wave.Square, 0.18f, 1.5f);
                case Sfx.Buy: return Arp(new[] { 880f, 1320f }, 0.07f, 0.12f, Wave.Triangle, 0.35f);
                case Sfx.Claim: return Arp(new[] { 660f, 880f, 1100f }, 0.07f, 0.16f, Wave.Triangle, 0.38f);
                case Sfx.Breakthrough: return Mix(Tone(200f, 1600f, 0.65f, Wave.Triangle, 0.35f, 1f, 0f, 0.1f), Arp(new[] { 523f, 659f, 784f }, 0.12f, 0.3f, Wave.Sine, 0.3f, 0.3f));
                case Sfx.Drop: return Arp(new[] { 1500f, 2000f, 2600f }, 0.05f, 0.1f, Wave.Sine, 0.3f);
                case Sfx.Pull: return Mix(Noise(0.09f, 0.25f, 2f, 1), Noise(0.09f, 0.25f, 2f, 2, false, 0.1f), Arp(new[] { 1046f, 1568f }, 0.08f, 0.16f, Wave.Triangle, 0.3f, 0.22f));
            }
            return new float[SampleRate / 100];
        }

        public static AudioClip CreateClip(Sfx sfx)
        {
            var s = Samples(sfx);
            var clip = AudioClip.Create("sfx_" + sfx, s.Length, 1, SampleRate, false);
            clip.SetData(s, 0);
            return clip;
        }

        // ---- building blocks -----------------------------------------------------------------

        static float Envelope(float t, float dur, float attack, float decayPower)
        {
            float a = attack > 0f ? Mathf.Min(1f, t / attack) : 1f;
            float d = Mathf.Pow(Mathf.Max(0f, 1f - t / dur), decayPower);
            return a * d;
        }

        /// <summary>Tone sliding from f0 to f1. <paramref name="start"/> delays it; <paramref name="attack"/> softens the onset.</summary>
        static float[] Tone(float f0, float f1, float dur, Wave wave, float volume, float decayPower, float start = 0f, float attack = 0.004f)
        {
            int lead = (int)(start * SampleRate), n = (int)(dur * SampleRate);
            var s = new float[lead + n];
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate, k = i / (float)n;
                double f = f0 + (f1 - f0) * k;
                phase += f / SampleRate;
                float p = (float)(phase - Math.Floor(phase));
                float v = wave == Wave.Sine ? Mathf.Sin(p * Mathf.PI * 2f)
                        : wave == Wave.Square ? (p < 0.5f ? 1f : -1f)
                        : 1f - 4f * Mathf.Abs(p - 0.5f);
                s[lead + i] = v * volume * Envelope(t, dur, attack, decayPower);
            }
            return s;
        }

        /// <summary>Noise burst. <paramref name="smooth"/> low-passes (rumble); <paramref name="highpass"/> keeps only the hiss.</summary>
        static float[] Noise(float dur, float volume, float decayPower, int smooth, bool highpass = false, float start = 0f)
        {
            int lead = (int)(start * SampleRate), n = (int)(dur * SampleRate);
            var s = new float[lead + n];
            var rng = new System.Random(1234 + smooth * 17 + n);
            float last = 0f;
            for (int i = 0; i < n; i++)
            {
                float v = (float)(rng.NextDouble() * 2 - 1);
                float lp = smooth > 0 ? last + (v - last) / (1 + smooth) : v;
                last = lp;
                float o = highpass ? v - lp : lp;
                s[lead + i] = o * volume * Envelope(i / (float)SampleRate, dur, 0.002f, decayPower);
            }
            return s;
        }

        /// <summary>Short notes one after another; each note lasts <paramref name="noteLen"/>.</summary>
        static float[] Arp(float[] freqs, float step, float noteLen, Wave wave, float volume, float start = 0f)
        {
            float[] mix = new float[0];
            for (int i = 0; i < freqs.Length; i++)
                mix = Mix(mix, Tone(freqs[i], freqs[i], noteLen, wave, volume, 1.6f, start + i * step));
            return mix;
        }

        static float[] Mix(params float[][] parts)
        {
            int n = 0;
            foreach (var p in parts) n = Math.Max(n, p.Length);
            var o = new float[n];
            foreach (var p in parts) for (int i = 0; i < p.Length; i++) o[i] += p[i];
            // soft limiter keeps stacked layers inside [-1, 1]
            for (int i = 0; i < n; i++) o[i] = (float)Math.Tanh(o[i]);
            return o;
        }
    }
}
