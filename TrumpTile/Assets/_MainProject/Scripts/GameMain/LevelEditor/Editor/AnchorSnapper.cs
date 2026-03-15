#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace TrumpTile.LevelEditor.Editor
{
    /// <summary>
    /// UI 앵커 자동 조정을 위한 유틸리티 클래스입니다.
    /// UI 선택 후 Crtl + [ 키를 누르면 앵커가 자동으로 UI 사이즈에 맞춰집니다.
    /// 여러 UI에 동시에 적용할 수 있습니다.
    /// </summary>
    public class AnchorSnapper
    {
        [MenuItem("Custom/Anchors to Corners %[")]
        public static void AnchorsToCorners()
        {
            foreach (Transform transform in Selection.transforms)
            {
                RectTransform t = transform as RectTransform;
                RectTransform p = transform.parent as RectTransform;

                if (t == null || p == null) continue;

                Vector2 newAnchorsMin = new Vector2(t.anchorMin.x + t.offsetMin.x / p.rect.width,
                                                    t.anchorMin.y + t.offsetMin.y / p.rect.height);
                Vector2 newAnchorsMax = new Vector2(t.anchorMax.x + t.offsetMax.x / p.rect.width,
                                                    t.anchorMax.y + t.offsetMax.y / p.rect.height);

                t.anchorMin = newAnchorsMin;
                t.anchorMax = newAnchorsMax;
                t.offsetMin = t.offsetMax = Vector2.zero;
            }
        }
    }
}
#endif