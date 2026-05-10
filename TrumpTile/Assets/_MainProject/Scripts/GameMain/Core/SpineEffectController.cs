using System;
using UnityEngine;
using Spine;
using Spine.Unity;

namespace TrumpTile.GameMain.Core
{
	// Spine 이펙트 프리팹에 부착.
	// Spine 에디터에서 삽입한 이벤트(mActionEventName)가 발화되면 OnActionPoint를 호출.
	// 애니메이션이 완료되면 OnEffectComplete를 호출하고 자동 파괴.
	public class SpineEffectController : MonoBehaviour
	{
		[SerializeField] private string mAnimationName = "animation";
		[Tooltip("Spine 에디터에서 설정한 이벤트 이름 (게임 로직 발화 시점)")]
		[SerializeField] private string mActionEventName = "action";
		[SerializeField] private Vector3 mScale = Vector3.one;
		[SerializeField] private Vector3 mPositionOffset = Vector3.zero;

		public event Action OnActionPoint;
		public event Action OnEffectComplete;

		private void Start()
		{
			transform.localScale = mScale;
			transform.position += mPositionOffset;

			SkeletonAnimation skeletonAnimation = GetComponent<SkeletonAnimation>();
			if (skeletonAnimation == null)
			{
				OnActionPoint?.Invoke();
				OnEffectComplete?.Invoke();
				Destroy(gameObject);
				return;
			}

			skeletonAnimation.AnimationState.Event += OnSpineEvent;

			TrackEntry entry = skeletonAnimation.AnimationState.SetAnimation(0, mAnimationName, false);
			if (entry != null)
			{
				entry.Complete += OnAnimationComplete;
			}
			else
			{
				OnActionPoint?.Invoke();
				OnEffectComplete?.Invoke();
				Destroy(gameObject);
			}
		}

		private void OnSpineEvent(TrackEntry trackEntry, Spine.Event e)
		{
			if (e.Data.Name == mActionEventName)
			{
				OnActionPoint?.Invoke();
			}
		}

		private void OnAnimationComplete(TrackEntry trackEntry)
		{
			OnEffectComplete?.Invoke();
			Destroy(gameObject);
		}
	}
}
