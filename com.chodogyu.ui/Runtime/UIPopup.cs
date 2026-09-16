using UnityEngine;

namespace CDG.UI
{
    /// <summary>
    /// 현재 Screen 위에 일시적으로 표시되는 Popup View입니다.
    /// 여러 Popup이 열리면 UIController가 열린 순서에 따라 Stack으로 관리합니다.
    /// </summary>
    public class UIPopup : UIView
    {
        [SerializeField]
        private bool blocksInput = true;

        /// <summary>
        /// Popup이 열려 있는 동안 자신보다 아래에 있는 UI의 입력을 차단할지 여부를 반환합니다.
        /// 기본값은 true입니다.
        /// </summary>
        public bool BlocksInput => blocksInput;

        internal void SetBlocksInput(bool blocksInput)
        {
            this.blocksInput = blocksInput;
        }
    }
}