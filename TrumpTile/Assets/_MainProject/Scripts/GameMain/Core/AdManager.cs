using UnityEngine;
using GoogleMobileAds.Api;
using TrumpTile.FrameLibrary;
using TrumpTile.GameMain.Data;

namespace TrumpTile.GameMain.Core
{
	/// <summary>
	/// Google AdMob 광고 관리
	/// 배너 광고 로드/표시/숨김 담당
	/// </summary>
	public class AdManager : Singleton_GameObject<AdManager>
	{
		// 광고 유닛 ID: 에디터/개발 빌드는 구글 공식 테스트 ID, 릴리즈 빌드는 실제 ID를 사용한다.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
		// [테스트] 구글 공식 테스트 유닛 ID — 실광고로 교체 금지 (본인 광고 클릭 = AdMob 정책 위반/계정 정지 위험)
		private const string ANDROID_BANNER_AD_UNIT_ID = "ca-app-pub-3940256099942544/6300978111";
		private const string IOS_BANNER_AD_UNIT_ID = "ca-app-pub-3940256099942544/2934735716";
		private const string ANDROID_REWARDED_AD_UNIT_ID = "ca-app-pub-3940256099942544/5224354917";
		private const string IOS_REWARDED_AD_UNIT_ID = "ca-app-pub-3940256099942544/1712485313";
#else
		// [실제] AdMob 콘솔에서 발급받은 광고 유닛 ID. (iOS는 미출시 → placeholder 유지, iOS 낼 때 교체)
		private const string ANDROID_BANNER_AD_UNIT_ID = "ca-app-pub-8303844869450983/3246966354";
		private const string IOS_BANNER_AD_UNIT_ID = "ca-app-pub-XXXXXXXXXXXXXXXX/XXXXXXXXXX";
		private const string ANDROID_REWARDED_AD_UNIT_ID = "ca-app-pub-8303844869450983/1997784977";
		private const string IOS_REWARDED_AD_UNIT_ID = "ca-app-pub-XXXXXXXXXXXXXXXX/XXXXXXXXXX";
#endif

		private BannerView mBannerView;
		private RewardedAd mRewardedAd;
		private bool mbIsInitialized = false;

		// 광고가 닫힐 때 호출 (bool: 보상 획득 여부)
		private System.Action<bool> mOnRewardedClosed;
		private bool mbRewardEarned = false;

		private bool IsAdsRemoved => PlayerDataManager.Inst != null && PlayerDataManager.Inst.IsAdsRemoved;

        private void Awake()
        {
			if(mbIsInitialized) return;
			DontDestroyOnLoad(gameObject);
			InitializeMobileAds();
        }

		private void OnEnable()
		{
			EventManager.Inst?.AddEvent(EventKeys.REMOVE_ADS_PURCHASED, OnRemoveAdsPurchased);
		}

		private void OnDisable()
		{
			EventManager.Inst?.RemoveEvent(EventKeys.REMOVE_ADS_PURCHASED, OnRemoveAdsPurchased);
		}

		private void OnRemoveAdsPurchased()
		{
			DestroyBannerAd();
		}

		private void InitializeMobileAds()
		{
			// GMA 11.x는 실기기에서 광고 이벤트 콜백을 기본적으로 백그라운드 스레드에서 호출한다.
			// 켜지 않으면 OnAdFullScreenContentClosed 등에서 Unity API 호출이 조용히 예외나며
			// 콜백(if(done)) 로직이 실행되지 않는다. 에디터는 기존 동작 유지를 위해 끈다.
#if UNITY_EDITOR
			MobileAds.RaiseAdEventsOnUnityMainThread = false;
#else
			MobileAds.RaiseAdEventsOnUnityMainThread = true;
#endif

			MobileAds.Initialize(initStatus =>
			{
				mbIsInitialized = true;
				Debug.Log("[AdManager] MobileAds 초기화 완료");
				LoadBannerAd();
				LoadRewardedAd();
			});
		}

		#region Banner

		private string GetBannerAdUnitId()
		{
#if UNITY_ANDROID
			return ANDROID_BANNER_AD_UNIT_ID;
#elif UNITY_IOS
			return IOS_BANNER_AD_UNIT_ID;
#else
			return "unused";
#endif
		}

		public void LoadBannerAd()
		{
			if(IsAdsRemoved)
			{
				return;
			}

			DestroyBannerAd();

			mBannerView = new BannerView(GetBannerAdUnitId(), AdSize.Banner, AdPosition.Bottom);

			mBannerView.OnBannerAdLoaded += OnBannerAdLoaded;
			mBannerView.OnBannerAdLoadFailed += OnBannerAdLoadFailed;

			mBannerView.LoadAd(new AdRequest());
		}

		public void ShowBannerAd()
		{
			if(IsAdsRemoved)
			{
				return;
			}

			if (mBannerView == null)
			{
				Debug.LogWarning("[AdManager] 배너가 없음 - 먼저 LoadBannerAd() 호출 필요");
				return;
			}

			mBannerView.Show();
			Debug.Log("[AdManager] 배너 광고 표시");
		}

		public void HideBannerAd()
		{
			if (mBannerView == null)
			{
				return;
			}

			mBannerView.Hide();
			Debug.Log("[AdManager] 배너 광고 숨김");
		}

		public void DestroyBannerAd()
		{
			if (mBannerView == null)
			{
				return;
			}

			mBannerView.OnBannerAdLoaded -= OnBannerAdLoaded;
			mBannerView.OnBannerAdLoadFailed -= OnBannerAdLoadFailed;

			mBannerView.Destroy();
			mBannerView = null;
			Debug.Log("[AdManager] 배너 광고 제거");
		}
		public float GetBannerHeightForAdjustView()
		{
			//배너가 아직 로드되지 않았거나(오프라인 등) 제거된 경우 0을 반환해 NRE를 방지한다.
			if (mBannerView == null)
			{
				return 0f;
			}
			return mBannerView.GetHeightInPixels();
		}
		#endregion

		#region Banner Callbacks

		private void OnBannerAdLoaded()
		{
			Debug.Log("[AdManager] 배너 광고 로드 성공");
			//ShowBannerAd();
        }

		private void OnBannerAdLoadFailed(LoadAdError error)
		{
			Debug.LogWarning($"[AdManager] 배너 광고 로드 실패: {error.GetMessage()}");
		}

		#endregion

		#region Rewarded

		private string GetRewardedAdUnitId()
		{
#if UNITY_ANDROID
			return ANDROID_REWARDED_AD_UNIT_ID;
#elif UNITY_IOS
			return IOS_REWARDED_AD_UNIT_ID;
#else
			return "unused";
#endif
		}

		public void LoadRewardedAd()
		{
			// 기존 광고가 있으면 정리
			if (mRewardedAd != null)
			{
				mRewardedAd.Destroy();
				mRewardedAd = null;
			}

			RewardedAd.Load(GetRewardedAdUnitId(), new AdRequest(), (RewardedAd ad, LoadAdError error) =>
			{
				if (error != null || ad == null)
				{
					Debug.LogWarning($"[AdManager] 리워드 광고 로드 실패: {error?.GetMessage()}");
					return;
				}

				Debug.Log("[AdManager] 리워드 광고 로드 성공");
				mRewardedAd = ad;
				RegisterRewardedEventHandlers(mRewardedAd);
			});
		}

		/// <summary>
		/// 리워드 광고 표시.
		/// onClosed 는 광고가 닫힌 뒤 호출되며, 보상 획득 여부(bool)를 전달한다.
		/// 광고가 준비되지 않은 경우 즉시 onClosed(false) 호출.
		/// </summary>
		public void ShowRewardedAd(System.Action<bool> onClosed)
		{
			if (mRewardedAd != null && mRewardedAd.CanShowAd())
			{
				mOnRewardedClosed = onClosed;
				mbRewardEarned = false;

				mRewardedAd.Show((Reward reward) =>
				{
					// 보상 획득 (아직 닫히지 않음). 닫힘 처리는 OnAdFullScreenContentClosed 에서
					mbRewardEarned = true;
				});
			}
			else
			{
				Debug.LogWarning("[AdManager] 리워드 광고가 준비되지 않음");
				LoadRewardedAd(); // 다음을 위해 재로드
				onClosed?.Invoke(false);
			}
		}

		private void RegisterRewardedEventHandlers(RewardedAd ad)
		{
			// 광고를 닫으면 콜백 호출 후 다음 광고 미리 로드
			ad.OnAdFullScreenContentClosed += () =>
			{
				Debug.Log("[AdManager] 리워드 광고 닫힘 - 재로드");
				HandleRewardedClosed(mbRewardEarned);
			};

			ad.OnAdFullScreenContentFailed += (AdError error) =>
			{
				Debug.LogWarning($"[AdManager] 리워드 광고 표시 실패: {error.GetMessage()}");
				HandleRewardedClosed(false);
			};
		}

		private void HandleRewardedClosed(bool bRewardEarned)
		{
			// 콜백을 먼저 떼어내 재진입/중복 호출 방지
			System.Action<bool> onClosed = mOnRewardedClosed;
			mOnRewardedClosed = null;
			mbRewardEarned = false;

			InvokeRevivedThenReloadAd(bRewardEarned, onClosed, LoadRewardedAd);
		}

		/// <summary>
		/// onClosed(게임 재개)를 먼저 실행한 뒤 reloadAd(다음 광고 사전 로드)를 실행한다.
		/// LoadRewardedAd()는 방금 닫힌 광고 객체를 그 광고 자신의 콜백 스택 안에서 Destroy()하는
		/// 재진입 호출이라 예외 발생 가능성이 있다. 순서를 바꾸지 않으면 그 예외가 onClosed 실행을
		/// 막아 "광고를 끝까지 봤는데도 게임이 재개되지 않는" 증상으로 이어진다.
		/// </summary>
		public static void InvokeRevivedThenReloadAd(bool bRewardEarned, System.Action<bool> onClosed, System.Action reloadAd)
		{
			try
			{
				onClosed?.Invoke(bRewardEarned);
			}
			finally
			{
				reloadAd?.Invoke();
			}
		}

		#endregion

		private void OnDestroy()
		{
			if (Inst == this)
			{
				DestroyBannerAd();
				if (mRewardedAd != null)
				{
					mRewardedAd.Destroy();
					mRewardedAd = null;
				}
			}
		}
	}
}
