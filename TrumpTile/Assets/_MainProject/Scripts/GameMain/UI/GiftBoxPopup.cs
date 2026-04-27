using System;
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
	/// <summary>
	/// 앨범 사진 해금 시 나타나는 보상 팝업.
	/// Show(unlockedPictures) 호출 → 보상 아이템 애니메이션 → 닫힘 → OnClosed 콜백.
	/// </summary>
	public class GiftBoxPopup : PopupBase
	{
		[Header("보상 아이템 아이콘 (ItemId 순서: Hammer/MagicStick/MagicHat/Bomb)")]
		[SerializeField] private GameObject mRewardItemRoot;
		[SerializeField] private Image[]    mRewardItemIcons;
		[SerializeField] private TMP_Text[] mRewardItemCounts;

		[Header("아이템 아이콘 스프라이트 (Inspector에서 ItemId 순서대로 할당)")]
		[SerializeField] private Sprite mHammerSprite;
		[SerializeField] private Sprite mMagicStickSprite;
		[SerializeField] private Sprite mMagicHatSprite;
		[SerializeField] private Sprite mBombSprite;

		[Header("아이템 등장 애니메이션")]
		[SerializeField] private float mItemShowDelay  = 0.1f;
		[SerializeField] private float mItemShowDuration = 0.4f;

		public event Action OnClosed;

		private List<TBAlbumPictureData> mUnlockedPictures;

		public void Show(List<TBAlbumPictureData> unlockedPictures)
		{
			mUnlockedPictures = unlockedPictures;
			base.Show();
			StartCoroutine(PlayRewardSequence());
		}

		private IEnumerator PlayRewardSequence()
		{
			RefreshRewardDisplay();

			yield return new WaitForSeconds(0.3f);

			yield return PlayItemAppearAnim();
		}

		private void RefreshRewardDisplay()
		{
			int hammer = 0, magicStick = 0, magicHat = 0, bomb = 0;

			foreach (TBAlbumPictureData pic in mUnlockedPictures)
			{
				hammer     += pic.HammerRewardCount;
				magicStick += pic.MagicStickRewardCount;
				magicHat   += pic.MagicHatRewardCount;
				bomb       += pic.BombRewardCount;
			}

			SetRewardSlot(0, mHammerSprite,     hammer);
			SetRewardSlot(1, mMagicStickSprite, magicStick);
			SetRewardSlot(2, mMagicHatSprite,   magicHat);
			SetRewardSlot(3, mBombSprite,        bomb);
		}

		private void SetRewardSlot(int index, Sprite sprite, int count)
		{
			if (index >= mRewardItemIcons.Length)
			{
				return;
			}
			bool bHasReward = count > 0;
			mRewardItemIcons[index].gameObject.SetActive(bHasReward);
			if (!bHasReward)
			{
				return;
			}
			mRewardItemIcons[index].sprite      = sprite;
			mRewardItemIcons[index].transform.localScale = Vector3.zero;
			if (index < mRewardItemCounts.Length)
			{
				mRewardItemCounts[index].text = "x" + count;
			}
		}

		private IEnumerator PlayItemAppearAnim()
		{
			for (int i = 0; i < mRewardItemIcons.Length; i++)
			{
				if (!mRewardItemIcons[i].gameObject.activeSelf)
				{
					continue;
				}
				mRewardItemIcons[i].transform
					.DOScale(1f, mItemShowDuration)
					.SetEase(Ease.OutBack);
				yield return new WaitForSeconds(mItemShowDelay);
			}
		}

		public override void Hide()
		{
			base.Hide();
			AlbumManager.Inst.ClearRecentlyUnlocked();
			OnClosed?.Invoke();
		}
	}
}
