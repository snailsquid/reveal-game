using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RevealGame.UI
{
    /// <summary>
    /// A dependency-free UGUI confetti burst intended for celebratory reward reveals.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RouletteConfettiBurst : MonoBehaviour
    {
        [Header("Burst Settings")]
        [Min(1)]
        [SerializeField] private int _pieceCount = 72;
        [Min(0.1f)]
        [SerializeField] private float _minimumLifetime = 1.5f;
        [Min(0.1f)]
        [SerializeField] private float _maximumLifetime = 2.4f;
        [Min(1f)]
        [SerializeField] private float _minimumSpeed = 700f;
        [Min(1f)]
        [SerializeField] private float _maximumSpeed = 1100f;
        [Min(1f)]
        [SerializeField] private float _gravity = 1350f;
        [SerializeField] private Vector2 _minimumSize = new Vector2(10f, 18f);
        [SerializeField] private Vector2 _maximumSize = new Vector2(18f, 30f);

        [Header("Colours")]
        [SerializeField] private Color[] _colours =
        {
            new Color(1f, 0.24f, 0.32f),
            new Color(1f, 0.72f, 0.08f),
            new Color(0.19f, 0.8f, 0.52f),
            new Color(0.16f, 0.62f, 1f),
            new Color(0.64f, 0.35f, 1f),
            new Color(1f, 0.32f, 0.72f)
        };

        private readonly List<ConfettiPiece> _activePieces = new();

        private RectTransform _burstLayer;
        private Coroutine _burstRoutine;

        private void Awake()
        {
            EnsureBurstLayer();
        }

        private void OnDisable()
        {
            StopBurst();
        }

        /// <summary>
        /// Fires a new burst, replacing any remaining pieces from the previous one.
        /// </summary>
        public void Play()
        {
            EnsureBurstLayer();
            StopBurst();

            if (_burstLayer == null || _colours == null || _colours.Length == 0)
                return;

            _burstLayer.gameObject.SetActive(true);
            _burstLayer.SetAsLastSibling();
            _burstRoutine = StartCoroutine(PlayBurst());
        }

        private IEnumerator PlayBurst()
        {
            Canvas.ForceUpdateCanvases();
            Vector2 layerSize = _burstLayer.rect.size;
            float spread = Mathf.Max(80f, layerSize.x * 0.16f);
            float launchHeight = -layerSize.y * 0.1f;

            for (int i = 0; i < _pieceCount; i++)
            {
                float launchAngle = Random.Range(42f, 138f) * Mathf.Deg2Rad;
                float speed = Random.Range(_minimumSpeed, Mathf.Max(_minimumSpeed, _maximumSpeed));
                Vector2 velocity = new Vector2(Mathf.Cos(launchAngle), Mathf.Sin(launchAngle)) * speed;
                Vector2 size = new Vector2(
                    Random.Range(_minimumSize.x, Mathf.Max(_minimumSize.x, _maximumSize.x)),
                    Random.Range(_minimumSize.y, Mathf.Max(_minimumSize.y, _maximumSize.y)));

                _activePieces.Add(CreatePiece(
                    new Vector2(Random.Range(-spread, spread), launchHeight + Random.Range(-20f, 40f)),
                    velocity,
                    size));
            }

            float elapsed = 0f;
            float maximumLifetime = Mathf.Max(_minimumLifetime, _maximumLifetime);
            while (elapsed < maximumLifetime)
            {
                float deltaTime = Time.unscaledDeltaTime;
                elapsed += deltaTime;

                for (int i = _activePieces.Count - 1; i >= 0; i--)
                {
                    ConfettiPiece piece = _activePieces[i];
                    piece.Age += deltaTime;
                    if (piece.Age >= piece.Lifetime)
                    {
                        Destroy(piece.Transform.gameObject);
                        _activePieces.RemoveAt(i);
                        continue;
                    }

                    piece.Velocity += Vector2.down * _gravity * deltaTime;
                    piece.Transform.anchoredPosition += piece.Velocity * deltaTime;
                    piece.Transform.Rotate(0f, 0f, piece.SpinDegreesPerSecond * deltaTime);

                    Color colour = piece.Image.color;
                    colour.a = Mathf.Clamp01((piece.Lifetime - piece.Age) / 0.35f);
                    piece.Image.color = colour;
                }

                yield return null;
            }

            ClearPieces();
            _burstLayer.gameObject.SetActive(false);
            _burstRoutine = null;
        }

        private ConfettiPiece CreatePiece(Vector2 position, Vector2 velocity, Vector2 size)
        {
            var pieceObject = new GameObject("Confetti", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var pieceTransform = pieceObject.GetComponent<RectTransform>();
            pieceTransform.SetParent(_burstLayer, false);
            pieceTransform.anchorMin = new Vector2(0.5f, 0.5f);
            pieceTransform.anchorMax = new Vector2(0.5f, 0.5f);
            pieceTransform.anchoredPosition = position;
            pieceTransform.sizeDelta = size;
            pieceTransform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            var image = pieceObject.GetComponent<Image>();
            image.color = _colours[Random.Range(0, _colours.Length)];
            image.raycastTarget = false;

            return new ConfettiPiece
            {
                Transform = pieceTransform,
                Image = image,
                Velocity = velocity,
                Lifetime = Random.Range(_minimumLifetime, Mathf.Max(_minimumLifetime, _maximumLifetime)),
                SpinDegreesPerSecond = Random.Range(-540f, 540f)
            };
        }

        private void EnsureBurstLayer()
        {
            if (_burstLayer != null)
                return;

            var layerObject = new GameObject("Confetti Burst", typeof(RectTransform));
            _burstLayer = layerObject.GetComponent<RectTransform>();
            _burstLayer.SetParent(transform, false);
            _burstLayer.anchorMin = Vector2.zero;
            _burstLayer.anchorMax = Vector2.one;
            _burstLayer.offsetMin = Vector2.zero;
            _burstLayer.offsetMax = Vector2.zero;
            _burstLayer.SetAsLastSibling();
            _burstLayer.gameObject.SetActive(false);
        }

        private void StopBurst()
        {
            if (_burstRoutine != null)
            {
                StopCoroutine(_burstRoutine);
                _burstRoutine = null;
            }

            ClearPieces();
            if (_burstLayer != null)
                _burstLayer.gameObject.SetActive(false);
        }

        private void ClearPieces()
        {
            foreach (ConfettiPiece piece in _activePieces)
            {
                if (piece.Transform != null)
                    Destroy(piece.Transform.gameObject);
            }

            _activePieces.Clear();
        }

        private sealed class ConfettiPiece
        {
            public RectTransform Transform;
            public Image Image;
            public Vector2 Velocity;
            public float Lifetime;
            public float Age;
            public float SpinDegreesPerSecond;
        }
    }
}
