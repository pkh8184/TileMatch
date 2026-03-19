using UnityEngine;
using GoogleMobileAds.Api;
using TrumpTile.FrameLibrary;

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

		private BannerView mBannerView;
		private bool mbIsInitialized = false;

		private void Start()
		{
			DontDestroyOnLoad(gameObject);
			InitializeMobileAds();
		}

		private void InitializeMobileAds()
		{
			MobileAds.Initialize(initStatus =>
			{
				mbIsInitialized = true;
				Debug.Log("[AdManager] MobileAds 초기화 완료");
				LoadBannerAd();
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
			DestroyBannerAd();

			mBannerView = new BannerView(GetBannerAdUnitId(), AdSize.Banner, AdPosition.Bottom);

			mBannerView.OnBannerAdLoaded += OnBannerAdLoaded;
			mBannerView.OnBannerAdLoadFailed += OnBannerAdLoadFailed;

			mBannerView.LoadAd(new AdRequest());
			Debug.Log("[AdManager] 배너 광고 로드 요청");

			// 테스트 코드
			ShowBannerAd();
		}

		public void ShowBannerAd()
		{
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

		#endregion

		#region Banner Callbacks

		private void OnBannerAdLoaded()
		{
			Debug.Log("[AdManager] 배너 광고 로드 성공");
		}

		private void OnBannerAdLoadFailed(LoadAdError error)
		{
			Debug.LogWarning($"[AdManager] 배너 광고 로드 실패: {error.GetMessage()}");
		}

		#endregion

		private void OnDestroy()
		{
			DestroyBannerAd();
		}
	}
}
