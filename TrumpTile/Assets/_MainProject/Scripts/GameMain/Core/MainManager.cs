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
		GetGemProgress,
		PiggyBankPopup,
		UnlockContent,
		TreasurePackProgress,
		DailyCheck,
		AlbumPending
	}
	public class MainSceneEventWrapper
	{
		public Func<IEnumerator> Coroutine;
		public EMainSceneEventType Type;
	}
	public class MainManager : MonoBehaviour
	{
		public static MainManager Instance;
		private List<MainSceneEventWrapper> mEventList;
		[SerializeField] private AlbumPopup mAlbumPopup;
		[SerializeField] private ProfileResourceDatabase mProfileResourceDB;

		public ProfileResourceDatabase ProfileResourceDatabase => mProfileResourceDB;
		private IEnumerator mEventCoroutine;
		private readonly WaitForSeconds mAlbumCheckDelay = new WaitForSeconds(0.5F);
        private async void Awake()
        {         	
			Instance = this;
			mEventList = new List<MainSceneEventWrapper>();
			mProfileResourceDB.Initialize();

			PlayerDataManager.Inst.Initialize();

            await ContentManager.Inst.Initialize();
                
            UIBase[] uiBaseArray = FindObjectsOfType<UIBase>(true);
            foreach (UIBase item in uiBaseArray)
            {
                item.Initialize();
            }
            _ = AdManager.Inst;

            //모든 UI 초기화 후, 저장된 언어로 로케일을 한 번 재적용한다.
            //(초기 로드 시 UI가 초기화되는 순간엔 로케일 적용이 누락될 수 있어, 언어변경 흐름(REFRESH_LANGUAGE)을 startup에 재현)
            EventManager.Inst.ActiveEvent(RequestEventKeys.REFRESH_LANGUAGE);

			EventManager.Inst.AddEvent(EventKeys.REWARD_ANIM_DONE, CheckAlbumPendingReward);
        }

		private IEnumerator Start()
		{
			yield return StartCoroutine(SceneTransister.Inst.Co_PlayFadeInAnim());
			
			DailyPuzzleManager.Inst.SetDailyPuzzlePlayEarlyFalse();

			if(PlayerDataManager.Inst.IsChampionsActive)
            {
                AudioEvent.Play(EAudioKey.BGM_Main_Champions);
			}
			else
			{
				AudioEvent.Play(EAudioKey.BGM_Main);
			}
			EventManager.Inst.ActiveEvent(EventKeys.MAIN_SCENE_LOAD_COMPLETE);

			// yield return mAlbumCheckDelay;
			// CheckAlbumPendingReward();
		}
        private void OnDestroy()
        {
            EventManager.Inst?.RemoveEvent(EventKeys.REWARD_ANIM_DONE, CheckAlbumPendingReward);
			if(Instance == this)
			{
				Instance = null;
			}
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
		public void AddEvent(Func<IEnumerator> routine, EMainSceneEventType type)
		{
			if(mEventList == null)
			{
				mEventList = new List<MainSceneEventWrapper>();
			}

			MainSceneEventWrapper wrapper = new MainSceneEventWrapper{Coroutine = routine, Type = type};
			
			mEventList.Add(wrapper);
		}
		public void PlayEvent(Action OnStart = null, Action OnEnd = null)
		{
			StartCoroutine(Co_PlayEvent(OnStart, OnEnd));
		}
		private IEnumerator Co_PlayEvent(Action OnStart, Action OnEnd)
		{
			bool isPlaying = false;
			while(true)
			{
				if(mEventList.Count == 0)
				{
					if(isPlaying)
					{
						isPlaying = false;
						OnEnd?.Invoke();
					}
					yield return null;
					continue;
				}
				else
				{
					if(!isPlaying)
					{
						isPlaying = true;
						OnStart?.Invoke();
					}
					Func<IEnumerator> routine = null;
					for(int i = 0; i < (int)EMainSceneEventType.AlbumPending; i++)
					{
						for(int j = 0; j < mEventList.Count; j++)
						{
							if(mEventList[j].Type == (EMainSceneEventType)i)
							{
								routine = mEventList[j].Coroutine;
								mEventList.Remove(mEventList[j]);
								break;
							}
						}
						if(routine != null) break;
					}

					yield return StartCoroutine(routine());	

					yield return null;				
				}
			}
		}
	}
}
