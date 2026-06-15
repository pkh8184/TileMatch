using UnityEngine;
using UnityEngine.InputSystem;

namespace TrumpTile.GameMain.Core
{
	/// <summary>
	/// 타일 터치 입력을 이벤트 기반(InputSystem Pointer press 액션)으로 처리한다.
	///
	/// [도입 배경]
	/// 기존 TileController.OnMouseDown 은 프레임당 1회 폴링 + 마우스 에뮬레이션이라,
	/// GC 히치 등으로 한 프레임이 길어지면 그 안에서 끝난 짧은 탭이 스냅샷에 안 잡혀 누락됐다.
	/// Pointer 의 press 액션은 입력 이벤트 큐에서 눌림 이벤트마다 콜백을 주므로 긴 프레임에서도 누락되지 않는다.
	///
	/// &lt;Pointer&gt;/press 는 마우스(에디터)와 터치스크린(실기)을 모두 포괄하므로
	/// EnhancedTouch/TouchSimulation 없이 양쪽에서 동일하게 동작한다.
	///
	/// 게임씬에만 배치해 사용한다. 화면 좌표를 월드로 변환해
	/// 그 지점의 타일 중 최상단 레이어를 결정적으로 골라 TileController.TrySelect 를 호출한다.
	/// </summary>
	public class TileTouchInputManager : MonoBehaviour
	{
		private InputAction mPressAction;

		private void OnEnable()
		{
			mPressAction = new InputAction("TilePress", InputActionType.Button, "<Pointer>/press");
			mPressAction.performed += OnPress;
			mPressAction.Enable();
		}

		private void OnDisable()
		{
			if (mPressAction == null)
			{
				return;
			}

			mPressAction.performed -= OnPress;
			mPressAction.Disable();
			mPressAction.Dispose();
			mPressAction = null;
		}

		private void OnPress(InputAction.CallbackContext context)
		{
			Pointer pointer = context.control.device as Pointer;
			if (pointer == null)
			{
				return;
			}

			Camera cam = Camera.main;
			if (cam == null)
			{
				return;
			}

			Vector2 screenPos = pointer.position.ReadValue();
			Vector3 worldPoint = cam.ScreenToWorldPoint(screenPos);
			Vector2 point2D = new Vector2(worldPoint.x, worldPoint.y);

			Collider2D[] hits = Physics2D.OverlapPointAll(point2D);
			if (hits == null || hits.Length == 0)
			{
				return;
			}

			TileController topTile = ResolveTopTile(hits);
			if (topTile != null)
			{
				// 차단/슬롯풀/애니메이션 등 모든 가드와 거부 피드백은 TrySelect 내부에서 그대로 처리된다.
				topTile.TrySelect();
			}
		}

		/// <summary>
		/// 겹친 타일들 중 레이어가 가장 높은(시각적으로 맨 위) 타일을 결정적으로 고른다.
		/// OnMouseDown 의 비결정적 단일 선택과 달리 항상 같은 결과를 보장한다.
		/// </summary>
		private TileController ResolveTopTile(Collider2D[] hits)
		{
			TileController topTile = null;
			foreach (Collider2D hit in hits)
			{
				if (hit == null)
				{
					continue;
				}

				TileController tile = hit.GetComponent<TileController>();
				if (tile == null)
				{
					continue;
				}

				if (topTile == null || tile.Layer > topTile.Layer)
				{
					topTile = tile;
				}
			}

			return topTile;
		}
	}
}
