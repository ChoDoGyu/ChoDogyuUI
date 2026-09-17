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

        /// <summary>
        /// UI Framework Editor Window를 열거나 기존 Window에 포커스를 이동합니다.
        /// </summary>
        [MenuItem(UIEditorConstants.OpenWindowMenuPath)]
        public static void Open()
        {
            UIFrameworkWindow window = GetWindow<UIFrameworkWindow>();

            window.titleContent = new GUIContent(
                UIEditorConstants.WindowTitle);

            window.minSize = new Vector2(
                MinimumWidth,
                MinimumHeight);

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
        }
    }
}