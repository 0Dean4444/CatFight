using System;
using System.IO;
using UnityEngine;

namespace CatFight.Managers
{
    public class CatFightAudioManager : MonoBehaviour
    {
        public static CatFightAudioManager Instance { get; private set; }

        public float BgmVolume { get; private set; } = 0.65f;
        public float SfxVolume { get; private set; } = 0.85f;
        public bool IsMuted { get; private set; } = false;
        public bool IsPlayingBossBgm { get; private set; } = false;

        private AudioSource bgmSource;
        private AudioSource sfxSource;
        private AudioSource menuCatVoiceSource;

        private static AudioClip chillBgmClip;
        private static AudioClip bossBgmClip;
        private static AudioClip bossWarningClip;
        private static AudioClip shootClip;
        private static AudioClip skillClip;
        private static AudioClip hitClip;
        private static AudioClip uiClickClip;
        private static AudioClip gachaFanfareClip;
        private static AudioClip realMeowClip1;
        private static AudioClip realMeowClip2;

        private const float TouchVoiceInterval = 3.5f;
        private const float BattleVoiceInterval = 14f;
        private float lastVoiceTime = float.NegativeInfinity;
        private float ambientMeowTimer = 30f;
        private float lastShootSfxTime;
        private float lastHitSfxTime;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;

            menuCatVoiceSource = gameObject.AddComponent<AudioSource>();
            menuCatVoiceSource.loop = false;
            menuCatVoiceSource.playOnAwake = false;

            GenerateProceduralClips();
            UpdateAudioVolumes();
            PlayNormalChillBgm();
        }

        private void Update()
        {
            if (IsMuted || IsPlayingBossBgm || BgmVolume <= 0.02f) return;
            ambientMeowTimer -= Time.unscaledDeltaTime;
            if (ambientMeowTimer > 0f) return;

            bool inMenu = GameManager.Instance != null && GameManager.Instance.CurrentAppState == Data.GameAppState.MainMenu;
            ambientMeowTimer = inMenu ? UnityEngine.Random.Range(30f, 50f) : UnityEngine.Random.Range(45f, 65f);
            if (Time.unscaledTime - lastVoiceTime < BattleVoiceInterval) return;
            if (inMenu) PlayMenuShowcaseCatVoice(BgmVolume * 0.65f);
            else PlayCatVoice(null, BgmVolume * 0.50f);
        }

        public void PlayMenuShowcaseCatVoice(float volumeScale = 0.72f)
        {
            PlayCatVoice(GameManager.Instance?.GetMenuShowcaseCat()?.id, volumeScale);
        }

        public void PlayCatVoice(string catId, float volumeScale = 0.72f)
        {
            if (IsMuted || SfxVolume <= 0.01f || menuCatVoiceSource == null || menuCatVoiceSource.isPlaying
                || Time.unscaledTime - lastVoiceTime < TouchVoiceInterval) return;
            AudioClip primary = realMeowClip1 ?? realMeowClip2 ?? skillClip;
            AudioClip secondary = realMeowClip2 ?? realMeowClip1 ?? skillClip;
            AudioClip chosenClip = primary;
            float basePitch = 1.0f;

            switch (catId)
            {
                case "cat_stomp":
                    // Mèo Mập: Trầm ấm, mũm mĩm (Deep chubby meow)
                    chosenClip = primary;
                    basePitch = 0.76f;
                    break;
                case "cat_thrower":
                    // Mèo Nghịch Ngợm: Tinh nghịch, lanh lảnh (Playful cheeky meow)
                    chosenClip = secondary;
                    basePitch = 1.15f;
                    break;
                case "cat_yarn":
                    // Mèo Len Đỏ: Giọng mèo con trong trẻo, cao vút (Sweet high kitten meow)
                    chosenClip = primary;
                    basePitch = 1.34f;
                    break;
                case "cat_ninja":
                    // Mèo Ninja: Nhanh, dứt khoát (Sharp agile ninja meow)
                    chosenClip = secondary;
                    basePitch = 1.24f;
                    break;
                case "cat_mage":
                    // Mèo Phù Thủy: Huyền ảo kèm âm chuông phép thuật (Mystic melodic meow)
                    chosenClip = primary;
                    basePitch = 1.05f;
                    if (skillClip != null && sfxSource != null)
                        sfxSource.PlayOneShot(skillClip, volumeScale * 0.35f);
                    break;
                case "cat_laser":
                    // Mèo Bác Học: Trầm tĩnh thông thái (Calm intellectual meow)
                    chosenClip = secondary;
                    basePitch = 0.90f;
                    break;
                case "cat_royal":
                    // Mèo Hoàng Gia: Uy nghi quý tộc (Regal deep meow)
                    chosenClip = primary;
                    basePitch = 0.84f;
                    break;
                default:
                    chosenClip = UnityEngine.Random.value < 0.5f ? primary : secondary;
                    basePitch = 1.0f;
                    break;
            }

            if (chosenClip != null)
            {
                lastVoiceTime = Time.unscaledTime;
                menuCatVoiceSource.pitch = Mathf.Clamp(basePitch + UnityEngine.Random.Range(-0.03f, 0.03f), 0.55f, 1.65f);
                menuCatVoiceSource.PlayOneShot(chosenClip, volumeScale);
            }
        }

        public void SetBgmVolume(float val)
        {
            BgmVolume = Mathf.Clamp01(val);
            UpdateAudioVolumes();
        }

        public void SetSfxVolume(float val)
        {
            SfxVolume = Mathf.Clamp01(val);
            UpdateAudioVolumes();
        }

        public void SetMuted(bool muted)
        {
            IsMuted = muted;
            UpdateAudioVolumes();
        }

        private void UpdateAudioVolumes()
        {
            if (bgmSource != null)
            {
                bgmSource.mute = IsMuted;
                bgmSource.volume = BgmVolume * (IsPlayingBossBgm ? 0.34f : 0.32f);
            }
            if (sfxSource != null)
            {
                sfxSource.mute = IsMuted;
                sfxSource.volume = SfxVolume;
            }
            if (menuCatVoiceSource != null)
            {
                menuCatVoiceSource.mute = IsMuted;
                menuCatVoiceSource.volume = SfxVolume;
            }
        }

        public void PlayNormalChillBgm()
        {
            if (bgmSource == null || chillBgmClip == null) return;
            if (bgmSource.clip == chillBgmClip && bgmSource.isPlaying && !IsPlayingBossBgm)
                return;

            IsPlayingBossBgm = false;
            bgmSource.clip = chillBgmClip;
            UpdateAudioVolumes();
            bgmSource.Play();
        }

        public void PlayBossBgm()
        {
            PlayOneShot(bossWarningClip, 0.80f);
            if (bossBgmClip == null)
                bossBgmClip = CreateBossBattleBgmClip();
            if (bgmSource == null || bossBgmClip == null) return;
            if (bgmSource.clip == bossBgmClip && bgmSource.isPlaying && IsPlayingBossBgm)
                return;

            IsPlayingBossBgm = true;
            bgmSource.clip = bossBgmClip;
            UpdateAudioVolumes();
            bgmSource.Play();
        }

        public void PlayShoot()
        {
            if (Time.unscaledTime - lastShootSfxTime < 0.055f) return;
            lastShootSfxTime = Time.unscaledTime;
            PlayOneShot(shootClip, 0.26f);
        }

        public void PlaySkill()
        {
            if (Time.unscaledTime - lastVoiceTime >= BattleVoiceInterval)
                PlayCatVoice(null, 0.58f);
        }

        public void PlayHit()
        {
            if (Time.unscaledTime - lastHitSfxTime < 0.055f) return;
            lastHitSfxTime = Time.unscaledTime;
            PlayOneShot(hitClip, 0.32f);
        }

        public void PlayUiClick() => PlayOneShot(uiClickClip, 0.55f);
        public void PlayGacha() => PlayOneShot(gachaFanfareClip, 0.80f);

        private void PlayOneShot(AudioClip clip, float scale)
        {
            if (IsMuted || sfxSource == null || clip == null || SfxVolume <= 0.01f)
                return;
            sfxSource.PlayOneShot(clip, SfxVolume * scale);
        }

        private void GenerateProceduralClips()
        {
            if (shootClip == null) shootClip = CreateToneClip("CutePopShoot", 0.055f, 620f, 320f, 0.34f);
            if (hitClip == null) hitClip = CreateToneClip("SoftPopHit", 0.048f, 340f, 180f, 0.30f);
            if (uiClickClip == null) uiClickClip = CreateToneClip("CuteUiClick", 0.042f, 760f, 1040f, 0.36f);
            if (skillClip == null) skillClip = CreateArpeggioClip("CatMeowChime", new float[] { 523.25f, 659.25f, 783.99f }, 0.065f);
            if (gachaFanfareClip == null) gachaFanfareClip = CreateArpeggioClip("GachaFanfare", new float[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.09f);
            if (bossWarningClip == null) bossWarningClip = CreateBossWarningSfxClip();

            // Load real recorded cat meow audio clips from Assets/Resources/Audio/
            if (realMeowClip1 == null) realMeowClip1 = LoadRealWavAudioClip("Audio/real_Meow", "real_Meow.wav");
            if (realMeowClip2 == null) realMeowClip2 = LoadRealWavAudioClip("Audio/real_Meow2", "real_Meow2.wav");

            if (chillBgmClip == null) chillBgmClip = CreateChillCuteBgmClip();
        }

        private AudioClip LoadRealWavAudioClip(string resourcePath, string wavFileName)
        {
            AudioClip imported = Resources.Load<AudioClip>(resourcePath);
            if (imported != null)
                return imported;

            try
            {
                string fullPath = Path.Combine(Application.dataPath, "Resources", "Audio", wavFileName);
                if (File.Exists(fullPath))
                {
                    byte[] wavBytes = File.ReadAllBytes(fullPath);
                    return ParsePcm16WavToAudioClip(wavFileName, wavBytes);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CatFightAudioManager] Could not load WAV {wavFileName}: {e.Message}");
            }
            return null;
        }

        private static AudioClip ParsePcm16WavToAudioClip(string clipName, byte[] wav)
        {
            if (wav == null || wav.Length < 44) return null;

            int channels = BitConverter.ToInt16(wav, 22);
            int sampleRate = BitConverter.ToInt32(wav, 24);
            int bitsPerSample = BitConverter.ToInt16(wav, 34);
            if (channels < 1 || sampleRate < 4000 || bitsPerSample != 16)
                return null;

            // Locate "data" chunk
            int pos = 12;
            int dataOffset = 44;
            int dataSize = wav.Length - 44;
            while (pos + 8 <= wav.Length)
            {
                string chunkId = System.Text.Encoding.ASCII.GetString(wav, pos, 4);
                int chunkSize = BitConverter.ToInt32(wav, pos + 4);
                if (chunkId == "data")
                {
                    dataOffset = pos + 8;
                    dataSize = Mathf.Min(chunkSize, wav.Length - dataOffset);
                    break;
                }
                pos += 8 + Mathf.Max(0, chunkSize);
            }

            int totalSamples = dataSize / 2;
            int frameCount = totalSamples / channels;
            if (frameCount <= 0) return null;

            float[] samples = new float[totalSamples];
            for (int i = 0; i < totalSamples; i++)
            {
                short s = BitConverter.ToInt16(wav, dataOffset + i * 2);
                samples[i] = s / 32768f;
            }

            AudioClip clip = AudioClip.Create(clipName, frameCount, channels, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateToneClip(string name, float duration, float startFreq, float endFreq, float amp)
        {
            int sampleRate = 22050;
            int samples = Mathf.Max(1, Mathf.RoundToInt(duration * sampleRate));
            float[] data = new float[samples];
            float phase = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float freq = Mathf.Lerp(startFreq, endFreq, t);
                phase += 2f * Mathf.PI * freq / sampleRate;
                float attack = Mathf.Clamp01(t / 0.10f);
                float env = attack * (1f - t);
                data[i] = Mathf.Sin(phase) * amp * env;
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateArpeggioClip(string name, float[] notes, float noteLength)
        {
            int sampleRate = 22050;
            int noteSamples = Mathf.RoundToInt(noteLength * sampleRate);
            int totalSamples = noteSamples * notes.Length;
            float[] data = new float[totalSamples];
            float phase = 0f;

            for (int n = 0; n < notes.Length; n++)
            {
                float freq = notes[n];
                for (int i = 0; i < noteSamples; i++)
                {
                    float t = (float)i / noteSamples;
                    phase += 2f * Mathf.PI * freq / sampleRate;
                    float attack = Mathf.Clamp01(t / 0.08f);
                    float decay = Mathf.Exp(-t * 3.2f);
                    data[n * noteSamples + i] = Mathf.Sin(phase) * 0.42f * attack * decay;
                }
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateBossWarningSfxClip()
        {
            int sampleRate = 22050;
            float duration = 0.80f;
            int totalSamples = Mathf.RoundToInt(duration * sampleRate);
            float[] data = new float[totalSamples];
            float phase = 0f;

            for (int i = 0; i < totalSamples; i++)
            {
                float time = (float)i / sampleRate;
                float pulsePos = (time % 0.40f) / 0.40f;
                float env = Mathf.Sin(pulsePos * Mathf.PI);
                float freq = Mathf.Lerp(261.63f, 349.23f, pulsePos);
                phase += 2f * Mathf.PI * freq / sampleRate;
                data[i] = (Mathf.Sin(phase) * 0.75f + Mathf.Sin(phase * 2f) * 0.25f) * env * 0.48f;
            }

            AudioClip clip = AudioClip.Create("BossWarningSiren", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // =========================================================================
        // 1. MULTI-SECTION, MULTI-KEY & DYNAMIC-TEMPO KAWAII CAT CAFE BGM MODEL
        // Features 4 Musical Movements with Key Modulations & Tempo/Groove Shifts:
        //   - Section A (F Major, 110 BPM Bossa-Swing Groove): Warm Rhodes EP, Kalimba & Music Box
        //   - Section B (G Major Modulation, 126 BPM Upbeat Playful Shuffle): Marimba Arpeggios & High Celesta
        //   - Section C (Eb Major Pastel Lo-Fi Bridge, 94 BPM Dreamy Half-Time): Rolled Harp Chords & Lyrical Lead
        //   - Section D (C Major -> F Major Turnaround, 116 BPM Joyful Duet): Kalimba + Celesta Call & Response
        // =========================================================================
        private enum KawaiiVoiceType
        {
            RhodesChord,
            KalimbaPluck,
            CelestaBell,
            UprightBass,
            WoodBlockClick,
            SoftShaker,
            WarmSubKick
        }

        private static float MidiNoteToFreq(int midi)
        {
            if (midi <= 0) return 0f;
            return 440f * Mathf.Pow(2f, (midi - 69) / 12f);
        }

        private static void RenderSynthNote(float[] buffer, int sampleRate, KawaiiVoiceType voice, float startTimeSec, float durationSec, float freq, float amplitude)
        {
            if (buffer == null || startTimeSec < 0f) return;
            int startSample = Mathf.RoundToInt(startTimeSec * sampleRate);
            int totalNoteSamples = Mathf.RoundToInt(durationSec * sampleRate);
            if (startSample >= buffer.Length || totalNoteSamples <= 0) return;

            int endSample = Mathf.Min(buffer.Length, startSample + totalNoteSamples);
            float phase = 0f;
            float modPhase = 0f;
            float overtonePhase = 0f;
            uint rngState = (uint)(startSample * 747796405u + 2891336453u);

            for (int idx = startSample; idx < endSample; idx++)
            {
                int localSample = idx - startSample;
                float t = (float)localSample / sampleRate;
                float norm = (float)localSample / totalNoteSamples;
                float val = 0f;

                switch (voice)
                {
                    case KawaiiVoiceType.RhodesChord:
                    {
                        // Warm Fender Rhodes Electric Piano with soft FM bell tine and 4.2 Hz cozy tremolo
                        phase += 2f * Mathf.PI * freq / sampleRate;
                        modPhase += 2f * Mathf.PI * (freq * 2f) / sampleRate;
                        overtonePhase += 2f * Mathf.PI * (freq * 4f) / sampleRate;

                        float attack = Mathf.Clamp01(t / 0.018f);
                        float release = norm > 0.82f ? 0.5f * (1f + Mathf.Cos(Mathf.PI * ((norm - 0.82f) / 0.18f))) : 1f;
                        float bodyDecay = Mathf.Exp(-t * 1.45f);
                        float tineDecay = Mathf.Exp(-t * 9.5f);
                        float tremolo = 0.88f + 0.12f * Mathf.Sin(2f * Mathf.PI * 4.2f * t);

                        float fmMod = Mathf.Sin(modPhase) * 0.35f * Mathf.Exp(-t * 4.2f);
                        float body = Mathf.Sin(phase + fmMod) * 0.82f + Mathf.Sin(modPhase) * 0.14f;
                        float tine = Mathf.Sin(overtonePhase) * 0.16f * tineDecay;

                        val = (body * bodyDecay + tine) * attack * release * tremolo * amplitude;
                        break;
                    }

                    case KawaiiVoiceType.KalimbaPluck:
                    {
                        // Cute Wooden Kalimba / Marimba Thumb-Piano Pluck with woody chirp transient
                        phase += 2f * Mathf.PI * freq / sampleRate;
                        modPhase += 2f * Mathf.PI * (freq * 2.98f) / sampleRate;
                        overtonePhase += 2f * Mathf.PI * (freq * 2f) / sampleRate;

                        float attack = Mathf.Clamp01(t / 0.004f);
                        float release = norm > 0.85f ? (1f - (norm - 0.85f) / 0.15f) : 1f;
                        float decay = Mathf.Exp(-t * 4.8f);
                        float woodClick = Mathf.Sin(modPhase) * Mathf.Exp(-t * 42f) * 0.32f;
                        float body = Mathf.Sin(phase) * 0.84f + Mathf.Sin(overtonePhase) * 0.16f * Mathf.Exp(-t * 8.5f);

                        val = (body * decay + woodClick) * attack * release * amplitude;
                        break;
                    }

                    case KawaiiVoiceType.CelestaBell:
                    {
                        // Sparkling Kawaii Music-Box / Celesta Lead with gentle pitch vibrato & crystalline shimmer
                        float vibrato = t > 0.08f ? Mathf.Sin(2f * Mathf.PI * 5.4f * (t - 0.08f)) * 0.0032f : 0f;
                        float instFreq = freq * (1f + vibrato);
                        phase += 2f * Mathf.PI * instFreq / sampleRate;
                        modPhase += 2f * Mathf.PI * (instFreq * 2f) / sampleRate;
                        overtonePhase += 2f * Mathf.PI * (instFreq * 3.01f) / sampleRate;

                        float attack = Mathf.Clamp01(t / 0.005f);
                        float release = norm > 0.86f ? 0.5f * (1f + Mathf.Cos(Mathf.PI * ((norm - 0.86f) / 0.14f))) : 1f;
                        float decay = Mathf.Exp(-t * 3.1f);
                        float chime = Mathf.Sin(phase) * 0.78f
                                    + Mathf.Sin(modPhase) * 0.16f * Mathf.Exp(-t * 5.2f)
                                    + Mathf.Sin(overtonePhase) * 0.09f * Mathf.Exp(-t * 11.0f);

                        val = chime * attack * release * decay * amplitude;
                        break;
                    }

                    case KawaiiVoiceType.UprightBass:
                    {
                        // Warm Acoustic Pizzicato Upright Bass
                        phase += 2f * Mathf.PI * freq / sampleRate;
                        modPhase += 2f * Mathf.PI * (freq * 2f) / sampleRate;
                        overtonePhase += 2f * Mathf.PI * (freq * 3f) / sampleRate;

                        float attack = Mathf.Clamp01(t / 0.007f);
                        float release = norm > 0.82f ? (1f - (norm - 0.82f) / 0.18f) : 1f;
                        float decay = Mathf.Exp(-t * 4.2f);
                        float pluck = Mathf.Sin(phase) * 0.80f
                                    + Mathf.Sin(modPhase) * 0.24f * Mathf.Exp(-t * 6.5f)
                                    + Mathf.Sin(overtonePhase) * 0.08f * Mathf.Exp(-t * 14f);

                        val = pluck * attack * release * decay * amplitude;
                        break;
                    }

                    case KawaiiVoiceType.WoodBlockClick:
                    {
                        // Cute Pitched Wooden Clave / Rim Tap
                        float wFreq = freq * Mathf.Lerp(1.12f, 0.96f, norm);
                        phase += 2f * Mathf.PI * wFreq / sampleRate;
                        modPhase += 2f * Mathf.PI * (wFreq * 1.62f) / sampleRate;
                        float env = Mathf.Clamp01(t / 0.002f) * Mathf.Exp(-t * 48f);
                        val = (Mathf.Sin(phase) * 0.75f + Mathf.Sin(modPhase) * 0.25f) * env * amplitude;
                        break;
                    }

                    case KawaiiVoiceType.SoftShaker:
                    {
                        // Cozy Brush Shaker
                        rngState = rngState * 1664525u + 1013904223u;
                        float noise = ((rngState & 0xFFFF) / 32768f) - 1f;
                        float env = Mathf.Sin(norm * Mathf.PI) * Mathf.Exp(-t * 28f);
                        val = noise * env * amplitude;
                        break;
                    }

                    case KawaiiVoiceType.WarmSubKick:
                    {
                        // Soft Lo-Fi Cushion Kick
                        float kFreq = Mathf.Lerp(128f, 46f, Mathf.Clamp01(t / 0.085f));
                        phase += 2f * Mathf.PI * kFreq / sampleRate;
                        float env = Mathf.Clamp01(t / 0.003f) * Mathf.Exp(-t * 22f);
                        val = Mathf.Sin(phase) * env * amplitude;
                        break;
                    }
                }

                buffer[idx] += val;
            }
        }

        private static void RenderChordVoicing(float[] buffer, int sampleRate, float startTimeSec, float durationSec, int[] midiNotes, float ampPerVoice, bool arpeggiateRoll = false)
        {
            if (midiNotes == null) return;
            for (int i = 0; i < midiNotes.Length; i++)
            {
                float rollOffset = arpeggiateRoll ? (i * 0.042f) : (i * 0.010f);
                float freq = MidiNoteToFreq(midiNotes[i]);
                if (freq > 10f)
                {
                    RenderSynthNote(buffer, sampleRate, KawaiiVoiceType.RhodesChord, startTimeSec + rollOffset, Mathf.Max(0.15f, durationSec - rollOffset), freq, ampPerVoice);
                }
            }
        }

        private AudioClip CreateChillCuteBgmClip()
        {
            int sampleRate = 22050;

            // =========================================================================================
            // 4 SECTIONS WITH DISTINCT KEYS (NHIỀU TÔNG) & TEMPOS/GROOVES (NHIỀU NHỊP ĐỘ):
            // - Section A (8 Bars, F Major, 110 BPM Bossa-Swing): 16th = 0.13636s -> Bar = 2.1818s
            // - Section B (8 Bars, G Major Modulation, 126 BPM Bouncy Shuffle): 16th = 0.11905s -> Bar = 1.9048s
            // - Section C (8 Bars, Eb Major Pastel Lo-Fi, 96 BPM Dreamy Chill): 16th = 0.15625s -> Bar = 2.5000s
            // - Section D (8 Bars, C -> F Major Joyful Party, 116 BPM Groove): 16th = 0.12931s -> Bar = 2.0690s
            // Total Duration: ~69.2 seconds of rich, non-repetitive multi-key kawaii music!
            // =========================================================================================
            float stepA = 60f / (110f * 4f); // 0.13636s per 16th note
            float stepB = 60f / (126f * 4f); // 0.11905s per 16th note
            float stepC = 60f / (96f * 4f);  // 0.15625s per 16th note
            float stepD = 60f / (116f * 4f); // 0.12931s per 16th note

            float durA = stepA * 16f * 8f;
            float durB = stepB * 16f * 8f;
            float durC = stepC * 16f * 8f;
            float durD = stepD * 16f * 8f;

            float totalDurationSec = durA + durB + durC + durD;
            int totalSamples = Mathf.RoundToInt(totalDurationSec * sampleRate);
            float[] data = new float[totalSamples];

            // Helper for swung 16th-note timing within a section
            Func<float, float, int, float, float> GetStepTime = (secStart, stepDur, step16, swingAmt) =>
            {
                float swingOffset = (step16 % 2 == 1) ? (stepDur * swingAmt) : 0f;
                return secStart + step16 * stepDur + swingOffset;
            };

            // =========================================================================================
            // SECTION A: "SUNNY CAT CAFE MORNING" — Key of F Major (110 BPM Cozy Bossa-Swing)
            // Progression: Fmaj9 | G13 | Em7 | A7b9 | Dm9 | G13 | Cmaj9 | C7sus4 -> C7
            // =========================================================================================
            float secAStart = 0f;
            int[][] chordsA = new int[][]
            {
                new int[] { 65, 69, 72, 76, 79 }, // Fmaj9 (F4 A4 C5 E5 G5)
                new int[] { 67, 71, 74, 77, 81 }, // G13   (G4 B4 D5 F5 A5)
                new int[] { 64, 67, 71, 74, 79 }, // Em7   (E4 G4 B4 D5 G5)
                new int[] { 61, 67, 69, 73, 76 }, // A7    (C#4 G4 A4 C#5 E5)
                new int[] { 62, 65, 69, 72, 76 }, // Dm9   (D4 F4 A4 C5 E5)
                new int[] { 65, 67, 71, 74, 79 }, // G7    (F4 G4 B4 D5 G5)
                new int[] { 60, 64, 67, 71, 74 }, // Cmaj9 (C4 E4 G4 B4 D5)
                new int[] { 60, 65, 67, 70, 74 }  // C9sus4(C4 F4 G4 Bb4 D5)
            };
            int[] bassRootsA = new int[] { 53, 55, 52, 57, 50, 55, 48, 48 };
            int[] bassFifthsA = new int[] { 60, 62, 59, 52, 57, 62, 55, 55 };

            // Section A Melodies: (barIndex, step16InBar, durationSteps, midiNote, instrument)
            int[,] notesA = new int[,]
            {
                // Bar 0 (Fmaj9): Cute Kalimba motif + Celesta response
                { 0, 0,  3, 77, 1 }, { 0, 3,  3, 81, 1 }, { 0, 6,  2, 84, 1 }, { 0, 8,  4, 81, 2 }, { 0, 12, 2, 79, 1 }, { 0, 14, 2, 77, 1 },
                // Bar 1 (G13)
                { 1, 0,  3, 79, 1 }, { 1, 3,  3, 83, 1 }, { 1, 6,  2, 86, 2 }, { 1, 8,  6, 84, 2 }, { 1, 14, 2, 81, 1 },
                // Bar 2 (Em7)
                { 2, 0,  2, 79, 1 }, { 2, 2,  2, 83, 1 }, { 2, 4,  3, 86, 2 }, { 2, 7,  3, 88, 2 }, { 2, 10, 4, 83, 1 }, { 2, 14, 2, 79, 1 },
                // Bar 3 (A7)
                { 3, 0,  3, 81, 2 }, { 3, 3,  3, 79, 2 }, { 3, 6,  2, 76, 1 }, { 3, 8,  4, 73, 1 }, { 3, 12, 4, 76, 2 },
                // Bar 4 (Dm9)
                { 4, 0,  3, 74, 1 }, { 4, 3,  3, 77, 1 }, { 4, 6,  2, 81, 1 }, { 4, 8,  4, 84, 2 }, { 4, 12, 2, 81, 2 }, { 4, 14, 2, 77, 1 },
                // Bar 5 (G7)
                { 5, 0,  2, 79, 1 }, { 5, 2,  2, 81, 1 }, { 5, 4,  3, 83, 2 }, { 5, 7,  3, 86, 2 }, { 5, 10, 4, 84, 2 }, { 5, 14, 2, 79, 1 },
                // Bar 6 (Cmaj9)
                { 6, 0,  3, 84, 2 }, { 6, 3,  3, 79, 1 }, { 6, 6,  2, 76, 1 }, { 6, 8,  4, 74, 1 }, { 6, 12, 4, 72, 2 },
                // Bar 7 (C9sus4 turnaround)
                { 7, 0,  2, 74, 1 }, { 7, 2,  2, 77, 1 }, { 7, 4,  2, 79, 1 }, { 7, 6,  2, 81, 2 }, { 7, 8,  4, 84, 2 }, { 7, 12, 4, 79, 2 }
            };

            for (int b = 0; b < 8; b++)
            {
                float barStart = secAStart + b * 16f * stepA;
                // Bossa Rhodes Chords (hit on beat 1, syncopated comp on step 6 and step 11)
                RenderChordVoicing(data, sampleRate, barStart, stepA * 5.8f, chordsA[b], 0.085f, true);
                RenderChordVoicing(data, sampleRate, GetStepTime(barStart, stepA, 6, 0.16f), stepA * 4.5f, chordsA[b], 0.070f, false);
                RenderChordVoicing(data, sampleRate, GetStepTime(barStart, stepA, 11, 0.16f), stepA * 4.8f, chordsA[b], 0.068f, false);

                // Bossa Nova Pizzicato Bass (steps 0, 6, 8, 14)
                RenderSynthNote(data, sampleRate, KawaiiVoiceType.UprightBass, barStart, stepA * 5.2f, MidiNoteToFreq(bassRootsA[b]), 0.23f);
                RenderSynthNote(data, sampleRate, KawaiiVoiceType.UprightBass, GetStepTime(barStart, stepA, 6, 0.16f), stepA * 2.2f, MidiNoteToFreq(bassFifthsA[b]), 0.17f);
                RenderSynthNote(data, sampleRate, KawaiiVoiceType.UprightBass, GetStepTime(barStart, stepA, 8, 0.16f), stepA * 5.2f, MidiNoteToFreq(bassRootsA[b]), 0.21f);
                RenderSynthNote(data, sampleRate, KawaiiVoiceType.UprightBass, GetStepTime(barStart, stepA, 14, 0.16f), stepA * 2.0f, MidiNoteToFreq(bassFifthsA[b]), 0.16f);

                // Cozy Bossa Percussion (SubKick on 0, 8; WoodBlock Clave on 3, 6, 12; Shaker on offbeats)
                RenderSynthNote(data, sampleRate, KawaiiVoiceType.WarmSubKick, barStart, 0.12f, 60f, 0.18f);
                RenderSynthNote(data, sampleRate, KawaiiVoiceType.WarmSubKick, barStart + 8f * stepA, 0.12f, 60f, 0.16f);
                RenderSynthNote(data, sampleRate, KawaiiVoiceType.WoodBlockClick, GetStepTime(barStart, stepA, 3, 0.16f), 0.06f, 920f, 0.09f);
                RenderSynthNote(data, sampleRate, KawaiiVoiceType.WoodBlockClick, GetStepTime(barStart, stepA, 6, 0.16f), 0.06f, 1140f, 0.08f);
                RenderSynthNote(data, sampleRate, KawaiiVoiceType.WoodBlockClick, GetStepTime(barStart, stepA, 12, 0.16f), 0.06f, 920f, 0.09f);
                for (int s = 2; s < 16; s += 4)
                    RenderSynthNote(data, sampleRate, KawaiiVoiceType.SoftShaker, GetStepTime(barStart, stepA, s, 0.16f), 0.065f, 0f, 0.045f);
            }

            for (int n = 0; n < notesA.GetLength(0); n++)
            {
                int bar = notesA[n, 0];
                int step16 = notesA[n, 1];
                int durSteps = notesA[n, 2];
                int midi = notesA[n, 3];
                int inst = notesA[n, 4];

                float noteStart = GetStepTime(secAStart + bar * 16f * stepA, stepA, step16, 0.16f);
                float noteDur = Mathf.Max(0.24f, durSteps * stepA * 1.45f); // Natural overlap ring-out!
                KawaiiVoiceType vType = (inst == 1) ? KawaiiVoiceType.KalimbaPluck : KawaiiVoiceType.CelestaBell;
                RenderSynthNote(data, sampleRate, vType, noteStart, noteDur, MidiNoteToFreq(midi), inst == 1 ? 0.25f : 0.23f);

                // Add a sweet lower 6th/3rd harmony chime on accented long notes
                if (durSteps >= 4)
                {
                    RenderSynthNote(data, sampleRate, KawaiiVoiceType.KalimbaPluck, noteStart + 0.015f, noteDur * 0.85f, MidiNoteToFreq(midi - 12), 0.11f);
                }
            }

            // =========================================================================================
            // SECTION B: "PLAYFUL YARN CHASE" — Key Modulation to G Major (126 BPM Upbeat Bouncy Shuffle!)
            // Faster tempo, playful walking bass, sparkling high register Celesta & bouncing Kalimba!
            // Progression: Cmaj7 | D9 | Bm7 | Em7 | Am7 | D13 | Gmaj7 | G7
            // =========================================================================================
            float secBStart = secAStart + durA;
            int[][] chordsB = new int[][]
            {
                new int[] { 60, 64, 67, 71, 74 }, // Cmaj9
                new int[] { 62, 66, 69, 72, 76 }, // D9
                new int[] { 59, 62, 66, 69, 74 }, // Bm7
                new int[] { 64, 67, 71, 74, 78 }, // Em9
                new int[] { 60, 64, 67, 69, 74 }, // Am11
                new int[] { 62, 66, 69, 72, 79 }, // D13
                new int[] { 67, 71, 74, 78, 81 }, // Gmaj9
                new int[] { 67, 71, 74, 77, 81 }  // G13
            };
            int[,] walkingBassB = new int[,]
            {
                { 48, 52, 55, 57 }, // C -> E -> G -> A
                { 50, 54, 57, 60 }, // D -> F# -> A -> C
                { 47, 50, 54, 57 }, // B -> D -> F# -> A
                { 52, 55, 59, 55 }, // E -> G -> B -> G
                { 45, 48, 52, 55 }, // A -> C -> E -> G
                { 50, 54, 57, 54 }, // D -> F# -> A -> F#
                { 43, 47, 50, 54 }, // G -> B -> D -> F#
                { 43, 50, 53, 55 }  // G -> D -> F -> G
            };

            int[,] notesB = new int[,]
            {
                // Bar 0 (Cmaj9): Upbeat bouncy 16th-note chase!
                { 0, 0,  2, 79, 2 }, { 0, 2,  2, 81, 1 }, { 0, 4,  2, 83, 2 }, { 0, 6,  2, 86, 1 }, { 0, 8,  4, 88, 2 }, { 0, 12, 2, 86, 1 }, { 0, 14, 2, 83, 1 },
                // Bar 1 (D9)
                { 1, 0,  3, 81, 2 }, { 1, 3,  3, 84, 1 }, { 1, 6,  2, 81, 1 }, { 1, 8,  4, 78, 2 }, { 1, 12, 4, 81, 1 },
                // Bar 2 (Bm7)
                { 2, 0,  2, 78, 1 }, { 2, 2,  2, 81, 1 }, { 2, 4,  2, 83, 2 }, { 2, 6,  2, 86, 2 }, { 2, 8,  4, 83, 2 }, { 2, 12, 2, 81, 1 }, { 2, 14, 2, 78, 1 },
                // Bar 3 (Em9)
                { 3, 0,  3, 79, 2 }, { 3, 3,  3, 76, 1 }, { 3, 6,  2, 74, 1 }, { 3, 8,  6, 76, 2 }, { 3, 14, 2, 79, 1 },
                // Bar 4 (Am11)
                { 4, 0,  2, 76, 1 }, { 4, 2,  2, 79, 1 }, { 4, 4,  2, 81, 2 }, { 4, 6,  2, 84, 2 }, { 4, 8,  4, 83, 2 }, { 4, 12, 4, 79, 1 },
                // Bar 5 (D13)
                { 5, 0,  2, 78, 1 }, { 5, 2,  2, 81, 1 }, { 5, 4,  2, 84, 2 }, { 5, 6,  2, 86, 2 }, { 5, 8,  4, 88, 2 }, { 5, 12, 4, 86, 2 },
                // Bar 6 (Gmaj9)
                { 6, 0,  3, 83, 2 }, { 6, 3,  3, 79, 1 }, { 6, 6,  2, 74, 1 }, { 6, 8,  4, 78, 2 }, { 6, 12, 4, 79, 2 },
                // Bar 7 (G13 playful trill into Section C)
                { 7, 0,  2, 79, 1 }, { 7, 2,  2, 81, 1 }, { 7, 4,  2, 83, 2 }, { 7, 6,  2, 84, 2 }, { 7, 8,  4, 86, 2 }, { 7, 12, 4, 83, 1 }
            };

            for (int b = 0; b < 8; b++)
            {
                float barStart = secBStart + b * 16f * stepB;
                // Bouncy staccato Rhodes off-beat chords (steps 0, 4, 8, 12 with light shuffle)
                RenderChordVoicing(data, sampleRate, barStart, stepB * 3.5f, chordsB[b], 0.075f, false);
                RenderChordVoicing(data, sampleRate, GetStepTime(barStart, stepB, 6, 0.22f), stepB * 3.5f, chordsB[b], 0.068f, false);
                RenderChordVoicing(data, sampleRate, GetStepTime(barStart, stepB, 12, 0.22f), stepB * 3.5f, chordsB[b], 0.068f, false);

                // 4-Beat Walking Pizzicato Bass
                for (int beat = 0; beat < 4; beat++)
                {
                    RenderSynthNote(data, sampleRate, KawaiiVoiceType.UprightBass, barStart + beat * 4f * stepB, stepB * 3.6f, MidiNoteToFreq(walkingBassB[b, beat]), 0.22f);
                }

                // Upbeat Shuffle Percussion
                RenderSynthNote(data, sampleRate, KawaiiVoiceType.WarmSubKick, barStart, 0.11f, 62f, 0.18f);
                RenderSynthNote(data, sampleRate, KawaiiVoiceType.WarmSubKick, barStart + 8f * stepB, 0.11f, 62f, 0.18f);
                RenderSynthNote(data, sampleRate, KawaiiVoiceType.WoodBlockClick, barStart + 4f * stepB, 0.055f, 1080f, 0.11f);
                RenderSynthNote(data, sampleRate, KawaiiVoiceType.WoodBlockClick, barStart + 12f * stepB, 0.055f, 1080f, 0.11f);
                RenderSynthNote(data, sampleRate, KawaiiVoiceType.WoodBlockClick, GetStepTime(barStart, stepB, 15, 0.22f), 0.05f, 1320f, 0.08f);
                for (int s = 2; s < 16; s += 2)
                    RenderSynthNote(data, sampleRate, KawaiiVoiceType.SoftShaker, GetStepTime(barStart, stepB, s, 0.22f), 0.05f, 0f, (s % 4 == 2) ? 0.052f : 0.030f);
            }

            for (int n = 0; n < notesB.GetLength(0); n++)
            {
                int bar = notesB[n, 0];
                int step16 = notesB[n, 1];
                int durSteps = notesB[n, 2];
                int midi = notesB[n, 3];
                int inst = notesB[n, 4];

                float noteStart = GetStepTime(secBStart + bar * 16f * stepB, stepB, step16, 0.22f);
                float noteDur = Mathf.Max(0.22f, durSteps * stepB * 1.4f);
                KawaiiVoiceType vType = (inst == 1) ? KawaiiVoiceType.KalimbaPluck : KawaiiVoiceType.CelestaBell;
                RenderSynthNote(data, sampleRate, vType, noteStart, noteDur, MidiNoteToFreq(midi), inst == 1 ? 0.25f : 0.24f);
            }

            // =========================================================================================
            // SECTION C: "DREAMY AFTERNOON NAP" — Key Modulation to Eb Major Pastel Lo-Fi (96 BPM Half-Time Chill)
            // Slower, floating dreamy tempo with rolled harp/celesta chords & warm expressive melody!
            // Progression: Abmaj9 | Bb13 | Gm7 | Cm9 | Fm9 | Bb13 | Ebmaj9 | C7b9
            // =========================================================================================
            float secCStart = secBStart + durB;
            int[][] chordsC = new int[][]
            {
                new int[] { 68, 72, 75, 79, 82 }, // Abmaj9
                new int[] { 70, 74, 77, 80, 84 }, // Bb9
                new int[] { 67, 70, 74, 77, 82 }, // Gm7
                new int[] { 60, 63, 67, 70, 74 }, // Cm9
                new int[] { 65, 68, 72, 75, 79 }, // Fm9
                new int[] { 65, 70, 74, 77, 82 }, // Bb7
                new int[] { 63, 67, 70, 74, 77 }, // Ebmaj9
                new int[] { 60, 64, 67, 70, 73 }  // C7b9 (pivot back to F Major!)
            };
            int[] bassRootsC = new int[] { 44, 46, 43, 48, 41, 46, 51, 48 };
            int[] bassFifthsC = new int[] { 51, 53, 50, 55, 48, 53, 58, 55 };

            int[,] notesC = new int[,]
            {
                // Bar 0 (Abmaj9): Lush, lyrical music-box melody with Kalimba echo
                { 0, 0,  6, 79, 2 }, { 0, 6,  2, 80, 2 }, { 0, 8,  4, 82, 2 }, { 0, 12, 4, 75, 1 },
                // Bar 1 (Bb9)
                { 1, 0,  6, 77, 2 }, { 1, 6,  2, 79, 2 }, { 1, 8,  8, 74, 2 },
                // Bar 2 (Gm7)
                { 2, 0,  4, 74, 1 }, { 2, 4,  4, 77, 1 }, { 2, 8,  4, 82, 2 }, { 2, 12, 4, 79, 2 },
                // Bar 3 (Cm9)
                { 3, 0,  6, 75, 2 }, { 3, 6,  2, 74, 1 }, { 3, 8,  8, 72, 2 },
                // Bar 4 (Fm9)
                { 4, 0,  4, 72, 1 }, { 4, 4,  4, 75, 1 }, { 4, 8,  6, 79, 2 }, { 4, 14, 2, 77, 2 },
                // Bar 5 (Bb7)
                { 5, 0,  4, 74, 2 }, { 5, 4,  4, 77, 1 }, { 5, 8,  4, 82, 2 }, { 5, 12, 4, 79, 2 },
                // Bar 6 (Ebmaj9)
                { 6, 0,  8, 75, 2 }, { 6, 8,  4, 74, 1 }, { 6, 12, 4, 70, 1 },
                // Bar 7 (C7b9 transition back to F Major)
                { 7, 0,  4, 72, 1 }, { 7, 4,  4, 76, 1 }, { 7, 8,  4, 79, 2 }, { 7, 12, 4, 82, 2 }
            };

            for (int b = 0; b < 8; b++)
            {
                float barStart = secCStart + b * 16f * stepC;
                // Lush rolled Rhodes + Celesta Harp arpeggio chords
                RenderChordVoicing(data, sampleRate, barStart, stepC * 15.0f, chordsC[b], 0.088f, true);
                for (int arp = 0; arp < chordsC[b].Length; arp++)
                {
                    RenderSynthNote(data, sampleRate, KawaiiVoiceType.KalimbaPluck, barStart + (8 + arp * 1.5f) * stepC, stepC * 4.5f, MidiNoteToFreq(chordsC[b][arp]), 0.095f);
                }

                // Deep Half-Time Bass
                RenderSynthNote(data, sampleRate, KawaiiVoiceType.UprightBass, barStart, stepC * 7.5f, MidiNoteToFreq(bassRootsC[b]), 0.22f);
                RenderSynthNote(data, sampleRate, KawaiiVoiceType.UprightBass, barStart + 10f * stepC, stepC * 5.5f, MidiNoteToFreq(bassFifthsC[b]), 0.17f);

                // Minimalist Lo-Fi Chill Heartbeat Kick & Soft Rim
                RenderSynthNote(data, sampleRate, KawaiiVoiceType.WarmSubKick, barStart, 0.13f, 56f, 0.16f);
                RenderSynthNote(data, sampleRate, KawaiiVoiceType.WoodBlockClick, barStart + 8f * stepC, 0.06f, 840f, 0.07f);
            }

            for (int n = 0; n < notesC.GetLength(0); n++)
            {
                int bar = notesC[n, 0];
                int step16 = notesC[n, 1];
                int durSteps = notesC[n, 2];
                int midi = notesC[n, 3];
                int inst = notesC[n, 4];

                float noteStart = GetStepTime(secCStart + bar * 16f * stepC, stepC, step16, 0.08f);
                float noteDur = Mathf.Max(0.32f, durSteps * stepC * 1.55f);
                KawaiiVoiceType vType = (inst == 1) ? KawaiiVoiceType.KalimbaPluck : KawaiiVoiceType.CelestaBell;
                RenderSynthNote(data, sampleRate, vType, noteStart, noteDur, MidiNoteToFreq(midi), 0.24f);
            }

            // =========================================================================================
            // SECTION D: "HAPPY CAT PARTY FINALE" — Return to F Major (116 BPM Joyful Duet & Turnaround)
            // Progression: Bbmaj7 | Am7 -> D7 | Gm7 | C9 | Fmaj7 -> Dm7 | Gm7 -> C7 | Fmaj9 | C13
            // =========================================================================================
            float secDStart = secCStart + durC;
            int[][] chordsD = new int[][]
            {
                new int[] { 65, 70, 74, 77, 81 }, // Bbmaj9
                new int[] { 64, 69, 72, 76, 78 }, // Am7/D9
                new int[] { 62, 67, 70, 74, 77 }, // Gm7
                new int[] { 60, 64, 67, 70, 74 }, // C9
                new int[] { 65, 69, 72, 76, 79 }, // Fmaj9
                new int[] { 62, 65, 69, 72, 76 }, // Dm9
                new int[] { 62, 67, 70, 74, 77 }, // Gm9
                new int[] { 60, 65, 69, 70, 74 }  // C13sus4
            };
            int[] bassRootsD = new int[] { 46, 45, 43, 48, 53, 50, 43, 48 };
            int[] bassFifthsD = new int[] { 53, 50, 50, 55, 60, 57, 50, 55 };

            int[,] notesD = new int[,]
            {
                // Bar 0 (Bbmaj9)
                { 0, 0,  3, 81, 2 }, { 0, 3,  3, 84, 2 }, { 0, 6,  2, 86, 1 }, { 0, 8,  4, 89, 2 }, { 0, 12, 4, 86, 1 },
                // Bar 1 (Am7 -> D7)
                { 1, 0,  3, 84, 2 }, { 1, 3,  3, 81, 1 }, { 1, 6,  2, 79, 1 }, { 1, 8,  4, 78, 2 }, { 1, 12, 4, 81, 2 },
                // Bar 2 (Gm7)
                { 2, 0,  2, 79, 1 }, { 2, 2,  2, 82, 1 }, { 2, 4,  3, 86, 2 }, { 2, 7,  3, 84, 2 }, { 2, 10, 4, 82, 1 }, { 2, 14, 2, 79, 1 },
                // Bar 3 (C9)
                { 3, 0,  3, 81, 2 }, { 3, 3,  3, 79, 1 }, { 3, 6,  2, 77, 1 }, { 3, 8,  6, 76, 2 }, { 3, 14, 2, 79, 1 },
                // Bar 4 (Fmaj9)
                { 4, 0,  3, 77, 1 }, { 4, 3,  3, 81, 1 }, { 4, 6,  2, 84, 2 }, { 4, 8,  4, 88, 2 }, { 4, 12, 4, 84, 1 },
                // Bar 5 (Dm9)
                { 5, 0,  3, 86, 2 }, { 5, 3,  3, 84, 1 }, { 5, 6,  2, 81, 1 }, { 5, 8,  4, 77, 2 }, { 5, 12, 4, 81, 1 },
                // Bar 6 (Gm9)
                { 6, 0,  2, 79, 1 }, { 6, 2,  2, 81, 1 }, { 6, 4,  3, 82, 2 }, { 6, 7,  3, 86, 2 }, { 6, 10, 6, 84, 2 },
                // Bar 7 (C13 turnaround back into Section A)
                { 7, 0,  3, 84, 2 }, { 7, 3,  3, 81, 1 }, { 7, 6,  2, 79, 1 }, { 7, 8,  4, 77, 2 }, { 7, 12, 4, 76, 1 }
            };

            for (int b = 0; b < 8; b++)
            {
                float barStart = secDStart + b * 16f * stepD;
                RenderChordVoicing(data, sampleRate, barStart, stepD * 5.6f, chordsD[b], 0.082f, true);
                RenderChordVoicing(data, sampleRate, GetStepTime(barStart, stepD, 6, 0.18f), stepD * 4.5f, chordsD[b], 0.070f, false);
                RenderChordVoicing(data, sampleRate, GetStepTime(barStart, stepD, 12, 0.18f), stepD * 3.8f, chordsD[b], 0.068f, false);

                RenderSynthNote(data, sampleRate, KawaiiVoiceType.UprightBass, barStart, stepD * 5.0f, MidiNoteToFreq(bassRootsD[b]), 0.23f);
                RenderSynthNote(data, sampleRate, KawaiiVoiceType.UprightBass, GetStepTime(barStart, stepD, 6, 0.18f), stepD * 2.2f, MidiNoteToFreq(bassFifthsD[b]), 0.17f);
                RenderSynthNote(data, sampleRate, KawaiiVoiceType.UprightBass, GetStepTime(barStart, stepD, 8, 0.18f), stepD * 4.8f, MidiNoteToFreq(bassFifthsD[b]), 0.20f);
                RenderSynthNote(data, sampleRate, KawaiiVoiceType.UprightBass, GetStepTime(barStart, stepD, 14, 0.18f), stepD * 2.0f, MidiNoteToFreq(bassRootsD[b]), 0.17f);

                RenderSynthNote(data, sampleRate, KawaiiVoiceType.WarmSubKick, barStart, 0.12f, 60f, 0.18f);
                RenderSynthNote(data, sampleRate, KawaiiVoiceType.WarmSubKick, barStart + 8f * stepD, 0.12f, 60f, 0.17f);
                RenderSynthNote(data, sampleRate, KawaiiVoiceType.WoodBlockClick, GetStepTime(barStart, stepD, 4, 0.18f), 0.06f, 980f, 0.10f);
                RenderSynthNote(data, sampleRate, KawaiiVoiceType.WoodBlockClick, GetStepTime(barStart, stepD, 12, 0.18f), 0.06f, 1180f, 0.10f);
                for (int s = 2; s < 16; s += 4)
                    RenderSynthNote(data, sampleRate, KawaiiVoiceType.SoftShaker, GetStepTime(barStart, stepD, s, 0.18f), 0.06f, 0f, 0.048f);
            }

            for (int n = 0; n < notesD.GetLength(0); n++)
            {
                int bar = notesD[n, 0];
                int step16 = notesD[n, 1];
                int durSteps = notesD[n, 2];
                int midi = notesD[n, 3];
                int inst = notesD[n, 4];

                float noteStart = GetStepTime(secDStart + bar * 16f * stepD, stepD, step16, 0.18f);
                float noteDur = Mathf.Max(0.24f, durSteps * stepD * 1.45f);
                KawaiiVoiceType vType = (inst == 1) ? KawaiiVoiceType.KalimbaPluck : KawaiiVoiceType.CelestaBell;
                RenderSynthNote(data, sampleRate, vType, noteStart, noteDur, MidiNoteToFreq(midi), 0.24f);
                if (inst == 2)
                {
                    // Duet counter-chime a third/sixth below on Kalimba
                    RenderSynthNote(data, sampleRate, KawaiiVoiceType.KalimbaPluck, noteStart + 0.012f, noteDur * 0.85f, MidiNoteToFreq(midi - 9), 0.12f);
                }
            }

            // =========================================================================================
            // COZY CAFE ROOM REVERB / DUAL-TAP WARM DELAY & ANALOG SOFT SATURATION
            // =========================================================================================
            int delaySamples1 = Mathf.RoundToInt(0.185f * sampleRate);
            int delaySamples2 = Mathf.RoundToInt(0.310f * sampleRate);
            float lpState = 0f;

            for (int i = 0; i < totalSamples; i++)
            {
                float dry = data[i];
                float echo1 = (i >= delaySamples1) ? data[i - delaySamples1] * 0.22f : 0f;
                float echo2 = (i >= delaySamples2) ? data[i - delaySamples2] * 0.12f : 0f;

                lpState = lpState * 0.62f + (echo1 + echo2) * 0.38f;
                float mixed = dry + lpState;

                // Soft warm analog saturation (prevents harsh digital clipping)
                data[i] = (float)Math.Tanh(mixed * 1.08f) * 0.88f;
            }

            AudioClip clip = AudioClip.Create("CatFight_ChillCuteBGM", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // =========================================================================
        // 2. BOSS BATTLE BGM ("Vua Chuột Cống" - Clean Phase-Accumulated Boss Theme)
        // =========================================================================
        private AudioClip CreateBossBattleBgmClip()
        {
            int sampleRate = 22050;
            float stepDur = 0.22f;
            int totalSteps = 32;
            int stepSamples = Mathf.RoundToInt(stepDur * sampleRate);
            int totalSamples = stepSamples * totalSteps;
            float[] data = new float[totalSamples];

            float[] bassNotes = new float[]
            {
                146.83f, 146.83f, 174.61f, 196.00f, 220.00f, 220.00f, 196.00f, 174.61f,
                116.54f, 116.54f, 146.83f, 174.61f, 110.00f, 110.00f, 138.59f, 164.81f,
                196.00f, 196.00f, 233.08f, 293.66f, 146.83f, 146.83f, 174.61f, 220.00f,
                110.00f, 138.59f, 164.81f, 220.00f, 277.18f, 220.00f, 164.81f, 138.59f
            };

            float[] bossLead = new float[]
            {
                293.66f, 0f,      349.23f, 392.00f, 440.00f, 0f,      466.16f, 440.00f,
                392.00f, 349.23f, 329.63f, 349.23f, 329.63f, 0f,      277.18f, 0f,
                293.66f, 0f,      349.23f, 440.00f, 587.33f, 0f,      523.25f, 466.16f,
                440.00f, 392.00f, 349.23f, 329.63f, 440.00f, 554.37f, 587.33f, 440.00f
            };

            float kickPhase = 0f;
            float bassPhase = 0f;
            float leadPhase = 0f;
            int prevStep = -1;

            for (int i = 0; i < totalSamples; i++)
            {
                int step = i / stepSamples;
                int stepInBar = step % 8;
                int sampleInStep = i % stepSamples;

                if (step != prevStep)
                {
                    prevStep = step;
                    kickPhase = 0f;
                }

                float stepTime = (float)sampleInStep / sampleRate;
                float stepNorm = (float)sampleInStep / stepSamples;
                float sampleVal = 0f;

                // 1. Clean Kick Drum on even steps
                if (stepInBar % 2 == 0 && stepTime < 0.09f)
                {
                    float kt = stepTime / 0.09f;
                    float kickFreq = Mathf.Lerp(120f, 45f, kt);
                    kickPhase += 2f * Mathf.PI * kickFreq / sampleRate;
                    sampleVal += Mathf.Sin(kickPhase) * (1f - kt) * 0.34f;
                }

                // 2. Boss Bassline
                float bFreq = bassNotes[step];
                if (bFreq > 1f)
                {
                    bassPhase += 2f * Mathf.PI * bFreq / sampleRate;
                    float bEnv = Mathf.Sin(stepNorm * Mathf.PI) * Mathf.Exp(-stepTime * 5.0f);
                    sampleVal += (Mathf.Sin(bassPhase) * 0.75f + Mathf.Sin(bassPhase * 2f) * 0.25f) * bEnv * 0.30f;
                }

                // 3. Boss Lead Melody
                float leadFreq = bossLead[step];
                if (leadFreq > 1f)
                {
                    leadPhase += 2f * Mathf.PI * leadFreq / sampleRate;
                    float lEnv = Mathf.Sin(stepNorm * Mathf.PI) * Mathf.Exp(-stepTime * 3.5f);
                    sampleVal += (Mathf.Sin(leadPhase) * 0.80f + Mathf.Sin(leadPhase * 3f) * 0.20f) * lEnv * 0.32f;
                }

                data[i] = Mathf.Clamp(sampleVal, -0.90f, 0.90f);
            }

            AudioClip clip = AudioClip.Create("CatFight_BossBattleBGM", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
