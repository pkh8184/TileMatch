using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using TrumpTile.FirebaseLibrary;
using TrumpTile.FrameLibrary;
using TrumpTile.GameMain.Data;
using System.Linq;
using Codice.Client.BaseCommands.Merge;

namespace TrumpTile.GameMain.Core
{
    [Serializable]
    public class LeaderboardDataWrapper
    {
        public List<TBLeaderNameData> DataList;
    }
    public class LeaderboardManager : Singleton_GameObject<LeaderboardManager>
    {
       // private const int DEFAULT_COUNT = 100;
        [SerializeField] private TBLeaderNameTable mLeaderNameTable;

        private LeaderboardDataWrapper mCurrentLeaderboardState;

        private const string PREFS_PATH = "LeaderboardData";
        //리더보드(더미 AI) 마지막 갱신 시각(로컬, Ticks) 저장 키
        private const string PREFS_LAST_REFRESH = "Leaderboard_LastRefresh";

        private const int TOTAL_DUMMY = 300;
        private const int NORMAL_GROUP = 150;
        private const int EFFORT_GROUP = 50;
        private const int LAZY_GROUP = 50;
        private const int NOTPLAY_GROUP = 25;
        private const int GOSU_GROUP = 20;
        private const int GOD_GROUP = 5;
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            CheckLeaderboardRefresh();
        }
        /// <summary>
        /// 리더보드(더미 AI) 갱신 주기 분기.
        /// - 월 경계를 넘었으면: 챔피언스 시즌 종료 → 전체 초기화(새 시즌 재구성)
        /// - 같은 달이면: 주 경계(월요일 기준)를 넘었는지 확인 → 주간 더미 AI 갱신
        /// 캘린더 경계(주/월)는 출석과 동일하게 로컬(GameTime.Now) 기준으로 판정한다.
        /// (서버가 UTC로 시즌 리셋을 강제한다면 GameTime.UtcNow / DateTimeKind.Utc 로 교체)
        /// </summary>
        private void CheckLeaderboardRefresh()
        {
            DateTime now = GameTime.Now;

            //마지막 갱신 기록이 없으면 첫 실행 → 최초 시즌 구성
            if(!TryGetLastRefreshDate(out DateTime lastRefresh))
            {
                Debug.Log("갱신 기록 없음, 새로 생성");
                InitLeaderboardData();
                SetLastRefreshDate(now);
                return;
            }

            mCurrentLeaderboardState = JsonUtility.FromJson<LeaderboardDataWrapper>(PlayerPrefs.GetString(PREFS_PATH, null));

            //1) 월 경계: 시즌 종료 → 전체 초기화 (월 변경은 주 변경을 포함하므로 우선 처리)
            bool bIsMonthChanged = now.Year != lastRefresh.Year || now.Month != lastRefresh.Month;
            if(bIsMonthChanged)
            {
                OnSeasonReset();
                SetLastRefreshDate(now);
                return;
            }

            //2) 주 경계(월요일 기준): 주간 더미 AI 갱신
            bool bIsWeekChanged = GetWeekStart(now) != GetWeekStart(lastRefresh);
            if(bIsWeekChanged)
            {
                OnWeeklyRefresh();
                SetLastRefreshDate(now);
            }
        }

        /// <summary>해당 시각이 속한 주의 시작(월요일 00:00)을 반환한다.</summary>
        private static DateTime GetWeekStart(DateTime dt)
        {
            //DayOfWeek: 일=0, 월=1, ... 토=6 → 월요일로부터 지난 일수(월=0)
            int daysSinceMonday = ((int)dt.DayOfWeek + 6) % 7;
            return dt.Date.AddDays(-daysSinceMonday);
        }

        /// <summary>
        /// 해당 시각이 이번 시즌(그 달) 기준 몇 주차인지 반환한다. (1-based, 월요일 기준 주)
        /// 1주차 = 그 달 1일이 속한 주(월요일 정렬). 이후 월요일을 넘길 때마다 +1.
        /// 예) 1일이 수요일이면 그 주(수~일)가 1주차, 다음 월요일부터 2주차.
        /// </summary>
        private static int GetSeasonWeek(DateTime dt)
        {
            DateTime monthStart = new DateTime(dt.Year, dt.Month, 1);
            DateTime firstWeekStart = GetWeekStart(monthStart);   //그 달 1일이 속한 주의 월요일(전월일 수 있음)
            DateTime currentWeekStart = GetWeekStart(dt);
            //둘 다 월요일 00:00이라 7의 배수. DST 대비 반올림.
            int weeksPassed = (int)Math.Round((currentWeekStart - firstWeekStart).TotalDays / 7.0);
            return weeksPassed + 1;
        }

        private bool TryGetLastRefreshDate(out DateTime date)
        {
            string raw = PlayerPrefs.GetString(PREFS_LAST_REFRESH, "");
            if(string.IsNullOrEmpty(raw) || !long.TryParse(raw, out long ticks))
            {
                date = default;
                return false;
            }
            date = new DateTime(ticks, DateTimeKind.Local);
            return true;
        }

        private void SetLastRefreshDate(DateTime date)
        {
            PlayerPrefs.SetString(PREFS_LAST_REFRESH, date.Ticks.ToString());
            PlayerPrefs.Save();
        }

        //월이 바뀌어 시즌이 종료된 경우: 챔피언스 시즌 리셋 + 더미 AI 전체 재구성
        //TODO: 실제 시즌 리셋/AI 재생성 로직 연결
        private void OnSeasonReset()
        {
            Debug.Log($"[LeaderboardManager] 시즌 종료. 랭킹 데이터가 초기화됩니다. 내 순위 : {FindMyRank()}");
            
            PlayerDataManager.Inst.UserData.ChampionsLevel = 1;

            InitLeaderboardData();
        }

        //같은 달 안에서 주(월요일)가 바뀐 경우: 더미 AI 주간 갱신
        //TODO: 실제 주간 AI 갱신 로직 연결
        private void OnWeeklyRefresh()
        {
            AdjustDummyTendencyToList(mCurrentLeaderboardState.DataList);

            PlayerPrefs.SetString(PREFS_PATH, JsonUtility.ToJson(mCurrentLeaderboardState));
            PlayerPrefs.Save();
        }

        private void InitLeaderboardData()
        {
            //ToList()는 얕은 복사(참조만 복사)라 원소를 수정하면 ScriptableObject 원본이 바뀐다.
            //Stage 조정이 원본을 건드리지 않도록 각 항목을 새 객체로 깊은 복사한다.
            List<TBLeaderNameData> datas = mLeaderNameTable.items.Select(src => new TBLeaderNameData
            {
                Index = src.Index,
                Nickname = src.Nickname,
                Profile = src.Profile,
                Frame = src.Frame,
                Stage = src.Stage
            }).ToList();

            LeaderboardDataWrapper wrapper = new LeaderboardDataWrapper();
            
            int currentWeek = GetSeasonWeek(GameTime.Now);
            
            wrapper.DataList = datas;
            for(int i = 0; i < currentWeek; i++)
            {
                AdjustDummyTendencyToList(wrapper.DataList);   
            }

            PlayerPrefs.SetString(PREFS_PATH, JsonUtility.ToJson(wrapper));
            PlayerPrefs.Save();

            mCurrentLeaderboardState = wrapper;
        }
        private void AdjustDummyTendencyToList(List<TBLeaderNameData> stageList)
        {          
            //0 ~ Count-1 인덱스로 실제 채워야 그룹 샘플링이 된다. (new List<int>(N)은 용량만 잡고 비어있음)
            List<int> capturedRef = Enumerable.Range(0, stageList.Count).ToList();

            List<int> normalGroup = capturedRef.OrderBy(x => UnityEngine.Random.value).Take(NORMAL_GROUP).ToList(); 
            foreach(var item in normalGroup)
            {
                capturedRef.Remove(item);

                int value = UnityEngine.Random.Range(20, 31);

                stageList[item].Stage += value;
            }

            List<int> effortGroup = capturedRef.OrderBy(x => UnityEngine.Random.value).Take(EFFORT_GROUP).ToList(); 
            foreach(var item in effortGroup)
            {
                capturedRef.Remove(item);

                int value = UnityEngine.Random.Range(30, 51);

                stageList[item].Stage += value;
            }

            List<int> lazyGroup = capturedRef.OrderBy(x => UnityEngine.Random.value).Take(LAZY_GROUP).ToList(); 
            foreach(var item in lazyGroup)
            {
                capturedRef.Remove(item);

                int value = UnityEngine.Random.Range(10, 21);

                stageList[item].Stage += value;
            }

            List<int> notPlayGroup = capturedRef.OrderBy(x => UnityEngine.Random.value).Take(NOTPLAY_GROUP).ToList(); 
            foreach(var item in notPlayGroup)
            {
                capturedRef.Remove(item);

                int value = UnityEngine.Random.Range(1, 6);

                stageList[item].Stage += value;
            }

            List<int> gosuGroup = capturedRef.OrderBy(x => UnityEngine.Random.value).Take(GOSU_GROUP).ToList(); 
            foreach(var item in gosuGroup)
            {
                capturedRef.Remove(item);

                int value = UnityEngine.Random.Range(60, 101);

                stageList[item].Stage += value;
            }

            List<int> godGroup = capturedRef.OrderBy(x => UnityEngine.Random.value).Take(GOD_GROUP).ToList(); 
            foreach(var item in godGroup)
            {
                capturedRef.Remove(item);

                int value = UnityEngine.Random.Range(120, 151);

                stageList[item].Stage += value;
            }
        }
        public List<TBLeaderNameData> GetRankerList()
        {
            List<TBLeaderNameData> sortedList = GetSortedAllDummys();
            
            int myRank = FindMyRank();

            //FindMyRank는 1-based 순위 → 0-based 삽입 인덱스로는 myRank - 1 (안 그러면 한 칸 밀림)
            sortedList.Insert(myRank - 1, null);

            return sortedList.Take(100).ToList();
        }
        public int FindMyRank()
        {
            List<TBLeaderNameData> sortedList = GetSortedAllDummys();

            int myStage = PlayerDataManager.Inst.ChampionsLevel;
            for(int i = 0; i < sortedList.Count; i++)
            {
                if(myStage >= sortedList[i].Stage)
                {
                    return i + 1;
                }
            }
            return sortedList.Count;
        }
        private List<TBLeaderNameData> GetSortedAllDummys()
        {
            List<TBLeaderNameData> sortedList = new List<TBLeaderNameData>(mCurrentLeaderboardState.DataList);
            //OrderByDescending은 새 시퀀스를 반환 → 결과를 다시 받아야 실제로 정렬됨
            sortedList = sortedList.OrderByDescending(x => x.Stage).ToList();

            return sortedList;
        }
        // private void LoadUserData()
		// {
		// 	string encryptedText = PlayerPrefs.GetString("UserData", "");
		// 	if(!string.IsNullOrEmpty(encryptedText) && DataEncryptor.TryDecrypt(encryptedText, out string json))
		// 	{
		// 		EncryptedUserData data = JsonUtility.FromJson<EncryptedUserData>(json);
		// 		mUserData = new UserData();
		// 		mUserData.FromEncryptedUserData(data);
		// 	}
		// 	else
		// 	{
		// 		mUserData = new UserData();
		// 	}
		// 	mUserData.LoadUnEncryptedData();

		// 	RefreshStreakLoginCount();
		// }
		// private void SaveUserData()
		// {
		// 	if(mUserData == null)
		// 	{
		// 		return;
		// 	}
		// 	EncryptedUserData userData = mUserData.ToEncryptedUserData();
		// 	string json = JsonUtility.ToJson(userData);

		// 	string encryptedText = DataEncryptor.Encrypt(json);
		// 	if(!string.IsNullOrEmpty(encryptedText))
		// 	{
		// 		PlayerPrefs.SetString("UserData", encryptedText);
		// 		PlayerPrefs.Save();
		// 	}
		// }
        // public async Task<LeaderboardResult> GetLeaderboardAsync(int n = DEFAULT_COUNT)
        // {
        //     Dictionary<object, object> raw = await FirebaseFunctionsService.RequestGetLeaderboardAsync(n);
        //     if (raw == null)
        //     {
        //         Debug.LogWarning("[LeaderboardManager] getLeaderboard 요청 실패 - 더미 데이터 사용");
        //         return CreateDummyResult();
        //     }
        //     return ParseResult(raw);
        // }

        // private LeaderboardResult CreateDummyResult()
        // {
        //     string[] dummyNames = { "TrumpKing", "AcePlayer", "CardMaster", "DeckHero", "RoyalFlush",
        //                             "PokerFace", "WildCard", "FullHouse", "StraightUp", "BluffPro" };

        //     LeaderboardResult result = new LeaderboardResult();
        //     result.topN = new List<LeaderboardEntryData>();

        //     for (int i = 0; i < dummyNames.Length; i++)
        //     {
        //         result.topN.Add(new LeaderboardEntryData
        //         {
        //             rank = i + 1,
        //             nickname = dummyNames[i],
        //             currentStage = 100 - (i * 7),
        //             profileImageIndex = i % 4,
        //             profileFrameIndex = i % 3
        //         });
        //     }

        //     result.myEntry = new LeaderboardEntryData
        //     {
        //         rank = 42,
        //         nickname = "ME",
        //         currentStage = 30,
        //         profileImageIndex = 0,
        //         profileFrameIndex = 0
        //     };

        //     return result;
        // }

        // private LeaderboardResult ParseResult(Dictionary<object, object> raw)
        // {
        //     LeaderboardResult result = new LeaderboardResult();
        //     result.topN = new List<LeaderboardEntryData>();

        //     if (raw.ContainsKey("topN") && raw["topN"] is List<object> topNList)
        //     {
        //         foreach (object item in topNList)
        //         {
        //             if (item is Dictionary<object, object> entryDict)
        //             {
        //                 result.topN.Add(ParseEntry(entryDict));
        //             }
        //         }
        //     }

        //     if (raw.ContainsKey("myEntry") && raw["myEntry"] is Dictionary<object, object> myDict)
        //     {
        //         result.myEntry = ParseEntry(myDict);
        //     }

        //     return result;
        // }

        // private LeaderboardEntryData ParseEntry(Dictionary<object, object> dict)
        // {
        //     LeaderboardEntryData entry = new LeaderboardEntryData();
        //     entry.rank = dict.ContainsKey("rank") ? Convert.ToInt32(dict["rank"]) : 0;
        //     entry.nickname = dict.ContainsKey("nickname") ? dict["nickname"].ToString() : "USER";
        //     entry.profileImageIndex = dict.ContainsKey("profileImageIndex") ? Convert.ToInt32(dict["profileImageIndex"]) : 0;
        //     entry.profileFrameIndex = dict.ContainsKey("profileFrameIndex") ? Convert.ToInt32(dict["profileFrameIndex"]) : 0;
        //     entry.currentStage = dict.ContainsKey("currentStage") ? Convert.ToInt32(dict["currentStage"]) : 1;
        //     return entry;
        // }
    }
}
