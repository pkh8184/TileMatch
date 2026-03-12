using System;
using System.Threading.Tasks;
using Firebase.Auth;

namespace TrumpTile.FirebaseLibrary
{
    public static class FirebaseAuthService
    {
        public static async Task Login()
        {
            if(FirebaseService.Auth.CurrentUser == null)
            {
                await SignInAnonymously();

                //구글플레이서비스 연동 후 아래 로직으로 교체해야함 
                //await SignInWithGooglePlayGameService();
            }
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

        }
    }

}
