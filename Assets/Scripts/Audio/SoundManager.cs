using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BOMBOMLemon
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] AudioSource bgmSource;
        [SerializeField] AudioSource seSource;

        [Header("BGM Clips")]
        public AudioClip titleMusic;
        public AudioClip fireMusic;

        [Header("SE Clips")]
        public AudioClip clickSE;
        public AudioClip piyoSE;
        public AudioClip goodSE;
        public AudioClip badSE;
        public AudioClip perfectSE;
        public AudioClip bombSE;
        public AudioClip showSE;
        public AudioClip lemonGetSE;
        public AudioClip gameClearSE;
        public AudioClip gameOverSE;

        private Dictionary<string, AudioClip> _seMap;
        private Dictionary<string, AudioClip> _bgmMap;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                BuildMaps();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void BuildMaps()
        {
            _seMap = new Dictionary<string, AudioClip>
            {
                { "click",     clickSE },
                { "piyo",      piyoSE },
                { "good",      goodSE },
                { "bad",       badSE },
                { "perfect",   perfectSE },
                { "bomb",      bombSE },
                { "show",      showSE },
                { "lemonget",  lemonGetSE },
                { "gameclear", gameClearSE },
                { "gameover",  gameOverSE },
            };
            _bgmMap = new Dictionary<string, AudioClip>
            {
                { "title_music", titleMusic },
                { "fire_music",  fireMusic },
            };
        }

        public void PlayBGM(string name)
        {
            if (!_bgmMap.TryGetValue(name, out var clip) || clip == null) return;
            if (bgmSource.clip == clip && bgmSource.isPlaying) return;
            bgmSource.clip = clip;
            bgmSource.loop = true;
            bgmSource.Play();
        }

        public void StopBGM()
        {
            bgmSource.Stop();
            bgmSource.clip = null;
        }

        public void PlaySE(string name)
        {
            if (!_seMap.TryGetValue(name, out var clip) || clip == null) return;
            seSource.PlayOneShot(clip);
        }

        public void PlaySE(string name, float duration)
        {
            if (!_seMap.TryGetValue(name, out var clip) || clip == null) return;
            StartCoroutine(PlayAndStop(clip, duration));
        }

        private IEnumerator PlayAndStop(AudioClip clip, float duration)
        {
            seSource.PlayOneShot(clip);
            yield return new WaitForSeconds(duration);
        }
    }
}
