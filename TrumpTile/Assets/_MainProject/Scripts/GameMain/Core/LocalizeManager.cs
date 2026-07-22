using TMPro;
using TrumpTile.FrameLibrary;
using TrumpTile.GameMain.Data;
using UnityEngine;

namespace TrumpTile.GameMain.Core
{
	public class LocalizeManager : Singleton_GameObject<LocalizeManager>
	{
		[SerializeField] private TBStringMasterTable mStringMasterTable;

		[SerializeField] private TMP_FontAsset[] mFontArray;

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
		public TMP_FontAsset GetFontAssetByLocale()
		{
			if(mFontArray.Length == 0)
			{
				return null;
			}
			int index = (int)SettingsManager.Inst.Language;
			if(index >= mFontArray.Length)
			{
				return mFontArray[0];
			}
			return mFontArray[index];
		}

		public bool IsRTL()
		{
			return SettingsManager.Inst != null && SettingsManager.Inst.Language == ELanguage.Arabic;
		}

		/// <summary>
		/// 코드에서 직접 .text를 세팅하는 동적 텍스트에 현재 언어 기준 RTL(아랍어) 여부를 적용한다.
		/// (TextLocalizeSetter를 안 거치는 텍스트용)
		/// </summary>
		public void ApplyRTL(params TMP_Text[] texts)
		{
			bool bRTL = IsRTL();
			foreach(TMP_Text text in texts)
			{
				if(text != null)
				{
					text.isRightToLeftText = bRTL;
				}
			}
		}
	}
}
