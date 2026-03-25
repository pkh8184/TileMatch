namespace TrumpTile.GameMain.Core
{
    public static class RequestEventKeys
    {
        // LobbyManager -> 앱 버전 업데이트 필요 시
        public const string REQUIRED_VERSION_UPDATE = "VersionUpdate";

        // 서버에 의한 플레이어 데이터 갱신 시
        public const string REFRESH_PLAYER_DATA = "RefreshData";
        public const string REFRESH_PLAYER_LOCAL_DATA = "RefreshLocalData";

        public const string LOADING_COMPLETE = "LoadingComplete";
    }
}