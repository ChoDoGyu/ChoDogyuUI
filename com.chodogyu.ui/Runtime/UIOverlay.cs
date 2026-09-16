using UnityEngine;

namespace CDG.UI
{
    /// <summary>
    /// Screen Navigation과 Popup Stack에 포함되지 않고 독립적으로 표시되는 Overlay View입니다.
    /// 표시 위치에 따라 일반 Overlay 또는 최상위 Overlay 계층에 배치할 수 있습니다.
    /// </summary>
    public class UIOverlay : UIView
    {
        [SerializeField]
        private UIOverlayLayer overlayLayer = UIOverlayLayer.Normal;

        [SerializeField]
        private bool blocksInput;

        /// <summary>
        /// Overlay가 배치될 표시 계층을 반환합니다.
        /// </summary>
        public UIOverlayLayer OverlayLayer => overlayLayer;

        /// <summary>
        /// Overlay가 열려 있는 동안 자신보다 아래에 있는 UI의 입력을 차단할지 여부를 반환합니다.
        /// 기본값은 false입니다.
        /// </summary>
        public bool BlocksInput => blocksInput;

        internal void SetOverlayLayer(UIOverlayLayer overlayLayer)
        {
            this.overlayLayer = overlayLayer;
        }

        internal void SetBlocksInput(bool blocksInput)
        {
            this.blocksInput = blocksInput;
        }
    }
}