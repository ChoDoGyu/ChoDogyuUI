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

        private UIValidationTargetType validationTargetType = UIValidationTargetType.Controller;
        private UIController validationController;
        private UIRegistry validationRegistry;
        private UIValidationReport validationReport;

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

            EditorGUILayout.Space(12f);

            DrawValidationSection();

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
                EditorApplication.delayCall += CreateRegistry;
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
                EditorApplication.delayCall += CreateViewPrefab;
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

        private void DrawValidationSection()
        {
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);

            EditorGUILayout.LabelField(
                "UIController 전체 구성 또는 UIRegistry와 등록 View Prefab 구성을 검사합니다.",
                EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space(6f);

            UIValidationTargetType nextTargetType =
                (UIValidationTargetType)EditorGUILayout.EnumPopup(
                    "Target Type",
                    validationTargetType);

            if (nextTargetType != validationTargetType)
            {
                validationTargetType = nextTargetType;
                validationReport = null;
            }

            DrawValidationTargetField();

            EditorGUILayout.Space(6f);

            if (GUILayout.Button("Run Validation"))
            {
                RunValidation();
            }

            if (validationReport != null)
            {
                EditorGUILayout.Space(8f);
                DrawValidationReport();
            }
        }

        private void DrawValidationTargetField()
        {
            switch (validationTargetType)
            {
                case UIValidationTargetType.Controller:
                    {
                        UIController nextController =
                            (UIController)EditorGUILayout.ObjectField(
                                "UI Controller",
                                validationController,
                                typeof(UIController),
                                true);

                        if (nextController != validationController)
                        {
                            validationController = nextController;
                            validationReport = null;
                        }

                        break;
                    }

                case UIValidationTargetType.Registry:
                    {
                        UIRegistry nextRegistry =
                            (UIRegistry)EditorGUILayout.ObjectField(
                                "UI Registry",
                                validationRegistry,
                                typeof(UIRegistry),
                                false);

                        if (nextRegistry != validationRegistry)
                        {
                            validationRegistry = nextRegistry;
                            validationReport = null;
                        }

                        break;
                    }
            }
        }

        private void DrawValidationReport()
        {
            if (validationReport.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "Validation 문제를 발견하지 않았습니다.",
                    MessageType.Info);

                return;
            }

            int errorCount = CountIssues(UIValidationSeverity.Error);
            int warningCount = CountIssues(UIValidationSeverity.Warning);

            EditorGUILayout.LabelField(
                $"Results — Errors: {errorCount}, Warnings: {warningCount}",
                EditorStyles.boldLabel);

            EditorGUILayout.Space(4f);

            for (int i = 0; i < validationReport.Issues.Count; i++)
            {
                DrawValidationIssue(validationReport.Issues[i]);
            }
        }

        private void DrawValidationIssue(UIValidationIssue issue)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            MessageType messageType = issue.Severity == UIValidationSeverity.Error
                ? MessageType.Error
                : MessageType.Warning;

            EditorGUILayout.HelpBox(
                $"{issue.Code}\n{issue.Message}",
                messageType);

            if (issue.Context != null)
            {
                EditorGUI.BeginDisabledGroup(true);

                EditorGUILayout.ObjectField(
                    "Context",
                    issue.Context,
                    typeof(UnityEngine.Object),
                    true);

                EditorGUI.EndDisabledGroup();
            }

            if (issue.Context != null || issue.CanFix)
            {
                EditorGUILayout.BeginHorizontal();

                if (issue.Context != null &&
                    GUILayout.Button("Select Context"))
                {
                    Selection.activeObject = issue.Context;
                    EditorGUIUtility.PingObject(issue.Context);
                }

                if (issue.CanFix &&
                    GUILayout.Button(issue.Fix.Label))
                {
                    ApplyValidationFix(issue);
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
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

        private void RunValidation()
        {
            if (!TryCreateValidationReport(out UIValidationReport report))
            {
                return;
            }

            validationReport = report;

            int errorCount = CountIssues(UIValidationSeverity.Error);
            int warningCount = CountIssues(UIValidationSeverity.Warning);

            SetStatus(
                $"Validation 완료 — Error: {errorCount}, Warning: {warningCount}",
                validationReport.HasErrors
                    ? MessageType.Warning
                    : MessageType.Info);
        }

        private bool TryCreateValidationReport(out UIValidationReport report)
        {
            report = null;

            switch (validationTargetType)
            {
                case UIValidationTargetType.Controller:
                    if (validationController == null)
                    {
                        SetStatus(
                            "Validation할 UIController를 지정하세요.",
                            MessageType.Warning);

                        return false;
                    }

                    report = UIValidationRunner.Validate(validationController);
                    return true;

                case UIValidationTargetType.Registry:
                    if (validationRegistry == null)
                    {
                        SetStatus(
                            "Validation할 UIRegistry를 지정하세요.",
                            MessageType.Warning);

                        return false;
                    }

                    report = UIValidationRunner.Validate(validationRegistry);
                    return true;

                default:
                    SetStatus(
                        "지원하지 않는 Validation Target Type입니다.",
                        MessageType.Warning);

                    return false;
            }
        }

        private void ApplyValidationFix(UIValidationIssue issue)
        {
            if (!issue.CanFix)
            {
                return;
            }

            Result result = issue.Fix.Apply();

            if (result.IsFailure)
            {
                SetStatus(result.Error.Message, MessageType.Warning);
                return;
            }

            if (TryCreateValidationReport(out UIValidationReport refreshedReport))
            {
                validationReport = refreshedReport;
            }

            SetStatus(
                $"Safe Fix를 적용했습니다: {issue.Fix.Label}",
                MessageType.Info);
        }

        private int CountIssues(UIValidationSeverity severity)
        {
            if (validationReport == null)
            {
                return 0;
            }

            int count = 0;

            foreach (UIValidationIssue issue in validationReport.Issues)
            {
                if (issue.Severity == severity)
                {
                    count++;
                }
            }

            return count;
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