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

        protected void OnDestroy()
        {
            DeSubscribeEvent();
        }
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

            SubscribeEvent();

            Refresh();
            //로컬 데이터(프로필 이미지, 프레임 등) 연결 되면 주석 해제
            RefreshLocalData();

            //씬에 존재하는 모든 버튼, 토글에 클릭 효과음 추가
            foreach(var button in GetComponentsInChildren<Button>(true))
            {
                button.onClick.AddListener(() => AudioEvent.Play(EAudioKey.SFX_BtnClick));
            }
            foreach(var toggle in GetComponentsInChildren<Toggle>(true))
            {
                toggle.onValueChanged.AddListener((isOn) => AudioEvent.Play(EAudioKey.SFX_BtnClick));
            }
            //로칼리이제이션 테이블 작성 되면 주석 해제
            //SetTMP_TextIsRTL();
            //EventManager.Inst.AddEvent(RequestEventKeys.REFRESH_LANGUAGE, (obj) => SetTMP_TextIsRTL());
        }

        public virtual void Show()
        {
            gameObject.SetActive(true);
        }

        public virtual void Hide()
        {
            gameObject.SetActive(false);
        }
        protected virtual void Refresh() { }
        protected virtual void RefreshLocalData() { }
        
        protected virtual void SubscribeEvent()
        {
            EventManager.Inst.AddEvent(RequestEventKeys.REFRESH_PLAYER_DATA, (obj) => Refresh());
            EventManager.Inst.AddEvent(RequestEventKeys.REFRESH_PLAYER_LOCAL_DATA, (obj) => RefreshLocalData());
        }
        protected virtual void DeSubscribeEvent()
        {
            EventManager.Inst?.RemoveEvent(RequestEventKeys.REFRESH_PLAYER_DATA);
            EventManager.Inst?.RemoveEvent(RequestEventKeys.REFRESH_PLAYER_LOCAL_DATA);
        }
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
