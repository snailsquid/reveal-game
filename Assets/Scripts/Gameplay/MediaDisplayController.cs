using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Video;

namespace RevealGame.Gameplay
{
    /// <summary>
    /// Handles loading and displaying images and videos from extracted ZIP files.
    /// Provides methods to load media from file paths and display them on UI elements.
    /// </summary>
    public class MediaDisplayController : MonoBehaviour
    {
        [Header("Fallback Settings")]
        [SerializeField] private Sprite _fallbackSprite;
        [SerializeField] private string _fallbackPath = "Sprites/broken.png";

        private Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();
        private Dictionary<string, VideoClip> _videoCache = new Dictionary<string, VideoClip>();

        /// <summary>
        /// Gets or creates the fallback sprite.
        /// </summary>
        public Sprite FallbackSprite
        {
            get
            {
                if (_fallbackSprite == null)
                {
                    _fallbackSprite = Resources.Load<Sprite>(_fallbackPath);
                }
                return _fallbackSprite;
            }
        }

        /// <summary>
        /// Loads a sprite from a file path.
        /// </summary>
        /// <param name="filePath">Absolute path to the image file</param>
        /// <returns>Sprite or null if loading fails</returns>
        public Sprite LoadSprite(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return FallbackSprite;

            // Check cache first
            if (_spriteCache.TryGetValue(filePath, out var cached))
                return cached;

            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"[MediaDisplayController] File not found: {filePath}");
                    return FallbackSprite;
                }

                byte[] fileBytes = File.ReadAllBytes(filePath);
                Texture2D texture = new Texture2D(2, 2);

                if (texture.LoadImage(fileBytes))
                {
                    Sprite sprite = Sprite.Create(
                        texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f)
                    );
                    sprite.name = Path.GetFileName(filePath);

                    _spriteCache[filePath] = sprite;
                    return sprite;
                }
                else
                {
                    Debug.LogError($"[MediaDisplayController] Failed to load image: {filePath}");
                    Destroy(texture);
                    return FallbackSprite;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[MediaDisplayController] Error loading {filePath}: {e.Message}");
                return FallbackSprite;
            }
        }

        /// <summary>
        /// Loads a sprite from bytes.
        /// </summary>
        /// <param name="fileBytes">Image file bytes</param>
        /// <param name="name">Name for the sprite</param>
        /// <returns>Sprite or null if loading fails</returns>
        public Sprite LoadSpriteFromBytes(byte[] fileBytes, string name = "sprite")
        {
            if (fileBytes == null || fileBytes.Length == 0)
                return FallbackSprite;

            try
            {
                Texture2D texture = new Texture2D(2, 2);

                if (texture.LoadImage(fileBytes))
                {
                    Sprite sprite = Sprite.Create(
                        texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f)
                    );
                    sprite.name = name;
                    return sprite;
                }
                else
                {
                    Debug.LogError($"[MediaDisplayController] Failed to load image from bytes");
                    Destroy(texture);
                    return FallbackSprite;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[MediaDisplayController] Error loading from bytes: {e.Message}");
                return FallbackSprite;
            }
        }

        /// <summary>
        /// Gets the URL for a video file.
        /// </summary>
        /// <param name="filePath">Absolute path to the video file</param>
        /// <returns>File URL or null if file doesn't exist</returns>
        public string GetVideoUrl(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return null;

            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[MediaDisplayController] Video file not found: {filePath}");
                return null;
            }

            return $"file://{filePath}";
        }

        /// <summary>
        /// Checks if a file is an image based on extension.
        /// </summary>
        public static bool IsImageFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            return ext == ".png" || ext == ".jpg" || ext == ".jpeg" ||
                   ext == ".gif" || ext == ".bmp" || ext == ".tga";
        }

        /// <summary>
        /// Checks if a file is a video based on extension.
        /// </summary>
        public static bool IsVideoFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            return ext == ".mp4" || ext == ".avi" || ext == ".mov" ||
                   ext == ".webm" || ext == ".mkv";
        }

        /// <summary>
        /// Clears the sprite cache.
        /// </summary>
        public void ClearCache()
        {
            foreach (var sprite in _spriteCache.Values)
            {
                if (sprite != null)
                {
                    Destroy(sprite.texture);
                    Destroy(sprite);
                }
            }
            _spriteCache.Clear();
            _videoCache.Clear();
        }

        private void OnDestroy()
        {
            ClearCache();
        }
    }
}
