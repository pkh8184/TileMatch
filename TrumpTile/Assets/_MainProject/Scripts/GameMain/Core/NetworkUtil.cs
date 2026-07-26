using UnityEngine;
using TrumpTile.FrameLibrary;

namespace TrumpTile.GameMain.Core
{
    /// <summary>
    /// 네트워크 연결 상태 확인 유틸.
    /// 인앱구매/데이터 불러오기 등 네트워크가 필요한 지점에서 사용한다.
    /// </summary>
    public static class NetworkUtil
    {
        /// <summary>디바이스가 네트워크에 연결되어 있는지 여부.</summary>
        public static bool IsConnected()
        {
            return Application.internetReachability != NetworkReachability.NotReachable;
        }

        /// <summary>
        /// 네트워크 연결을 확인한다. 끊겨 있으면 NETWORK_NOT_CONNECT 이벤트를 발생시키고 false를 반환한다.
        /// (이벤트 구독/처리는 구독부에서 담당)
        /// </summary>
        public static bool EnsureConnected()
        {
            if(IsConnected())
            {
                return true;
            }

            EventManager.Inst?.ActiveEvent(EventKeys.NETWORK_NOT_CONNECT);
            return false;
        }
    }
}
