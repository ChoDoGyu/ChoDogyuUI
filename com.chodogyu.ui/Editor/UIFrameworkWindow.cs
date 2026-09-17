using System.IO;
using CDG.Core.Results;
using UnityEditor;
using UnityEngine;

namespace CDG.UI.Editor
{
    /// <summary>
    /// UI Root, Registry, View Prefab 생성과 Validation 기능을 제공하는
    /// ChoDogyu UI Framework의 중심 Editor Window입니다.
    /// </summary>
    public sealed class UIFrameworkWindow : EditorWindow
    {
        private const float MinimumWidth = 420f;
        private const float MinimumHeight = 320f;

        private Vector2 scrollPosition;

        private UIViewPrefabType viewPrefabType = UIViewPrefabType.Screen;
        private string viewId;
        private UIRegistry viewRegistry;
        private bool popupBlocksInput = true;
        private UIOverlayLayer overlayLayer = UIOverlayLayer.Normal;
        private bool overlayBlocksInput;

        private string statusMessage;
        private MessageType statusMessageType = MessageType.None;

        /// <summary>
        /// UI Framework Editor Window를 열거나 기존 Window에 포커스를 이동합니다.
        /// </summary>
        [MenuItem(UIEditorConstants.OpenWindowMenuPath)]
        public static void Open()
        {
            UIFrameworkWindow window = GetWindow<UIFrameworkWindow>();

            window.titleContent = new GUIContent(UIEditorConstants.WindowTitle);
            window.minSize = new Vector2(MinimumWidth, MinimumHeight);
            window.Show();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.LabelField(
                "ChoDogyu UI Framework & Editor Tool",
                EditorStyles.boldLabel);

            EditorGUILayout.Space(8f);

            EditorGUILayout.HelpBox(
                "UI Root, Registry, View Prefab 생성 및 Validation 기능을 제공하는 Editor Tool입니다.",
                MessageType.Info);

            EditorGUILayout.Space(12f);

            DrawRootSection();

            EditorGUILayout.Space(12f);

            DrawRegistrySection();

            EditorGUILayout.Space(12f);

            DrawViewPrefabSection();

            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.Space(12f);
                EditorGUILayout.HelpBox(statusMessage, statusMessageType);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawRootSection()
        {
            EditorGUILayout.LabelField("UI Root", EditorStyles.boldLabel);

            EditorGUILayout.LabelField(
                "현재 활성 Scene에 CDG UI Root와 기본 Layer를 생성합니다.",
                EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space(4f);

            if (GUILayout.Button("Create UI Root"))
            {
                CreateRoot();
            }
        }

        private void DrawRegistrySection()
        {
            EditorGUILayout.LabelField("UI Registry", EditorStyles.boldLabel);

            EditorGUILayout.LabelField(
                "프로젝트의 Assets 폴더 내부에 새로운 UIRegistry Asset을 생성합니다.",
                EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space(4f);

            if (GUILayout.Button("Create UI Registry"))
            {
                CreateRegistry();
            }
        }

        private void DrawViewPrefabSection()
        {
            EditorGUILayout.LabelField("View Prefab", EditorStyles.boldLabel);

            EditorGUILayout.LabelField(
                "Screen, Popup, Overlay Prefab을 생성하고 선택한 Registry에 자동으로 등록할 수 있습니다.",
                EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space(6f);

            viewPrefabType = (UIViewPrefabType)EditorGUILayout.EnumPopup("View Type", viewPrefabType);
            viewId = EditorGUILayout.TextField("UI ID", viewId);

            viewRegistry = (UIRegistry)EditorGUILayout.ObjectField(
                "Registry (Optional)",
                viewRegistry,
                typeof(UIRegistry),
                false);

            DrawViewTypeOptions();

            EditorGUILayout.Space(6f);

            if (GUILayout.Button("Create View Prefab"))
            {
                CreateViewPrefab();
            }
        }

        private void DrawViewTypeOptions()
        {
            switch (viewPrefabType)
            {
                case UIViewPrefabType.Popup:
                    popupBlocksInput = EditorGUILayout.Toggle("Blocks Input", popupBlocksInput);
                    break;

                case UIViewPrefabType.Overlay:
                    overlayLayer = (UIOverlayLayer)EditorGUILayout.EnumPopup("Overlay Layer", overlayLayer);
                    overlayBlocksInput = EditorGUILayout.Toggle("Blocks Input", overlayBlocksInput);
                    break;
            }
        }

        private void CreateRoot()
        {
            Result<UIController> result = UIRootCreator.Create();

            if (result.IsFailure)
            {
                SetStatus(result.Error.Message, MessageType.Warning);
                return;
            }

            SetStatus("CDG UI Root를 생성했습니다.", MessageType.Info);
        }

        private void CreateRegistry()
        {
            string defaultName = Path.GetFileNameWithoutExtension(UIEditorConstants.DefaultRegistryAssetName);

            string assetPath = EditorUtility.SaveFilePanelInProject(
                "Create UI Registry",
                defaultName,
                "asset",
                "UIRegistry Asset을 생성할 위치를 선택하세요.");

            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            Result<UIRegistry> result = UIRegistryAssetCreator.Create(assetPath);

            if (result.IsFailure)
            {
                SetStatus(result.Error.Message, MessageType.Warning);
                return;
            }

            SetStatus($"UIRegistry를 생성했습니다: {assetPath}", MessageType.Info);
        }

        private void CreateViewPrefab()
        {
            UIId id = new UIId(viewId);

            if (id.IsEmpty)
            {
                SetStatus("View Prefab의 UI ID를 입력하세요.", MessageType.Warning);
                return;
            }

            string assetPath = EditorUtility.SaveFilePanelInProject(
                "Create View Prefab",
                GetDefaultViewPrefabName(),
                "prefab",
                "View Prefab을 생성할 위치를 선택하세요.");

            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            bool blocksInput = GetBlocksInput();

            Result<UIView> result = UIViewPrefabCreationService.Create(
                assetPath,
                viewPrefabType,
                id,
                viewRegistry,
                blocksInput,
                overlayLayer);

            if (result.IsFailure)
            {
                SetStatus(result.Error.Message, MessageType.Warning);
                return;
            }

            string registryMessage = viewRegistry != null
                ? $" Registry '{viewRegistry.name}'에도 등록했습니다."
                : string.Empty;

            SetStatus($"View Prefab을 생성했습니다: {assetPath}.{registryMessage}", MessageType.Info);
        }

        private bool GetBlocksInput()
        {
            switch (viewPrefabType)
            {
                case UIViewPrefabType.Popup:
                    return popupBlocksInput;

                case UIViewPrefabType.Overlay:
                    return overlayBlocksInput;

                default:
                    return false;
            }
        }

        private string GetDefaultViewPrefabName()
        {
            switch (viewPrefabType)
            {
                case UIViewPrefabType.Screen:
                    return "UIScreen";

                case UIViewPrefabType.Popup:
                    return "UIPopup";

                case UIViewPrefabType.Overlay:
                    return "UIOverlay";

                default:
                    return "UIView";
            }
        }

        private void SetStatus(string message, MessageType messageType)
        {
            statusMessage = message;
            statusMessageType = messageType;
            Repaint();
        }
    }
}