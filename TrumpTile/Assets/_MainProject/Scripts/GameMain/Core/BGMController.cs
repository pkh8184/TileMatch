using System.Collections;
using UnityEngine;

namespace TrumpTile.GameMain.Core
{
	/// <summary>
	/// BGM 재생/정지/페이드/크로스페이드 전담 컨트롤러
	/// AudioManager가 초기화 시 참조를 주입함
	/// </summary>
	public class BGMController : MonoBehaviour
	{
		[SerializeField] private float mFadeDuration = 1F;

		private AudioSource mBgmSource;
		private float mTargetVolume = 0.5F;
		private bool mBBgmEnabled = true;
		private bool mBIsMuted = false;
		private Coroutine mFadeCoroutine;

		public void Initialize(AudioSource bgmSource)
		{
			mBgmSource = bgmSource;
			mBgmSource.loop = true;
			mBgmSource.playOnAwake = false;
		}

		#region 볼륨/활성화 설정

		public void SetEnabled(bool bEnabled)
		{
			mBBgmEnabled = bEnabled;
			ApplyVolume();
		}

		public void SetMuted(bool bMuted)
		{
			mBIsMuted = bMuted;
			ApplyVolume();
		}

		public void SetVolume(float volume)
		{
			mTargetVolume = Mathf.Clamp01(volume);
			ApplyVolume();
		}

		private void ApplyVolume()
		{
			if (mBgmSource == null)
			{
				return;
			}
			mBgmSource.volume = (mBBgmEnabled && !mBIsMuted) ? mTargetVolume : 0F;
		}

		#endregion

		#region 재생 제어

		public void Play(AudioClip clip, bool bFade = true)
		{
			if (clip == null || mBgmSource == null)
			{
				return;
			}
			if (!mBBgmEnabled)
			{
				return;
			}
			if (mBgmSource.clip == clip && mBgmSource.isPlaying)
			{
				return;
			}

			if (bFade && mBgmSource.isPlaying)
			{
				StopFade();
				mFadeCoroutine = StartCoroutine(CrossFade(clip));
			}
			else
			{
				mBgmSource.clip = clip;
				mBgmSource.volume = (mBIsMuted) ? 0F : mTargetVolume;
				mBgmSource.Play();
			}
		}

		public void Pause()
		{
			if (mBgmSource != null)
			{
				mBgmSource.Pause();
			}
		}

		public void Resume()
		{
			if (mBgmSource != null)
			{
				mBgmSource.UnPause();
			}
		}

		public void Stop(bool bFade = true)
		{
			if (mBgmSource == null)
			{
				return;
			}

			if (bFade)
			{
				StopFade();
				mFadeCoroutine = StartCoroutine(FadeOut());
			}
			else
			{
				mBgmSource.Stop();
			}
		}

		#endregion

		#region Coroutine

		private void StopFade()
		{
			if (mFadeCoroutine != null)
			{
				StopCoroutine(mFadeCoroutine);
				mFadeCoroutine = null;
			}
		}

		private IEnumerator CrossFade(AudioClip newClip)
		{
			float halfDuration = mFadeDuration / 2F;
			float elapsed = 0F;
			float startVolume = mBgmSource.volume;

			// 페이드 아웃
			while (elapsed < halfDuration)
			{
				elapsed += Time.deltaTime;
				mBgmSource.volume = Mathf.Lerp(startVolume, 0F, elapsed / halfDuration);
				yield return null;
			}

			mBgmSource.Stop();
			mBgmSource.clip = newClip;
			mBgmSource.Play();

			// 페이드 인
			elapsed = 0F;
			float targetVolume = mBIsMuted ? 0F : mTargetVolume;

			while (elapsed < halfDuration)
			{
				elapsed += Time.deltaTime;
				mBgmSource.volume = Mathf.Lerp(0F, targetVolume, elapsed / halfDuration);
				yield return null;
			}

			mBgmSource.volume = targetVolume;
			mFadeCoroutine = null;
		}

		private IEnumerator FadeOut()
		{
			float elapsed = 0F;
			float startVolume = mBgmSource.volume;

			while (elapsed < mFadeDuration)
			{
				elapsed += Time.deltaTime;
				mBgmSource.volume = Mathf.Lerp(startVolume, 0F, elapsed / mFadeDuration);
				yield return null;
			}

			mBgmSource.Stop();
			mBgmSource.volume = mBIsMuted ? 0F : mTargetVolume;
			mFadeCoroutine = null;
		}

		#endregion
	}
}
