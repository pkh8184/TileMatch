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
        
        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            
            // 랜덤 스프라이트 선택
            if (particleSprites != null && particleSprites.Length > 0)
            {
                spriteRenderer.sprite = particleSprites[Random.Range(0, particleSprites.Length)];
            }
        }

        /// <summary>
        /// 색상 설정
        /// </summary>
        public void SetColor(Color color)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = color;
            }
        }
    }
}
