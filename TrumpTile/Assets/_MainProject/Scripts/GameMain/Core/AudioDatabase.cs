using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrumpTile.GameMain.Core
{
	/// <summary>
	/// EAudioKey와 AudioClip을 매핑하는 데이터 에셋
	/// Create → TrumpTile → AudioDatabase 로 생성
	/// </summary>
	[CreateAssetMenu(fileName = "AudioDatabase", menuName = "TrumpTile/AudioDatabase")]
	public class AudioDatabase : ScriptableObject
	{
		[Serializable]
		public class AudioEntry
		{
			public EAudioKey Key;
			public AudioClip Clip;
		}

		[SerializeField] private List<AudioEntry> mEntries = new List<AudioEntry>();

		private Dictionary<EAudioKey, AudioClip> mLookup;

		public void Initialize()
		{
			mLookup = new Dictionary<EAudioKey, AudioClip>();
			foreach (AudioEntry entry in mEntries)
			{
				if (entry.Clip == null)
				{
					continue;
				}
				mLookup[entry.Key] = entry.Clip;
			}
		}

		/// <summary>
		/// 키에 해당하는 클립 반환. 없으면 null.
		/// </summary>
		public AudioClip GetClip(EAudioKey key)
		{
			if (mLookup == null)
			{
				Initialize();
			}
			mLookup.TryGetValue(key, out AudioClip clip);
			return clip;
		}
	}
}
