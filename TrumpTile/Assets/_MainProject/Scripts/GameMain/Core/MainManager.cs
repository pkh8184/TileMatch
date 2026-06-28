using System;
using System.Collections;
using System.Collections.Generic;
using TrumpTile.FrameLibrary;
using TrumpTile.GameMain.Data;
using TrumpTile.GameMain.UI;
using UnityEngine;

namespace TrumpTile.GameMain.Core
{
	public enum EMainSceneEventType
	{
		GetReward = 0,
		ContentComplete,
		ContentCheck,
		UnlockContent
	}
	public class MainSceneEventWrapper
	{
		public Action Action;
		public int Order;
		public EMainSceneEventType Type;
	}
	public class MainManager : MonoBehaviour
	{
		public static MainManager Instance;

		private List<MainSceneEventWrapper> mEventList;
		[SerializeField] private AlbumPopup mAlbumPopup;

		private readonly WaitForSeconds mAlbumCheckDelay = new WaitForSeconds(0.5F);
        private async void Awake()
        {         
			Instance = this;
			
			mEventList = new List<MainSceneEventWrapper>();

			PlayerDataManager.Inst.Initialize();

            await ContentManager.Inst.Initialize();
                
            UIBase[] uiBaseArray = FindObjectsOfType<UIBase>(true);
            foreach (UIBase item in uiBaseArray)
            {
                item.Initialize();
            }
            _ = AdManager.Inst;

			EventManager.Inst.AddEvent("RewardAnimDone", CheckAlbumPendingReward);
        }

		private IEnumerator Start()
		{
			yield return StartCoroutine(SceneTransister.Inst.Co_PlayFadeInAnim());
			
			DailyPuzzleManager.Inst.SetDailyPuzzlePlayEarlyFalse();

			if(PlayerDataManager.Inst.CurrentStage >= CoreData.MAX_STAGE)
            {
                AudioEvent.Play(EAudioKey.BGM_Main_Champions);
			}
			else
			{
				AudioEvent.Play(EAudioKey.BGM_Main);
			}
			EventManager.Inst.ActiveEvent("MainSceneLoadComplete");

			// yield return mAlbumCheckDelay;
			// CheckAlbumPendingReward();
		}
        private void OnDestroy()
        {
            EventManager.Inst?.RemoveEvent("RewardAnimDone", CheckAlbumPendingReward);
        }
        private void CheckAlbumPendingReward()
		{
			if (AlbumManager.Inst == null || mAlbumPopup == null)
			{
				return;
			}

			AlbumManager.Inst.CheckPendingReward(pendingPictures =>
			{
				mAlbumPopup.Show();
				mAlbumPopup.PlayRewardSequence(pendingPictures);
			});
		}
		public void AddEvent(Action action, int order, EMainSceneEventType type)
		{
			if(mEventList == null)
			{
				mEventList = new List<MainSceneEventWrapper>();
			}

			MainSceneEventWrapper wrapper = new MainSceneEventWrapper{Action = action, Order = order, Type = type};
			
			for(int i = 0; i < mEventList.Count; i++)
			{
				EMainSceneEventType compareType = mEventList[i].Type;

				if(type < compareType)
				{
					mEventList.Add(wrapper);
				}
				else
				{
					
				}
			}
		}
	}
}
