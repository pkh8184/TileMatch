using UnityEngine;
using Spine;
using Spine.Unity;

namespace TrumpTile.GameMain.Core
{
	// Spine 이펙트 프리팹에 부착. Start 시 지정 애니메이션을 재생하고 완료되면 자동 파괴.
	public class SpineEffectController : MonoBehaviour
	{
		[SerializeField] private string mAnimationName = "animation";
		[SerializeField] private Vector3 mScale = Vector3.one;
		[SerializeField] private Vector3 mPositionOffset = Vector3.zero;

		private void Start()
		{
			transform.localScale = mScale;
			transform.position += mPositionOffset;

			SkeletonAnimation skeletonAnimation = GetComponent<SkeletonAnimation>();
			if (skeletonAnimation == null)
			{
				Destroy(gameObject);
				return;
			}

			TrackEntry entry = skeletonAnimation.AnimationState.SetAnimation(0, mAnimationName, false);
			if (entry != null)
			{
				entry.Complete += OnAnimationComplete;
			}
			else
			{
				Destroy(gameObject);
			}
		}

		private void OnAnimationComplete(TrackEntry trackEntry)
		{
			Destroy(gameObject);
		}
	}
}
