namespace TrumpTile.GameMain.Core
{
	/// <summary>
	/// 오디오 식별 키 - AudioDatabase에서 클립을 조회할 때 사용
	/// </summary>
	public enum EAudioKey
	{
		// BGM
		BGM_MainMenu,
		BGM_Gameplay,

		// SFX - Tile
		SFX_TileSelect,
		SFX_TileMove,
		SFX_TileMatch,
		SFX_Combo,

		// SFX - UI
		SFX_ButtonClick,
		SFX_PopupOpen,
		SFX_PopupClose,

		// SFX - Game
		SFX_GameClear,
		SFX_GameOver,
		SFX_Star,
		SFX_ItemUse,
		SFX_Shuffle,
		SFX_Undo,
		SFX_Hint,

		// SFX - Special
		SFX_Warning,
		SFX_Error,
	}
}
