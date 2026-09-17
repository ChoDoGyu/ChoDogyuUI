using System.Collections;
using CDG.Core.Results;
using UnityEngine;

namespace CDG.UI.Samples.BasicUsage
{
    /// <summary>
    /// UI Framework의 Screen, Popup, Overlay, Back 및 Transition 기본 사용 흐름을 보여주는 Sample Controller입니다.
    /// </summary>
    public sealed class BasicUISampleController : MonoBehaviour
    {
        private static readonly UIId HomeScreenId = new UIId("sample.screen.home");
        private static readonly UIId DetailsScreenId = new UIId("sample.screen.details");
        private static readonly UIId SettingsPopupId = new UIId("sample.popup.settings");
        private static readonly UIId LoadingOverlayId = new UIId("sample.overlay.loading");

        [SerializeField]
        private UIController uiController;

        [SerializeField]
        [Min(0f)]
        private float loadingDuration = 1.5f;

        private string statusMessage = "Sample 준비 완료";

        private void Start()
        {
            if (uiController == null)
            {
                Debug.LogError("Basic UI Sample에 UIController가 지정되지 않았습니다.");
                statusMessage = "UIController가 지정되지 않았습니다.";
                return;
            }

            OpenHome();
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(16f, 16f, 280f, 320f), GUI.skin.box);

            GUILayout.Label("ChoDogyu UI Framework");
            GUILayout.Label("Basic Usage Sample");
            GUILayout.Space(8f);

            GUI.enabled = uiController != null;

            if (GUILayout.Button("Reset Home"))
            {
                OpenHome();
            }

            if (GUILayout.Button("Open Details (Push)"))
            {
                OpenDetails();
            }

            if (GUILayout.Button("Open Settings Popup"))
            {
                OpenSettingsPopup();
            }

            if (GUILayout.Button("Show Loading Overlay"))
            {
                ShowLoadingOverlay();
            }

            if (GUILayout.Button("Back"))
            {
                Back();
            }

            GUI.enabled = true;

            GUILayout.Space(8f);
            GUILayout.Label($"Status: {statusMessage}");

            if (uiController != null)
            {
                GUILayout.Label($"History: {uiController.ScreenHistoryCount}");
                GUILayout.Label($"Popup Count: {uiController.PopupCount}");
                GUILayout.Label($"Can Back: {uiController.CanBack}");
                GUILayout.Label($"Busy: {uiController.IsBusy}");
            }

            GUILayout.EndArea();
        }

        private void OpenHome()
        {
            Result<UIScreen> result = uiController.OpenScreen(HomeScreenId, UIScreenOpenMode.Reset);

            if (result.IsFailure)
            {
                SetFailure(result.Error.Message);
                return;
            }

            statusMessage = "Home Screen을 열었습니다.";
        }

        private void OpenDetails()
        {
            Result<UIScreen> result = uiController.OpenScreen(DetailsScreenId, UIScreenOpenMode.Push);

            if (result.IsFailure)
            {
                SetFailure(result.Error.Message);
                return;
            }

            statusMessage = "Details Screen을 Push 방식으로 열었습니다.";
        }

        private void OpenSettingsPopup()
        {
            Result<UIPopup> result = uiController.OpenPopup(SettingsPopupId);

            if (result.IsFailure)
            {
                SetFailure(result.Error.Message);
                return;
            }

            statusMessage = "Settings Popup을 열었습니다.";
        }

        private void ShowLoadingOverlay()
        {
            Result<UIOverlay> result = uiController.OpenOverlay(LoadingOverlayId);

            if (result.IsFailure)
            {
                SetFailure(result.Error.Message);
                return;
            }

            statusMessage = "Loading Overlay를 열었습니다.";
            StartCoroutine(CloseLoadingOverlayAfterDelay());
        }

        private void Back()
        {
            Result result = uiController.Back();

            if (result.IsFailure)
            {
                SetFailure(result.Error.Message);
                return;
            }

            statusMessage = "Back 요청을 처리했습니다.";
        }

        private IEnumerator CloseLoadingOverlayAfterDelay()
        {
            yield return new WaitForSecondsRealtime(loadingDuration);

            Result result = uiController.Close(LoadingOverlayId);

            if (result.IsFailure)
            {
                SetFailure(result.Error.Message);
                yield break;
            }

            statusMessage = "Loading Overlay를 닫았습니다.";
        }

        private void SetFailure(string message)
        {
            statusMessage = $"실패: {message}";
            Debug.LogWarning($"[Basic UI Sample] {message}");
        }
    }
}