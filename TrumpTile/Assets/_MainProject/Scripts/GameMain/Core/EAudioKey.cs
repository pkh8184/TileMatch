namespace TrumpTile.GameMain.Core
{
	/// <summary>
	/// 오디오 식별 키 — AudioDatabase에서 클립을 조회할 때 사용
	/// </summary>
	public enum EAudioKey
	{
		// BGM
		BGM_Main,
		BGM_Ingame,

		// SFX - Tile
		SFX_TileMove,
		SFX_TileMatch,

		// SFX - Item
		SFX_Clock,
		SFX_Hammer,
		SFX_Bomb,
		SFX_MagicHat_01,
		SFX_MagicHat_02,

		// SFX - UI
		SFX_BtnClick,
		SFX_LevelName_01,
		SFX_LevelName_02,
		SFX_SceneTransition_In,
		SFX_SceneTransition_Out,
		SFX_Purchase,

		// SFX - Game
		SFX_StageClear,
		SFX_StageLosed,
		SFX_StageLosed_02,
		SFX_StageFail,
		SFX_Reject,

		// additional clip
		BGM_Ingame_Hard,
		BGM_Ingame_VeryHard,
		SFX_LevelName_Hard,
		SFX_LevelName_VeryHard,
		SFX_TileMatch_Hard,
		SFX_TileMatch_VeryHard,
		BGM_Ingame_Bonus,
		SFX_TileMatch_Bonus,
		SFX_LevelName_Bonus,
		SFX_TileMove_Hard,
		SFX_TileMove_VeryHard,
		SFX_TileMove_Bonus,
		SFX_DailyCheck_StickerOff,

		// SFX - Album
		SFX_UnlockInteract,
	}
}
