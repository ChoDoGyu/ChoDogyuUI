\# ChoDogyu UI Framework \& Editor Tool Documentation



\## 1. 개요



ChoDogyu UI Framework \& Editor Tool은 Unity 프로젝트에서 UI의 생성과 표시 흐름을 일관된 방식으로 관리하기 위한 Runtime Framework와 UI 제작 및 검증을 지원하는 Editor Tool을 함께 제공하는 패키지입니다.



Framework가 관리하는 주요 UI 흐름:



```text

UIRegistry

↓

UIController

↓

Runtime Instance 생성 및 Cache

↓

Screen / Popup / Overlay 관리

↓

Lifecycle

↓

Transition

↓

Input Blocking

↓

Back 처리

```



Framework는 UI 흐름을 관리하지만 게임 상태와 비즈니스 규칙은 소유하지 않습니다.



예:



```text

플레이어 레벨

아이템 보유 여부

퀘스트 진행 상태

네트워크 상태

게임 설정 값

```



이러한 데이터는 사용하는 게임이 관리합니다.



UI Framework는 해당 데이터를 표현하는 View의 생성, 표시 순서, 열기와 닫기, 화면 이동을 관리합니다.



\---



\## 2. 책임 범위



UI Framework가 담당합니다.



```text

UI 식별

UI Registry

UI Prefab 관리

Runtime Instance 생성

Runtime Instance Cache

Screen Navigation

Screen History

Popup Stack

Overlay Layer

Input Blocking

Back 처리

UIView Lifecycle

Transition

Fade Transition

UI 상태 조회

Result 기반 오류 표현

UI Root 생성

UI Registry 생성

View Prefab 생성

Registry 자동 등록

Validation

Safe Fix

```



담당하지 않습니다.



```text

게임 비즈니스 로직

게임 상태 저장

게임별 UI 데이터 정의

Addressables

Resources 자동 탐색

비동기 Asset Loading

UI Pooling

동일 UIId Multi Instance

Popup 숫자 Priority

Command Queue

Transition 강제 Cancel

Animator 기반 Transition Framework

DOTween

Slide 기본 Transition

Scale 기본 Transition

MVVM

Data Binding

Reflection Binding

Code Generation

UI Toolkit

Runtime Singleton

Service Locator

DontDestroyOnLoad 정책

Scene 전환 관리

Input System 감시

키보드 Back 감시

게임패드 Back 감시

Application 종료

TextMeshPro 의존

임의 SortingOrder 관리

```



\---



\## 3. Runtime 기본 구조



전체 Runtime 구조:



```text

UIController

├─ UIInstanceStore

├─ UIScreenNavigator

├─ UIPopupStack

├─ UIOverlayOrder

├─ UIInputCoordinator

└─ UIViewLifecycleCoordinator

&#x20;     └─ UIViewTransition

&#x20;         └─ UIFadeTransition

```



외부 API의 중심은 `UIController`입니다.



내부 관리 클래스는 Framework 구현 세부사항이며 사용하는 프로젝트에서 직접 조작하지 않습니다.



\---



\## 4. UIController



`UIController`는 Runtime UI 흐름의 중심 진입점입니다.



주요 Public API:



```text

OpenScreen

OpenPopup

OpenOverlay

Close

Back

GetOrCreate

IsOpen

```



주요 상태 조회:



```text

Registry

ScreenLayer

OverlayLayer

PopupLayer

TopOverlayLayer



CurrentScreen

ScreenHistoryCount

TopPopup

PopupCount

CanBack

IsBusy

```



`UIController`는 Singleton이 아닙니다.



씬 또는 프로젝트 구조에 맞게 사용하는 프로젝트가 직접 배치하고 참조합니다.



\---



\## 5. UIView 계층



모든 Runtime View는 `UIView`를 기반으로 합니다.



```text

UIView

├─ UIScreen

├─ UIPopup

└─ UIOverlay

```



`UIView`는 다음 공통 정보를 가집니다.



```text

UIId

UIViewState

CanvasGroup

UIViewTransition

Lifecycle Hook

```



\---



\## 6. UIViewState



View의 기본 상태:



```text

Closed

Opening

Open

Closing

```



정상적인 흐름:



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



상태를 건너뛰거나 동시에 두 Lifecycle 요청을 수행하지 않습니다.



\---



\## 7. UIView Lifecycle



Opening의 실제 흐름:



```text

State = Opening

↓

입력 비활성화

↓

OnOpening()

↓

GameObject 활성화

↓

Opening Transition

↓

State = Open

↓

입력 활성화

↓

OnOpened()

```



Closing:



```text

State = Closing

↓

입력 비활성화

↓

OnClosing()

↓

Closing Transition

↓

GameObject 비활성화

↓

State = Closed

↓

OnClosed()

```



Transition이 없는 View는 Transition 구간을 즉시 완료합니다.



\---



\## 8. Lifecycle Hook



UIView를 상속하면 다음 Hook을 사용할 수 있습니다.



```csharp

protected virtual void OnOpening()

protected virtual void OnOpened()

protected virtual void OnClosing()

protected virtual void OnClosed()

```



예:



```csharp

using CDG.UI;



public sealed class InventoryScreen : UIScreen

{

&#x20;   protected override void OnOpening()

&#x20;   {

&#x20;       // UI가 활성화되기 전에 필요한 데이터를 준비합니다.

&#x20;   }



&#x20;   protected override void OnOpened()

&#x20;   {

&#x20;       // Opening Transition까지 완료되었습니다.

&#x20;   }



&#x20;   protected override void OnClosing()

&#x20;   {

&#x20;       // Closing Transition 전에 필요한 처리를 수행합니다.

&#x20;   }



&#x20;   protected override void OnClosed()

&#x20;   {

&#x20;       // UI가 완전히 닫혔습니다.

&#x20;   }

}

```



\### OnOpening



GameObject 활성화 전에 호출됩니다.



다음과 같은 용도에 적합합니다.



```text

텍스트 갱신

목록 데이터 준비

현재 게임 상태 반영

초기 UI 상태 설정

```



\### OnOpened



View가 완전히 Open 상태가 된 후 호출됩니다.



\### OnClosing



Closing Transition을 시작하기 전에 호출됩니다.



\### OnClosed



GameObject가 비활성화되고 Closed 상태가 된 후 호출됩니다.



\---



\## 9. CanvasGroup 요구 사항



모든 Framework View에는 `CanvasGroup`이 필요합니다.



Framework는 CanvasGroup을 통해 다음 값을 관리합니다.



```text

interactable

blocksRaycasts

```



Opening 또는 Closing 중:



```text

interactable = false

blocksRaycasts = false

```



Open 완료:



```text

interactable = true

blocksRaycasts = true

```



CanvasGroup이 없는 View에 Lifecycle 요청을 수행하면:



```text

UI\_MISSING\_CANVAS\_GROUP

```



실패 Result를 반환합니다.



\---



\## 10. UIId



모든 View는 `UIId`로 식별됩니다.



예:



```text

screen.home

screen.inventory

popup.settings

overlay.loading

```



Runtime:



```csharp

UIId id = new UIId("screen.home");

```



빈 값은 유효한 ID가 아닙니다.



\---



\## 11. UIId 사용 원칙



UIId는 UI의 런타임 식별자입니다.



다음과 같이 의미가 드러나는 문자열을 권장합니다.



```text

screen.home

screen.character

screen.inventory



popup.settings

popup.confirm



overlay.loading

overlay.network

```



Framework는 특정 문자열 Naming Convention을 강제하지 않지만 Registry 내에서 ID는 고유해야 합니다.



\---



\## 12. UIRegistry



`UIRegistry`는 Framework가 사용할 View Prefab을 저장하는 `ScriptableObject`입니다.



예:



```text

UIRegistry

├─ HomeScreen

├─ InventoryScreen

├─ SettingsPopup

└─ LoadingOverlay

```



각 Prefab은 자신의 `UIId`를 가집니다.



Registry는 ID를 기준으로 Prefab Lookup을 구성합니다.



\---



\## 13. Registry 규칙



정상 Registry의 조건:



```text

Prefab != null

UIId != Empty

UIId 중복 없음

```



잘못된 항목이 있으면 Registry Lookup 자체가 실패합니다.



대표 오류:



```text

UI\_INVALID\_REGISTRY

UI\_INVALID\_ID

UI\_DUPLICATE\_ID

```



\---



\## 14. Registry Public API



Prefab 수:



```csharp

int count = registry.Count;

```



전체 목록:



```csharp

IReadOnlyList<UIView> prefabs = registry.Prefabs;

```



등록 여부:



```csharp

bool exists = registry.Contains(id);

```



안전한 조회:



```csharp

if (registry.TryGet(id, out UIView prefab))

{

}

```



Result 기반 조회:



```csharp

Result<UIView> result = registry.Get(id);

```



\---



\## 15. Runtime Instance 생성 정책



Registry에 등록했다고 해서 모든 View를 시작 시 생성하지 않습니다.



최초 요청 시:



```text

UIId

↓

Registry Lookup

↓

Prefab 확인

↓

View Type 확인

↓

대상 Layer 확인

↓

Instantiate

↓

Runtime Cache 등록

```



이 방식을 Lazy Instantiate 정책으로 사용합니다.



\---



\## 16. Instance Cache



처음 생성한 Runtime Instance는 Cache에 저장됩니다.



```text

첫 Open

→ Instantiate



Close

→ 비활성화



두 번째 Open

→ Cache Instance 재사용

```



Close 시 기본적으로 Destroy하지 않습니다.



\---



\## 17. 동일 UIId 정책



v1에서는 동일한 UIId에 대해 하나의 Runtime Instance만 관리합니다.



```text

popup.item

→ Instance A

```



같은 ID를 다시 열어:



```text

popup.item

→ Instance B

```



처럼 여러 개 생성하는 Multi Instance 구조는 지원하지 않습니다.



필요한 경우 서로 다른 UIId를 사용하거나 이후 확장 기능으로 구현해야 합니다.



\---



\## 18. 파괴된 Cache 처리



외부 코드에서 Cached GameObject가 파괴된 경우 Framework는 더 이상 유효한 Instance로 사용하지 않습니다.



다음 요청에서 필요하면 Registry Prefab으로 다시 생성할 수 있습니다.



다만 Framework가 Runtime View를 외부에서 임의로 Destroy하는 사용 방식을 권장하지는 않습니다.



\---



\## 19. GetOrCreate



API:



```csharp

Result<T> GetOrCreate<T>(UIId id) where T : UIView

```



예:



```csharp

Result<UIScreen> result =

&#x20;   uiController.GetOrCreate<UIScreen>(

&#x20;       new UIId("screen.inventory"));

```



동작:



```text

Cache 있음

→ 기존 Instance 반환



Cache 없음

→ Registry 조회

→ Instantiate

→ Cache

→ Instance 반환

```



등록 Prefab 타입과 요청 타입이 다르면:



```text

UI\_INVALID\_TYPE

```



을 반환합니다.



\---



\## 20. 표시 Layer



기본 Root:



```text

CDG UI Root

├─ Screen Layer

├─ Overlay Layer

├─ Popup Layer

└─ Top Overlay Layer

```



표시 우선순위:



```text

Top Overlay Layer

Popup Layer

Overlay Layer

Screen Layer

```



즉:



```text

Topmost Overlay

> Popup

> Normal Overlay

> Screen

```



입니다.



\---



\## 21. UIScreen



`UIScreen`은 게임의 주요 화면 단위를 표현합니다.



예:



```text

Home

Inventory

Character

Shop

Result

```



동시에 하나의 Screen을 Current Screen으로 관리합니다.



\---



\## 22. Screen Navigation



Screen Open Mode:



```text

Push

Replace

Reset

```



API:



```csharp

uiController.OpenScreen(id);

```



기본 동작은 Push입니다.



명시적으로 지정:



```csharp

uiController.OpenScreen(

&#x20;   id,

&#x20;   UIScreenOpenMode.Push);

```



\---



\## 23. Push



Push는 현재 Screen을 History에 보관하고 새 Screen을 엽니다.



```text

Current = Home

History = Empty



Home

↓ Push Details



Current = Details

History = Home

```



Back:



```text

Details

↓ Back



Current = Home

History = Empty

```



\---



\## 24. Replace



Replace는 현재 Screen을 History에 추가하지 않습니다.



기존 History는 그대로 유지합니다.



```text

History = A

Current = B



B

↓ Replace C



History = A

Current = C

```



Back:



```text

C

↓ Back



Current = A

```



\---



\## 25. Reset



Reset은 기존 History를 제거하고 새 Screen을 기준점으로 만듭니다.



```text

A

↓ Push

B

↓ Push

C



History = A, B

Current = C

```



Reset:



```text

C

↓ Reset Home



History = Empty

Current = Home

```



\---



\## 26. Screen 전환 순서



현재 Screen이 존재할 때 새로운 Screen을 여는 경우:



```text

현재 Screen Closing 시작

↓

Closing Transition

↓

현재 Screen Closed

↓

대상 Screen Opening 시작

↓

Opening Transition

↓

대상 Screen Open

```



두 Screen을 동시에 완전히 열린 상태로 전환하는 구조가 아닙니다.



\---



\## 27. Screen Navigation Busy



Screen 전환이 진행 중이면 새로운 Screen Navigation 요청을 받지 않습니다.



```text

Navigation 진행 중

↓

새 OpenScreen 요청

↓

UI\_BUSY

```



명령을 Queue하지 않습니다.



호출 측이 전환 완료 후 다시 요청해야 합니다.



\---



\## 28. UIPopup



`UIPopup`은 현재 Screen 위에 일시적으로 표시되는 UI입니다.



예:



```text

Settings

Confirm

Reward

Information

```



Popup은 Stack으로 관리됩니다.



\---



\## 29. Popup Stack



열린 순서:



```text

Popup A

↓

Popup B

↓

Popup C

```



Stack:



```text

Top

Popup C

Popup B

Popup A

Bottom

```



`TopPopup`은 현재 Stack 최상단 Popup을 반환합니다.



\---



\## 30. Popup Back



Popup이 하나 이상 열려 있으면 Screen History보다 Popup이 먼저 Back 대상이 됩니다.



```text

Screen History 존재

Popup 존재

↓

Back

↓

Top Popup Close

```



Popup이 모두 닫힌 후에 Screen History Back이 가능합니다.



\---



\## 31. Popup BlocksInput



`UIPopup.BlocksInput` 기본값:



```text

true

```



Blocking Popup이 열리면 자신보다 아래 UI의 입력을 차단합니다.



예:



```text

Popup B

BlocksInput = true



Popup A

Screen

```



결과:



```text

Popup B

→ 입력 가능



Popup A

→ 입력 차단



Screen

→ 입력 차단

```



\---



\## 32. Non-Blocking Popup



Popup의 `BlocksInput`을 false로 지정할 수도 있습니다.



```text

Popup

BlocksInput = false

```



상위 Blocking View가 없다면 아래 UI도 입력 가능 상태를 유지할 수 있습니다.



\---



\## 33. UIOverlay



`UIOverlay`는 Screen History와 Popup Stack에 포함되지 않는 독립 View입니다.



대표 용도:



```text

Loading

Network Block

Tutorial Effect

Global Overlay

Screen Fade

```



\---



\## 34. Overlay Layer



Overlay는 두 종류의 표시 Layer를 가집니다.



```text

Normal

Topmost

```



\### Normal



```text

Popup

Normal Overlay

Screen

```



\### Topmost



```text

Topmost Overlay

Popup

Normal Overlay

Screen

```



\---



\## 35. Overlay BlocksInput



Overlay의 기본값:



```text

BlocksInput = false

```



단순 표시용 Overlay는 아래 UI 입력을 막지 않습니다.



Blocking Overlay가 필요한 경우 명시적으로:



```text

BlocksInput = true

```



로 설정합니다.



\---



\## 36. Input 계산 우선순위



Framework는 다음 순서로 입력 가능 상태를 계산합니다.



```text

1\. Topmost Overlay

2\. Popup

3\. Normal Overlay

4\. Current Screen

```



각 계층에서는 위에 있는 View부터 아래로 평가합니다.



Blocking View가 발견되면 그 아래 View들은 입력 불가 상태가 됩니다.



\---



\## 37. Open 상태와 입력



단순히 GameObject가 활성이라고 해서 입력 가능한 것은 아닙니다.



Framework는 다음 조건을 사용합니다.



```text

View State == Open

그리고

상위 Blocking View 없음

```



두 조건을 만족해야 입력이 활성화됩니다.



Opening 또는 Closing View는 입력할 수 없습니다.



\---



\## 38. Back API



Back 입력 자체를 Framework가 감지하지 않습니다.



사용하는 프로젝트가 입력을 감지한 뒤:



```csharp

Result result = uiController.Back();

```



을 호출합니다.



예:



```text

Android Back

Escape

Gamepad B

UI Back Button

```



어떤 입력을 Back으로 사용할지는 프로젝트가 결정합니다.



\---



\## 39. Back 처리 우선순위



정확한 우선순위:



```text

1\. Blocking Topmost Overlay

2\. Top Popup

3\. Blocking Normal Overlay

4\. Screen History

5\. NoBackTarget

```



\---



\## 40. Blocking Topmost Overlay와 Back



Blocking Topmost Overlay가 존재하면:



```text

Back

↓

UI\_BACK\_BLOCKED

```



입니다.



Overlay 자체를 자동으로 닫지 않습니다.



필요한 시점에:



```csharp

uiController.Close(overlayId);

```



를 명시적으로 호출해야 합니다.



\---



\## 41. Popup과 Back



Blocking Topmost Overlay가 없다면 Popup Stack을 확인합니다.



Popup이 존재하면:



```text

Back

↓

Top Popup Close

```



합니다.



Screen History는 처리하지 않습니다.



\---



\## 42. Blocking Normal Overlay와 Back



Popup이 없더라도 Blocking Normal Overlay가 존재하면:



```text

Back

↓

UI\_BACK\_BLOCKED

```



입니다.



Screen History로 전달하지 않습니다.



\---



\## 43. Screen History Back



Blocking Overlay와 Popup이 없고 History가 존재하면:



```text

Back

↓

Current Screen Closing

↓

이전 Screen Opening

↓

이전 Screen 복원

```



을 수행합니다.



\---



\## 44. NoBackTarget



다음 상태:



```text

Blocking Overlay 없음

Popup 없음

Screen History 없음

```



에서 Back을 호출하면:



```text

UI\_NO\_BACK\_TARGET

```



을 반환합니다.



Framework는 Application을 종료하지 않습니다.



\---



\## 45. CanBack



`UIController.CanBack`은 현재 상태에서 Back 처리가 가능한지 나타냅니다.



대표적으로 false인 상황:



```text

Blocking Topmost Overlay 존재

Blocking Normal Overlay 존재

Screen Navigation 진행 중

History 없음

Popup Transition 중

```



Popup이 정상 Open 상태이거나 복원 가능한 Screen History가 있으면 true가 될 수 있습니다.



\---



\## 46. Transition



Transition은 `UIViewTransition`을 기반으로 합니다.



```text

UIViewTransition

└─ UIFadeTransition

```



View에 Transition 컴포넌트가 없으면 Lifecycle은 즉시 완료됩니다.



\---



\## 47. UIViewTransition



사용자 정의 Transition은 다음 두 동작을 구현합니다.



```csharp

protected abstract IEnumerator OnPlayOpening(

&#x20;   CanvasGroup canvasGroup);



protected abstract IEnumerator OnPlayClosing(

&#x20;   CanvasGroup canvasGroup);

```



Framework가 View의 CanvasGroup을 전달합니다.



Transition은 시각 효과에 집중합니다.



\---



\## 48. Transition 책임



Transition이 담당:



```text

Alpha

Position

Scale

기타 시각적 변화

```



사용자 정의 구현에서 필요할 수 있습니다.



Framework가 담당:



```text

UIViewState

Lifecycle

GameObject 활성 / 비활성

Input 활성 / 비활성

Transition 실행 시점

```



Transition이 Framework Lifecycle 상태를 직접 변경하면 안 됩니다.



\---



\## 49. UIFadeTransition



기본 제공 Transition:



```text

UIFadeTransition

```



기본 값:



```text

Opening Duration = 0.2

Closing Duration = 0.2

```



Opening:



```text

Alpha 0

↓

Alpha 1

```



Closing:



```text

현재 Alpha

↓

Alpha 0

```



\---



\## 50. Unscaled Time



`UIFadeTransition`은:



```csharp

Time.unscaledDeltaTime

```



을 사용합니다.



따라서:



```text

Time.timeScale = 0

```



상태에서도 Fade Transition을 진행할 수 있습니다.



Pause UI 같은 상황에서도 사용할 수 있도록 설계했습니다.



\---



\## 51. Duration 0



Fade Duration이 0이면 해당 Transition을 즉시 완료합니다.



Opening:



```text

Alpha = 1

```



Closing:



```text

Alpha = 0

```



로 즉시 적용됩니다.



\---



\## 52. Transition Busy



Opening 또는 Closing Transition이 진행 중인 동일 View에 다시 Lifecycle 요청을 보내면:



```text

UI\_BUSY

```



를 반환합니다.



v1에서는 Transition을 강제 Cancel하지 않습니다.



\---



\## 53. Command Queue 정책



v1에서는 UI 요청을 자동 Queue하지 않습니다.



예:



```text

Opening 중 Close 요청

```



을 저장했다가 나중에 자동 실행하지 않습니다.



즉:



```text

현재 요청

→ Result 확인

→ 실패라면 호출 측에서 이후 정책 결정

```



구조입니다.



\---



\## 54. Close



API:



```csharp

Result Close(UIId id);

```



현재 열린 View를 닫습니다.



View 타입에 따라 Runtime 관리 구조에서도 정리합니다.



```text

UIPopup

→ Popup Stack 제거



UIOverlay

→ Overlay Order 제거



UIScreen

→ Current Screen 상태 반영

```



Instance 자체는 Cache에 유지됩니다.



\---



\## 55. AlreadyOpen



이미 Open 상태인 View에 다시 Open을 요청하면:



```text

UI\_ALREADY\_OPEN

```



입니다.



자동으로 성공 처리하지 않습니다.



중복 요청이 발생했다는 사실을 호출 측에서 확인할 수 있도록 실패 Result를 반환합니다.



\---



\## 56. AlreadyClosed



이미 Closed 상태인 View를 닫으려 하면:



```text

UI\_ALREADY\_CLOSED

```



입니다.



아직 Runtime Instance가 생성되지 않았더라도 Registry에 정상 등록된 View라면 Closed 상태로 판단할 수 있습니다.



\---



\## 57. Result 기반 API



주요 Runtime 작업은 ChoDogyu Core의 Result 계열을 사용합니다.



예:



```csharp

Result<UIPopup> result =

&#x20;   uiController.OpenPopup(

&#x20;       new UIId("popup.settings"));



if (result.IsFailure)

{

&#x20;   Debug.LogWarning(

&#x20;       $"{result.Error.Code}: {result.Error.Message}");



&#x20;   return;

}



UIPopup popup = result.Value;

```



오류 메시지를 문자열 비교하는 대신 `Error.Code`를 사용하여 실패 원인을 분기할 수 있습니다.



\---



\## 58. Runtime Error Codes



```text

UI\_INVALID\_ID

UI\_NOT\_FOUND

UI\_INVALID\_REGISTRY

UI\_DUPLICATE\_ID

UI\_MISSING\_REGISTRY

UI\_MISSING\_LAYER

UI\_INVALID\_TYPE

UI\_MISSING\_CANVAS\_GROUP

UI\_ALREADY\_OPEN

UI\_ALREADY\_CLOSED

UI\_BUSY

UI\_BACK\_BLOCKED

UI\_NO\_BACK\_TARGET

```



\---



\## 59. UI\_INVALID\_ID



다음과 같은 경우입니다.



```text

빈 UIId

Registry Prefab의 빈 ID

```



\---



\## 60. UI\_NOT\_FOUND



요청한 ID가 Registry에 존재하지 않는 경우입니다.



```text

OpenPopup("popup.unknown")

↓

UI\_NOT\_FOUND

```



\---



\## 61. UI\_INVALID\_REGISTRY



Registry에 null Prefab 같은 잘못된 데이터가 포함된 경우입니다.



\---



\## 62. UI\_DUPLICATE\_ID



두 개 이상의 Registry Prefab이 동일한 UIId를 가진 경우입니다.



```text

HomeScreen

ID = screen.home



OtherScreen

ID = screen.home

```



허용하지 않습니다.



\---



\## 63. UI\_MISSING\_REGISTRY



UIController에 Registry가 할당되지 않은 상태에서 Registry가 필요한 요청을 수행한 경우입니다.



\---



\## 64. UI\_MISSING\_LAYER



생성할 View가 들어가야 할 Layer가 UIController에 지정되지 않은 경우입니다.



예:



```text

UIScreen

↓

Screen Layer 없음

↓

UI\_MISSING\_LAYER

```



\---



\## 65. UI\_INVALID\_TYPE



Registry의 Prefab 타입과 요청한 타입이 맞지 않는 경우입니다.



예:



```text

ID = popup.settings

실제 Prefab = UIPopup



GetOrCreate<UIScreen>()

↓

UI\_INVALID\_TYPE

```



\---



\## 66. UI\_MISSING\_CANVAS\_GROUP



Lifecycle 및 입력 관리에 필요한 CanvasGroup이 View에 없는 경우입니다.



\---



\## 67. UI\_ALREADY\_OPEN



이미 Open인 View를 다시 열려고 한 경우입니다.



\---



\## 68. UI\_ALREADY\_CLOSED



이미 Closed인 View를 다시 닫으려고 한 경우입니다.



\---



\## 69. UI\_BUSY



대표 상황:



```text

Opening 중

Closing 중

Screen Navigation 중

```



새 요청을 즉시 처리할 수 없음을 의미합니다.



\---



\## 70. UI\_BACK\_BLOCKED



Blocking Overlay가 Back 전달을 차단하는 경우입니다.



Overlay를 자동 닫지 않습니다.



\---



\## 71. UI\_NO\_BACK\_TARGET



처리할 Popup 또는 Screen History가 없는 경우입니다.



\---



\## 72. UI Root Editor Tool



Editor Window:



```text

Tools

→ ChoDogyu

→ UI Framework

→ Open Window

```



UI Root 생성 기능은 현재 활성 Scene에 다음 구조를 만듭니다.



```text

CDG UI Root

├─ Screen Layer

├─ Overlay Layer

├─ Popup Layer

└─ Top Overlay Layer

```



Root:



```text

Canvas

GraphicRaycaster

UIController

```



\---



\## 73. EventSystem 정책



Editor Tool은 EventSystem을 자동 생성하지 않습니다.



이유:



```text

Legacy Input

Input System

Custom Input

기존 EventSystem

멀티플레이어 입력

프로젝트별 입력 구성

```



등 프로젝트마다 입력 환경이 다를 수 있기 때문입니다.



Framework가 특정 Input System 구성을 강제하지 않습니다.



\---



\## 74. Registry 생성 Tool



Editor Window에서:



```text

Create UI Registry

```



를 통해 프로젝트 `Assets` 내부에 새로운 `UIRegistry` Asset을 생성할 수 있습니다.



저장 위치는 사용자가 결정합니다.



\---



\## 75. View Prefab 생성 Tool



생성 가능한 타입:



```text

Screen

Popup

Overlay

```



기본 생성 구조:



```text

RectTransform

CanvasGroup

UIView 구현 타입

```



Root GameObject는 비활성 상태로 생성됩니다.



\---



\## 76. Screen Prefab 생성



Screen 타입을 선택하면:



```text

UIScreen

```



이 포함된 Prefab을 생성합니다.



UIId를 입력해야 합니다.



\---



\## 77. Popup Prefab 생성



Popup 타입:



```text

UIPopup

```



추가 설정:



```text

Blocks Input

```



기본 Modal Popup 용도로는 true를 사용할 수 있습니다.



\---



\## 78. Overlay Prefab 생성



Overlay 타입:



```text

UIOverlay

```



설정:



```text

Overlay Layer

\- Normal

\- Topmost



Blocks Input

\- true

\- false

```



\---



\## 79. Registry 자동 등록



View 생성 시 Registry를 함께 지정하면 생성 성공 후 Prefab을 Registry에 등록합니다.



흐름:



```text

Prefab 생성

↓

Prefab 저장

↓

Registry 등록

```



등록 단계에서 실패하면 잘못된 Registry 상태가 남지 않도록 생성 작업을 안전하게 처리합니다.



\---



\## 80. Validation 목적



Validation은 Runtime 실행 전에 UI 구성 문제를 확인하기 위한 Editor 기능입니다.



검사 대상은 크게:



```text

Registry

Controller

View

Transition

```



으로 나뉩니다.



\---



\## 81. Registry Validation



대표 검사:



```text

Missing Prefab

빈 UIId

중복 UIId

잘못된 View 등록

```



\---



\## 82. Controller Validation



대표 검사:



```text

Registry 미지정

Screen Layer 누락

Overlay Layer 누락

Popup Layer 누락

Top Overlay Layer 누락

중복 Layer 참조

```



\---



\## 83. View Validation



대표 검사:



```text

빈 UIId

CanvasGroup 누락

Root GameObject 활성 상태

View 타입 구성

Overlay 설정

Transition 구성

```



\---



\## 84. Transition Validation



기본 Fade Transition의 잘못된 설정 등을 검사합니다.



예:



```text

음수 Opening Duration

음수 Closing Duration

```



\---



\## 85. Safe Fix



Validation Issue 중 수정 결과가 명확한 항목은 Safe Fix를 제공할 수 있습니다.



현재 대표 Safe Fix:



```text

CanvasGroup 추가

Prefab Root 비활성화

Fade Duration 음수 값을 0 이상으로 보정

```



\---



\## 86. Safe Fix하지 않는 항목



사용자의 의도가 필요한 항목은 자동 수정하지 않습니다.



예:



```text

어떤 UIId를 사용할지

중복 ID 중 어떤 값을 변경할지

어떤 Registry를 Controller에 연결할지

어떤 Transform을 Layer로 사용할지

Overlay를 Normal로 할지 Topmost로 할지

```



Framework가 임의로 선택하지 않습니다.



\---



\## 87. Validation Severity



Validation 결과는 문제의 성격에 따라 Error 또는 Warning으로 표현할 수 있습니다.



Error는 현재 구성에서 정상적인 Framework 사용을 방해하는 문제를 나타냅니다.



Warning은 즉시 실패를 만들지는 않지만 확인이 필요한 설정을 나타낼 수 있습니다.



\---



\## 88. Sample



패키지는 `Basic Usage` Sample을 제공합니다.



Package Manager:



```text

ChoDogyu UI Framework \& Editor Tool

→ Samples

→ Basic Usage

→ Import

```



\---



\## 89. Basic Usage Sample 구조



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

&#x20;  ├─ CDG.UI.Samples.BasicUsage.asmdef

&#x20;  └─ BasicUISampleController.cs

```



\---



\## 90. Basic Usage Sample 흐름



Screen:



```text

Home

↓ Push

Details

↓ Back

Home

```



Popup:



```text

Home

↓

Settings Popup

↓ Back

Popup Close

```



Overlay:



```text

Loading Topmost Overlay

↓

일정 시간 유지

↓

Close

```



\---



\## 91. Sample의 목적



Sample은 게임을 제공하기 위한 것이 아닙니다.



다음 Framework 기능을 빠르게 확인하기 위한 최소 소비자 예제입니다.



```text

Screen Navigation

Screen History

Popup Stack

Overlay

Back

Input Blocking

Fade Transition

Runtime Cache

Result 처리

```



\---



\## 92. Sample과 EventSystem



Basic Usage Sample의 조작 패널은 IMGUI를 사용합니다.



Sample 자체가 특정 EventSystem 또는 Input System 구성을 강제하지 않기 위해서입니다.



실제 게임 UI에서는 프로젝트가 선택한 EventSystem과 Button 등을 사용할 수 있습니다.



\---



\## 93. Runtime Singleton 정책



Framework는 Singleton을 제공하지 않습니다.



다음과 같은 구조를 강제하지 않습니다.



```text

UIManager.Instance

ServiceLocator.Get<UIController>()

Global Static UI

```



프로젝트가 자신의 Architecture에 맞게 UIController 참조를 관리합니다.



\---



\## 94. Scene 수명 정책



Framework는:



```text

DontDestroyOnLoad

```



를 자동 적용하지 않습니다.



UI Root를 Scene마다 둘지, Persistent Scene에 둘지, Bootstrap 구조에서 관리할지는 사용하는 프로젝트가 결정합니다.



\---



\## 95. Scene Transition 정책



Framework는 Scene Load를 직접 수행하지 않습니다.



예:



```text

UI 버튼

↓

게임 Scene Service

↓

Scene Load

```



와 같은 구조에서 Scene 책임은 별도 시스템이 담당합니다.



UI Framework는 UI 흐름에 집중합니다.



\---



\## 96. UI Data 정책



Framework는 View에 표시할 게임 데이터를 정의하지 않습니다.



예:



```text

Inventory Item

Quest Data

Player Status

Settings

```



는 각 게임 시스템의 데이터입니다.



UI는 해당 데이터를 전달받아 표시합니다.



\---



\## 97. Data Binding 정책



v1에서는 다음 기능을 제공하지 않습니다.



```text

MVVM

Observable Binding

Reflection Binding

Generated Binding

Automatic Presenter

Automatic ViewModel

```



일반 C# 참조 또는 프로젝트 Architecture를 통해 View 데이터를 연결합니다.



\---



\## 98. Addressables 정책



v1의 UIRegistry는 직접 Prefab 참조를 사용합니다.



```text

UIRegistry

→ UIView Prefab

```



다음은 포함하지 않습니다.



```text

Addressables Key

Async Load

Asset Handle

Unload Policy

Remote Asset

```



필요하면 이후 별도 Asset Loading 계층과 조합할 수 있습니다.



\---



\## 99. Pooling 정책



Runtime View는 Cache 재사용 방식을 사용합니다.



```text

Create Once

→ Close

→ Inactive

→ Reopen

```



일반적인 Object Pool과는 다른 목적입니다.



v1에서는 ChoDogyu Object Pooling Package에 의존하지 않습니다.



\---



\## 100. 테스트



Runtime 테스트:



```text

220 Passed

0 Failed

```



Editor 테스트:



```text

67 Passed

0 Failed

```



검증 범위:



```text

UIId

UIRegistry

Instance 생성

Instance Cache

Screen Navigation

Push

Replace

Reset

History

Popup Stack

Overlay

Input Blocking

Back

Lifecycle

Transition

Fade

Error Result



UI Root 생성

Registry 생성

View Prefab 생성

Registry 등록

Validation

Safe Fix

Editor Tool

```



\---



\## 101. UPM 독립 설치 검증



완전히 새로운 Unity 프로젝트에서 검증했습니다.



환경:



```text

Unity 6.3 LTS

6000.3.9f1

```



흐름:



```text

ChoDogyu Core v1.0.0 설치

↓

ChoDogyu UI Framework 설치

↓

Unity UI Dependency Resolve

↓

Compile

↓

Editor Window 실행

↓

UI Root 생성

↓

Registry 생성

↓

UIScreen Prefab 생성

↓

Registry 자동 등록

↓

Validation

↓

Basic Usage Sample Import

↓

Sample Play

```



결과:



```text

Git UPM 설치 성공

Compile 성공

Editor Tool 정상

Runtime 정상

Sample 정상

Missing Script 없음

Missing Reference 없음

Console Error 없음

```



\---



\## 102. 패키지 의존성



필수:



```text

ChoDogyu Core 1.0.0

Unity UI 2.0.0

```



Unity UI:



```text

com.unity.ugui

```



는 `package.json` dependencies로 관리합니다.



ChoDogyu Core는 Git UPM으로 먼저 설치합니다.



\---



\## 103. 다른 CDG Package와의 관계



UI Framework는 다음 패키지와 독립적입니다.



```text

ChoDogyu Data

ChoDogyu Save

ChoDogyu Pooling

ChoDogyu Editor Tools

```



필요하다면 게임 프로젝트에서 함께 조합할 수 있습니다.



예:



```text

Data

→ UI에 표시할 정적 데이터



Save

→ 사용자 UI 설정 저장



Pooling

→ 게임 Object 재사용



UI

→ 화면 흐름과 View 관리

```



각 패키지는 서로의 책임을 불필요하게 침범하지 않습니다.



\---



\## 104. 권장 사용 흐름



새 프로젝트에서 일반적인 구성:



```text

1\. Core 설치

2\. UI Framework 설치

3\. UI Root 생성

4\. UI Registry 생성

5\. View Prefab 생성

6\. Registry 등록

7\. Validation

8\. 게임 코드에서 UIController 호출

```



\---



\## 105. 기본 Screen 사용 예



```csharp

using CDG.Core.Results;

using CDG.UI;

using UnityEngine;



public sealed class GameUIController : MonoBehaviour

{

&#x20;   \[SerializeField]

&#x20;   private UIController uiController;



&#x20;   public void OpenInventory()

&#x20;   {

&#x20;       Result<UIScreen> result = uiController.OpenScreen(

&#x20;           new UIId("screen.inventory"),

&#x20;           UIScreenOpenMode.Push);



&#x20;       if (result.IsFailure)

&#x20;       {

&#x20;           Debug.LogWarning(

&#x20;               $"{result.Error.Code}: {result.Error.Message}");

&#x20;       }

&#x20;   }

}

```



\---



\## 106. 기본 Popup 사용 예



```csharp

using CDG.Core.Results;

using CDG.UI;

using UnityEngine;



public sealed class PopupExample : MonoBehaviour

{

&#x20;   \[SerializeField]

&#x20;   private UIController uiController;



&#x20;   public void OpenSettings()

&#x20;   {

&#x20;       Result<UIPopup> result = uiController.OpenPopup(

&#x20;           new UIId("popup.settings"));



&#x20;       if (result.IsFailure)

&#x20;       {

&#x20;           Debug.LogWarning(

&#x20;               $"{result.Error.Code}: {result.Error.Message}");

&#x20;       }

&#x20;   }

}

```



\---



\## 107. 기본 Overlay 사용 예



```csharp

using CDG.Core.Results;

using CDG.UI;

using UnityEngine;



public sealed class LoadingExample : MonoBehaviour

{

&#x20;   \[SerializeField]

&#x20;   private UIController uiController;



&#x20;   private static readonly UIId LoadingId =

&#x20;       new UIId("overlay.loading");



&#x20;   public void ShowLoading()

&#x20;   {

&#x20;       Result<UIOverlay> result =

&#x20;           uiController.OpenOverlay(LoadingId);



&#x20;       if (result.IsFailure)

&#x20;       {

&#x20;           Debug.LogWarning(

&#x20;               $"{result.Error.Code}: {result.Error.Message}");

&#x20;       }

&#x20;   }



&#x20;   public void HideLoading()

&#x20;   {

&#x20;       Result result =

&#x20;           uiController.Close(LoadingId);



&#x20;       if (result.IsFailure)

&#x20;       {

&#x20;           Debug.LogWarning(

&#x20;               $"{result.Error.Code}: {result.Error.Message}");

&#x20;       }

&#x20;   }

}

```



\---



\## 108. Back 사용 예



```csharp

using CDG.Core.Results;

using CDG.UI;

using UnityEngine;



public sealed class BackInputHandler : MonoBehaviour

{

&#x20;   \[SerializeField]

&#x20;   private UIController uiController;



&#x20;   public void RequestBack()

&#x20;   {

&#x20;       Result result = uiController.Back();



&#x20;       if (result.IsFailure)

&#x20;       {

&#x20;           Debug.Log(

&#x20;               $"{result.Error.Code}: {result.Error.Message}");

&#x20;       }

&#x20;   }

}

```



입력 감지는 사용하는 프로젝트가 담당합니다.



\---



\## 109. 설계 핵심



Framework의 핵심 목표는 다음과 같습니다.



```text

UI 역할 분리

명확한 화면 흐름

중복 Instance 방지

명시적인 Lifecycle

명시적인 실패 Result

예측 가능한 Back

예측 가능한 Input Blocking

프로젝트 비종속

Editor 제작 지원

검증 가능한 구조

```



\---



\## 110. 버전



현재 버전:



```text

v1.0.0

```



v1은 안정적인 기본 UI 흐름과 Editor 제작 도구에 집중합니다.



향후 기능을 추가하더라도 기존 Public API와 책임 경계를 가능한 한 유지하는 방향을 우선합니다.

