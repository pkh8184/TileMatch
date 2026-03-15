using System.Collections;
using System.Collections.Generic;
using TrumpTile.FirebaseLibrary;
using TrumpTile.GameMain.Data;
using UnityEngine;

namespace TrumpTile.GameMain.Core
{
    public class LobbyManager : MonoBehaviour
    {
        private async void Awake()
        {
            //파이어베이스 기능 초기화
            await FirebaseService.Initialize();

            //로그인
            await FirebaseAuthService.Login();

            object result = await FirebaseFunctionsService.RequestLogin(Application.version);
            if(result is string)
            {
                //버전 업데이트가 필요한 경우 이벤트 발행
                //EventManager.Inst.ActiveEvent(RequestEventKeys.REQUIRED_VERSION_UPDATE, (object)null);
                Debug.Log("Required Version Update : 앱이 최신 버전이 아닙니다.");
                return;
            }
            //유저 데이터 생성 및 읽어오기
            PlayerDataManager.Inst.UserData = new UserData(result as Dictionary<object, object>);
        }
        //임시 로직 -> 팝업 UI로 이동 예정
        //private void GoToPlayStoreForUpdate()
        //{
        //    string packageName = Application.identifier;

        //    string marketUrl = $"market://details?id={packageName}";

        //    string webUrl = $"https://play.google.com/store/apps/details?id={packageName}";

        //    try
        //    {
        //        if (Application.platform == RuntimePlatform.Android)
        //        {
        //            Application.OpenURL(marketUrl);
        //        }
        //        else
        //        {
        //            Application.OpenURL(webUrl);
        //        }
        //    }
        //    catch
        //    {
        //        Application.OpenURL(webUrl);
        //    }
        //}
    }
}
