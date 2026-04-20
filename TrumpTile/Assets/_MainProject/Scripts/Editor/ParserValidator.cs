using System;
using UnityEditor;
using UnityEngine;

namespace TrumpTile.Editor
{
	public static class ParserValidator
	{
		[MenuItem("Tools/Parsers/Validate")]
		public static void Validate()
		{
			ESheetType[] sheetTypes = (ESheetType[])Enum.GetValues(typeof(ESheetType));
			bool bAllValid = true;

			foreach (ESheetType sheetType in sheetTypes)
			{
				string typeName = $"TrumpTile.Editor.{sheetType}Parser";
				Type parserType = Type.GetType(typeName);

				if (parserType == null)
				{
					Debug.LogWarning($"[ParserValidator] '{sheetType}'에 대한 파서 클래스가 없습니다 → {typeName}");
					bAllValid = false;
				}
				else
				{
					Debug.Log($"[ParserValidator] '{sheetType}' ✓ {typeName}");
				}
			}

			if (bAllValid)
			{
				Debug.Log("[ParserValidator] 모든 ESheetType에 파서가 등록되어 있습니다.");
			}
		}
	}
}
