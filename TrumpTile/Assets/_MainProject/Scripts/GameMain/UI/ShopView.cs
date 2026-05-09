using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TrumpTile.GameMain.Data;
using DG.Tweening;
using TrumpTile.GameMain.Core;

namespace TrumpTile.GameMain.UI
{
    public class ShopView : ViewBase
    {
        [Header("목록에 맞는 제품 생성을 위한 프리팹")]
        [SerializeField] private GameObject mBundlePrefab;
        [SerializeField] private GameObject mSinglePrefab;

        [Header("ShopView 접근 시 애니메이션 효과를 주기 위한 RectTransform 참조")]
        [SerializeField] private RectTransform mUIContainerTransform;
        [SerializeField] private float mAnimDuration = 0.5f;

        public override void Show()
        {
            base.Show();

            AdManager.Inst.HideBannerAd();
            StartCoroutine(Co_PlayPackageShowAnim());
        }
        public override void Hide()
        {
            base.Hide();

            AdManager.Inst.ShowBannerAd();
        }
        private IEnumerator Co_PlayPackageShowAnim()
        {
            VerticalLayoutGroup layoutGroup = mUIContainerTransform.GetComponent<VerticalLayoutGroup>();

            layoutGroup.enabled = false;

            Sequence sequence = DOTween.Sequence();
            for (int i = 0; i < mUIContainerTransform.childCount; i++)
            {
                RectTransform rect = mUIContainerTransform.GetChild(i).GetComponent<RectTransform>();

                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x + Screen.width, rect.anchoredPosition.y);
                sequence.Insert(i * 0.1f, rect.DOAnchorPosX(rect.anchoredPosition.x - Screen.width, 0.3f).SetEase(Ease.OutQuad));
            }
            sequence.OnComplete(() => layoutGroup.enabled = true);

            yield return sequence.WaitForCompletion();
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

