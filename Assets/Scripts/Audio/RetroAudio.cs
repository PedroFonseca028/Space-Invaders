using UnityEngine;

namespace SpaceInvaders
{
    // Every sound effect is synthesized on the fly (square/sine sweeps plus a
    // little noise), so the game needs zero imported audio assets.
    public static class RetroAudio
    {
        private static AudioSource[] _pool;
        private static int _poolIndex;

        private static AudioClip _shoot, _explosion, _playerHit, _bossExplode, _bossHum;
        private static AudioClip _gameOver, _victory, _levelUp, _uiBlip;
        private static AudioClip[] _steps;

        private static AudioClip CreateClip(string name, float duration, System.Func<float, float> sample)
        {
            const int sampleRate = 44100;
            int length = Mathf.Max(1, Mathf.CeilToInt(duration * sampleRate));
            var data = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sampleRate;
                data[i] = Mathf.Clamp(sample(t), -1f, 1f);
            }
            var clip = AudioClip.Create(name, length, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float Square(float freq, float t) => Mathf.Sign(Mathf.Sin(2f * Mathf.PI * freq * t));
        private static float Sine(float freq, float t) => Mathf.Sin(2f * Mathf.PI * freq * t);

        private static AudioClip Sequence(string name, float[] freqs, float noteDur, bool square)
        {
            float total = freqs.Length * noteDur;
            return CreateClip(name, total, t =>
            {
                int idx = Mathf.Min(freqs.Length - 1, (int)(t / noteDur));
                float localT = t - idx * noteDur;
                float env = 1f - (localT / noteDur) * 0.7f;
                float f = freqs[idx];
                float wave = square ? Square(f, t) : Sine(f, t);
                return wave * env * 0.45f;
            });
        }

        private static void EnsureClips()
        {
            if (_shoot != null) return;

            _shoot = CreateClip("sfx_shoot", 0.12f, t =>
            {
                float freq = Mathf.Lerp(1300f, 500f, t / 0.12f);
                float env = 1f - t / 0.12f;
                return Square(freq, t) * env * 0.3f;
            });

            _explosion = CreateClip("sfx_explosion", 0.28f, t =>
            {
                float env = Mathf.Exp(-t * 12f);
                float noise = Random.value * 2f - 1f;
                float tone = Sine(Mathf.Lerp(220f, 60f, t / 0.28f), t);
                return (noise * 0.6f + tone * 0.4f) * env * 0.5f;
            });

            _playerHit = CreateClip("sfx_playerhit", 0.5f, t =>
            {
                float env = Mathf.Exp(-t * 5f);
                float noise = Random.value * 2f - 1f;
                float tone = Sine(Mathf.Lerp(300f, 40f, t / 0.5f), t);
                return (noise * 0.5f + tone * 0.5f) * env * 0.6f;
            });

            _bossExplode = CreateClip("sfx_bossexplode", 0.55f, t =>
            {
                float env = Mathf.Exp(-t * 5.5f);
                float noise = Random.value * 2f - 1f;
                float tone = Sine(Mathf.Lerp(160f, 30f, t / 0.55f), t);
                return (noise * 0.5f + tone * 0.5f) * env * 0.7f;
            });

            _bossHum = CreateClip("sfx_bosshum", 0.5f, t =>
            {
                float vibrato = Mathf.Sin(2f * Mathf.PI * 7f * t) * 45f;
                return Sine(480f + vibrato, t) * 0.25f;
            });

            _steps = new AudioClip[4];
            float[] stepFreqs = { 110f, 98f, 87f, 82f };
            for (int i = 0; i < 4; i++)
            {
                float f = stepFreqs[i];
                _steps[i] = CreateClip("sfx_step" + i, 0.09f, t =>
                {
                    float env = 1f - t / 0.09f;
                    return Square(f, t) * env * 0.5f;
                });
            }

            _gameOver = Sequence("sfx_gameover", new[] { 400f, 320f, 240f, 160f }, 0.22f, true);
            _victory = Sequence("sfx_victory", new[] { 392f, 494f, 587f, 784f }, 0.2f, false);
            _levelUp = Sequence("sfx_levelup", new[] { 523f, 784f }, 0.14f, false);
            _uiBlip = Sequence("sfx_ui", new[] { 660f }, 0.07f, true);
        }

        private static void EnsurePool()
        {
            if (_pool != null && _pool.Length > 0 && _pool[0] != null) return;

            var go = new GameObject("RetroAudioService");
            Object.DontDestroyOnLoad(go);
            _pool = new AudioSource[6];
            for (int i = 0; i < _pool.Length; i++)
            {
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f;
                _pool[i] = src;
            }
            _poolIndex = 0;
        }

        private static void PlayOneShot(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;
            EnsurePool();
            _pool[_poolIndex].PlayOneShot(clip, volume);
            _poolIndex = (_poolIndex + 1) % _pool.Length;
        }

        public static void PlayShoot() { EnsureClips(); PlayOneShot(_shoot); }
        public static void PlayExplosion() { EnsureClips(); PlayOneShot(_explosion); }
        public static void PlayPlayerHit() { EnsureClips(); PlayOneShot(_playerHit); }
        public static void PlayBossExplode() { EnsureClips(); PlayOneShot(_bossExplode, 1f); }
        public static void PlayStep(int index) { EnsureClips(); PlayOneShot(_steps[((index % 4) + 4) % 4], 0.8f); }
        public static void PlayGameOver() { EnsureClips(); PlayOneShot(_gameOver); }
        public static void PlayVictory() { EnsureClips(); PlayOneShot(_victory); }
        public static void PlayLevelUp() { EnsureClips(); PlayOneShot(_levelUp); }
        public static void PlayUiBlip() { EnsureClips(); PlayOneShot(_uiBlip, 0.6f); }

        public static AudioClip GetBossHumClip()
        {
            EnsureClips();
            return _bossHum;
        }
    }
}
