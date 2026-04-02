using System.Collections.Generic;
using UnityEngine;

namespace TrumpTile.GameMain.Core
{
	/// <summary>
	/// SFX 재생 전담 컨트롤러 - 오브젝트 풀 방식
	/// AudioManager가 초기화 시 풀 크기와 부모 GameObject를 주입함
	/// </summary>
	public class SFXController : MonoBehaviour
	{
		[SerializeField] private int mPoolSize = 10;
		[SerializeField] private float mPitchVariation = 0.1F;

		private List<AudioSource> mSfxPool = new List<AudioSource>();
		private int mCurrentIndex = 0;
		private float mSfxVolume = 1F;
		private bool mBSfxEnabled = true;
		private bool mBIsMuted = false;

		public void Initialize(int poolSize)
		{
			mPoolSize = poolSize;
			for (int i = 0; i < mPoolSize; i++)
			{
				AudioSource source = gameObject.AddComponent<AudioSource>();
				source.playOnAwake = false;
				mSfxPool.Add(source);
			}
		}

		#region 볼륨/활성화 설정

		public void SetEnabled(bool bEnabled)
		{
			mBSfxEnabled = bEnabled;
		}

		public void SetMuted(bool bMuted)
		{
			mBIsMuted = bMuted;
		}

		public void SetVolume(float volume)
		{
			mSfxVolume = Mathf.Clamp01(volume);
		}

		#endregion

		#region 재생

		public void Play(AudioClip clip, float volumeMultiplier = 1F, bool bRandomPitch = false)
		{
			if (clip == null || !mBSfxEnabled || mBIsMuted)
			{
				return;
			}

			AudioSource source = GetNextSource();
			source.clip = clip;
			source.volume = mSfxVolume * volumeMultiplier;
			source.pitch = bRandomPitch
				? 1F + Random.Range(-mPitchVariation, mPitchVariation)
				: 1F;
			source.Play();
		}

		/// <summary>
		/// 음계 피치를 지정해서 재생 (슬롯 채워질 때 도레미 효과 등)
		/// </summary>
		public void PlayWithPitch(AudioClip clip, float pitch)
		{
			if (clip == null || !mBSfxEnabled || mBIsMuted)
			{
				return;
			}

			AudioSource source = GetNextSource();
			source.clip = clip;
			source.volume = mSfxVolume;
			source.pitch = pitch;
			source.Play();
		}

		public void StopAll()
		{
			foreach (AudioSource source in mSfxPool)
			{
				if (source != null)
				{
					source.Stop();
				}
			}
		}

		#endregion

		private AudioSource GetNextSource()
		{
			AudioSource source = mSfxPool[mCurrentIndex];
			mCurrentIndex = (mCurrentIndex + 1) % mSfxPool.Count;
			return source;
		}
	}
}
