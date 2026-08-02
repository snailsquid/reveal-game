using System.Collections;
using NUnit.Framework;
using RevealGame.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace RevealGame.Tests.Editor
{
    [TestFixture]
    public sealed class RevealCardCoverEditModeTests
    {
        private const string PrefabPath = "Assets/Prefabs/UI/RevealCard.prefab";
        private const float Tolerance = 0.01f;

        [UnityTest]
        public IEnumerator RevealCardPrefab_ImageCoversContainerInPrefabMode()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab);

            var instance = Object.Instantiate(prefab);
            try
            {
                var container = instance.transform.Find("ImageContainer") as RectTransform;
                var imageTransform = instance.transform.Find("ImageContainer/Image") as RectTransform;
                Assert.IsNotNull(container);
                Assert.IsNotNull(imageTransform);

                var image = imageTransform.GetComponent<Image>();
                var dynamicFitter = imageTransform.GetComponent<ImageAspectRatioFitter>();
                Assert.IsNotNull(image.sprite);
                Assert.IsNotNull(dynamicFitter);
                Assert.IsNull(imageTransform.GetComponent<AspectRatioFitter>());

                dynamicFitter.RefreshAspectRatio();
                yield return null;
                yield return null;
                Canvas.ForceUpdateCanvases();

                var spriteSize = image.sprite.rect.size;
                Assert.AreEqual(
                    spriteSize.x / spriteSize.y,
                    imageTransform.rect.width / imageTransform.rect.height,
                    Tolerance);
                Assert.GreaterOrEqual(imageTransform.rect.width + Tolerance, container.rect.width);
                Assert.GreaterOrEqual(imageTransform.rect.height + Tolerance, container.rect.height);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }
    }
}
