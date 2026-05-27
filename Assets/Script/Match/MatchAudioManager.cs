using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

namespace Match
{
    public class MatchAudioManager : NetworkBehaviour
    {
        public static MatchAudioManager Instance { get; private set; }

        public enum MusicState
        {
            Ambient,
            Chase,
            Escape
        }

        [Header("Music Clips")]
        public AudioClip ambientMusic;
        public AudioClip chaseMusic;
        public AudioClip escapeMusic;

        [Header("Audio Sources")]
        public AudioSource currentMusicSource;
        public AudioSource nextMusicSource;

        [Header("Settings")]
        public float crossfadeDuration = 2.0f;
        public float chaseDistance = 15f;

        private MusicState _currentState = MusicState.Ambient;
        private MusicState _targetState = MusicState.Ambient;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            if (currentMusicSource) currentMusicSource.loop = true;
            if (nextMusicSource) nextMusicSource.loop = true;
        }

        private void Start()
        {
            PlayMusic(ambientMusic);
        }

        private void Update()
        {
            if (MatchManager.Instance == null || MatchManager.Instance.CurrentState.Value != MatchManager.MatchState.Playing)
                return;

            if (_currentState == MusicState.Escape) return; // Escape overrides chase logic

            // Determine if chase music should play
            bool shouldChase = false;
            
            // Need references to players. We can find them by tag or type
            RobberController robber = FindAnyObjectByType<RobberController>();
            DogController[] dogs = FindObjectsByType<DogController>(FindObjectsSortMode.None);

            if (robber != null && !robber.IsMuteShoeActive) // Mute shoe reduces chase detection to near 0
            {
                foreach (var dog in dogs)
                {
                    if (Vector3.Distance(robber.transform.position, dog.transform.position) <= chaseDistance)
                    {
                        shouldChase = true;
                        break;
                    }
                }
            }

            MusicState desiredState = shouldChase ? MusicState.Chase : MusicState.Ambient;
            
            if (desiredState != _targetState)
            {
                _targetState = desiredState;
                AudioClip clipToPlay = desiredState == MusicState.Chase ? chaseMusic : ambientMusic;
                StartCoroutine(CrossfadeMusic(clipToPlay));
            }
        }

        [Rpc(SendTo.Everyone)]
        public void SetMusicStateRpc(MusicState state)
        {
            _currentState = state;
            _targetState = state;
            AudioClip clipToPlay = ambientMusic;
            if (state == MusicState.Chase) clipToPlay = chaseMusic;
            else if (state == MusicState.Escape) clipToPlay = escapeMusic;

            StartCoroutine(CrossfadeMusic(clipToPlay));
        }

        private void PlayMusic(AudioClip clip)
        {
            if (currentMusicSource == null || clip == null) return;
            currentMusicSource.clip = clip;
            currentMusicSource.volume = 1f;
            currentMusicSource.Play();
        }

        private IEnumerator CrossfadeMusic(AudioClip newClip)
        {
            if (newClip == null || currentMusicSource == null || nextMusicSource == null) yield break;
            if (currentMusicSource.clip == newClip) yield break;

            nextMusicSource.clip = newClip;
            nextMusicSource.volume = 0f;
            nextMusicSource.Play();

            float timer = 0f;
            while (timer < crossfadeDuration)
            {
                timer += Time.deltaTime;
                float t = timer / crossfadeDuration;
                currentMusicSource.volume = Mathf.Lerp(1f, 0f, t);
                nextMusicSource.volume = Mathf.Lerp(0f, 1f, t);
                yield return null;
            }

            currentMusicSource.Stop();
            currentMusicSource.volume = 0f;
            
            // Swap references
            AudioSource temp = currentMusicSource;
            currentMusicSource = nextMusicSource;
            nextMusicSource = temp;
            
            _currentState = _targetState;
        }
    }
}
