using System;
using System.Collections;
using UnityEngine;

namespace LootCraft.Audio
{
    /// <summary>
    /// SFX for the inventory screen: map each <see cref="SoundType"/> to clips in the Inspector.
    /// Optional: call <c>PlaySound</c> from UI / <see cref="LootCraft.Core.InventoryGameplayService"/> hooks.
    /// </summary>
    public sealed class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        /// <summary>Hooks aligned with ТЗ buttons, слоты, попап и драг.</summary>
        public enum SoundType
        {
            Button,
            Coin,
            ItemAdd,
            AmmoAdd,
            Shoot,
            Error,
            PopupOpen,
            PopupClose,
            SlotUnlock,
            DragDrop,
            DeleteItem,
        }

        [Tooltip("Master gate for all SFX (toggle from UI or settings).")]
        [SerializeField] private bool soundEnabled = true;

        [Tooltip("Minimum seconds before the same list entry can fire another one-shot.")]
        [SerializeField] private float playRetriggerDelay = 0.08f;

        [Header("Clips (one row per SoundType you need)")]
        [Tooltip("Each row: pick Sound Type, assign one or more Audio Clips. At runtime one child AudioSource is created per row — do not assign AudioSource manually.")]
        [SerializeField] private Sounds[] sounds = Array.Empty<Sounds>();

        private AudioSource[] _sources;
        private float[] _retriggerTimers;
        private bool[] _canPlay;
        private int _count;

        public bool SoundEnabled
        {
            get => soundEnabled;
            set => soundEnabled = value;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _count = sounds != null ? sounds.Length : 0;
            _sources = new AudioSource[_count];
            _retriggerTimers = new float[_count];
            _canPlay = new bool[_count];

            for (int i = 0; i < _count; i++)
            {
                _canPlay[i] = true;
                var go = new GameObject($"Audio_{sounds[i].sound}");
                go.transform.SetParent(transform, false);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                _sources[i] = src;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            AdvanceRetriggerWindows();
            AdvanceOrderedCooldowns();
        }

        private void OnValidate()
        {
            if (sounds == null)
                return;
            for (int i = 0; i < sounds.Length; i++)
            {
                var e = sounds[i];
                e.soundName = e.sound.ToString();
                sounds[i] = e;
            }
        }

        public static void PlaySound(SoundType sound, float volumeScale = 1f, float pitch = 1f, float delay = 0f)
        {
            if (Instance == null || !Instance.soundEnabled)
                return;

            int idx = Instance.FindIndex(sound);
            if (idx < 0)
                return;
            if (!Instance._canPlay[idx])
                return;

            if (delay <= 0f)
                Instance.PlayOneShotInternal(idx, volumeScale, pitch);
            else
                Instance.StartCoroutine(Instance.SoundDelay(idx, volumeScale, pitch, delay));
        }

        public static void PlaySoundLoop(SoundType sound, float volumeScale = 1f, float pitch = 1f)
        {
            if (Instance == null || !Instance.soundEnabled)
                return;

            int idx = Instance.FindIndex(sound);
            if (idx < 0)
                return;

            var entry = Instance.sounds[idx];
            if (entry.audioClip == null || entry.audioClip.Length == 0)
                return;

            var src = Instance._sources[idx];
            src.clip = entry.audioClip[0];
            src.volume = Mathf.Clamp01(entry.volume * volumeScale);
            src.pitch = pitch;
            src.loop = true;
            src.Play();
        }

        public static void StopSoundLoop(SoundType sound)
        {
            if (Instance == null)
                return;

            int idx = Instance.FindIndex(sound);
            if (idx < 0)
                return;

            var src = Instance._sources[idx];
            if (src.isPlaying)
            {
                src.loop = false;
                src.Stop();
            }
        }

        public static void StopSound(SoundType sound)
        {
            StopSoundLoop(sound);
        }

        private IEnumerator SoundDelay(int index, float volumeScale, float pitch, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (this == null || !soundEnabled)
                yield break;
            PlayOneShotInternal(index, volumeScale, pitch);
        }

        private void PlayOneShotInternal(int index, float volumeScale, float pitch)
        {
            if (index < 0 || index >= _count)
                return;

            ref Sounds s = ref sounds[index];
            if (s.audioClip == null || s.audioClip.Length == 0)
                return;

            AudioClip clip;
            if (s.playOrdered)
            {
                clip = s.audioClip[Mathf.Min(s.playedIndex, s.audioClip.Length - 1)];
                s.playedIndex++;
                s.deltaCooldown = s.cooldown;
            }
            else
            {
                clip = s.audioClip[UnityEngine.Random.Range(0, s.audioClip.Length)];
            }

            var src = _sources[index];
            src.pitch = pitch;
            src.PlayOneShot(clip, Mathf.Clamp01(s.volume * volumeScale));
            _canPlay[index] = false;
            _retriggerTimers[index] = 0f;
        }

        private int FindIndex(SoundType type)
        {
            for (int i = 0; i < _count; i++)
            {
                if (sounds[i].sound == type)
                    return i;
            }

            return -1;
        }

        private void AdvanceRetriggerWindows()
        {
            for (int i = 0; i < _count; i++)
            {
                if (_canPlay[i])
                    continue;
                _retriggerTimers[i] += Time.deltaTime;
                if (_retriggerTimers[i] >= playRetriggerDelay)
                {
                    _canPlay[i] = true;
                    _retriggerTimers[i] = 0f;
                }
            }
        }

        private void AdvanceOrderedCooldowns()
        {
            for (int i = 0; i < _count; i++)
            {
                ref Sounds s = ref sounds[i];
                if (!s.playOrdered || s.cooldown <= 0f)
                    continue;
                if (s.deltaCooldown <= 0f)
                    continue;
                s.deltaCooldown -= Time.deltaTime;
                if (s.deltaCooldown <= 0f)
                {
                    s.deltaCooldown = 0f;
                    s.playedIndex = 0;
                }
            }
        }

        [Serializable]
        public struct Sounds
        {
            [HideInInspector] public string soundName;
            public SoundType sound;
            public AudioClip[] audioClip;
            public bool playOrdered;
            public float cooldown;
            [Range(0f, 1f)] public float volume;

            [HideInInspector] public int playedIndex;
            [HideInInspector] public float deltaCooldown;
        }
    }
}
