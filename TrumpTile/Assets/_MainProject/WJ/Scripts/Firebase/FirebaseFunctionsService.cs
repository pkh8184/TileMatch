using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Firebase.Functions;

namespace TrumpTile.FirebaseLibrary
{
    public static class FirebaseFunctionsService
    {
        public static async Task RequestStartStage()
        {
            try
            {
                await FirebaseService.Functions.GetHttpsCallable(FirebaseFunctionsNames.START_STAGE).CallAsync();
            }
            catch (Exception e)
            {
                return;
            }
        }

        /// <summary>
        /// [saveData] 유저 데이터(5필드) + 리더보드 더미 스냅샷을 서버에 저장한다.
        /// payload 형태: { user: {...}, leaderboard: { data, lastRefresh } }
        /// </summary>
        public static async Task<bool> RequestSaveData(Dictionary<string, object> payload)
        {
            try
            {
                await FirebaseService.Functions.GetHttpsCallable(FirebaseFunctionsNames.SAVE_DATA).CallAsync(payload);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// [loadData] UID로 서버에 저장된 유저 데이터를 읽어온다. 실패 시 null.
        /// </summary>
        public static async Task<(bool notFound, Dictionary<object, object> data)> RequestLoadData()
        {
            try
            {
                HttpsCallableResult result = await FirebaseService.Functions.GetHttpsCallable(FirebaseFunctionsNames.LOAD_DATA).CallAsync();
                return (false, result != null ? result.Data as Dictionary<object, object> : null);
            }
            catch (FunctionsException fe)
            {
                //서버에 UID 문서가 없으면(한 번도 온라인 저장하지 않은 계정) not-found 로 응답한다.
                if (fe.ErrorCode == FunctionsErrorCode.NotFound)
                {
                    return (true, null);
                }
                return (false, null);
            }
            catch (Exception)
            {
                return (false, null);
            }
        }

        public static async Task<object> RequestLogin(string version)
        {
            try
            {
                HttpsCallableResult result = await FirebaseService.Functions.GetHttpsCallable(FirebaseFunctionsNames.PROGRESS_LOGIN).CallAsync(version);

                if (result == null)
                {
                    return null;
                }
                if(result.Data is string)
                {
                    return string.Empty;
                }
                return result.Data as Dictionary<object, object>;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        // 수정: profile 데이터를 함께 전송
        public static async Task<Dictionary<object, object>> RequestEndStageAsync(string nickname, int profileImageIndex, int profileFrameIndex)
        {
            try
            {
                HttpsCallableResult result = await FirebaseService.Functions
                    .GetHttpsCallable(FirebaseFunctionsNames.END_STAGE)
                    .CallAsync(new Dictionary<string, object>
                    {
                        { "nickname", nickname },
                        { "profileImageIndex", profileImageIndex },
                        { "profileFrameIndex", profileFrameIndex }
                    });

                if (result == null)
                {
                    return null;
                }
                return result.Data as Dictionary<object, object>;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        // 추가: 리더보드 조회
        public static async Task<Dictionary<object, object>> RequestGetLeaderboardAsync(int n)
        {
            try
            {
                HttpsCallableResult result = await FirebaseService.Functions
                    .GetHttpsCallable(FirebaseFunctionsNames.GET_LEADERBOARD)
                    .CallAsync(new Dictionary<string, object> { { "n", n } });

                if (result == null)
                {
                    return null;
                }
                return result.Data as Dictionary<object, object>;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public static async Task RequestUpdateAlbumRewardedStage(int stage)
        {
            try
            {
                await FirebaseService.Functions
                    .GetHttpsCallable(FirebaseFunctionsNames.UPDATE_ALBUM_REWARDED_STAGE)
                    .CallAsync(new Dictionary<string, object> { { "lastAlbumRewardedStage", stage } });
            }
            catch (Exception e)
            {
                return;
            }
        }

        public static async Task<Dictionary<object, object>> RequestPurchaseProduct()
        {
            return await RequestCallableFunctionHaveReturnValue(FirebaseFunctionsNames.PURCHASE_PRODUCT);
        }

        private static async Task<Dictionary<object, object>> RequestCallableFunctionHaveReturnValue(string functionName)
        {
            try
            {
                HttpsCallableResult result = await FirebaseService.Functions.GetHttpsCallable(functionName).CallAsync();

                if (result == null)
                {
                    return null;
                }
                return result.Data as Dictionary<object, object>;
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}
