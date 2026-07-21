using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;
using UnityEngine;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
    public enum EMiniRewardAnimType
    {
        ViewContent,
        PopupContent,
        Custom,
        None
    }
    public class MiniRewardPayload
    {
        public List<RewardDisplayInfo> Infos;
        public EMiniRewardAnimType Type = EMiniRewardAnimType.None;
        public Vector2 target = Vector2.zero;
        public string parentName;
    }
    public class MiniRewardAnimator : MonoBehaviour
    {
        [System.Serializable]
        public class MiniRewardConfig
        {
            public Image Image;
            public TMP_Text Text;
        }
        [Header("애니메이션 시작 위치")]
        [SerializeField] private float mStartPosY;
        [Header("애니메이션 지속 시간")]
        [SerializeField] private float mDuration = 1f;
        [Header("보상 스프라이트")]
        [SerializeField] private Sprite[] mSpriteArray;
        [Header("연출이 적용될 이미지 / 텍스트")]
        [SerializeField] private MiniRewardConfig[] mMiniRewardConfigArray;
         [Header("보상 이미지 간격")]
        [SerializeField] private float mPlacementWidth;
        private RectTransform mRect;
        private CanvasGroup mCanvasGroup;

        private Sequence animSeq;
        private void Awake()
        {
            mRect = transform.GetComponent<RectTransform>();
            foreach(var item in mMiniRewardConfigArray)
            {
                item.Image.gameObject.SetActive(false);
            }
            mCanvasGroup = transform.GetComponent<CanvasGroup>();

            EventManager.Inst.AddEvent<MiniRewardPayload>(EventKeys.PLAY_MINI_REWARD_ANIM, PlayMiniRewardAnim);
        }
        private void OnDestroy()
        {
            EventManager.Inst?.RemoveEvent<MiniRewardPayload>(EventKeys.PLAY_MINI_REWARD_ANIM, PlayMiniRewardAnim);
        }
        public void PlayMiniRewardAnim(MiniRewardPayload payload)
        {
            string transformName;
            if(payload.Type == EMiniRewardAnimType.None || payload.Type == EMiniRewardAnimType.ViewContent)
            {
                transformName = "Canvas_View";
            }
            else if(payload.Type == EMiniRewardAnimType.PopupContent)
            {
                transformName = "Canvas_Popup";
            }
            else
            {
                transformName = payload.parentName;
            }
            PlayAnim(GameObject.Find(transformName).transform, payload.Infos, payload.target);
        }
        private void PlayAnim(Transform parent, List<RewardDisplayInfo> info, Vector2 target)
        {
            int count = info.Count;
            float pivot = count % 2 == 1? 0 : -(mPlacementWidth / 2);

            float startX = pivot - (mPlacementWidth * ((count - 1) / 2));
            if(animSeq != null && animSeq.active)
            {
                animSeq.Kill();
            }
            foreach(var item in mMiniRewardConfigArray)
            {
                item.Image.gameObject.SetActive(false);
            }

            transform.SetParent(parent);
            Vector2 pos = target == Vector2.zero? Vector2.up * mStartPosY : target;

            mRect.anchoredPosition = pos;
            mCanvasGroup.alpha = 1;
            for(int i = 0; i < count; i++)
            {
                mMiniRewardConfigArray[i].Image.rectTransform.anchoredPosition = new Vector2(startX + mPlacementWidth * i, 0);
                mMiniRewardConfigArray[i].Image.sprite = info[i].Type == ERewardType.Gold? mSpriteArray[0] : mSpriteArray[info[i].ItemId - 1004];
                mMiniRewardConfigArray[i].Text.text = "+" + info[i].Amount.ToString();

                mMiniRewardConfigArray[i].Image.gameObject.SetActive(true);
            }

            animSeq = DOTween.Sequence();
            animSeq.Append(mRect.DOAnchorPosY(pos.y + 100f , mDuration));
            animSeq.Join(mCanvasGroup.DOFade(0, mDuration));
        }
    }   
}
