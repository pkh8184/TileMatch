using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TrumpTile.GameMain.Data;
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
        //젬이 앵커된 그리드 레이어. originList는 모두 이 레이어의 2×2 footprint 셀이다. 못 구하면 -1.
        public int GemLayer => (mOriginIndexList != null && mOriginIndexList.Count > 0) ? mOriginIndexList[0].Item3 : -1;
        private RectTransform mTargetRect;
        private bool mbIsAnim;
        private Sequence mSeq;
        private int mCapturedGemCount;
        //획득 시점에 확정한 젬 배율(2배 버프). 연출 도중 버프가 만료돼도 값이 흔들리지 않도록 캡처해서 쓴다.
        private int mGemMultiplier = 1;
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
        public bool CheckCanCollect()
        {
            if(mbIsAnim)
            {
                return false;
            }
            if(BoardManager.Instance.CheckBoardMapEmpty(mCheckIndexList, mOriginIndexList, this))
            {
                mbIsAnim = true;
                Collect();
                return true;
            }
            return false;
        }
        public void Collect()
        {
            //젬 2배 버프가 켜져 있으면 획득 수치에 배율을 적용한다.
            //날아가는 스프라이트 개수는 프리팹에 고정돼 있으므로 연출은 그대로 두고 수치만 배로 올린다.
            mGemMultiplier = PlayerDataManager.Inst != null ? PlayerDataManager.Inst.GemMultiplier : 1;

            PlayCollectAnim();
            mCapturedGemCount = CoreContainer.RewardContainer.Gem;
            CoreContainer.RewardContainer.AddGem(mGemCount * mGemMultiplier);
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

            int gemCount = 0;
            for(int i = mGemCount; i >= 1; i--)
            {
                Transform gem = mSpriteRendererArray[i].transform;
                gem.SetParent(null);
                Vector3 target = new Vector3(world.x,world.y, gem.position.z);
                seq.Insert(0.1f * (i - 1), gem.DOMove(target, 0.4f).SetEase(Ease.InBack));
                seq.Join(gem.DOScale(0.08f, 0.4f));
                seq.InsertCallback((0.1f * (i-1)) + 0.4f, () =>
                {
                    gemCount++;
                    gem.gameObject.SetActive(false);
                    //배율을 곱해야 연출이 끝났을 때 HUD 표기가 RewardContainer.Gem과 일치한다.
                    EventManager.Inst.ActiveEvent(EventKeys.COLLECT_GEM, mCapturedGemCount + (gemCount * mGemMultiplier));
                    AudioEvent.Play(EAudioKey.SFX_Ingame_Collect_Gem);
                });
            }
            seq.Append(mSpriteRendererArray[0].DOFade(0, 0.3f));
            seq.OnComplete(() =>
            {
                gameObject.SetActive(false);

                //젬이 완전히 사라진 뒤 블록 상태를 다시 계산한다.
                //수집 시점의 UpdateAllBlockedStates는 이 젬 오브젝트가 아직 살아 있어서
                //IsCoveredByActiveGem이 true였고, 아래 레이어 타일들이 선택 불가로 굳어버린다.
                //여기서 갱신하지 않으면 다른 타일을 제거하기 전까지 그 타일들이 계속 안 눌린다.
                BoardManager.Instance?.UpdateAllBlockedStates();
            });
        }
    }   
}
