\# Changelog



ChoDogyu UI Framework \& Editor Tool 패키지의 주요 변경 사항을 기록합니다.



\## \[1.0.0] - 2026-09-17



첫 정식 배포 버전입니다.



\### Added



\#### Runtime Core



\- Runtime Assembly `CDG.UI` 추가

\- Root Namespace `CDG.UI` 적용

\- `UIController` 기반 UI 제어 진입점 제공

\- `UIId` 기반 UI 식별 구조 추가

\- `UIRegistry` 기반 UIView Prefab 등록 및 조회 기능 추가

\- Runtime Instance Lazy Instantiate 지원

\- 생성된 Runtime Instance Cache 및 재사용 지원

\- 동일 UIId에 하나의 Cached Instance를 사용하는 정책 적용

\- 닫힌 UI를 Destroy하지 않고 비활성화하여 재사용

\- 외부에서 파괴된 Cached Instance 재생성 지원

\- ChoDogyu Core의 `Result`, `Result<T>`, `ResultError` 기반 오류 처리 적용

\- Runtime Singleton 및 Service Locator 비사용



\#### UIView



\- 모든 UI View의 기반 클래스 `UIView` 추가

\- UI 식별자 `Id` 제공

\- 현재 Lifecycle 상태 `State` 제공

\- `IsOpen` 상태 조회 제공

\- `IsTransitioning` 상태 조회 제공

\- `CanvasGroup` 기반 Lifecycle 및 입력 관리

\- `OnOpening()` Lifecycle Hook 제공

\- `OnOpened()` Lifecycle Hook 제공

\- `OnClosing()` Lifecycle Hook 제공

\- `OnClosed()` Lifecycle Hook 제공



\#### Lifecycle



\- `Closed` 상태 추가

\- `Opening` 상태 추가

\- `Open` 상태 추가

\- `Closing` 상태 추가

\- `Closed → Opening → Open → Closing → Closed` 상태 흐름 적용

\- Opening 시작 시 입력 비활성화

\- Opening 완료 후 입력 활성화

\- Closing 시작 시 입력 비활성화

\- Closing 완료 후 GameObject 비활성화

\- Transition이 없는 View의 즉시 Lifecycle 처리 지원

\- 이미 열린 View에 Open 요청 시 `UI\_ALREADY\_OPEN` 반환

\- 이미 닫힌 View에 Close 요청 시 `UI\_ALREADY\_CLOSED` 반환

\- Opening 또는 Closing 중 중복 요청 시 `UI\_BUSY` 반환

\- CanvasGroup 누락 시 `UI\_MISSING\_CANVAS\_GROUP` 반환

\- UI Command Queue 미사용

\- Transition 강제 Cancel 미지원



\#### UIScreen



\- `UIScreen` View 타입 추가

\- 동시에 하나의 Current Screen 관리

\- Screen History 관리

\- `OpenScreen(UIId)` API 추가

\- `OpenScreen(UIId, UIScreenOpenMode)` API 추가

\- 기본 Screen Open Mode를 Push로 설정

\- `UIScreenOpenMode.Push` 추가

\- `UIScreenOpenMode.Replace` 추가

\- `UIScreenOpenMode.Reset` 추가

\- Push 시 현재 Screen을 History에 보존

\- Replace 시 현재 Screen을 History에 추가하지 않고 교체

\- Replace 시 기존 History 유지

\- Reset 시 기존 Screen History 전체 제거

\- Back 시 이전 Screen History 복원

\- 이전 Screen Closing 완료 후 다음 Screen Opening 시작

\- Screen Navigation 중 추가 Navigation 요청 차단

\- Screen Navigation 중 `UI\_BUSY` 반환

\- `CurrentScreen` 상태 조회 제공

\- `ScreenHistoryCount` 상태 조회 제공



\#### UIPopup



\- `UIPopup` View 타입 추가

\- 여러 Popup의 Stack 관리 지원

\- Popup Open 순서를 기준으로 Stack 구성

\- 가장 최근에 연 Popup을 최상단에 배치

\- `OpenPopup(UIId)` API 추가

\- `TopPopup` 상태 조회 제공

\- `PopupCount` 상태 조회 제공

\- Back 시 최상단 Popup 우선 Close

\- Popup Close 완료 후 Stack에서 제거

\- `BlocksInput` 설정 추가

\- Popup `BlocksInput` 기본값 `true`

\- Blocking Popup 아래 UI의 입력 차단 지원

\- Non-Blocking Popup 구성 지원



\#### UIOverlay



\- `UIOverlay` View 타입 추가

\- Screen History와 독립된 Overlay 흐름 제공

\- Popup Stack과 독립된 Overlay 흐름 제공

\- `UIOverlayLayer` 추가

\- `Normal` Overlay Layer 추가

\- `Topmost` Overlay Layer 추가

\- `OpenOverlay(UIId)` API 추가

\- Overlay Open 순서 관리

\- Normal Overlay를 Screen보다 위, Popup보다 아래에 배치

\- Topmost Overlay를 Popup보다 위에 배치

\- `BlocksInput` 설정 추가

\- Overlay `BlocksInput` 기본값 `false`

\- Blocking Overlay 구성 지원

\- Overlay는 Back 대상이 아닌 정책 적용

\- Overlay Close는 명시적인 `Close()` 호출로 처리



\#### Display Order



\- 기본 UI 표시 계층 정의

\- `Screen Layer` 추가

\- `Overlay Layer` 추가

\- `Popup Layer` 추가

\- `Top Overlay Layer` 추가

\- `Topmost Overlay > Popup > Normal Overlay > Screen` 표시 순서 적용



\#### Input Blocking



\- `UIInputCoordinator` 기반 입력 상태 계산 추가

\- Topmost Overlay 우선 입력 평가

\- Popup 입력 평가

\- Normal Overlay 입력 평가

\- Current Screen 입력 평가

\- `Topmost Overlay → Popup → Normal Overlay → Screen` 순서 적용

\- 상위 Blocking View 아래 UI 입력 차단

\- Open 상태의 View만 입력 가능

\- Opening View 입력 차단

\- Closing View 입력 차단

\- Lifecycle과 Input Blocking 통합 관리



\#### Back



\- `UIController.Back()` API 추가

\- `CanBack` 상태 조회 제공

\- Blocking Topmost Overlay의 Back 전달 차단

\- Popup 존재 시 Top Popup 우선 Close

\- Blocking Normal Overlay의 Screen History Back 전달 차단

\- Screen History 존재 시 이전 Screen 복원

\- 처리할 대상이 없을 경우 `UI\_NO\_BACK\_TARGET` 반환

\- Blocking Overlay 존재 시 `UI\_BACK\_BLOCKED` 반환

\- Blocking Overlay를 Back으로 자동 닫지 않는 정책 적용

\- Application 자동 종료 미지원

\- 키보드 입력 직접 감지 미지원

\- 게임패드 입력 직접 감지 미지원

\- Android Back Button 직접 감지 미지원

\- 프로젝트가 입력을 감지한 뒤 `Back()`을 호출하는 구조 적용



\#### Runtime Instance Cache



\- `UIInstanceStore` 기반 Runtime Instance 관리

\- 최초 요청 시 Registry Prefab Instantiate

\- View 종류에 맞는 Layer 자동 배치

\- 생성된 Instance Cache 저장

\- Close 후 GameObject 비활성화

\- 재요청 시 기존 Instance 재사용

\- 동일 UIId Multi Instance 미지원

\- `GetOrCreate<T>()` API 제공

\- Cached Instance 타입 검증

\- Registry Prefab 타입 검증

\- 타입 불일치 시 `UI\_INVALID\_TYPE` 반환



\#### UIRegistry



\- `UIRegistry` ScriptableObject 추가

\- UIView Prefab 목록 관리

\- `Count` 제공

\- Read-only `Prefabs` 목록 제공

\- `Contains()` 제공

\- `TryGet()` 제공

\- `Get()` Result API 제공

\- Registry Lookup Cache 구성

\- null Prefab 검증

\- 빈 UIId 검증

\- 중복 UIId 검증

\- Registry 변경 시 Lookup Cache 무효화

\- 등록되지 않은 UIId에 `UI\_NOT\_FOUND` 반환



\#### Transition



\- `UIViewTransition` 추상 클래스 추가

\- Opening Transition 확장 지점 제공

\- Closing Transition 확장 지점 제공

\- Coroutine 기반 Transition 실행

\- Framework가 CanvasGroup을 Transition에 전달

\- Transition과 Lifecycle 책임 분리

\- 사용자 정의 Transition 구현 지원

\- Transition이 없는 View 즉시 처리 지원



\#### UIFadeTransition



\- 기본 Fade Transition 추가

\- Opening Fade In 지원

\- Closing Fade Out 지원

\- 기본 Opening Duration `0.2초`

\- 기본 Closing Duration `0.2초`

\- `Time.unscaledDeltaTime` 기반 전환

\- `Time.timeScale = 0` 상태에서도 Fade 지원

\- Duration `0` 즉시 완료 지원

\- 음수 Duration Validation 및 Safe Fix 지원



\#### Runtime State Query



\- `CurrentScreen` 제공

\- `ScreenHistoryCount` 제공

\- `TopPopup` 제공

\- `PopupCount` 제공

\- `CanBack` 제공

\- `IsBusy` 제공

\- `IsOpen(UIId)` 제공

\- Opening 상태를 `IsOpen = false`로 처리

\- Screen Navigation 및 Transition 진행 상태를 Busy에 반영



\#### Runtime Error Codes



\- `UI\_INVALID\_ID`

\- `UI\_NOT\_FOUND`

\- `UI\_INVALID\_REGISTRY`

\- `UI\_DUPLICATE\_ID`

\- `UI\_MISSING\_REGISTRY`

\- `UI\_MISSING\_LAYER`

\- `UI\_INVALID\_TYPE`

\- `UI\_MISSING\_CANVAS\_GROUP`

\- `UI\_ALREADY\_OPEN`

\- `UI\_ALREADY\_CLOSED`

\- `UI\_BUSY`

\- `UI\_BACK\_BLOCKED`

\- `UI\_NO\_BACK\_TARGET`



\#### Editor Assembly



\- Editor Assembly `CDG.UI.Editor` 추가

\- Root Namespace `CDG.UI.Editor` 적용

\- Editor Platform 전용 Assembly 구성

\- `CDG.UI` 참조

\- `CDG.Core` 참조



\#### UI Framework Editor Window



\- 통합 UI Framework Editor Window 추가

\- 메뉴 경로 `Tools/ChoDogyu/UI Framework/Open Window` 추가

\- UI Root 생성 기능 제공

\- UI Registry 생성 기능 제공

\- View Prefab 생성 기능 제공

\- Validation 기능 제공

\- Safe Fix 기능 제공

\- Scroll View 기반 Editor UI 구성

\- 파일 저장 창 호출 시 IMGUI Layout 오류 방지를 위한 지연 실행 적용



\#### UI Root Creation



\- 활성 Scene에 `CDG UI Root` 생성 기능 추가

\- `Canvas` 자동 구성

\- `GraphicRaycaster` 자동 구성

\- `UIController` 자동 구성

\- `Screen Layer` 생성

\- `Overlay Layer` 생성

\- `Popup Layer` 생성

\- `Top Overlay Layer` 생성

\- UIController Layer 참조 자동 연결

\- EventSystem 자동 생성 미지원

\- 특정 Input System 구성 비강제



\#### UI Registry Creation



\- Editor Window에서 UIRegistry Asset 생성 지원

\- Assets 내부 저장 위치 사용자 선택

\- 기본 Registry 파일명 `UIRegistry.asset` 제공



\#### View Prefab Creation



\- `UIScreen` Prefab 생성 지원

\- `UIPopup` Prefab 생성 지원

\- `UIOverlay` Prefab 생성 지원

\- 기본 `RectTransform` 구성

\- 기본 `CanvasGroup` 구성

\- 선택한 UIView Component 구성

\- UIId 설정

\- Prefab Root 기본 비활성화

\- Popup `BlocksInput` 설정 지원

\- Overlay `Normal / Topmost` 설정 지원

\- Overlay `BlocksInput` 설정 지원

\- 생성 시 UIRegistry 선택 지원

\- 생성 성공 후 Registry 자동 등록 지원

\- 등록 실패 시 불완전한 생성 결과를 남기지 않는 처리 적용



\#### Validation



\- Registry Validation 추가

\- Controller Validation 추가

\- View Validation 추가

\- Transition Validation 추가

\- 빈 UIId 검사

\- 중복 UIId 검사

\- Missing Prefab 검사

\- CanvasGroup 누락 검사

\- View Prefab Root 활성 상태 검사

\- UIController Registry 미지정 검사

\- Layer 누락 검사

\- 중복 Layer 참조 검사

\- 잘못된 Overlay 구성 검사

\- View 타입 및 Registry 구성 검사

\- Fade Transition 설정 검사

\- Validation Error / Warning 분리



\#### Safe Fix



\- CanvasGroup 누락 Safe Fix 지원

\- View Prefab Root 활성 상태 Safe Fix 지원

\- 음수 Fade Duration Safe Fix 지원

\- 결과가 명확한 설정만 자동 수정

\- UIId 자동 결정 미지원

\- 중복 ID 자동 변경 미지원

\- Registry 자동 선택 미지원

\- Layer 자동 추측 미지원

\- Overlay Layer 자동 변경 미지원



\#### Basic Usage Sample



\- Package Manager용 `Basic Usage` Sample 추가

\- `SampleUIRegistry` 추가

\- `HomeScreen` Sample Prefab 추가

\- `DetailsScreen` Sample Prefab 추가

\- `SettingsPopup` Sample Prefab 추가

\- `LoadingOverlay` Sample Prefab 추가

\- `BasicUISample` Scene 추가

\- `BasicUISampleController` 추가

\- Sample 전용 Assembly 추가

\- Home → Details Push Navigation 예제 제공

\- Details → Home Back 예제 제공

\- Settings Popup Open / Back 예제 제공

\- Topmost Blocking Loading Overlay 예제 제공

\- Overlay 명시적 Close 예제 제공

\- Fade Transition 예제 제공

\- Runtime Instance Cache 재사용 예제 제공

\- Screen History 상태 표시

\- Popup Count 상태 표시

\- CanBack 상태 표시

\- Busy 상태 표시

\- 특정 EventSystem 구성을 강제하지 않기 위해 IMGUI 조작 패널 사용

\- TextMeshPro 비의존 Sample 구성



\#### Package Structure



\- Package Name `com.chodogyu.ui`

\- Display Name `ChoDogyu UI Framework \& Editor Tool`

\- Package Version `1.0.0`

\- Runtime Assembly `CDG.UI`

\- Editor Assembly `CDG.UI.Editor`

\- Runtime Namespace `CDG.UI`

\- Editor Namespace `CDG.UI.Editor`

\- Unity 6.3 기준 패키지 구성

\- Unity UI `com.unity.ugui` 2.0.0 의존성 선언

\- ChoDogyu Core 1.0.0 사용

\- ChoDogyu Data Framework 비의존

\- ChoDogyu Save / Load Framework 비의존

\- ChoDogyu Object Pooling 비의존

\- ChoDogyu General Editor Tools 비의존

\- Addressables 비의존

\- TextMeshPro 비의존

\- DOTween 비의존

\- 특정 게임 및 장르 코드 비포함



\#### Tests



\- Runtime Test 220개 통과

\- Editor Test 67개 통과

\- 전체 Failed 0 확인

\- UIId 검증

\- UIRegistry 등록 및 조회 검증

\- Registry 오류 검증

\- Runtime Instance 생성 검증

\- Instance Cache 재사용 검증

\- Destroy된 Cache 재생성 검증

\- Screen Push 검증

\- Screen Replace 검증

\- Screen Reset 검증

\- Screen History 검증

\- Screen Back 복원 검증

\- Screen Navigation Busy 검증

\- Popup Stack 검증

\- Popup Back 검증

\- Overlay Layer 검증

\- Overlay Input Blocking 검증

\- Popup Input Blocking 검증

\- Back 우선순위 검증

\- Blocking Overlay Back 차단 검증

\- Lifecycle 상태 검증

\- AlreadyOpen 검증

\- AlreadyClosed 검증

\- Busy 검증

\- CanvasGroup 누락 검증

\- Transition 검증

\- Fade Transition 검증

\- UI Root 생성 검증

\- Registry 생성 검증

\- View Prefab 생성 검증

\- Registry 자동 등록 검증

\- Validation 검증

\- Safe Fix 검증

\- Editor Window 동작 검증



\#### UPM Verification



\- 완전히 새로운 Unity 6.3 프로젝트에서 Git UPM 설치 검증

\- Unity 6.3 LTS `6000.3.9f1` 환경 검증

\- ChoDogyu Core v1.0.0 선설치 검증

\- ChoDogyu UI Framework Git 설치 검증

\- Unity UI 2.0.0 Dependency Resolve 검증

\- Runtime Assembly Compile 검증

\- Editor Assembly Compile 검증

\- UI Framework Editor Window 실행 검증

\- UI Root 생성 검증

\- UI Registry 생성 검증

\- UIScreen Prefab 생성 검증

\- Registry 자동 등록 검증

\- Registry Validation `0 Error / 0 Warning` 확인

\- Controller Validation `0 Error / 0 Warning` 확인

\- Basic Usage Sample Import 검증

\- Sample Scene Missing Script 없음 확인

\- Sample Scene Missing Reference 없음 확인

\- Home Screen 실행 검증

\- Screen Push 검증

\- Screen Back 검증

\- Popup Open / Back 검증

\- Loading Overlay Open / Close 검증

\- 개발 프로젝트 및 로컬 패키지 경로 비의존 검증



\#### Documentation



\- 저장소 README 추가

\- 패키지 README 추가

\- CHANGELOG 추가

\- 상세 Documentation 추가

\- 설치 방법 문서화

\- Runtime 구조 문서화

\- UIController Public API 문서화

\- UIId 및 UIRegistry 규칙 문서화

\- UIScreen Push / Replace / Reset 문서화

\- Screen History 및 Back 정책 문서화

\- Popup Stack 문서화

\- Overlay Layer 문서화

\- Input Blocking 우선순위 문서화

\- UIView Lifecycle 문서화

\- Transition 확장 방법 문서화

\- UIFadeTransition 문서화

\- Runtime Error Code 문서화

\- Editor Tool 사용법 문서화

\- Validation 및 Safe Fix 정책 문서화

\- Basic Usage Sample 문서화

\- Framework 책임 범위 문서화

\- UPM 독립 설치 검증 결과 문서화

