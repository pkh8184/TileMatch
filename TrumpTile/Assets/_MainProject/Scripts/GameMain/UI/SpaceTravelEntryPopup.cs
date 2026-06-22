using TrumpTile.GameMain.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
	public class SpaceTravelEntryPopup : PopupBase
	{
		[Header("참가하기 버튼")]
		[SerializeField] private Button mJoinButton;
		[Header("닫기 버튼")]
		[SerializeField] private Button mCloseButton;
		private SpaceTravelContent mContentData;

		public override void Initialize()
		{
			base.Initialize();

			mContentData = ContentManager.Inst.GetContentData<SpaceTravelContent>("SpaceTravel");

			if (mContentData == null || !mContentData.Unlock)
			{
				return;
			}

			if (mContentData.ShowUnlockPopup)
			{
				Show();
			}

			mJoinButton.onClick.AddListener(OnJoinClicked);
			mCloseButton.onClick.AddListener(OnCloseClicked);

			EventManager.Inst.AddEvent(EventKeys.SPACE_TRAVEL_SHOW_ENTRY, Show);
		}

		private void OnDestroy()
		{
			EventManager.Inst?.RemoveEvent(EventKeys.SPACE_TRAVEL_SHOW_ENTRY, Show);
		}

		private void OnJoinClicked()
		{
			mContentData.StartEvent();
			Hide();
			EventManager.Inst.ActiveEvent(EventKeys.SPACE_TRAVEL_SHOW_GATHER);
		}

		private void OnCloseClicked()
		{
			Hide();
			EventManager.Inst.ActiveEvent(EventKeys.SPACE_TRAVEL_SET_RED_DOT);
		}
	}
}
