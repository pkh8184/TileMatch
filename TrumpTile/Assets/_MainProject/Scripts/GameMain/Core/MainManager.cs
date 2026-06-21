using System.Collections;
using TrumpTile.GameMain.Data;
using TrumpTile.GameMain.UI;
using UnityEngine;

namespace TrumpTile.GameMain.Core
{
	public class MainManager : MonoBehaviour
	{
		[SerializeField] private AlbumPopup mAlbumPopup;

		private readonly WaitForSeconds mAlbumCheckDelay = new WaitForSeconds(0.5F);

        private async void Awake()
        {
            PlayerDataManager.Inst.Initialize();

            await ContentManager.Inst.Initialize();
                
            UIBase[] uiBaseArray = FindObjectsOfType<UIBase>(true);
            foreach (UIBase item in uiBaseArray)
            {
                item.Initialize();
            }
            _ = AdManager.Inst;
        }

		private IEnumerator Start()
		{
			yield return StartCoroutine(SceneTransister.Inst.Co_PlayFadeInAnim());
			if(PlayerDataManager.Inst.CurrentStage >= CoreData.MAX_STAGE)
            {
                AudioEvent.Play(EAudioKey.BGM_Main_Champions);
			}
			else
			{
				AudioEvent.Play(EAudioKey.BGM_Main);
			}
			EventManager.Inst.ActiveEvent("MainSceneLoadComplete");

			yield return mAlbumCheckDelay;
			CheckAlbumPendingReward();
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
	}
}
