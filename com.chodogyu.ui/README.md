# ChoDogyu UI Framework & Editor Tool

Unity 프로젝트에서 Screen, Popup, Overlay의 UI 흐름을 일관된 방식으로 관리하고, UI 제작과 검증을 지원하기 위한 범용 UI Framework & Editor Tool입니다.

게임별 UI 내용과 비즈니스 로직은 사용하는 프로젝트가 담당하고, Framework는 UI 식별, 생성, 캐시, 화면 전환, Popup Stack, Overlay 계층, Back 처리, Lifecycle, Input Blocking 및 Transition 흐름을 관리합니다.

Runtime 기능과 Editor 제작 도구를 함께 제공하며 특정 게임이나 장르에 종속되지 않는 독립적인 Unity Package Manager 패키지로 구성했습니다.

---

## 주요 기능

### Runtime

- `UIScreen`, `UIPopup`, `UIOverlay` 기반 UI 역할 분리
- `UIId` 기반 UI 식별
- `UIRegistry` 기반 Prefab 등록 및 조회
- Registry Prefab Lazy Instantiate
- 생성된 Runtime Instance 캐시 및 재사용
- Screen Navigation
  - Push
  - Replace
  - Reset
- Screen History 및 Back 복원
- Popup Stack
- Normal / Topmost Overlay 계층
- Popup 및 Overlay Input Blocking
- 일관된 Back 처리 우선순위
- `Closed → Opening → Open → Closing → Closed` Lifecycle
- `CanvasGroup` 기반 입력 활성화 관리
- Transition 추상화
- 기본 `UIFadeTransition`
- 사용자 정의 Transition 확장
- Result 기반 오류 처리
- Runtime UI 상태 조회

### Editor

- UI Root 자동 생성
- UI Registry Asset 생성
- Screen / Popup / Overlay Prefab 생성
- 생성 Prefab의 Registry 자동 등록
- UI 구성 Validation
- 안전하게 자동 수정 가능한 항목에 대한 Safe Fix
- 통합 UI Framework Editor Window

---

## 요구 사항

- Unity 6.3 이상
- ChoDogyu Core 1.0.0
- Unity UI (`com.unity.ugui`) 2.0.0

개발 및 검증 환경:

```text
Unity 6.3 LTS
6000.3.9f1
```

UI Framework는 `CDG.Core.Results`의 `Result`, `Result<T>`, `ResultError`를 사용하므로 ChoDogyu Core가 필요합니다.

Unity UI는 이 패키지의 `package.json` 의존성으로 선언되어 있어 UI Framework 설치 시 Unity Package Manager가 함께 Resolve합니다.

다음 CDG 패키지는 필수 의존성이 아닙니다.

```text
ChoDogyu Data Framework
ChoDogyu Save / Load Framework
ChoDogyu Object Pooling
ChoDogyu General Editor Tools
```

---

## 설치

ChoDogyu Core를 먼저 설치한 뒤 UI Framework를 설치합니다.

### 1. ChoDogyu Core 설치

```text
https://github.com/ChoDoGyu/ChoDogyuCore.git?path=/com.chodogyu.core#v1.0.0
```

### 2. ChoDogyu UI Framework & Editor Tool 설치

```text
https://github.com/ChoDoGyu/ChoDogyuUI.git?path=/com.chodogyu.ui#v1.0.0
```

Unity에서:

```text
Window
→ Package Management
→ Package Manager
→ Install package from git URL...
```

Core를 먼저 설치한 뒤 UI Framework를 설치합니다.

---

## 기본 구조

Runtime의 중심 구조:

```text
UIController
├─ UIInstanceStore
├─ UIScreenNavigator
├─ UIPopupStack
├─ UIOverlayOrder
├─ UIInputCoordinator
└─ UIViewLifecycleCoordinator
      └─ UIViewTransition
          └─ UIFadeTransition
```

외부에서는 내부 관리 객체를 직접 조작하지 않고 `UIController`를 UI 흐름의 진입점으로 사용합니다.

---

## UI Root

Editor Tool을 사용하면 기본 UI Root를 생성할 수 있습니다.

```text
CDG UI Root
├─ Screen Layer
├─ Overlay Layer
├─ Popup Layer
└─ Top Overlay Layer
```

`CDG UI Root`에는 다음 컴포넌트가 구성됩니다.

```text
Canvas
GraphicRaycaster
UIController
```

Framework는 EventSystem을 자동으로 생성하지 않습니다.

입력 시스템과 EventSystem 구성은 사용하는 프로젝트가 결정합니다.

---

## Editor Tool

상단 메뉴에서 Editor Window를 열 수 있습니다.

```text
Tools
→ ChoDogyu
→ UI Framework
→ Open Window
```

Editor Window에서는 다음 기능을 제공합니다.

```text
Create UI Root
Create UI Registry
Create View Prefab
Validation
Safe Fix
```

---

## UI View 종류

모든 Runtime UI는 `UIView`를 기반으로 합니다.

```text
UIView
├─ UIScreen
├─ UIPopup
└─ UIOverlay
```

각 View는 서로 다른 UI 흐름의 책임을 가집니다.

---

## UIScreen

`UIScreen`은 메인 화면 흐름을 표현합니다.

예:

```text
Main Menu
Inventory
Character
Settings Screen
Result Screen
```

동시에 하나의 Screen이 현재 Screen으로 관리됩니다.

Screen을 열 때 다음 세 가지 방식을 사용할 수 있습니다.

```text
Push
Replace
Reset
```

---

## Screen Push

현재 Screen을 History에 보존하고 새로운 Screen을 엽니다.

```text
Home
↓ Push
Details
```

상태:

```text
Current = Details
History = Home
```

이후 `Back()`을 호출하면 이전 Home Screen이 복원됩니다.

사용 예:

```csharp
using CDG.Core.Results;
using CDG.UI;

UIId detailsId = new UIId("screen.details");

Result<UIScreen> result = uiController.OpenScreen(
    detailsId,
    UIScreenOpenMode.Push);
```

`OpenScreen(UIId)`의 기본 동작 역시 Push입니다.

---

## Screen Replace

현재 Screen을 History에 추가하지 않고 새로운 Screen으로 교체합니다.

기존 History는 유지됩니다.

```text
History
A

Current
B

B → Replace → C

History
A

Current
C
```

사용 예:

```csharp
Result<UIScreen> result = uiController.OpenScreen(
    new UIId("screen.result"),
    UIScreenOpenMode.Replace);
```

---

## Screen Reset

기존 Screen History를 모두 제거하고 새로운 Screen을 엽니다.

```text
A
↓ Push
B
↓ Push
C

C
↓ Reset Home

Current = Home
History = Empty
```

사용 예:

```csharp
Result<UIScreen> result = uiController.OpenScreen(
    new UIId("screen.home"),
    UIScreenOpenMode.Reset);
```

로그인 완료, 메인 화면 복귀 또는 전체 UI 흐름 초기화처럼 이전 화면으로 돌아갈 필요가 없는 상황에 사용할 수 있습니다.

---

## UIPopup

`UIPopup`은 현재 Screen 위에 일시적으로 표시되는 UI입니다.

여러 Popup을 열 수 있으며 열린 순서대로 Stack으로 관리합니다.

```text
Popup A
↓
Popup B
↓
Popup C
```

현재 최상단:

```text
Popup C
```

`Back()`을 호출하면 가장 위의 Popup부터 닫힙니다.

사용 예:

```csharp
UIId popupId = new UIId("popup.settings");

Result<UIPopup> result = uiController.OpenPopup(popupId);
```

Popup의 `BlocksInput` 기본값은 `true`입니다.

따라서 일반적인 Modal Popup은 별도 설정 없이 자신보다 아래 UI의 입력을 차단할 수 있습니다.

---

## UIOverlay

`UIOverlay`는 Screen History와 Popup Stack에 포함되지 않고 독립적으로 표시되는 UI입니다.

예:

```text
Loading
Network Blocking
Tutorial Highlight
Global Notice
Screen Effect
```

Overlay는 두 계층 중 하나에 배치됩니다.

```text
Normal
Topmost
```

### Normal

```text
Popup
↑
Normal Overlay
↑
Screen
```

Screen보다 위, Popup보다 아래에 표시됩니다.

### Topmost

```text
Topmost Overlay
↑
Popup
↑
Normal Overlay
↑
Screen
```

Popup보다도 위에 표시됩니다.

사용 예:

```csharp
UIId loadingId = new UIId("overlay.loading");

Result<UIOverlay> result = uiController.OpenOverlay(loadingId);
```

Overlay의 `BlocksInput` 기본값은 `false`입니다.

Loading 화면처럼 하위 UI 조작을 막아야 하는 Overlay는 Prefab에서 `Blocks Input`을 활성화할 수 있습니다.

---

## 표시 계층

Framework의 기본 표시 순서는 다음과 같습니다.

```text
높음

Top Overlay Layer
Popup Layer
Overlay Layer
Screen Layer

낮음
```

즉:

```text
Topmost Overlay
> Popup
> Normal Overlay
> Screen
```

순서입니다.

---

## UIId

모든 View는 `UIId`를 통해 식별됩니다.

예:

```text
screen.home
screen.inventory
popup.settings
overlay.loading
```

Runtime 코드에서는 다음과 같이 사용할 수 있습니다.

```csharp
UIId homeId = new UIId("screen.home");
```

빈 ID는 유효하지 않습니다.

Registry 내부에서도 각 View Prefab은 고유한 UIId를 가져야 합니다.

---

## UIRegistry

`UIRegistry`는 Runtime에서 사용할 UIView Prefab을 보관하는 `ScriptableObject`입니다.

```text
UIRegistry
├─ HomeScreen
├─ InventoryScreen
├─ SettingsPopup
└─ LoadingOverlay
```

각 Prefab의 `UIId`는 Registry 안에서 중복될 수 없습니다.

Runtime에서는 다음과 같은 조회 API를 사용할 수 있습니다.

```csharp
bool contains = registry.Contains(id);

bool found = registry.TryGet(id, out UIView prefab);

Result<UIView> result = registry.Get(id);
```

Editor Tool로 View Prefab을 생성하면 지정한 Registry에 자동으로 등록할 수 있습니다.

---

## Lazy Instantiate와 Cache

Registry에 등록된 모든 UI를 시작할 때 미리 생성하지 않습니다.

처음 필요한 시점에:

```text
UIId 요청
↓
Registry Prefab 조회
↓
Instantiate
↓
적절한 Layer에 배치
↓
Runtime Cache 저장
```

과정을 수행합니다.

이후 같은 UIId를 다시 요청하면 기존 Runtime Instance를 재사용합니다.

UI를 닫아도 기본적으로 Destroy하지 않고 비활성 상태로 유지합니다.

```text
첫 Open
→ Instantiate

Close
→ 비활성화

두 번째 Open
→ 기존 Instance 재사용
```

동일한 UIId는 하나의 Cached Instance로 관리됩니다.

---

## GetOrCreate

UI를 직접 열지 않고 Runtime Instance를 가져와야 하는 경우:

```csharp
Result<UIScreen> result =
    uiController.GetOrCreate<UIScreen>(
        new UIId("screen.home"));
```

아직 생성되지 않았다면 Registry의 Prefab으로 생성하고, 이미 생성되어 있다면 Cached Instance를 반환합니다.

등록된 View 타입과 요청한 타입이 다르면 실패 Result를 반환합니다.

---

## Close

지정한 UI를 닫습니다.

```csharp
Result result = uiController.Close(
    new UIId("popup.settings"));
```

Transition이 존재하면 Closing Transition이 시작되고, 완료된 후 GameObject가 비활성화됩니다.

Runtime Instance 자체는 Cache에 유지됩니다.

---

## UI 상태 확인

특정 UI가 완전히 열린 상태인지 확인:

```csharp
bool isOpen = uiController.IsOpen(
    new UIId("popup.settings"));
```

`Opening` 상태는 `false`입니다.

Controller에서도 현재 UI 흐름을 조회할 수 있습니다.

```text
CurrentScreen
ScreenHistoryCount
TopPopup
PopupCount
CanBack
IsBusy
```

---

## Lifecycle

UIView의 기본 상태 흐름:

```text
Closed
↓
Opening
↓
Open
↓
Closing
↓
Closed
```

View를 상속하여 Lifecycle Hook을 사용할 수 있습니다.

```csharp
using CDG.UI;

public sealed class InventoryScreen : UIScreen
{
    protected override void OnOpening()
    {
        // 화면이 열리기 전에 데이터를 준비합니다.
    }

    protected override void OnOpened()
    {
        // 화면 열기가 완료된 후 처리합니다.
    }

    protected override void OnClosing()
    {
        // 닫힘 Transition 전에 처리합니다.
    }

    protected override void OnClosed()
    {
        // 완전히 닫힌 후 처리합니다.
    }
}
```

`OnOpening()`은 GameObject가 활성화되기 전에 호출됩니다.

`OnOpened()`는 Opening Transition까지 완료된 후 호출됩니다.

`OnClosing()`은 Closing Transition 전에 호출됩니다.

`OnClosed()`는 GameObject가 비활성화되고 Closed 상태가 된 후 호출됩니다.

---

## CanvasGroup과 입력 상태

UIView Prefab에는 `CanvasGroup`이 필요합니다.

Framework는 Lifecycle에 따라 View의 입력 상태를 관리합니다.

```text
Opening
Interactable = false
Blocks Raycasts = false

Open
Interactable = true
Blocks Raycasts = true

Closing
Interactable = false
Blocks Raycasts = false
```

Popup과 Overlay의 Blocking 설정은 이 View 상태와 함께 전체 UI 입력 우선순위를 계산하는 데 사용됩니다.

---

## Back

외부에서는:

```csharp
Result result = uiController.Back();
```

을 호출하여 현재 UI 흐름에 맞는 Back 처리를 요청합니다.

Framework는 키보드, 게임패드, Android Back Button 등을 직접 감시하지 않습니다.

실제 입력 감지는 사용하는 프로젝트가 담당하고, 감지 후 `UIController.Back()`을 호출합니다.

Back 처리 우선순위:

```text
1. Blocking Topmost Overlay
   → Back 차단

2. 최상단 Popup
   → Popup 닫기

3. Blocking Normal Overlay
   → Screen History로의 Back 전달 차단

4. Screen History
   → 이전 Screen 복원

5. 대상 없음
   → NoBackTarget
```

Blocking Overlay는 Back으로 자동으로 닫히지 않습니다.

필요한 시점에 명시적으로 `Close()`해야 합니다.

Framework는 Back 요청으로 Application을 종료하지 않습니다.

---

## Transition

View에 `UIViewTransition` 구현을 추가하면 Opening / Closing 과정에 시각 효과를 적용할 수 있습니다.

Transition이 없는 View는 즉시 상태 전환을 완료합니다.

기본 제공 Transition:

```text
UIFadeTransition
```

기본값:

```text
Opening Duration = 0.2초
Closing Duration = 0.2초
```

Fade는 `Time.unscaledDeltaTime`을 사용하므로 `Time.timeScale`의 영향을 받지 않습니다.

---

## 사용자 정의 Transition

사용자 정의 효과가 필요한 경우 `UIViewTransition`을 상속할 수 있습니다.

```csharp
using System.Collections;
using CDG.UI;
using UnityEngine;

public sealed class CustomTransition : UIViewTransition
{
    protected override IEnumerator OnPlayOpening(CanvasGroup canvasGroup)
    {
        yield break;
    }

    protected override IEnumerator OnPlayClosing(CanvasGroup canvasGroup)
    {
        yield break;
    }
}
```

Transition 구현은 Framework가 전달하는 `CanvasGroup`에 시각 효과를 적용합니다.

View의 입력 활성화 여부는 Framework가 관리하므로 사용자 Transition에서 직접 변경하지 않는 것을 권장합니다.

---

## 중복 요청

동일한 UI에 잘못된 Lifecycle 요청을 보내도 Framework가 상태를 구분하여 실패 Result를 반환합니다.

```text
Open 상태에서 다시 Open
→ UI_ALREADY_OPEN

Closed 상태에서 다시 Close
→ UI_ALREADY_CLOSED

Opening / Closing 중 새 요청
→ UI_BUSY
```

명령을 자동으로 Queue하지 않습니다.

호출 측에서 Result를 확인하고 필요한 흐름을 결정합니다.

---

## 오류 처리

Runtime Public API는 ChoDogyu Core의 Result 타입을 사용합니다.

예:

```csharp
Result<UIPopup> result = uiController.OpenPopup(
    new UIId("popup.settings"));

if (result.IsFailure)
{
    Debug.LogWarning(
        $"{result.Error.Code}: {result.Error.Message}");

    return;
}

UIPopup popup = result.Value;
```

오류 메시지는 사람이 확인하기 위한 설명이며, 코드에서는 안정적인 `Error.Code`를 기준으로 분기할 수 있습니다.

---

## Runtime Error Codes

```text
UI_INVALID_ID
UI_NOT_FOUND
UI_INVALID_REGISTRY
UI_DUPLICATE_ID
UI_MISSING_REGISTRY
UI_MISSING_LAYER
UI_INVALID_TYPE
UI_MISSING_CANVAS_GROUP
UI_ALREADY_OPEN
UI_ALREADY_CLOSED
UI_BUSY
UI_BACK_BLOCKED
UI_NO_BACK_TARGET
```

### UI_INVALID_ID

UIId가 비어 있거나 유효하지 않은 경우입니다.

### UI_NOT_FOUND

요청한 UIId가 Registry에 등록되어 있지 않은 경우입니다.

### UI_INVALID_REGISTRY

Registry에 null Prefab 등 잘못된 항목이 포함된 경우입니다.

### UI_DUPLICATE_ID

Registry에 같은 UIId가 두 번 이상 등록된 경우입니다.

### UI_MISSING_REGISTRY

UIController에 Registry가 지정되지 않은 경우입니다.

### UI_MISSING_LAYER

Runtime Instance를 배치할 UI Layer가 지정되지 않은 경우입니다.

### UI_INVALID_TYPE

등록된 View 타입과 요청한 View 타입이 호환되지 않는 경우입니다.

### UI_MISSING_CANVAS_GROUP

Lifecycle과 입력 관리에 필요한 CanvasGroup이 없는 경우입니다.

### UI_ALREADY_OPEN

이미 열린 UI에 다시 Open을 요청한 경우입니다.

### UI_ALREADY_CLOSED

이미 닫힌 UI에 다시 Close를 요청한 경우입니다.

### UI_BUSY

View가 Opening 또는 Closing 상태이거나 Screen Navigation이 진행 중인 경우입니다.

### UI_BACK_BLOCKED

Blocking Overlay가 Back 전달을 차단하고 있는 경우입니다.

### UI_NO_BACK_TARGET

닫을 Popup이나 복원할 Screen History가 없는 경우입니다.

---

## Validation

Editor Tool은 UI 구성 오류를 실행 전에 확인할 수 있도록 Validation 기능을 제공합니다.

주요 검사 대상:

```text
빈 UIId
중복 UIId
Registry의 Missing Prefab
CanvasGroup 누락
View Prefab Root 활성 상태
UIController Registry 미지정
UI Layer 누락
중복 Layer
잘못된 Overlay Layer
Transition 설정
View 타입 및 Registry 구성 불일치
```

---

## Safe Fix

자동 수정 결과가 명확한 일부 Validation Issue에는 Safe Fix를 제공합니다.

예:

```text
CanvasGroup 추가
View Prefab Root 비활성화
잘못된 Fade Duration 보정
```

ID 결정, Layer 선택처럼 사용자의 의도가 필요한 항목은 임의로 자동 수정하지 않습니다.

---

## View Prefab 생성

Editor Tool에서 다음 View Prefab을 생성할 수 있습니다.

```text
Screen
Popup
Overlay
```

생성되는 View Prefab은 기본적으로:

```text
RectTransform
CanvasGroup
선택한 UIView 타입
```

을 가지며 Root는 비활성 상태로 생성됩니다.

Popup은 Input Blocking 여부를 설정할 수 있습니다.

Overlay는 다음을 설정할 수 있습니다.

```text
Layer
- Normal
- Topmost

Blocks Input
- true
- false
```

Registry를 지정하면 생성 성공 후 Prefab을 자동 등록합니다.

---

## Basic Usage Sample

Package Manager에서 `Basic Usage` Sample을 Import할 수 있습니다.

Sample에서 다음 기본 흐름을 확인할 수 있습니다.

```text
Home Screen
↓ Push
Details Screen
↓ Back
Home Screen

Settings Popup
↓ Back
Popup Close

Loading Topmost Overlay
↓
명시적 Close

Fade Transition
Runtime Instance Cache
Screen History
Popup Stack
Back
Input Blocking
```

Sample은 Framework 사용 흐름을 보여주기 위한 최소 예제이며 게임별 로직을 포함하지 않습니다.

---

## 책임 범위

UI Framework가 담당하는 범위:

```text
UI 식별
UI Registry
Runtime Instance 생성
Runtime Instance Cache
Screen Navigation
Screen History
Popup Stack
Overlay Layer
Input Blocking
Back 처리
View Lifecycle
Transition
UI 상태 조회
Result 기반 오류 처리
UI Root 생성
Registry 생성
View Prefab 생성
Validation
Safe Fix
```

담당하지 않는 범위:

```text
게임 비즈니스 로직
게임별 UI 데이터
Addressables
Resources 자동 탐색
비동기 Asset Loading
UI Pooling
동일 UIId Multi Instance
숫자 기반 Popup Priority
Command Queue
Transition 강제 Cancel
Animator Transition Framework
DOTween 의존
Slide / Scale 기본 Transition
MVVM
Data Binding
Reflection 기반 Binding
Code Generation
UI Toolkit
Runtime Singleton
Service Locator
DontDestroyOnLoad 정책
Scene 전환 관리
Input System 직접 감시
키보드 / 게임패드 Back 직접 감시
Application 종료
TMP 의존
임의 SortingOrder 관리
```

---

## 테스트

Unity Test Framework 기반으로 Runtime 및 Editor 기능을 검증했습니다.

```text
Runtime
220 Passed

Editor
67 Passed

Failed
0
```

주요 Runtime 검증 범위:

```text
UIId
UIRegistry
Runtime Instance 생성 및 Cache
Screen Push / Replace / Reset
Screen History
Popup Stack
Overlay
Input Blocking
Back
Lifecycle
Transition
Fade
Error Result
```

주요 Editor 검증 범위:

```text
UI Root 생성
Registry 생성
View Prefab 생성
Registry 자동 등록
Validation
Safe Fix
Editor Window 동작
```

---

## UPM 독립 설치 검증

완전히 새로운 Unity 6.3 프로젝트에서 다음 흐름을 검증했습니다.

```text
ChoDogyu Core v1.0.0 Git 설치
→ 성공

ChoDogyu UI Framework Git 설치
→ 성공

Unity UI Dependency Resolve
→ 성공

Compile
→ 성공

UI Framework Editor Window
→ 정상

UI Root 생성
→ 성공

UI Registry 생성
→ 성공

UIScreen Prefab 생성
→ 성공

Registry 자동 등록
→ 성공

Registry Validation
→ 0 Error / 0 Warning

Controller Validation
→ 0 Error / 0 Warning

Basic Usage Sample Import
→ 성공

Sample Scene 실행
→ 성공

Screen Navigation
→ 정상

Popup
→ 정상

Overlay
→ 정상

Back
→ 정상
```

개발 프로젝트 또는 로컬 패키지 경로에 의존하지 않는 Git UPM 설치 및 사용 흐름을 확인했습니다.

---

## 설계 원칙

UI Framework는 UI 흐름을 관리하지만 게임의 상태와 규칙을 소유하지 않습니다.

예:

```text
Quest 완료 여부
아이템 보유 여부
플레이어 레벨
설정 값
네트워크 상태
```

이러한 정보는 게임 시스템이 관리합니다.

UI는 해당 상태를 전달받아 표현하고, Framework는 View의 생성과 화면 흐름을 관리합니다.

또한 Runtime Singleton이나 Service Locator를 강제하지 않습니다.

`UIController`를 어디에 배치하고 어떻게 참조할지는 사용하는 프로젝트가 결정합니다.

---

## 버전

현재 패키지 버전:

```text
v1.0.0
```

주요 변경 사항:

```text
com.chodogyu.ui/CHANGELOG.md
```

패키지 사용법:

```text
com.chodogyu.ui/README.md
```

세부 설계 및 사용 규칙:

```text
com.chodogyu.ui/Documentation~/index.md
```