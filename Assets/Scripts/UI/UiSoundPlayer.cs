using System.Collections;
using UnityEngine;

namespace RevealGame.UI
{
    /// <summary>
    /// Plays shared, non-spatial interface sounds through one persistent audio source.
    /// </summary>
    public static class UiSoundPlayer
    {
        private const string SettingsResourcePath = "UiSoundSettings";
        private const float DefaultRouletteSpinDuration = 12.5f;

        private static UiSoundSettings _settings;
        private static AudioSource _effectsAudioSource;
        private static AudioSource _gameFlowAudioSource;
        private static MonoBehaviour _coroutineHost;
        private static int _gameFlowTransitionVersion;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _settings = null;
            _effectsAudioSource = null;
            _gameFlowAudioSource = null;
            _coroutineHost = null;
            _gameFlowTransitionVersion = 0;
        }

        public static void PlayButtonHover()
        {
            Play(GetSettings()?.ButtonHover);
        }

        public static void PlayButtonClick()
        {
            Play(GetSettings()?.ButtonClick);
        }

        public static void PlayRouletteTick()
        {
            Play(GetSettings()?.RouletteTick);
        }

        /// <summary>
        /// Starts the default loop used by the title, lootbox selection, and pre-guessing phases.
        /// </summary>
        public static void PlayDefaultMusic()
        {
            PlayLoop(GetSettings()?.DefaultMusic);
        }

        /// <summary>
        /// Starts the loop used while the player is submitting a guess.
        /// </summary>
        public static void PlayGuessingMusic()
        {
            PlayLoop(GetSettings()?.GuessingMusic);
        }

        /// <summary>
        /// Plays the correct-answer sting, then starts the intermediate loop when it finishes.
        /// </summary>
        public static void PlayGuessCorrectThenIntermediate()
        {
            int transitionVersion = BeginGameFlowTransition();
            AudioClip correctClip = GetSettings()?.GuessCorrect;
            if (correctClip == null)
            {
                PlayIntermediateMusicIfCurrent(transitionVersion);
                return;
            }

            var source = EnsureGameFlowAudioSource();
            source.clip = correctClip;
            source.loop = false;
            source.pitch = 1f;
            source.Play();
            EnsureCoroutineHost().StartCoroutine(PlayIntermediateAfterCorrect(transitionVersion, correctClip.length));
        }

        /// <summary>
        /// Starts the non-looping spin track and scales it to complete with the reel spin.
        /// </summary>
        /// <param name="spinDuration">The duration of the reel spin in seconds.</param>
        public static void PlaySpin(float spinDuration)
        {
            AudioClip spinClip = GetSettings()?.Spin;
            float pitch = spinClip != null && spinDuration > 0f
                ? DefaultRouletteSpinDuration / spinDuration
                : 1f;

            PlayOneShotTrack(spinClip, pitch);
        }

        private static UiSoundSettings GetSettings()
        {
            if (_settings == null)
                _settings = Resources.Load<UiSoundSettings>(SettingsResourcePath);

            return _settings;
        }

        private static void Play(AudioClip clip)
        {
            if (clip == null)
                return;

            EnsureEffectsAudioSource().PlayOneShot(clip);
        }

        private static void PlayLoop(AudioClip clip)
        {
            BeginGameFlowTransition();
            if (clip == null)
                return;

            var source = EnsureGameFlowAudioSource();
            if (source.isPlaying && source.loop && source.clip == clip)
                return;

            source.clip = clip;
            source.loop = true;
            source.pitch = 1f;
            source.Play();
        }

        private static void PlayOneShotTrack(AudioClip clip, float pitch = 1f)
        {
            BeginGameFlowTransition();
            var source = EnsureGameFlowAudioSource();
            source.Stop();
            source.clip = clip;
            source.loop = false;
            source.pitch = pitch;

            if (clip != null)
                source.Play();
        }

        private static IEnumerator PlayIntermediateAfterCorrect(int transitionVersion, float correctClipDuration)
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, correctClipDuration));
            PlayIntermediateMusicIfCurrent(transitionVersion);
        }

        private static void PlayIntermediateMusicIfCurrent(int transitionVersion)
        {
            if (transitionVersion != _gameFlowTransitionVersion)
                return;

            AudioClip intermediateClip = GetSettings()?.IntermediateMusic;
            if (intermediateClip == null)
                return;

            var source = EnsureGameFlowAudioSource();
            source.clip = intermediateClip;
            source.loop = true;
            source.pitch = 1f;
            source.Play();
        }

        private static int BeginGameFlowTransition()
        {
            _gameFlowTransitionVersion++;
            return _gameFlowTransitionVersion;
        }

        private static AudioSource EnsureEffectsAudioSource()
        {
            if (_effectsAudioSource != null)
                return _effectsAudioSource;

            var audioObject = EnsureAudioObject();
            _effectsAudioSource = audioObject.AddComponent<AudioSource>();
            Configure2DAudioSource(_effectsAudioSource);
            return _effectsAudioSource;
        }

        private static AudioSource EnsureGameFlowAudioSource()
        {
            if (_gameFlowAudioSource != null)
                return _gameFlowAudioSource;

            var audioObject = EnsureAudioObject();
            _gameFlowAudioSource = audioObject.AddComponent<AudioSource>();
            Configure2DAudioSource(_gameFlowAudioSource);
            return _gameFlowAudioSource;
        }

        private static MonoBehaviour EnsureCoroutineHost()
        {
            if (_coroutineHost != null)
                return _coroutineHost;

            _coroutineHost = EnsureAudioObject().AddComponent<UiSoundCoroutineHost>();
            return _coroutineHost;
        }

        private static GameObject EnsureAudioObject()
        {
            var audioObject = GameObject.Find(nameof(UiSoundPlayer));
            if (audioObject != null)
                return audioObject;

            audioObject = new GameObject(nameof(UiSoundPlayer));
            Object.DontDestroyOnLoad(audioObject);
            return audioObject;
        }

        private static void Configure2DAudioSource(AudioSource source)
        {
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
        }

        private sealed class UiSoundCoroutineHost : MonoBehaviour { }
    }
}
