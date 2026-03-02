namespace TrumpTile.FrameLibrary
{
	public class Singleton<T> where T : class, new()
	{
		private static T mInst;

		public static T Inst
		{
			get
			{
				if (mInst == null)
				{
					mInst = new T();
				}
				return mInst;
			}
		}
	}
}
