using UnityEngine;
using TrumpTile.FrameLibrary;
using TrumpTile.GameMain.Core;

namespace TrumpTile.GameMain.UI
{
    /// <summary>
    /// NETWORK_NOT_CONNECT 이벤트를 수신하면 "네트워크 연결 필요" 팝업을 띄운다.
    /// 인앱결제 구매/구매 복원/데이터 불러오기 등에서 오프라인일 때 발생한다.
    /// 씬에 배치하고 mNetworkPopup에 팝업을 할당한다.
    /// </summary>
    public class NetworkPopupHandler : MonoBehaviour
    {
        [Header("네트워크 연결 필요 팝업")]
        [SerializeField] private PublicPopup mNetworkPopup;

        private void OnEnable()
        {
            EventManager.Inst?.AddEvent(EventKeys.NETWORK_NOT_CONNECT, OnNetworkNotConnect);
        }

        private void OnDisable()
        {
            EventManager.Inst?.RemoveEvent(EventKeys.NETWORK_NOT_CONNECT, OnNetworkNotConnect);
        }

        private void OnNetworkNotConnect()
        {
            if(mNetworkPopup != null)
            {
                mNetworkPopup.Show();
            }
        }
    }
}
