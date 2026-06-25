using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TrumpTile.GameMain.Data;

namespace TrumpTile.GameMain.UI
{
	public class LeaderboardEntryView : MonoBehaviour
	{
		[SerializeField] private TextMeshProUGUI mRankText;
		[SerializeField] private Image mProfileImage;
		[SerializeField] private Image mProfileFrame;
		[SerializeField] private TextMeshProUGUI mNicknameText;
		[SerializeField] private TextMeshProUGUI mStageText;
		[SerializeField] private Image mBackground;

		[Header("내 항목 색상")]
		[SerializeField] private Color mMyEntryColor = new Color(1F, 0.9F, 0.5F, 1F);
		[SerializeField] private Color mDefaultColor = Color.white;

		public void SetData(LeaderboardEntryData data, bool bIsMyEntry)
		{
			mRankText.text = data.rank.ToString();
			mNicknameText.text = data.nickname;
			mStageText.text = $"Lv.{data.currentStage}";

			if (mBackground != null)
			{
				mBackground.color = bIsMyEntry ? mMyEntryColor : mDefaultColor;
			}
		}
	}
}
