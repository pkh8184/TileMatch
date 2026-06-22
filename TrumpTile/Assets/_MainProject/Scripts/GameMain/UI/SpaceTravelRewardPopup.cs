using TMPro;
using TrumpTile.GameMain.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
	public class SpaceTravelRewardPopup : PopupBase
	{
		[Header("함께 달성한 인원 텍스트")]
		[SerializeField] private TMP_Text mAchievementText;
		[Header("보상 받기 버튼")]
		[SerializeField] private Button mClaimButton;

		private SpaceTravelContent mContentData;

		public override void Initialize()
		{
			base.Initialize();

			mContentData = ContentManager.Inst.GetContentData<SpaceTravelContent>("SpaceTravel");

			if (mContentData == null)
			{
				return;
			}

			EventManager.Inst.AddEvent(EventKeys.SPACE_TRAVEL_SHOW_REWARD, OnShowReward);
			mClaimButton.onClick.AddListener(OnClaimClicked);
		}

		private void OnDestroy()
		{
			EventManager.Inst?.RemoveEvent(EventKeys.SPACE_TRAVEL_SHOW_REWARD, OnShowReward);
		}

		private void OnShowReward()
		{
			int fakeCount = mContentData.GetFakePlayerCount();
			int totalCount = fakeCount + 1; // 나 포함

			mAchievementText.text = totalCount > 1
				? $"{totalCount}명과 함께 최종 달성!"
				: "혼자서 최종 달성!";

			Show();
		}

		private void OnClaimClicked()
		{
			mContentData.GrantFinalReward();
			Hide();
			EventManager.Inst.ActiveEvent(EventKeys.SPACE_TRAVEL_SHOW_PROGRESS);
			EventManager.Inst.ActiveEvent("PlayRewardAnim");
		}
	}
}
