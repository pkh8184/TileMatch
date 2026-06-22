using TrumpTile.GameMain.Core;
using TMPro;
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
		[Header("이벤트 설명 텍스트")]
		[SerializeField] private TMP_Text mDescriptionText;

		private SpaceTravelContent mContentData;

		public override void Initialize()
		{
			base.Initialize();

			mContentData = ContentManager.Inst.GetContentData<SpaceTravelContent>("SpaceTravel");

			if (mContentData == null || !mContentData.Unlock)
			{
				return;
			}

			mJoinButton.onClick.AddListener(OnJoinClicked);
			mCloseButton.onClick.AddListener(OnCloseClicked);

			EventManager.Inst.AddEvent("SpaceTravel_ShowEntry", Show);
		}

		private void OnDestroy()
		{
			EventManager.Inst?.RemoveEvent("SpaceTravel_ShowEntry", Show);
		}

		private void OnJoinClicked()
		{
			mContentData.StartEvent();
			Hide();
			EventManager.Inst.ActiveEvent("SpaceTravel_ShowGather");
		}

		private void OnCloseClicked()
		{
			Hide();
			EventManager.Inst.ActiveEvent("SpaceTravel_SetRedDot");
		}
	}
}
