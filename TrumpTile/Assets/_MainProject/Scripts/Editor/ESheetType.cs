namespace TrumpTile.Editor
{
	public enum ESheetType
	{
		// 새 시트 추가 시 여기에만 추가
		// 파서 클래스명 규칙: {ESheetType값}Parser (예: TBStageParser)
		[SheetName("TB_Stage")]
		TBStage,
		[SheetName("TB_Item")]
		TBItem,
		[SheetName("TB_Album")]
		TBAlbum,
		[SheetName("TB_Picture")]
		TBPicture,
	}
}