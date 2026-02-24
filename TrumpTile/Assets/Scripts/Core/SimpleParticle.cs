using UnityEngine;

namespace TrumpTile.Effects
{
    /// <summary>
    /// 간단한 파티클 이펙트 (프리팹용)
    /// SpriteRenderer만 있으면 동작
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SimpleParticle : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Sprite[] particleSprites;

        private SpriteRenderer mSpriteRenderer;

        private void Awake()
        {
            mSpriteRenderer = GetComponent<SpriteRenderer>();

            // 랜덤 스프라이트 선택
            if (particleSprites != null && particleSprites.Length > 0)
            {
                mSpriteRenderer.sprite = particleSprites[Random.Range(0, particleSprites.Length)];
            }
        }

        /// <summary>
        /// 색상 설정
        /// </summary>
        public void SetColor(Color color)
        {
            if (mSpriteRenderer != null)
            {
                mSpriteRenderer.color = color;
            }
        }
    }
}
