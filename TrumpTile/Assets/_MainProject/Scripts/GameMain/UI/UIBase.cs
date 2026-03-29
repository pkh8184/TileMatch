using TMPro;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
    public class UIBase : MonoBehaviour
    {
        [Header("View 혹은 Popup을 켜고 끄는 버튼\n(켜거나 끄지 않는 오브젝트인 경우 할당 X)")]
        [SerializeField] private Button showButton;
        [SerializeField] private Button hideButton;

        //씬 매니저가 씬에 존재하는 모든 UIBase를 순회하여 호출
        public virtual void Initialize()
        {
            if (showButton != null)
            {
                showButton.onClick.AddListener(Show);
            }
            if (hideButton != null)
            {
                hideButton.onClick.AddListener(Hide);
            }

            EventManager.Inst.AddEvent(RequestEventKeys.REFRESH_PLAYER_DATA, (obj) => Refresh());
            EventManager.Inst.AddEvent(RequestEventKeys.REFRESH_PLAYER_LOCAL_DATA, (obj) => RefreshLocalData());

            Refresh();
            //RefreshLocalData();

            SetTMP_TextIsRTL();
        }

        protected virtual void Show()
        {
            gameObject.SetActive(true);
        }

        protected virtual void Hide()
        {
            gameObject.SetActive(false);
        }
        protected virtual void Refresh() { }
        protected virtual void RefreshLocalData() { }
        
        /// <summary>
        /// 현재 언어가 아랍어로 설정된 경우 TMP_Text의 IsRTL을 true로 해줌. 
        /// 오른쪽에서부터 텍스트 시작
        /// </summary>
        private void SetTMP_TextIsRTL()
        {
            if(LocalizationSettings.SelectedLocale.Identifier.Code != "ar")
            {
                return;
            }

            foreach(TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.GetComponent<IgnoreRTL>() != null) continue;
                text.isRightToLeftText = true;
            }
        }
    }
}
