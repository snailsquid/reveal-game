using NUnit.Framework;
using RevealGame.Data;
using RevealGame.UI;
using UnityEngine;
using UnityEngine.UI;

namespace RevealGame.Tests
{
    [TestFixture]
    public sealed class ImageAspectRatioFitterTests
    {
        private const float Tolerance = 0.01f;

        [TestCase(4, 1, 800f, 200f)]
        [TestCase(1, 2, 400f, 800f)]
        public void RefreshAspectRatio_EnvelopeParentCoversContainer(
            int spriteWidth,
            int spriteHeight,
            float expectedWidth,
            float expectedHeight)
        {
            var fixture = CreateFixture();
            var sprite = CreateSprite(spriteWidth, spriteHeight);

            try
            {
                fixture.Image.sprite = sprite;
                fixture.DynamicFitter.RefreshAspectRatio();
                Canvas.ForceUpdateCanvases();

                Assert.IsNull(fixture.Image.GetComponent<AspectRatioFitter>());
                Assert.AreEqual(expectedWidth, fixture.Image.rectTransform.rect.width, Tolerance);
                Assert.AreEqual(expectedHeight, fixture.Image.rectTransform.rect.height, Tolerance);
                Assert.GreaterOrEqual(
                    fixture.Image.rectTransform.rect.width + Tolerance,
                    fixture.Container.rect.width);
                Assert.GreaterOrEqual(
                    fixture.Image.rectTransform.rect.height + Tolerance,
                    fixture.Container.rect.height);
            }
            finally
            {
                DestroyFixture(fixture, sprite);
            }
        }

        [Test]
        public void RefreshAspectRatio_WhenSpriteChangesUpdatesCoverGeometry()
        {
            var fixture = CreateFixture();
            var wideSprite = CreateSprite(4, 1);
            var tallSprite = CreateSprite(1, 2);

            try
            {
                fixture.Image.sprite = wideSprite;
                fixture.DynamicFitter.RefreshAspectRatio();
                Canvas.ForceUpdateCanvases();
                Assert.AreEqual(800f, fixture.Image.rectTransform.rect.width, Tolerance);

                fixture.Image.sprite = tallSprite;
                fixture.DynamicFitter.RefreshAspectRatio();
                Canvas.ForceUpdateCanvases();
                Assert.AreEqual(800f, fixture.Image.rectTransform.rect.height, Tolerance);
            }
            finally
            {
                DestroyFixture(fixture, wideSprite, tallSprite);
            }
        }

        [TestCase(ItemImageAlignment.Left, 0f)]
        [TestCase(ItemImageAlignment.Center, 0.5f)]
        [TestCase(ItemImageAlignment.Right, 1f)]
        public void ApplyAlignment_WithEnvelopeParentUsesPivotAsObjectPosition(
            ItemImageAlignment alignment,
            float expectedPivot)
        {
            var fixture = CreateFixture();
            var sprite = CreateSprite(4, 1);

            try
            {
                fixture.Image.sprite = sprite;
                fixture.DynamicFitter.RefreshAspectRatio();
                ItemImageAlignmentUtility.Apply(
                    fixture.Image,
                    new ItemData { imageAlignment = alignment });
                Canvas.ForceUpdateCanvases();

                Assert.AreEqual(0f, fixture.Image.rectTransform.anchorMin.x, Tolerance);
                Assert.AreEqual(1f, fixture.Image.rectTransform.anchorMax.x, Tolerance);
                Assert.AreEqual(expectedPivot, fixture.Image.rectTransform.pivot.x, Tolerance);
                Assert.AreEqual(0f, fixture.Image.rectTransform.anchoredPosition.x, Tolerance);
                Assert.AreEqual(800f, fixture.Image.rectTransform.rect.width, Tolerance);
            }
            finally
            {
                DestroyFixture(fixture, sprite);
            }
        }

        private static CoverFixture CreateFixture()
        {
            var canvasObject = new GameObject("Cover Canvas", typeof(RectTransform), typeof(Canvas));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var containerObject = new GameObject("Image Container", typeof(RectTransform));
            var container = containerObject.GetComponent<RectTransform>();
            container.SetParent(canvasObject.transform, false);
            container.anchorMin = new Vector2(0.5f, 0.5f);
            container.anchorMax = new Vector2(0.5f, 0.5f);
            container.sizeDelta = new Vector2(400f, 200f);

            var imageObject = new GameObject("Image", typeof(RectTransform));
            var imageRect = imageObject.GetComponent<RectTransform>();
            imageRect.SetParent(container, false);
            imageRect.anchorMin = new Vector2(0.5f, 0.5f);
            imageRect.anchorMax = new Vector2(0.5f, 0.5f);

            var image = imageObject.AddComponent<Image>();
            var dynamicFitter = imageObject.AddComponent<ImageAspectRatioFitter>();

            return new CoverFixture(canvasObject, container, image, dynamicFitter);
        }

        private static Sprite CreateSprite(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f));
        }

        private static void DestroyFixture(CoverFixture fixture, params Sprite[] sprites)
        {
            foreach (var sprite in sprites)
            {
                if (sprite == null)
                    continue;

                var texture = sprite.texture;
                Object.DestroyImmediate(sprite);
                Object.DestroyImmediate(texture);
            }

            Object.DestroyImmediate(fixture.CanvasObject);
        }

        private readonly struct CoverFixture
        {
            public CoverFixture(
                GameObject canvasObject,
                RectTransform container,
                Image image,
                ImageAspectRatioFitter dynamicFitter)
            {
                CanvasObject = canvasObject;
                Container = container;
                Image = image;
                DynamicFitter = dynamicFitter;
            }

            public GameObject CanvasObject { get; }
            public RectTransform Container { get; }
            public Image Image { get; }
            public ImageAspectRatioFitter DynamicFitter { get; }
        }
    }
}
