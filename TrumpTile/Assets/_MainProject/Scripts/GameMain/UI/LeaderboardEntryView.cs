using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TrumpTile.GameMain.Data;

namespace TrumpTile.GameMain.UI
{
	public class LeaderboardEntryView : MonoBehaviour
	{
		[SerializeField] private TextMeshProUGUI mRankText;
		[SerializeField] private Image mRankBackground;
		[SerializeField] private Image mMedalImage;
		[SerializeField] private Image mProfileImage;
		[SerializeField] private Image mProfileFrame;
		[SerializeField] private TextMeshProUGUI mNicknameText;
		[SerializeField] private TextMeshProUGUI mStageText;
		[SerializeField] private Image mBackground;

		[Header("메달 스프라이트 (인덱스 0=금, 1=은, 2=동)")]
		[SerializeField] private Sprite[] mMedalSprites;

		[Header("내 항목 색상")]
		[SerializeField] private Color mMyEntryColor = new Color(1F, 0.9F, 0.5F, 1F);
		[SerializeField] private Color mDefaultColor = Color.white;

		private const int MEDAL_RANK_MAX = 3;

		public void SetData(LeaderboardEntryData data, bool bIsMyEntry)
		{
			SetRankDisplay(data.rank);
			mNicknameText.text = data.nickname;
			mStageText.text = data.currentStage.ToString();

			if (mBackground != null)
			{
				mBackground.color = bIsMyEntry ? mMyEntryColor : mDefaultColor;
			}
		}

		private void SetRankDisplay(int rank)
		{
			bool bIsMedalRank = rank >= 1 && rank <= MEDAL_RANK_MAX;

			if (mRankBackground != null)
			{
				mRankBackground.gameObject.SetActive(!bIsMedalRank);
			}

			if (mRankText != null)
			{
				mRankText.gameObject.SetActive(!bIsMedalRank);
				if (!bIsMedalRank)
				{
					mRankText.text = rank.ToString();
				}
			}

			if (mMedalImage != null)
			{
				mMedalImage.gameObject.SetActive(bIsMedalRank);
				if (bIsMedalRank && mMedalSprites != null && mMedalSprites.Length >= MEDAL_RANK_MAX)
				{
					mMedalImage.sprite = mMedalSprites[rank - 1];
				}
			}
		}
	}
}
