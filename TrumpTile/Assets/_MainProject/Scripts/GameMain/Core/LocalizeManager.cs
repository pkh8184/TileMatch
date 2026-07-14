using TMPro;
using TrumpTile.FrameLibrary;
using TrumpTile.GameMain.Data;
using UnityEngine;

namespace TrumpTile.GameMain.Core
{
	public class LocalizeManager : Singleton_GameObject<LocalizeManager>
	{
		[SerializeField] private TBStringMasterTable mStringMasterTable;

		public string GetString(int key)
		{
			if (mStringMasterTable == null)
			{
				Debug.LogWarning("[LocalizeManager] StringMasterTable이 할당되지 않았습니다.");
				return $"[{key}]";
			}

			TBStringMasterData data = mStringMasterTable.GetByKey(key);
			if (data == null)
			{
				Debug.LogWarning($"[LocalizeManager] Key {key}에 해당하는 문자열이 없습니다.");
				return $"[{key}]";
			}

			return GetLocalizedString(data);
		}

		private string GetLocalizedString(TBStringMasterData data)
		{
			if (SettingsManager.Inst == null)
			{
				return data.Ko;
			}

			switch (SettingsManager.Inst.Language)
			{
				case ELanguage.Korean:    return data.Ko;
				case ELanguage.English:   return data.En;
				case ELanguage.Japanese:  return data.Ja;
				case ELanguage.Chinese:   return data.Zh;
				case ELanguage.Vietnamese: return data.Vi;
				case ELanguage.Hindi:     return data.Hi;
				case ELanguage.Arabic:    return data.Ar;
				default:                  return data.Ko;
			}
		}
	}
}
