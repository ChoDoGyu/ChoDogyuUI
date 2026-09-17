# ChoDogyu UI Framework & Editor Tool

Unity 프로젝트에서 Screen, Popup, Overlay의 UI 흐름을 일관된 방식으로 관리하고, UI 제작과 검증을 지원하기 위한 범용 UI Framework & Editor Tool입니다.

Runtime에서는 UI 식별, Registry, Instance Cache, Screen Navigation, Popup Stack, Overlay, Back, Input Blocking, Lifecycle, Transition을 관리합니다.

Editor에서는 UI Root, Registry, View Prefab 생성과 Validation 및 Safe Fix 기능을 제공합니다.

특정 게임이나 장르에 종속되지 않으며 Unity Package Manager를 통해 독립적으로 설치할 수 있도록 구성했습니다.

---

## 주요 기능

### Runtime

- `UIScreen`, `UIPopup`, `UIOverlay` 기반 역할 분리
- `UIId` 기반 UI 식별
- `UIRegistry` 기반 Prefab 관리
- Runtime Instance Lazy Instantiate
- Runtime Instance Cache 및 재사용
- Screen Navigation
  - Push
  - Replace
  - Reset
- Screen History
- Popup Stack
- Normal / Topmost Overlay
- Input Blocking
- 일관된 Back 처리
- 명시적인 UIView Lifecycle
- Transition 추상화
- 기본 Fade Transition
- Result 기반 오류 처리
- UI 상태 조회

### Editor

- UI Root 생성
- UI Registry Asset 생성
- Screen / Popup / Overlay Prefab 생성
- Registry 자동 등록
- UI 구성 Validation
- Safe Fix
- 통합 UI Framework Editor Window

---

## 핵심 구조

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

외부에서는 `UIController`를 UI 흐름의 중심 진입점으로 사용합니다.

Framework 내부의 Navigation, Stack, Input, Lifecycle 책임은 각각 분리하여 관리합니다.

---

## View 구조

```text
UIView
├─ UIScreen
├─ UIPopup
└─ UIOverlay
```

### UIScreen

주요 화면 흐름을 담당합니다.

```text
Home
Inventory
Character
Result
```

Screen Navigation:

```text
Push
Replace
Reset
```

### UIPopup

현재 화면 위에 표시되는 Popup을 Stack으로 관리합니다.

```text
Popup A
↓
Popup B
↓
Popup C
```

Back 요청 시 가장 위의 Popup부터 닫습니다.

### UIOverlay

Screen History와 Popup Stack에 포함되지 않는 독립 UI입니다.

```text
Normal Overlay
Topmost Overlay
```

Loading, Network Blocking, Tutorial Overlay 같은 UI에 사용할 수 있습니다.

---

## UI 표시 순서

```text
높음

Topmost Overlay
Popup
Normal Overlay
Screen

낮음
```

실제 Root 구조:

```text
CDG UI Root
├─ Screen Layer
├─ Overlay Layer
├─ Popup Layer
└─ Top Overlay Layer
```

---

## Screen Navigation

### Push

현재 Screen을 History에 저장하고 새로운 Screen을 엽니다.

```text
Home
↓ Push
Details

Current = Details
History = Home
```

Back:

```text
Details
↓ Back
Home
```

### Replace

현재 Screen을 History에 추가하지 않고 교체합니다.

기존 History는 유지합니다.

### Reset

기존 Screen History를 모두 제거하고 새로운 Screen을 기준 화면으로 설정합니다.

```text
A
↓ Push
B
↓ Push
C
↓ Reset Home

Current = Home
History = Empty
```

---

## Popup Stack

Popup은 열린 순서대로 Stack에 등록됩니다.

```text
Top
Popup C
Popup B
Popup A
Bottom
```

Back 우선순위에서 Popup은 Screen History보다 먼저 처리됩니다.

---

## Overlay

Overlay는 다음 두 Layer를 지원합니다.

```text
Normal
Topmost
```

Normal Overlay:

```text
Popup
Normal Overlay
Screen
```

Topmost Overlay:

```text
Topmost Overlay
Popup
Normal Overlay
Screen
```

Overlay는 Screen History에 들어가지 않으며 기본적으로 Back 대상도 아닙니다.

필요한 시점에 명시적으로 `Close()`합니다.

---

## Input Blocking

입력 가능 여부는 다음 순서로 계산합니다.

```text
1. Topmost Overlay
2. Popup
3. Normal Overlay
4. Screen
```

상위 UI가 `BlocksInput`을 사용하면 그 아래 UI의 입력을 차단합니다.

기본값:

```text
UIPopup
BlocksInput = true

UIOverlay
BlocksInput = false
```

Opening 또는 Closing 상태의 View는 입력할 수 없습니다.

---

## Back 처리

Back 입력 자체는 Framework가 감지하지 않습니다.

게임이 입력을 감지한 뒤:

```csharp
uiController.Back();
```

을 호출합니다.

처리 순서:

```text
1. Blocking Topmost Overlay
   → Back 차단

2. Top Popup
   → Popup Close

3. Blocking Normal Overlay
   → Back 차단

4. Screen History
   → 이전 Screen 복원

5. 대상 없음
   → NoBackTarget
```

Framework는 Back 요청으로 Application을 종료하지 않습니다.

---

## Lifecycle

UIView의 상태:

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

사용 가능한 Lifecycle Hook:

```text
OnOpening
OnOpened
OnClosing
OnClosed
```

Opening과 Closing 동안 입력은 비활성화됩니다.

---

## Transition

Transition 기반 클래스:

```text
UIViewTransition
```

기본 제공 구현:

```text
UIFadeTransition
```

기본 Fade 시간:

```text
Opening Duration = 0.2초
Closing Duration = 0.2초
```

`Time.unscaledDeltaTime`을 사용하므로 `Time.timeScale = 0` 상태에서도 동작합니다.

사용자 정의 Transition은 `UIViewTransition`을 상속하여 구현할 수 있습니다.

---

## Runtime Instance Cache

Registry에 등록된 모든 UI를 시작 시 생성하지 않습니다.

```text
최초 요청
↓
Registry 조회
↓
Instantiate
↓
Cache
```

Close:

```text
GameObject 비활성화
```

재사용:

```text
다음 Open
↓
기존 Cached Instance 사용
```

동일한 UIId는 하나의 Runtime Instance로 관리합니다.

---

## Editor Tool

메뉴:

```text
Tools
→ ChoDogyu
→ UI Framework
→ Open Window
```

제공 기능:

```text
Create UI Root
Create UI Registry
Create View Prefab
Validation
Safe Fix
```

---

## Validation

Editor 단계에서 UI 구성 오류를 검사할 수 있습니다.

대표 검사:

```text
빈 UIId
중복 UIId
Missing Prefab
CanvasGroup 누락
Prefab Root 활성 상태
UIController Registry 미지정
Layer 누락
중복 Layer
Overlay 구성
Transition 설정
View 타입 및 Registry 구성
```

---

## Safe Fix

결과가 명확한 일부 문제는 자동 수정할 수 있습니다.

```text
CanvasGroup 추가
Prefab Root 비활성화
음수 Fade Duration 보정
```

반대로 다음처럼 사용자 의도가 필요한 항목은 임의로 변경하지 않습니다.

```text
UIId 결정
중복 ID 변경
Registry 선택
Layer 선택
Overlay Layer 선택
```

---

## 저장소 구조

```text
ChoDogyuUI/
├─ UIDevelopment/
│  └─ 패키지 개발 및 검증용 Unity 프로젝트
│
├─ com.chodogyu.ui/
│  ├─ Runtime/
│  ├─ Editor/
│  ├─ Tests/
│  │  ├─ Runtime/
│  │  └─ Editor/
│  ├─ Samples~/
│  │  └─ BasicUsage/
│  ├─ Documentation~/
│  │  └─ index.md
│  ├─ package.json
│  ├─ README.md
│  └─ CHANGELOG.md
│
├─ .gitattributes
├─ .gitignore
└─ README.md
```

### UIDevelopment

UI Framework의 개발, 테스트 및 통합 검증에 사용하는 Unity 프로젝트입니다.

실제 UPM 배포 대상에는 포함되지 않습니다.

### com.chodogyu.ui

실제로 사용하는 Unity Package Manager 패키지입니다.

다른 Unity 프로젝트에서는 이 폴더를 Git UPM 패키지로 설치할 수 있습니다.

---

## 요구 사항

- Unity 6.3 이상
- ChoDogyu Core 1.0.0
- Unity UI 2.0.0

개발 및 검증 환경:

```text
Unity 6.3 LTS
6000.3.9f1
```

Unity UI:

```text
com.unity.ugui
```

는 UI 패키지의 `package.json` 의존성으로 선언되어 있습니다.

ChoDogyu Core는 먼저 설치해야 합니다.

---

## 설치

### 1. ChoDogyu Core 설치

```text
https://github.com/ChoDoGyu/ChoDogyuCore.git?path=/com.chodogyu.core#v1.0.0
```

### 2. ChoDogyu UI Framework & Editor Tool 설치

```text
https://github.com/ChoDoGyu/ChoDogyuUI.git?path=/com.chodogyu.ui#v1.0.0
```

Unity:

```text
Window
→ Package Management
→ Package Manager
→ Install package from git URL...
```

Core를 먼저 설치한 후 UI Framework를 설치합니다.

---

## 기본 사용 흐름

일반적인 프로젝트 구성 순서:

```text
1. ChoDogyu Core 설치
2. ChoDogyu UI Framework 설치
3. UI Framework Editor Window 열기
4. UI Root 생성
5. UI Registry 생성
6. View Prefab 생성
7. Registry 등록
8. Validation
9. 게임 코드에서 UIController 사용
```

---

## 기본 Screen 사용

```csharp
using CDG.Core.Results;
using CDG.UI;

UIId inventoryId = new UIId("screen.inventory");

Result<UIScreen> result = uiController.OpenScreen(
    inventoryId,
    UIScreenOpenMode.Push);
```

---

## 기본 Popup 사용

```csharp
Result<UIPopup> result = uiController.OpenPopup(
    new UIId("popup.settings"));
```

---

## 기본 Overlay 사용

```csharp
Result<UIOverlay> result = uiController.OpenOverlay(
    new UIId("overlay.loading"));
```

Close:

```csharp
Result result = uiController.Close(
    new UIId("overlay.loading"));
```

---

## Basic Usage Sample

Package Manager에서 `Basic Usage` Sample을 Import할 수 있습니다.

Sample 구성:

```text
Basic Usage
├─ Data
│  └─ SampleUIRegistry.asset
├─ Prefabs
│  ├─ HomeScreen.prefab
│  ├─ DetailsScreen.prefab
│  ├─ SettingsPopup.prefab
│  └─ LoadingOverlay.prefab
├─ Scenes
│  └─ BasicUISample.unity
└─ Scripts
   └─ BasicUISampleController.cs
```

Sample에서 확인 가능한 흐름:

```text
Home
↓ Push
Details
↓ Back
Home

Settings Popup
↓ Back
Close

Loading Topmost Overlay
↓
명시적 Close
```

추가로 다음 기능을 확인할 수 있습니다.

```text
Screen History
Popup Stack
Input Blocking
Back
Fade Transition
Runtime Instance Cache
```

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

오류 메시지가 아닌 `Error.Code`를 기준으로 실패 원인을 구분할 수 있습니다.

---

## 책임 범위

Framework가 담당:

```text
UI 식별
UI Registry
Runtime Instance 생성 및 Cache
Screen Navigation
Screen History
Popup Stack
Overlay
Input Blocking
Back
Lifecycle
Transition
Runtime 상태 조회
Result 기반 오류 처리
Editor 제작 Tool
Validation
Safe Fix
```

Framework가 담당하지 않음:

```text
게임 비즈니스 로직
게임별 UI 데이터
Addressables
Async Asset Loading
UI Pooling
동일 UIId Multi Instance
Command Queue
Animator Transition Framework
DOTween
MVVM
Data Binding
UI Toolkit
Runtime Singleton
Service Locator
DontDestroyOnLoad
Scene Loading
Input System 직접 감시
Application 종료
TextMeshPro 의존
```

---

## 테스트

Unity Test Framework 기반으로 검증했습니다.

```text
Runtime
220 Passed

Editor
67 Passed

Failed
0
```

주요 Runtime 검증:

```text
UIId
UIRegistry
Instance 생성 및 Cache
Screen Push / Replace / Reset
Screen History
Popup Stack
Overlay
Input Blocking
Back
Lifecycle
Transition
Fade
오류 처리
```

주요 Editor 검증:

```text
UI Root 생성
Registry 생성
View Prefab 생성
Registry 자동 등록
Validation
Safe Fix
Editor Window
```

---

## UPM 독립 설치 검증

완전히 새로운 Unity 6.3 프로젝트에서 Git UPM 설치 및 실제 사용 흐름을 검증했습니다.

```text
ChoDogyu Core v1.0.0 Git 설치
→ 성공

ChoDogyu UI Framework Git 설치
→ 성공

Unity UI Dependency Resolve
→ 성공

Runtime / Editor Compile
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

개발 프로젝트 또는 로컬 패키지 경로에 의존하지 않는 독립 설치를 확인했습니다.

---

## 다른 CDG 패키지와의 관계

UI Framework는 다음 패키지에 의존하지 않습니다.

```text
ChoDogyu Data Framework
ChoDogyu Save / Load Framework
ChoDogyu Object Pooling
ChoDogyu General Editor Tools
```

필요하다면 게임 프로젝트에서 서로 조합할 수 있습니다.

```text
Core
→ 공통 Result 기반

Data
→ 정적 게임 데이터

Save
→ 플레이 상태 저장

Pooling
→ Runtime Object 재사용

UI
→ 화면 흐름 및 View 관리
```

각 패키지는 독립적인 책임을 유지합니다.

---

## 설계 방향

이 Framework는 가능한 한 다음 원칙을 유지합니다.

```text
명확한 UI 역할 분리
명시적인 상태 전환
예측 가능한 Back 동작
예측 가능한 Input Blocking
중복 Runtime Instance 방지
게임 비즈니스 로직과 UI 흐름 분리
내부 책임 분리
최소한의 패키지 의존성
외부 프레임워크 비강제
Editor 단계 Validation
독립 설치 가능한 UPM 구조
```

---

## 문서

패키지 사용법:

```text
com.chodogyu.ui/README.md
```

상세 설계 및 사용 규칙:

```text
com.chodogyu.ui/Documentation~/index.md
```

버전 변경 사항:

```text
com.chodogyu.ui/CHANGELOG.md
```

---

## 버전

현재 버전:

```text
v1.0.0
```

Package:

```text
com.chodogyu.ui
```

Runtime Assembly:

```text
CDG.UI
```

Editor Assembly:

```text
CDG.UI.Editor
```