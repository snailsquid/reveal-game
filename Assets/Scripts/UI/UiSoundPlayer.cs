using UnityEngine;

namespace RevealGame.UI
{
    /// <summary>
    /// Plays shared, non-spatial interface sounds through one persistent audio source.
    /// </summary>
    public static class UiSoundPlayer
    {
        private const string SettingsResourcePath = "UiSoundSettings";

        private static UiSoundSettings _settings;
        private static AudioSource _audioSource;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _settings = null;
            _audioSource = null;
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

            EnsureAudioSource().PlayOneShot(clip);
        }

        private static AudioSource EnsureAudioSource()
        {
            if (_audioSource != null)
                return _audioSource;

            var audioObject = new GameObject(nameof(UiSoundPlayer));
            Object.DontDestroyOnLoad(audioObject);

            _audioSource = audioObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.loop = false;
            _audioSource.spatialBlend = 0f;
            return _audioSource;
        }
    }
}
