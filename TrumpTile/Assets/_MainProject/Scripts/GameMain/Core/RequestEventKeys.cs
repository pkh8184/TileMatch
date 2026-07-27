namespace TrumpTile.GameMain.Core
{
    /// <summary>
    /// 03.29 곽원준 : 서버에 요청하는 이벤트 키들만 위치하게끔 클래스 분리해야함
    /// </summary>
    public static class RequestEventKeys
    {
        // LobbyManager -> 앱 버전 업데이트 필요 시
        public const string REQUIRED_VERSION_UPDATE = "VersionUpdate";

        // 서버에 의한 플레이어 데이터 갱신 시
        public const string REFRESH_PLAYER_DATA = "RefreshData";


        // 다른 클래스로 뺴야하는 이벤트들 (임시, 03.29)
        public const string REFRESH_PLAYER_LOCAL_DATA = "RefreshLocalData";
        public const string REFRESH_LANGUAGE = "RefreshLanguage";
    }
}