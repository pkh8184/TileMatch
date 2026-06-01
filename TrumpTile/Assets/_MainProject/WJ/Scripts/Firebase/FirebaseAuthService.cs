using System;
using System.Threading.Tasks;
using Firebase.Auth;
using UnityEngine;

namespace TrumpTile.FirebaseLibrary
{
    public static class FirebaseAuthService
    {
        public static async Task Login()
        {
            try
            {
                await SignInWithGooglePlayGameService();
                    // 로그인 성공 후 IAP 초기화
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[IAPManager] 구글 로그인 실패: {e.Message}");
            }
            // if(FirebaseService.Auth.CurrentUser == null)
            // {
            //     //await SignInAnonymously();

            //     //구글플레이서비스 연동 후 아래 로직으로 교체해야함 
            //     try
            //     {
            //         await SignInWithGooglePlayGameService();
            //         // 로그인 성공 후 IAP 초기화
            //     }
            //     catch (Exception e)
            //     {
            //         UnityEngine.Debug.LogError($"[IAPManager] 구글 로그인 실패: {e.Message}");
            //     }
            // }
        }


        private static async Task SignInAnonymously()
        {
            try
            {
                AuthResult result = await FirebaseService.Auth.SignInAnonymouslyAsync();
            }
            catch (Exception e)
            {
                
            }
        }
        private static async Task SignInWithGooglePlayGameService()
        {
            Social.localUser.Authenticate((success, error) =>
            {
                if(success)
                {
                    Debug.Log($"PlayGames 로그인 성공");
                }
                else
                {
                    Debug.Log($"PlayGames 로그인 실패");
                }
            });
        }
    }

}
