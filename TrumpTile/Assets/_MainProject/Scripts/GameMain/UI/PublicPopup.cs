using System;
using UnityEngine;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
    /// <summary>
    /// 공용으로 사용되는 팝업입니다.
    /// 공용 팝업의 요소로는 '내용', '확인', '취소' 버튼이 존재할 수 있습니다.
    /// 요소들의 존재 여부를 체크하고 초기화 시에 참조를 획득합니다.
    /// 취소 버튼은 단순 팝업 닫기 버튼이고,
    /// 컨펌 버튼은 존재하는 경우 상위 부모에서 기능을 주입해줍니다.
    /// </summary>
    public class PublicPopup : PopupBase
    {
        private const string CONFIRM_BUTTON_NAME = "ConfirmButton";
        private const string CANCLE_BUTTON_NAME = "CancleButton";

        private Button mConfirmButton;
        private Button mCancleButton;

        //상위에서 주입한 컨펌 동작. 초기화 순서(FindObjectsOfType 순서)에 상관없이 유지되도록
        //버튼 리스너가 아니라 이 필드에 보관하고, 클릭 시점에 꺼내 쓴다.
        private Action mConfirmAction;

        public override void Initialize()
        {
            base.Initialize();

            mConfirmButton = FindChildButton(CONFIRM_BUTTON_NAME);
            mCancleButton = FindChildButton(CANCLE_BUTTON_NAME);

            //remove 후 add: 재초기화(RefreshAllViews 등) 시 중복 등록 방지. 명명 메서드라 이 리스너만 정리됨.
            if (mCancleButton != null)
            {
                mCancleButton.onClick.RemoveListener(OnCancleClick);
                mCancleButton.onClick.AddListener(OnCancleClick);
            }
            if (mConfirmButton != null)
            {
                mConfirmButton.onClick.RemoveListener(OnConfirmClick);
                mConfirmButton.onClick.AddListener(OnConfirmClick);
            }
        }

        /// <summary>
        /// 컨펌 버튼이 눌렸을 때 실행할 동작을 주입한다.
        /// 주입된 동작이 있으면 기본 동작(Hide) 대신 그 동작만 실행된다.
        /// (앱 종료 / 스토어 이동처럼 팝업을 닫으면 안 되는 케이스가 있기 때문)
        /// </summary>
        public void AddActionToConfirmButton(Action action)
        {
            if (action == null)
            {
                return;
            }

            mConfirmAction = action;
        }

        private void OnConfirmClick()
        {
            if (mConfirmAction != null)
            {
                mConfirmAction();
                return;
            }

            Hide();
        }

        private void OnCancleClick()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 하위 전체(비활성 포함)에서 이름으로 버튼을 찾는다.
        /// transform.Find는 직계 자식만 보는데 실제 버튼은 연출용 패널(mPopupObj) 아래에 있어
        /// 항상 null이 잡혔고, 그 탓에 주입한 컨펌 동작이 버튼에 아예 연결되지 않았다.
        /// </summary>
        private Button FindChildButton(string buttonName)
        {
            foreach (Button button in GetComponentsInChildren<Button>(true))
            {
                if (button.gameObject.name == buttonName)
                {
                    return button;
                }
            }

            return null;
        }

        /// <summary>
        /// 공용 팝업은 버튼 없이도 노출되는 경우가 있습니다. (ex : 버전 불일치, 네트워크 불안정 등)
        /// 해당 경우를 위한 함수입니다.
        /// </summary>
        public void ShowWithOutButton()
        {
            Show();
        }
    }
}
