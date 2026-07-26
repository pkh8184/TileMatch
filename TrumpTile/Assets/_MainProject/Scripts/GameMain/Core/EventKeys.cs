namespace TrumpTile.GameMain.Core
{
	public static class EventKeys
	{
		// SlotManager → 타일 매치 발생
		public const string MATCH_OCCURRED = "Match_Occurred";

		// ComboSystem → 콤보 레이블 발동 (페이로드: ComboTriggeredPayload)
		public const string COMBO_TRIGGERED = "Combo_Triggered";

		// ComboSystem → 콤보 타이머 만료로 연속 카운트 초기화
		public const string COMBO_RESET = "Combo_Reset";

		// Audio — 페이로드: AudioEventPayload (제네릭 ActiveEvent<AudioEventPayload> 사용)
		public const string AUDIO_PLAY   = "Audio_Play";
		public const string AUDIO_PAUSE  = "Audio_Pause";
		public const string AUDIO_STOP   = "Audio_Stop";
		public const string AUDIO_RESUME = "Audio_Resume";

		// 씬/로딩 흐름
		public const string MAIN_SCENE_LOAD_COMPLETE    = "MainSceneLoadComplete";
		public const string MAIN_SCENE_ENTER_EVENT_DONE = "MainSceneEnterEventDone";
		public const string INGAME_LOADING_COMPLETE     = "IngameLoadingComplete";
		public const string TIMER_SETTING_COMPLETE      = "TimerSettingComplete";

		// 게임 진행/결과
		public const string GAME_OVER           = "GameOver";
		public const string GAME_OVER_TIME_OUT  = "GameOver_TimeOut";
		public const string GAME_OVER_SLOT_FULL = "GameOver_SlotFull";
		public const string STAGE_FAILED        = "StageFailed";
		public const string LEVEL_CLEAR         = "LevelClear";
		public const string RESTART_LEVEL       = "RestartLevel";
		public const string ON_EXIT_BUTTON      = "OnExitButton";

		// 상점/구매
		public const string ACCESS_SHOP_VIEW   = "AccessShopView";
		public const string PURCHASE_CONFIRMED = "PurchaseConfirmed";
		public const string PURCHASE_SUCCESS   = "PurchaseSuccess";
		public const string PURCHASE_FAILED    = "PurchaseFailed";
		public const string PURCHASE_ITEM      = "PurchaseItem";
		public const string REMOVE_ADS_PURCHASED = "RemoveAdsPurchased";

		// 아이템/콘텐츠 갱신
		public const string ITEM_COUNT_CHANGED   = "ItemCountChanged";
		public const string CONTENT_DATA_REFRESH = "ContentDataRefresh";
		public const string REFRESH_ROULETTE_DATA = "RefreshRouletteData";

		// 보상 애니메이션
		public const string PLAY_REWARD_ANIM      = "PlayRewardAnim";
		public const string PLAY_MINI_REWARD_ANIM = "PlayMiniRewardAnim";
		public const string REWARD_ANIM_DONE      = "RewardAnimDone";
		public const string GOLD_REWARD_ARRIVED   = "GoldRewardArrived";
		public const string GOLD_REWARD_ANIM_DONE = "GoldRewardAnimDone";
		public const string ITEM_REWARD_ARRIVED   = "ItemRewardArrived";
		public const string GEM_REWARD_ARRIVED    = "GemRewardArrived";
		public const string REFRESH_GOLD_TEXT     = "RefreshGoldText";
		public const string REFRESH_GEM_UI        = "RefreshGemUI";
		public const string PIGGY_REWARD_CONFIRM  = "PiggyRewardConfirm";

		// 인게임 타일/젬
		public const string BONUS_TILE_MATCH = "BonusTileMatch";
		public const string COLLECT_GEM      = "CollectGem";

		// 네트워크 (인앱구매/데이터 불러오기 등에서 네트워크 미연결 시 발생)
		public const string NETWORK_NOT_CONNECT = "NetworkNotConnect";
	}
}
