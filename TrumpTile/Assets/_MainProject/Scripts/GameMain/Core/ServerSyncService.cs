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

        //부팅 시 "이 설치가 서버 데이터를 받아야 하는가"가 확정됐는지 여부.
        //확정 전에 저장하면 재설치/기기변경 유저의 서버 데이터를 새 로컬 데이터(스테이지1·골드0)로
        //덮어써서 계정이 날아간다. 확정되기 전까지는 SaveToServer를 막는다.
        private static bool mbRestoreResolved = false;

        public static bool IsRestoreResolved => mbRestoreResolved;

        private static void SetServerDataLoaded()
        {
            PlayerPrefs.SetInt(PREFS_SERVER_DATA_LOADED, 1);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 이 기기의 로컬 데이터에 "플레이한 흔적"이 있는지.
        /// 신규 설치·재설치 직후는 스테이지1 / 골드0이므로 false가 된다.
        /// (아이템은 초기 지급분이 1개씩 있어 판별 기준으로 쓸 수 없다)
        /// </summary>
        private static bool HasLocalProgress()
        {
            if(PlayerDataManager.Inst == null || PlayerDataManager.Inst.UserData == null)
            {
                return false;
            }

            return PlayerDataManager.Inst.CurrentStage > 1
                || PlayerDataManager.Inst.Gold > 0
                || PlayerDataManager.Inst.ChampionsLevel > 0;
        }

        /// <summary>
        /// 부팅 시(구글 로그인 직후) 자동 복원.
        /// 이 설치에서 아직 복원한 적이 없고 서버에 계정 데이터가 있으면 자동으로 내려받는다.
        /// 성공/데이터없음으로 결론이 나야 이후 서버 저장이 허용된다.
        /// </summary>
        public static async Task<ELoadResult> TryAutoRestoreOnBoot()
        {
            //자동 복원 도입 이전부터 플레이하던 설치를 보호한다.
            //구버전은 스테이지 클리어·결제 때만 서버에 저장해서, 마지막 클리어 이후 일일 컨텐츠로 번
            //재화가 서버에 없다(로컬 1200골드 / 서버 600골드 같은 상태). 이때 자동 복원을 돌리면
            //로컬이 옛 서버 데이터로 덮여 그 차액이 증발한다.
            //로컬에 실제 진행이 있으면 = 이 기기에서 플레이하던 유저 → 로컬을 정답으로 보고 복원을 생략한다.
            //(재설치/기기변경 유저는 로컬이 스테이지1·골드0이라 이 조건에 걸리지 않아 정상적으로 복원된다)
            if(!IsServerDataLoaded && HasLocalProgress())
            {
                SetServerDataLoaded();
                mbRestoreResolved = true;
                Debug.Log("[ServerSyncService] 기존 진행 데이터 감지 - 자동 복원 생략(로컬 우선)");
                return ELoadResult.AlreadyLoaded;
            }

            //이미 이 설치에서 복원을 마쳤음 → 서버 호출 없이 저장 허용.
            if(IsServerDataLoaded)
            {
                mbRestoreResolved = true;
                return ELoadResult.AlreadyLoaded;
            }

            //오프라인이면 서버에 데이터가 있는지 알 수 없다. 부팅 중이므로 팝업 없이 조용히 넘어가고,
            //LoadFromServer(EnsureConnected)가 네트워크 팝업을 띄우지 않도록 여기서 먼저 끊는다.
            if(!NetworkUtil.IsConnected())
            {
                mbRestoreResolved = false;
                Debug.Log("[ServerSyncService] 부팅 자동 복원 스킵 - 네트워크 미연결 (이번 세션 서버 저장 차단)");
                return ELoadResult.NetworkUnavailable;
            }

            ELoadResult result = await LoadFromServer();

            switch(result)
            {
                case ELoadResult.Success:
                    //LoadFromServer가 플래그를 세팅한다.
                    mbRestoreResolved = true;
                    break;
                case ELoadResult.NoData:
                    //서버에 계정 문서가 없음 = 덮어쓸 데이터가 없음 → 저장 허용.
                    //다음 부팅마다 다시 조회하지 않도록 플래그도 세운다.
                    SetServerDataLoaded();
                    mbRestoreResolved = true;
                    break;
                default:
                    //NetworkUnavailable / Failed → 서버에 데이터가 있는지 모르는 상태.
                    //이번 세션은 서버 저장을 막고 다음 부팅에서 재시도한다.
                    mbRestoreResolved = false;
                    Debug.LogWarning($"[ServerSyncService] 부팅 자동 복원 미해결({result}) - 이번 세션 서버 저장 차단");
                    break;
            }

            return result;
        }

        /// <summary>
        /// 유저 데이터 + 리더보드 스냅샷을 서버에 저장한다.
        /// 오프라인이면 조용히 스킵한다(로컬엔 이미 저장되어 있음). 성공 여부 반환.
        /// </summary>
        public static async Task<bool> SaveToServer()
        {
            //복원 여부가 확정되지 않았으면 저장하지 않는다.
            //재설치 유저가 복원 전에 한 판 클리어해 서버를 스테이지2·골드0으로 덮어쓰는 사고를 막는다.
            if(!mbRestoreResolved)
            {
                Debug.Log("[ServerSyncService] 서버 저장 차단 - 복원 미해결 상태(로컬에는 저장됨)");
                return false;
            }

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

            //부팅 자동 복원이 실패한 뒤 수동 버튼으로 복원한 경우에도 서버 저장이 다시 허용되어야 한다.
            mbRestoreResolved = true;

            return ELoadResult.Success;
        }
    }
}
