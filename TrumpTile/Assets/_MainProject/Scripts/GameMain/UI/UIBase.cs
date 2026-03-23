using TrumpTile.GameMain.Data;
using UnityEngine;
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

            if (PlayerDataManager.Inst != null)
            {
                PlayerDataManager.Inst.OnPlayerDataRefresh += Refresh;
                PlayerDataManager.Inst.OnPlayerLocalDataRefresh += RefreshLocalData;
            }
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
    }
}
