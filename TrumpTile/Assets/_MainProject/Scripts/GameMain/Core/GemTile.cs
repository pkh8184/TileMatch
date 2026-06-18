using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace TrumpTile.GameMain.Core
{
    public class GemTile : MonoBehaviour
    {
        [Header("소팅 오더 조절할 스프라이트 렌더러들")]
        [SerializeField] private SpriteRenderer[] mSpriteRendererArray;
        private int mGemCount;
        private List<(int,int,int)> mCheckIndexList;
        private List<(int,int,int)> mOriginIndexList;
        private RectTransform mTargetRect;
        private bool mbIsAnim;
        private Sequence mSeq;
        public void Initialize(int count, List<(int,int,int)> checkList, List<(int,int,int)> originList, int layer, Vector3 pos, Vector3 scale)
        {
            mGemCount = count;
            mCheckIndexList = checkList;
            mOriginIndexList = originList;
            for(int i = 0; i < mSpriteRendererArray.Length; i++)
            {
                mSpriteRendererArray[i].sortingOrder = layer + i;
            }
            transform.position = pos;
            transform.localScale = scale;

            for(int i = 1; i < mSpriteRendererArray.Length - 1; i++)
            {
                if(i <= mGemCount)
                {
                    mSpriteRendererArray[i].gameObject.SetActive(true);
                }
                else
                {
                    mSpriteRendererArray[i].gameObject.SetActive(false);
                }
            }
            mTargetRect = GameObject.Find("Gem").transform.GetChild(0).GetComponent<RectTransform>();
        }
        public void InputInteraction()
        {
            if(mbIsAnim)
            {
                return;
            }
            if(mSeq != null && mSeq.active)
            {
                mSeq.Kill();
            }
            transform.rotation = Quaternion.identity;
            mSeq = DOTween.Sequence();

            mSeq.Append(transform.DORotate(new Vector3(0, 0, 10f), 0.1f).SetRelative().SetEase(Ease.InOutSine));
            mSeq.Append(transform.DORotate(new Vector3(0, 0, -20f), 0.2f).SetRelative().SetEase(Ease.InOutSine));
            mSeq.Append(transform.DORotate(new Vector3(0, 0, 10f), 0.1f).SetRelative().SetEase(Ease.InOutSine));

            AudioEvent.Play(EAudioKey.SFX_Ingame_GemBox_Interaction);
        }
        public void CheckCanCollect()
        {
            if(mbIsAnim)
            {
                return;
            }
            if(BoardManager.Instance.CheckBoardMapEmpty(mCheckIndexList, mOriginIndexList, this))
            {
                mbIsAnim = true;
                Collect();
            }
        }
        public void Collect()
        {
            PlayCollectAnim();
        }
        private void PlayCollectAnim()
        {
            Sequence seq = DOTween.Sequence();
            seq.Append(transform.DORotate(new Vector3(0, 0, 10f), 0.1f).SetRelative().SetEase(Ease.InOutSine));
            seq.Append(transform.DORotate(new Vector3(0, 0, -20f), 0.2f).SetRelative().SetEase(Ease.InOutSine));
            seq.Append(transform.DORotate(new Vector3(0, 0, 10f), 0.1f).SetRelative().SetEase(Ease.InOutSine));
            seq.AppendInterval(0.1f);

            seq.AppendCallback(() => AudioEvent.Play(EAudioKey.SFX_Ingame_GemBox_Open));
            Transform cover = mSpriteRendererArray[mSpriteRendererArray.Length - 1].transform;
            seq.Append(cover.DORotate(new Vector3(0, 0, -20f), 0.3f).SetRelative());
            seq.Join(cover.DOLocalMove(new Vector3(0.7f, 0.7f, 0), 0.3f));
            seq.Join(mSpriteRendererArray[mSpriteRendererArray.Length - 1].DOFade(0, 0.3f));
            
            seq.OnComplete(() => PlayFlyAnim());
            
        }
        private void PlayFlyAnim()
        {
            Camera gameCam = Camera.main;

            Vector3 screen = RectTransformUtility.WorldToScreenPoint(gameCam, mTargetRect.position);
            Vector3 world  = gameCam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, gameCam.nearClipPlane));

            Sequence seq = DOTween.Sequence();

            for(int i = mGemCount; i >= 1; i--)
            {
                Transform gem = mSpriteRendererArray[i].transform;
                gem.SetParent(null);
                Vector3 target = new Vector3(world.x,world.y, gem.position.z);
                seq.Insert(0.1f * (i - 1), gem.DOMove(target, 0.4f).SetEase(Ease.InBack));
                seq.Join(gem.DOScale(0.08f, 0.4f));
                seq.InsertCallback((0.1f * (i-1)) + 0.4f, () =>
                {
                    gem.gameObject.SetActive(false);
                    CoreContainer.GetGemCount++;
                    EventManager.Inst.ActiveEvent("CollectGem");
                    AudioEvent.Play(EAudioKey.SFX_Ingame_Collect_Gem);
                });
            }
            seq.Append(mSpriteRendererArray[0].DOFade(0, 0.3f));
            seq.OnComplete(() => gameObject.SetActive(false));
        }
    }   
}
