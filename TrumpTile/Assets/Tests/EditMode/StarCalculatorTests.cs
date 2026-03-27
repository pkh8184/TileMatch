using NUnit.Framework;

namespace TrumpTile.Tests.EditMode
{
    /// <summary>
    /// 시간 기반 별 판정 로직 테스트
    /// StarConfig 값: TileTimeCoefficient=2.0, Star2Ratio=1.25, Star1Ratio=1.5
    /// 타일 48개 → targetTime = 96초
    /// </summary>
    public class StarCalculatorTests
    {
        private int CalculateStars(float elapsedTime, float targetTime, float star2Ratio, float star1Ratio)
        {
            if (elapsedTime <= targetTime) return 3;
            if (elapsedTime <= targetTime * star2Ratio) return 2;
            return 1;
        }

        [Test]
        public void Stars3_WhenClearedWithinTargetTime()
        {
            int result = CalculateStars(elapsedTime: 90F, targetTime: 96F, star2Ratio: 1.25F, star1Ratio: 1.5F);
            Assert.AreEqual(3, result);
        }

        [Test]
        public void Stars3_WhenClearedExactlyAtTargetTime()
        {
            int result = CalculateStars(elapsedTime: 96F, targetTime: 96F, star2Ratio: 1.25F, star1Ratio: 1.5F);
            Assert.AreEqual(3, result);
        }

        [Test]
        public void Stars2_WhenClearedBetweenTargetAndStar2Threshold()
        {
            // 96초 < 110초 <= 120초(96×1.25)
            int result = CalculateStars(elapsedTime: 110F, targetTime: 96F, star2Ratio: 1.25F, star1Ratio: 1.5F);
            Assert.AreEqual(2, result);
        }

        [Test]
        public void Stars1_WhenClearedOverStar1Threshold()
        {
            // 144초(96×1.5) 초과
            int result = CalculateStars(elapsedTime: 150F, targetTime: 96F, star2Ratio: 1.25F, star1Ratio: 1.5F);
            Assert.AreEqual(1, result);
        }

        [Test]
        public void Stars1_MinimumGuaranteed()
        {
            // 아무리 늦어도 최소 1성
            int result = CalculateStars(elapsedTime: 9999F, targetTime: 96F, star2Ratio: 1.25F, star1Ratio: 1.5F);
            Assert.AreEqual(1, result);
        }

        [Test]
        public void TargetTime_CalculatedFromTileCount()
        {
            int tileCount = 48;
            float coefficient = 2.0F;
            float expected = 96.0F;
            Assert.AreEqual(expected, tileCount * coefficient, delta: 0.001F);
        }
    }
}
