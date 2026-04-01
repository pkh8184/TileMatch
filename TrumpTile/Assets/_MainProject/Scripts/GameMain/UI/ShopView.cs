using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TrumpTile.GameMain.Data;
using DG.Tweening;

namespace TrumpTile.GameMain.UI
{
    public class ShopView : ViewBase
    {
        [Header("ShopView 텍스트")]
        [SerializeField] private TMP_Text mGoldText;
        [SerializeField] private TMP_Text mBlackholeText;
        [SerializeField] private TMP_Text mTimerText;
        [SerializeField] private TMP_Text mBombText;

        [Header("목록에 맞는 제품 생성을 위한 프리팹")]
        [SerializeField] private GameObject mBundlePrefab;
        [SerializeField] private GameObject mSinglePrefab;

        [Header("ShopView 접근 시 애니메이션 효과를 주기 위한 RectTransform 참조")]
        [SerializeField] private RectTransform mUIContainerTransform;
        [SerializeField] private float mAnimDuration = 0.5f;

        protected override void Refresh()
        {
            mGoldText.text = PlayerDataManager.Inst.GetDataToString(EPlayerDataType.Gold);
            mBlackholeText.text = PlayerDataManager.Inst.GetDataToString(EPlayerDataType.BlackHole);
            mTimerText.text = PlayerDataManager.Inst.GetDataToString(EPlayerDataType.Timer);
            mBombText.text = PlayerDataManager.Inst.GetDataToString(EPlayerDataType.Bomb);
        }
        public override void Show()
        {
            base.Show();

            mUIContainerTransform.anchoredPosition = new Vector2(mUIContainerTransform.anchoredPosition.x, Screen.height * 2);
            mUIContainerTransform.DOAnchorPos(Vector3.zero, mAnimDuration);
        }
        /// <summary>
        /// 제품 목록에 존재하는 제품들의 UI를 종류에 맞게 생성합니다.
        /// 생성과 동시에 구매 버튼에 제품의 가격으로 구매 이벤트를 추가해줍니다.
        /// </summary>
        private void CreateProduct()
        {

        }
    }
}

