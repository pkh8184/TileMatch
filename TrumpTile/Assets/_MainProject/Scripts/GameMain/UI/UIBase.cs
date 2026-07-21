using TMPro;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
    public interface IPurchasable
    {
        /// <summary>
        /// 이벤트를 일괄 호출하기 때문에, 현재 활성화되지 않은 구매 가능 UI의 경우 그에 대한 가드 처리 구현해야함
        /// </summary>
        public void OnPurchaseSuccess();
    }
    public class UIBase : MonoBehaviour
    {
        [Header("View 혹은 Popup을 켜고 끄는 버튼\n(켜거나 끄지 않는 오브젝트인 경우 할당 X)")]
        [SerializeField] protected Button mShowButton;
        [SerializeField] protected Button mHideButton;

        private CanvasGroup mCanvasGroup;

        /// <summary>
        /// UI 전체의 입력 차단/허용. 연출 진행 중 하위 버튼 클릭을 막을 때 사용.
        /// 하위 모든 Selectable(버튼/토글)이 한 번에 비활성화됨.
        /// </summary>
        protected void SetInteractable(bool interactable)
        {
            if (mCanvasGroup == null)
            {
                mCanvasGroup = GetComponent<CanvasGroup>();
                if (mCanvasGroup == null)
                {
                    mCanvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            mCanvasGroup.interactable = interactable;
        }

        //씬 매니저가 씬에 존재하는 모든 UIBase를 순회하여 호출
        public virtual void Initialize()
        {
            if (mShowButton != null)
            {
                mShowButton.onClick.AddListener(Show);
            }
            if (mHideButton != null)
            {
                mHideButton.onClick.AddListener(Hide);
            }

            SubscribeEvent();

            //로컬 데이터(프로필 이미지, 프레임 등) 연결 되면 주석 해제
            RefreshLocalData();
            Refresh();
            
            //씬에 존재하는 모든 버튼, 토글에 클릭 효과음 추가
            foreach(var button in GetComponentsInChildren<Button>(true))
            {
                button.onClick.AddListener(() => AudioEvent.Play(EAudioKey.SFX_BtnClick));
            }
            foreach(var toggle in GetComponentsInChildren<Toggle>(true))
            {
                toggle.onValueChanged.AddListener((isOn) => AudioEvent.Play(EAudioKey.SFX_BtnClick));
            }
            //연출 락 등으로 인터랙터블이 꺼져도 회색으로 변하지 않도록 Disabled 색을 Normal과 동일하게 맞춤
            foreach(var selectable in GetComponentsInChildren<Selectable>(true))
            {
                ColorBlock colors = selectable.colors;
                colors.disabledColor = colors.normalColor;
                selectable.colors = colors;
            }

            RefreshLanguage();
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
            EventManager.Inst.AddEvent(RequestEventKeys.REFRESH_PLAYER_DATA, Refresh);
            //EventManager.Inst.AddEvent("PurchaseConfirmed", Refresh);
            EventManager.Inst.AddEvent(RequestEventKeys.REFRESH_PLAYER_LOCAL_DATA, RefreshLocalData);
            EventManager.Inst.AddEvent(RequestEventKeys.REFRESH_LANGUAGE, OnRefreshLanguage);
        }
        protected virtual void UnSubscribeEvent()
        {
            EventManager.Inst?.RemoveEvent(RequestEventKeys.REFRESH_PLAYER_DATA, Refresh);
            //EventManager.Inst?.RemoveEvent("PurchaseConfirmed", Refresh);
            EventManager.Inst?.RemoveEvent(RequestEventKeys.REFRESH_PLAYER_LOCAL_DATA, RefreshLocalData);
            EventManager.Inst?.RemoveEvent(RequestEventKeys.REFRESH_LANGUAGE, OnRefreshLanguage);
        }
        public void Deinitialize()
        {
            UnSubscribeEvent();

            //Initialize에서 등록한 런타임 onClick 리스너를 정리한다.
            //(RefreshAllViews 등으로 재초기화될 때 리스너가 중복 누적되어 한 번 클릭에 여러 번 실행되는 것 방지)
            if (mShowButton != null)
            {
                mShowButton.onClick.RemoveAllListeners();
            }
            if (mHideButton != null)
            {
                mHideButton.onClick.RemoveAllListeners();
            }
            foreach (Button button in GetComponentsInChildren<Button>(true))
            {
                button.onClick.RemoveAllListeners();
            }
            foreach (Toggle toggle in GetComponentsInChildren<Toggle>(true))
            {
                toggle.onValueChanged.RemoveAllListeners();
            }
        }
        private void OnRefreshLanguage()
        {
            Refresh();
            RefreshLanguage();
        }
        private void RefreshLanguage()
        {
            foreach (TextLocalizeSetter setter in GetComponentsInChildren<TextLocalizeSetter>(true))
            {
                setter.Initialize();
            }

        }
    }
}
