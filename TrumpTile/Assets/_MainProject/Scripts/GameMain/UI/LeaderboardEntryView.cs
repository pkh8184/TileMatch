using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TrumpTile.GameMain.Data;
using TrumpTile.GameMain.Core;

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

		public void SetData(TBLeaderNameData data, int rank)
		{
			SetRankDisplay(rank);
			mNicknameText.text = data.Nickname;
			mStageText.text = data.Stage.ToString();
			mProfileImage.sprite = MainManager.Instance.ProfileResourceDatabase.GetProfileSprite(data.Profile);
			mProfileFrame.sprite = MainManager.Instance.ProfileResourceDatabase.GetFrameSprite(data.Frame);

			if (mBackground != null)
			{
				mBackground.color = mDefaultColor;
			}
		}
		public void SetMyData(int rank)
		{
			if(rank > 100)
			{
				mRankBackground.gameObject.SetActive(false);
			}
			else
			{
				SetRankDisplay(rank);
			}
			
			mNicknameText.text = PlayerDataManager.Inst.GetNickname();
			mStageText.text = PlayerDataManager.Inst.ChampionsLevel.ToString();
			mProfileImage.sprite = MainManager.Instance.ProfileResourceDatabase.GetProfileSprite(PlayerDataManager.Inst.GetProfileImageIndex() + 101);
			mProfileFrame.sprite = MainManager.Instance.ProfileResourceDatabase.GetFrameSprite(PlayerDataManager.Inst.GetProfileFrameIndex() + 301);
			
			if (mBackground != null)
			{
				mBackground.color = mMyEntryColor;
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
