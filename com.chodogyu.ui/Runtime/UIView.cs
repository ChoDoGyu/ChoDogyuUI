using UnityEngine;

namespace CDG.UI
{
    /// <summary>
    /// CDG.UI에서 관리하는 모든 UI View의 기본 클래스입니다.
    /// UI 식별자와 현재 상태를 보유하며, Screen, Popup, Overlay가 공통으로 사용하는 수명주기 훅을 제공합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class UIView : MonoBehaviour
    {
        [SerializeField]
        private UIId id;

        private CanvasGroup canvasGroup;
        private UIViewState state = UIViewState.Closed;

        /// <summary>
        /// Registry와 Runtime에서 이 View를 식별하는 고유 ID를 반환합니다.
        /// </summary>
        public UIId Id => id;

        /// <summary>
        /// 현재 View의 열림 및 닫힘 진행 상태를 반환합니다.
        /// </summary>
        public UIViewState State => state;

        /// <summary>
        /// View가 완전히 열린 상태인지 여부를 반환합니다.
        /// Opening 상태는 포함하지 않습니다.
        /// </summary>
        public bool IsOpen => state == UIViewState.Open;

        /// <summary>
        /// View가 열리거나 닫히는 전환 과정에 있는지 여부를 반환합니다.
        /// </summary>
        public bool IsTransitioning => state == UIViewState.Opening || state == UIViewState.Closing;

        internal CanvasGroup CanvasGroup
        {
            get
            {
                if (canvasGroup == null)
                {
                    canvasGroup = GetComponent<CanvasGroup>();
                }

                return canvasGroup;
            }
        }

        internal void SetId(UIId id)
        {
            this.id = id;
        }

        internal void SetState(UIViewState state)
        {
            this.state = state;
        }

        internal void InvokeOpening()
        {
            OnOpening();
        }

        internal void InvokeOpened()
        {
            OnOpened();
        }

        internal void InvokeClosing()
        {
            OnClosing();
        }

        internal void InvokeClosed()
        {
            OnClosed();
        }

        /// <summary>
        /// View가 열리기 시작할 때 호출됩니다.
        /// GameObject가 활성화되기 전에 View 데이터를 준비해야 하는 경우 재정의할 수 있습니다.
        /// </summary>
        protected virtual void OnOpening()
        {
        }

        /// <summary>
        /// View의 열기 처리가 모두 완료되어 Open 상태가 된 후 호출됩니다.
        /// </summary>
        protected virtual void OnOpened()
        {
        }

        /// <summary>
        /// View가 닫히기 시작할 때 호출됩니다.
        /// 닫힘 Transition이 시작되기 전에 필요한 처리를 구현할 수 있습니다.
        /// </summary>
        protected virtual void OnClosing()
        {
        }

        /// <summary>
        /// View의 닫기 처리가 모두 완료되어 Closed 상태가 된 후 호출됩니다.
        /// </summary>
        protected virtual void OnClosed()
        {
        }
    }
}