using UnityEngine;
using TMPro;
using TrumpTile.GameMain.Data;
using TrumpTile.GameMain.Core;

namespace TrumpTile.GameMain.UI
{
    public enum EPlayerDataType
    {
        //재화
        Gold,
        Star,
        //아이템
        Bomb,
        BlackHole,
        Timer,
        //스테이지
        CurrentStage,
        FirstTryClearCount,
        MaxStreakClearStageCount,
        ClearedStage,
        //하우징
        CurrentHousingChapter,
        CurrentHousingSubChapter,
        CompletedChapterCount,
        //로그인
        MaxStreakLoginCount,
        FirstLoginDate
    }
    public class PlayerDataView : MonoBehaviour
    {
        [SerializeField] protected EPlayerDataType mPlayerDataType;

        protected TMP_Text mValueText;

        protected virtual void Awake()
        {
            mValueText = transform.Find("TMP_Amount").GetComponent<TMP_Text>();
        }
        private void OnEnable()
        {
            Refresh();
            EventManager.Inst.AddEvent(RequestEventKeys.REFRESH_UI, Refresh);
        }
        private void OnDisable()
        {
            EventManager.Inst.RemoveEvent(RequestEventKeys.REFRESH_UI);
        }
        protected virtual void Refresh(object obj = null)
        {
            mValueText.text = PlayerDataManager.Inst.GetDataToString(mPlayerDataType);
        }
    }
}
