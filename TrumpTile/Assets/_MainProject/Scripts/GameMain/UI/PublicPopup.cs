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

        private Button mConfirmButton;
        private Button mCancleButton;

        public override void Initialize()
        {
            base.Initialize();

            mConfirmButton = transform.Find("ConfirmButton")?.GetComponent<Button>();
            mCancleButton = transform.Find("CancleButton")?.GetComponent<Button>();

            if (mCancleButton != null)
            {
                mCancleButton.onClick.AddListener(() => gameObject.SetActive(false));
            }
            if(mConfirmButton != null)
            {
                mConfirmButton.onClick.AddListener(Hide);
            }
        }

        public void AddActionToConfirmButton(Action action)
        {
            if(mConfirmButton != null)
            {
                if(action == null)
                {
                    return;
                }
                else
                {
                    mConfirmButton.onClick.RemoveAllListeners();
                    mConfirmButton.onClick.AddListener(() => action());
                }
            }
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

