using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using RevealGame.UI;

namespace RevealGame.Editor
{
    public static class SetupHintGameViews
    {
        [MenuItem("Tools/Setup Hint Game Views")]
        public static void Setup()
        {
            var hintGameScreen = GameObject.Find("Canvas/HintGameScreen");
            if (hintGameScreen == null)
            {
                Debug.LogError("Could not find HintGameScreen in scene");
                return;
            }
            
            var gameManagerObj = GameObject.Find("GameManager");
            if (gameManagerObj == null)
            {
                Debug.LogError("Could not find GameManager in scene");
                return;
            }
            
            var resultPopup = hintGameScreen.transform.Find("ResultPopup");
            var resultText = hintGameScreen.transform.Find("ResultPopup/ResultText");
            var continueButton = hintGameScreen.transform.Find("ResultPopup/ContinueButton");
            
            var hintGameUIController = hintGameScreen.GetComponent<HintGameUIController>();
            if (hintGameUIController == null)
            {
                Debug.LogError("HintGameScreen does not have HintGameUIController component");
                return;
            }

            var openingCoordinator = hintGameScreen.GetComponent<HintGameOpeningCoordinator>();
            if (openingCoordinator == null)
                openingCoordinator = hintGameScreen.AddComponent<HintGameOpeningCoordinator>();
            openingCoordinator.SetFieldValue("_gameManager", gameManagerObj.GetComponent<GameManager>());

            // The current prefab already owns these phase views. Keep the menu command
            // safe to run repeatedly when opening an older scene or revalidating setup.
            var existingHintView = hintGameScreen.GetComponent<HintGameView>();
            var existingAnswerPopup = resultPopup != null
                ? resultPopup.GetComponent<AnswerPopupView>()
                : null;
            var existingRouletteDisplay = hintGameScreen.GetComponentInChildren<RouletteDisplay>(true);
            if (existingHintView != null && existingAnswerPopup != null && existingRouletteDisplay != null)
            {
                EditorSceneManager.MarkSceneDirty(hintGameScreen.scene);
                Debug.Log("Hint Game Views are already configured.");
                return;
            }
            
            // Setup HintGameView
            var hintViewGO = new GameObject("HintGameView")
            {
                layer = hintGameScreen.layer
            };
            hintViewGO.transform.SetParent(hintGameScreen.transform, false);
            var hintView = hintViewGO.AddComponent<HintGameView>();
            
            // Move existing UI elements to HintGameView as children
            var hintMediaImage = hintGameScreen.transform.Find("HintMediaImage");
            if (hintMediaImage != null)
            {
                hintMediaImage.SetParent(hintViewGO.transform, true);
                hintView.SetFieldValue("_hintMediaImage", hintMediaImage.GetComponent<RawImage>());
            }
            
            var hintTitleText = hintGameScreen.transform.Find("HintTitleText");
            if (hintTitleText != null)
            {
                hintTitleText.SetParent(hintViewGO.transform, true);
                hintView.SetFieldValue("_hintTitleText", hintTitleText.GetComponent<Text>());
            }
            
            var previousButton = hintGameScreen.transform.Find("PreviousHintImageButton");
            if (previousButton != null)
            {
                previousButton.SetParent(hintViewGO.transform, true);
                hintView.SetFieldValue("_previousHintImageButton", previousButton.GetComponent<Button>());
            }
            
            var nextButton = hintGameScreen.transform.Find("NextHintImageButton");
            if (nextButton != null)
            {
                nextButton.SetParent(hintViewGO.transform, true);
                hintView.SetFieldValue("_nextHintImageButton", nextButton.GetComponent<Button>());
            }
            
            var paginationText = hintGameScreen.transform.Find("HintImagePaginationText");
            if (paginationText != null)
            {
                paginationText.SetParent(hintViewGO.transform, true);
                hintView.SetFieldValue("_hintImagePaginationText", paginationText.GetComponent<Text>());
            }
            
            var backButton = hintGameScreen.transform.Find("BackButton");
            if (backButton != null)
            {
                backButton.SetParent(hintViewGO.transform, true);
                hintView.SetFieldValue("_backButton", backButton.GetComponent<Button>());
            }
            
            var answerInput = hintGameScreen.transform.Find("AnswerInput");
            if (answerInput != null)
            {
                answerInput.SetParent(hintViewGO.transform, true);
                hintView.SetFieldValue("_answerInput", answerInput.GetComponent<InputField>());
            }
            
            var submitButton = hintGameScreen.transform.Find("SubmitButton");
            if (submitButton != null)
            {
                submitButton.SetParent(hintViewGO.transform, true);
                hintView.SetFieldValue("_submitButton", submitButton.GetComponent<Button>());
            }
            
            // Setup AnswerPopupView
            AnswerPopupView answerPopup = null;
            if (resultPopup != null)
            {
                answerPopup = resultPopup.gameObject.AddComponent<AnswerPopupView>();
                answerPopup.SetFieldValue("_popupRoot", resultPopup.gameObject);
                
                if (resultText != null)
                {
                    answerPopup.SetFieldValue("_resultText", resultText.GetComponent<Text>());
                }
                
                if (continueButton != null)
                {
                    answerPopup.SetFieldValue("_continueButton", continueButton.GetComponent<Button>());
                }
            }
            
            // Setup RouletteDisplay
            var rouletteDisplayGO = new GameObject("RouletteDisplay")
            {
                layer = hintGameScreen.layer
            };
            rouletteDisplayGO.transform.SetParent(hintGameScreen.transform, false);
            var rouletteDisplay = rouletteDisplayGO.AddComponent<RouletteDisplay>();
            rouletteDisplay.SetFieldValue("_rouletteRoot", rouletteDisplayGO);

            // HintGameUIController owns ordered navigation. Each step requests
            // Next or Previous through its own HintGameNavigationStep.
            
            // Mark scene as dirty
            EditorSceneManager.MarkSceneDirty(hintGameScreen.scene);
            
            Debug.Log("Hint Game Views setup complete!");
        }
        
        private static void SetFieldValue<T>(this Component component, string fieldName, T value)
        {
            var componentType = component.GetType();
            var type = componentType;
            var field = default(System.Reflection.FieldInfo);
            while (type != null && field == null)
            {
                field = type.GetField(fieldName, System.Reflection.BindingFlags.NonPublic |
                                                               System.Reflection.BindingFlags.Instance |
                                                               System.Reflection.BindingFlags.DeclaredOnly);
                type = type.BaseType;
            }
            if (field != null)
            {
                field.SetValue(component, value);
            }
            else
            {
                Debug.LogWarning($"Field {fieldName} not found on {componentType.Name}");
            }
        }
    }
}
