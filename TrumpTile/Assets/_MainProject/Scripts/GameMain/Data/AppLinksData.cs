using UnityEngine;

namespace TrumpTile.GameMain.Data
{
	/// <summary>
	/// 앱 외부 링크 데이터 (약관, 소셜 등)
	/// ScriptableObject로 관리하여 URL 변경 시 한 곳에서만 수정
	/// </summary>
	[CreateAssetMenu(fileName = "AppLinksData", menuName = "TrumpTile/App Links Data")]
	public class AppLinksData : ScriptableObject
	{
		[Header("약관")]
		[SerializeField] private string mTermsUrl = "";
		[SerializeField] private string mPrivacyUrl = "";

		[Header("소셜")]
		[SerializeField] private string mInstagramUrl = "";
		[SerializeField] private string mTwitterUrl = "";
		[SerializeField] private string mYoutubeUrl = "";

		public string TermsUrl => mTermsUrl;
		public string PrivacyUrl => mPrivacyUrl;
		public string InstagramUrl => mInstagramUrl;
		public string TwitterUrl => mTwitterUrl;
		public string YoutubeUrl => mYoutubeUrl;
	}
}
