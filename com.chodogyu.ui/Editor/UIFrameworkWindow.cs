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

            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.Space(12f);
                EditorGUILayout.HelpBox(statusMessage, statusMessageType);
            }
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

        private void SetStatus(string message, MessageType messageType)
        {
            statusMessage = message;
            statusMessageType = messageType;
            Repaint();
        }
    }
}