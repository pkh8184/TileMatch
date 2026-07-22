using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;
using UnityEngine;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
    public class ExcitTravelRewardUI : MonoBehaviour
    {
        [Header("커버 이미지 / 적용할 커버 스프라이트")]
        [SerializeField] private Image mCoverImage;
        [SerializeField] private Sprite mFreeSprite;
        [SerializeField] private Sprite mPaidSprite;
        [SerializeField] private Sprite mFreeButtonSprite;
        [SerializeField] private Sprite mPaidButtonSprite;
        [Header("구성 요소 프리팹 / 부모 트랜스폼")]
        [SerializeField] private GameObject mElementPrefab;
        [SerializeField] private Transform mElementParent;
        [Header("구성 요소 스프라이트")]
        [SerializeField] private Sprite[] mElementSpriteArray;

        [Header("컨펌 버튼 / lock 오브젝트")]
        [SerializeField] private Button mConfirmButton;
        [SerializeField] private GameObject mLockObject;
        private TMP_Text mButtonText;
        private bool mbIsFree;
        public bool IsFree => mbIsFree;
        public void Initialize(bool isFree, List<RewardDisplayInfo> info, Action action, string cost)
        {
            foreach(var item in info)
            {
                GameObject obj = Instantiate(mElementPrefab, mElementParent);
                
                Image image = obj.GetComponent<Image>();
                if(item.Type == ERewardType.Gold)
                {
                    image.sprite = mElementSpriteArray[0];
                }
                else if(item.Type == ERewardType.Item)
                {
                    image.sprite = mElementSpriteArray[item.ItemId - 1004];
                }
                TMP_Text text = obj.GetComponentInChildren<TMP_Text>();
                text.text = "X" + item.Amount.ToString();
            }

            mConfirmButton?.onClick.AddListener(() => action());
            mButtonText = mConfirmButton.GetComponentInChildren<TMP_Text>();
            mButtonText.text = cost;
            if(isFree)
            {
                mCoverImage.sprite = mFreeSprite;
                mConfirmButton.image.sprite = mFreeButtonSprite;
            }
            else
            {
                mCoverImage.sprite = mPaidSprite;
                mConfirmButton.image.sprite = mPaidButtonSprite;
            }

            mButtonText.gameObject.SetActive(false);
            mConfirmButton.interactable = false;
            mConfirmButton.image.color = Color.gray;
            mLockObject.SetActive(true);
            mbIsFree = isFree;
        }
        public void SetValid()
        {
            mButtonText.gameObject.SetActive(true);
            mConfirmButton.interactable = true;
            mConfirmButton.image.color = Color.white;

            mLockObject.SetActive(false);
        }
        public void SetLocalize()
        {
            if(mbIsFree)
            {
                mButtonText.text = LocalizeManager.Inst.GetString(200102);
                mButtonText.font = LocalizeManager.Inst.GetFontAssetByLocale();

                //아랍어면 RTL 적용
                LocalizeManager.Inst.ApplyRTL(mButtonText);
            }
        }
    }   
}
