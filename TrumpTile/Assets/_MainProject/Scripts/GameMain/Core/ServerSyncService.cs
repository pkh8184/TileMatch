using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using TrumpTile.FirebaseLibrary;
using TrumpTile.GameMain.Data;

namespace TrumpTile.GameMain.Core
{
    //데이터 불러오기 결과.
    public enum ELoadResult
    {
        Success,             //성공적으로 불러와 로컬에 반영함
        NetworkUnavailable,  //네트워크 미연결 (NETWORK_NOT_CONNECT 이벤트도 발생)
        NoData,              //서버에 저장된 데이터 없음(한 번도 온라인 저장 안 한 계정)
        Failed,              //기타 실패
        AlreadyLoaded        //이미 이 설치에서 불러옴 → 서버 호출 안 함(비용 절감)
    }

    /// <summary>
    /// 서버 저장/불러오기 오케스트레이터.
    /// 유저 데이터(5필드) + 리더보드 더미 스냅샷을 서버 스키마에 맞춰 saveData/loadData와 연결한다.
    /// 네트워크 가드는 여기서 담당하고, FirebaseFunctionsService는 순수 전송만 한다.
    /// </summary>
    public static class ServerSyncService
    {
        //이 설치에서 서버 데이터를 이미 불러왔는지 여부(PlayerPrefs). 앱 삭제 시 초기화되어 다시 불러올 수 있다.
        private const string PREFS_SERVER_DATA_LOADED = "IsServerDataLoaded";

        //true면 데이터 불러오기 버튼이 서버를 호출하지 않는다(비용 절감).
        public static bool IsServerDataLoaded => PlayerPrefs.GetInt(PREFS_SERVER_DATA_LOADED, 0) == 1;

        private static void SetServerDataLoaded()
        {
            PlayerPrefs.SetInt(PREFS_SERVER_DATA_LOADED, 1);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 유저 데이터 + 리더보드 스냅샷을 서버에 저장한다.
        /// 오프라인이면 조용히 스킵한다(로컬엔 이미 저장되어 있음). 성공 여부 반환.
        /// </summary>
        public static async Task<bool> SaveToServer()
        {
            if(!NetworkUtil.IsConnected())
            {
                Debug.Log("[ServerSyncService] 서버 저장 스킵 - 네트워크 미연결");
                return false;
            }

            //로그인 보장 (이미 캐싱된 UID 있으면 로그인 생략). 실패 시 저장 스킵.
            if(!await FirebaseAuthService.EnsureLoggedIn())
            {
                Debug.LogWarning("[ServerSyncService] 서버 저장 스킵 - 로그인 실패");
                return false;
            }

            Dictionary<string, object> payload = new Dictionary<string, object>
            {
                { "user", PlayerDataManager.Inst.BuildServerUserData() }
            };

            //리더보드 더미 스냅샷이 있으면 함께 올린다.
            if(LeaderboardManager.Inst != null)
            {
                LeaderboardServerData leaderboard = LeaderboardManager.Inst.ExportForServer();
                if(leaderboard != null && !string.IsNullOrEmpty(leaderboard.data))
                {
                    payload["leaderboard"] = new Dictionary<string, object>
                    {
                        { "data", leaderboard.data },
                        { "lastRefresh", leaderboard.lastRefresh }
                    };
                }
            }

            return await FirebaseFunctionsService.RequestSaveData(payload);
        }

        /// <summary>
        /// 서버에서 유저 데이터 + 리더보드 스냅샷을 불러와 로컬에 반영한다.
        /// 데이터 불러오기는 네트워크 필수 → 미연결 시 NETWORK_NOT_CONNECT 이벤트 발생 후 false 반환.
        /// </summary>
        public static async Task<ELoadResult> LoadFromServer()
        {
            //이미 이 설치에서 불러왔으면 네트워크/로그인/서버호출 전부 스킵(비용 절감). 앱 삭제 시 PlayerPrefs 초기화로 다시 가능.
            if(IsServerDataLoaded)
            {
                return ELoadResult.AlreadyLoaded;
            }

            if(!NetworkUtil.EnsureConnected())
            {
                return ELoadResult.NetworkUnavailable;
            }

            //로그인 보장 (이미 캐싱된 UID 있으면 로그인 생략). 실패 시 불러오기 스킵.
            if(!await FirebaseAuthService.EnsureLoggedIn())
            {
                Debug.LogWarning("[ServerSyncService] 데이터 불러오기 실패 - 로그인 실패");
                return ELoadResult.Failed;
            }

            (bool notFound, Dictionary<object, object> data) = await FirebaseFunctionsService.RequestLoadData();

            //서버에 UID 문서가 없음 = 한 번도 (온라인) 저장한 적 없는 계정
            if(notFound)
            {
                return ELoadResult.NoData;
            }
            if(data == null)
            {
                return ELoadResult.Failed;
            }

            //유저 5필드 반영
            PlayerDataManager.Inst.ApplyServerUserData(data);

            //리더보드 더미 스냅샷 복원
            if(data.TryGetValue("leaderboard", out object leaderboardObj)
                && leaderboardObj is Dictionary<object, object> leaderboard)
            {
                string lbData = leaderboard.TryGetValue("data", out object d) ? d as string : null;
                string lastRefresh = leaderboard.TryGetValue("lastRefresh", out object r) ? r as string : null;
                LeaderboardManager.Inst?.RestoreFromServer(lbData, lastRefresh);
            }

            //불러오기 성공 → 플래그 true (이후 재호출 시 서버 호출 차단, 앱 삭제 전까지 유지)
            SetServerDataLoaded();

            return ELoadResult.Success;
        }
    }
}
