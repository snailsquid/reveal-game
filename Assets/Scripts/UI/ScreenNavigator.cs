using UnityEngine;

namespace RevealGame.UI
{
    /// <summary>
    /// Owns activation and discovery of the mutually exclusive game screens.
    /// </summary>
    public sealed class ScreenNavigator
    {
        private GameObject _titleScreen;
        private GameObject _lootboxScreen;
        private GameObject _hintGameScreen;

        public GameObject TitleScreen => _titleScreen;
        public GameObject LootboxScreen => _lootboxScreen;
        public GameObject HintGameScreen => _hintGameScreen;

        public ScreenNavigator(GameObject titleScreen, GameObject lootboxScreen, GameObject hintGameScreen)
        {
            _titleScreen = titleScreen;
            _lootboxScreen = lootboxScreen;
            _hintGameScreen = hintGameScreen;
        }

        public void ResolveScreenReferences(Transform root)
        {
            if (root == null)
                return;

            var canvas = root.GetComponentInParent<Canvas>()
                ?? root.GetComponentInChildren<Canvas>(true);
            var searchRoot = canvas != null ? canvas.transform : root;

            if (_titleScreen == null || (!_titleScreen.activeInHierarchy && _titleScreen.transform.parent == null))
                _titleScreen = FindScreenChild(searchRoot, "TitleScreen") ?? _titleScreen;
            if (_lootboxScreen == null || (!_lootboxScreen.activeInHierarchy && _lootboxScreen.transform.parent == null))
                _lootboxScreen = FindScreenChild(searchRoot, "LootboxScreen") ?? _lootboxScreen;
            if (_hintGameScreen == null || (!_hintGameScreen.activeInHierarchy && _hintGameScreen.transform.parent == null))
                _hintGameScreen = FindScreenChild(searchRoot, "HintGameScreen") ?? _hintGameScreen;
        }

        public void ShowTitleScreen()
        {
            ShowOnly(_titleScreen);
        }

        public void ShowLootboxScreen()
        {
            ShowOnly(_lootboxScreen);
        }

        public void ShowHintGameScreen()
        {
            ShowOnly(_hintGameScreen);
        }

        private void ShowOnly(GameObject activeScreen)
        {
            if (activeScreen == null)
            {
                Debug.LogError("[ScreenNavigator] Cannot change screens because the target screen is not assigned.");
                return;
            }

            TransitionScreen(_titleScreen, _titleScreen == activeScreen);
            TransitionScreen(_lootboxScreen, _lootboxScreen == activeScreen);
            TransitionScreen(_hintGameScreen, _hintGameScreen == activeScreen);
        }

        private static void TransitionScreen(GameObject screen, bool shouldShow)
        {
            if (screen == null)
                return;

            var transition = UiTransition.GetOrAdd(screen);
            if (shouldShow)
                transition.Show();
            else if (screen.activeSelf)
                transition.Hide();
        }

        private static GameObject FindScreenChild(Transform root, string name)
        {
            foreach (Transform child in root)
            {
                if (child.gameObject.name == name)
                    return child.gameObject;
            }

            return null;
        }
    }
}
