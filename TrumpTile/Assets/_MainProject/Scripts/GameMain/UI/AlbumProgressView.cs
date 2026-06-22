using DG.Tweening;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;
using UnityEngine;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
	public class AlbumProgressView : MonoBehaviour
	{
		[SerializeField] private Slider mProgressSlider;

		private void OnEnable()
		{
			if (AlbumManager.Inst != null)
			{
				AlbumManager.Inst.OnPictureCollected += OnPictureCollected;
			}
			Refresh();
		}

		private void OnDisable()
		{
			if (AlbumManager.Inst != null)
			{
				AlbumManager.Inst.OnPictureCollected -= OnPictureCollected;
			}
		}

		private void Refresh()
		{
			if (AlbumManager.Inst == null)
			{
				return;
			}

			(int collected, int total) = AlbumManager.Inst.GetCurrentProgress();
			float ratio = total > 0 ? (float)collected / total : 0F;

			if (mProgressSlider != null) mProgressSlider.value = ratio;
		}

		private void OnPictureCollected(TBPictureCollectData picture)
		{
			(int collected, int total) = AlbumManager.Inst.GetCurrentProgress();
			float targetRatio = total > 0 ? (float)collected / total : 0F;

			if (mProgressSlider != null)
			{
				mProgressSlider.DOValue(targetRatio, 0.4F).SetEase(Ease.OutQuad);
			}
		}
	}
}
