using UnityEngine;
using DG.Tweening;
using System.Collections;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class PopupBase : UIBase
    {
        //열려 있는 팝업 수. 서브클래스가 직접 건드리지 못하도록 private으로 두고
        //MarkOpen/MarkClosed/ResetOpenPopupCount로만 조작한다. (중복 증감으로 카운트가 새는 것을 막기 위함)
        private static int mOpenPopupCount = 0;
        public static bool IsAnyPopupOpen => mOpenPopupCount > 0;

        [Header("Popup 켜기, 끄기 애니메이션 길이(초)")]
        [SerializeField] protected float mShowDuration = 1f;
        [SerializeField] protected float mHideDuration = 1f;

        [Header("Show / Hide 애니메이션을 적용할 실제 팝업창")]
        [SerializeField] protected GameObject mPopupObj;

        protected Sequence mCurrentSeq;

        //이 팝업이 mOpenPopupCount에 반영돼 있는지. 중복 증가/감소를 막는다.
        private bool mbCounted;

        //Show 연출 때문에 입력을 막아둔 상태인지.
        //연출이 중간에 끊겨도 복구할 수 있도록 트윈과 별개로 들고 있는다.
        private bool mbShowAnimLock;

        public override void Initialize()
        {
            base.Initialize();

            mOpenPopupCount = 0;
            mbCounted = false;
            mbShowAnimLock = false;
        }

        public override void Show()
        {
            base.Show();
            MarkOpen();

            PlayShowAnim();
        }

        public override void Hide()
        {
            SetInteractable(false);
            PlayHideAnim();
        }

        /// <summary>이 팝업을 '열림'으로 집계한다. 이미 집계돼 있으면 무시한다.</summary>
        protected void MarkOpen()
        {
            if (mbCounted)
            {
                return;
            }

            mbCounted = true;
            mOpenPopupCount++;
        }

        /// <summary>
        /// 이 팝업을 '닫힘'으로 집계한다. 여러 번 호출해도 안전하다.
        /// (연출 완료 콜백과 OnDisable 양쪽에서 불리므로 반드시 멱등해야 한다)
        /// </summary>
        protected void MarkClosed()
        {
            if (!mbCounted)
            {
                return;
            }

            mbCounted = false;
            mOpenPopupCount = Mathf.Max(0, mOpenPopupCount - 1);
        }

        /// <summary>열린 팝업 집계를 통째로 초기화한다. (씬 이탈 등 강제 정리용)</summary>
        protected static void ResetOpenPopupCount()
        {
            mOpenPopupCount = 0;
        }

        protected virtual void OnDisable()
        {
            //Sequence.Kill()은 OnComplete를 호출하지 않는다.
            //집계를 연출 완료 콜백에만 맡기면 연출이 끊겼을 때 카운트가 새고,
            //IsAnyPopupOpen이 영구히 true가 되어 타일 입력까지 계속 막힌다.
            //실제로 꺼지는 이 시점에서 확정 정리한다.
            KillCurrentSeq();
            mbShowAnimLock = false;
            MarkClosed();
        }

        protected virtual void OnDestroy()
        {
            KillCurrentSeq();
        }

        /// <summary>
        /// 백그라운드 복귀 시, Show 연출 트윈이 완료 콜백 없이 사라졌다면 입력 잠금을 푼다.
        /// 이 복구가 없으면 CanvasGroup.interactable이 false로 고정되는데,
        /// UIBase에서 disabledColor를 normalColor로 맞춰놔 눈으로는 정상 팝업과 구분되지 않는다.
        /// (= 멀쩡해 보이는데 눌리지 않는 버튼)
        /// </summary>
        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                return;
            }
            if (!mbShowAnimLock)
            {
                return;
            }
            //연출이 아직 살아서 진행 중이면 그대로 두고 OnComplete에 맡긴다.
            if (mCurrentSeq != null && mCurrentSeq.IsActive())
            {
                return;
            }

            mbShowAnimLock = false;
            SetInteractable(true);
        }

        protected void KillCurrentSeq()
        {
            if (mCurrentSeq == null)
            {
                return;
            }

            mCurrentSeq.Kill();
            mCurrentSeq = null;
        }

        protected virtual void PlayShowAnim()
        {
            if (mPopupObj == null) return;

            KillCurrentSeq();
            mPopupObj.transform.localScale = Vector2.zero;

            //연출 중 오클릭 방지. 해제는 OnComplete 또는 OnApplicationPause 복구가 담당한다.
            mbShowAnimLock = true;
            SetInteractable(false);

            mCurrentSeq = DOTween.Sequence();
            mCurrentSeq.SetUpdate(true);
            PopupScaleAnimator.AppendPopIn(mCurrentSeq, mPopupObj.transform, mShowDuration);
            mCurrentSeq.OnComplete(() =>
            {
                mbShowAnimLock = false;
                SetInteractable(true);
            });
        }

        protected virtual void PlayHideAnim()
        {
            if (mPopupObj == null) return;

            KillCurrentSeq();
            mbShowAnimLock = false;

            mCurrentSeq = DOTween.Sequence();
            mCurrentSeq.SetUpdate(true);
            mCurrentSeq.Append(mPopupObj.transform.DOScale(0, mHideDuration).SetEase(Ease.InBack));
            mCurrentSeq.OnComplete(() =>
            {
                MarkClosed();
                gameObject.SetActive(false);
            });
        }
    }
}
