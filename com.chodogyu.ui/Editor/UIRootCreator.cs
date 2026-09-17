using CDG.Core.Results;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CDG.UI.Editor
{
    /// <summary>
    /// 지정한 Scene에 UI Framework가 사용하는 Root와 Layer 계층을 생성합니다.
    /// 일반 사용 시 현재 활성 Scene을 대상으로 하며, 하나의 Scene에는 하나의 UIController만 생성합니다.
    /// </summary>
    internal static class UIRootCreator
    {
        private const string CreateUndoName = "Create CDG UI Root";

        /// <summary>
        /// 현재 활성 Scene에 CDG UI Root와 기본 Layer 계층을 생성합니다.
        /// 이미 UIController가 존재하는 경우 중복 Root를 생성하지 않고 실패합니다.
        /// </summary>
        /// <returns>생성된 UIController 또는 생성 실패 정보를 포함하는 결과입니다.</returns>
        internal static Result<UIController> Create()
        {
            return Create(SceneManager.GetActiveScene());
        }

        /// <summary>
        /// 지정한 Scene에 CDG UI Root와 기본 Layer 계층을 생성합니다.
        /// Editor 테스트와 내부 생성 흐름에서 Scene을 명시적으로 지정할 때 사용합니다.
        /// </summary>
        /// <param name="scene">UI Root를 생성할 Scene입니다.</param>
        /// <returns>생성된 UIController 또는 생성 실패 정보를 포함하는 결과입니다.</returns>
        internal static Result<UIController> Create(Scene scene)
        {
            UIController existingController = FindControllerInScene(scene);

            if (existingController != null)
            {
                return Result<UIController>.Failure(new ResultError(
                    UIEditorErrorCodes.RootAlreadyExists,
                    $"Scene '{scene.name}'에 이미 UIController가 존재합니다."));
            }

            GameObject rootObject = new GameObject(
                UIEditorConstants.RootObjectName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(GraphicRaycaster));

            SceneManager.MoveGameObjectToScene(rootObject, scene);

            UIController controller = rootObject.AddComponent<UIController>();
            Canvas canvas = rootObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            RectTransform screenLayer = CreateLayer(UIEditorConstants.ScreenLayerName, rootObject.transform);
            RectTransform overlayLayer = CreateLayer(UIEditorConstants.OverlayLayerName, rootObject.transform);
            RectTransform popupLayer = CreateLayer(UIEditorConstants.PopupLayerName, rootObject.transform);
            RectTransform topOverlayLayer = CreateLayer(UIEditorConstants.TopOverlayLayerName, rootObject.transform);

            controller.SetLayers(screenLayer, overlayLayer, popupLayer, topOverlayLayer);

            if (!EditorSceneManager.IsPreviewScene(scene))
            {
                Undo.RegisterCreatedObjectUndo(rootObject, CreateUndoName);
                Selection.activeGameObject = rootObject;
                EditorSceneManager.MarkSceneDirty(scene);
            }

            return Result<UIController>.Success(controller);
        }

        private static UIController FindControllerInScene(Scene scene)
        {
            GameObject[] rootObjects = scene.GetRootGameObjects();

            foreach (GameObject rootObject in rootObjects)
            {
                UIController controller = rootObject.GetComponentInChildren<UIController>(true);

                if (controller != null)
                {
                    return controller;
                }
            }

            return null;
        }

        private static RectTransform CreateLayer(string name, Transform parent)
        {
            GameObject layerObject = new GameObject(name, typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(layerObject, parent.gameObject.scene);

            RectTransform rectTransform = layerObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.localScale = Vector3.one;

            return rectTransform;
        }
    }
}