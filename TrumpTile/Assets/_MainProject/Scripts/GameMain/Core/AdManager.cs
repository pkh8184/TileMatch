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
		// 테스트용 광고 ID (실제 배포 시 교체 필요)
		private const string ANDROID_BANNER_AD_UNIT_ID = "ca-app-pub-3940256099942544/6300978111";
		private const string IOS_BANNER_AD_UNIT_ID = "ca-app-pub-3940256099942544/2934735716";
		private const string ANDROID_REWARDED_AD_UNIT_ID = "ca-app-pub-3940256099942544/5224354917";
		private const string IOS_REWARDED_AD_UNIT_ID = "ca-app-pub-3940256099942544/1712485313";

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
			EventManager.Inst?.AddEvent("RemoveAdsPurchased", OnRemoveAdsPurchased);
		}

		private void OnDisable()
		{
			EventManager.Inst?.RemoveEvent("RemoveAdsPurchased", OnRemoveAdsPurchased);
		}

		private void OnRemoveAdsPurchased()
		{
			DestroyBannerAd();
		}

		private void InitializeMobileAds()
		{
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

			LoadRewardedAd();
			onClosed?.Invoke(bRewardEarned);
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
