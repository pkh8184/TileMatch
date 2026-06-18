using UnityEngine;
using UnityEngine.InputSystem;

namespace TrumpTile.GameMain.Core
{
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
			foreach(var item in hits)
			{
				Debug.Log(item.name);
			}
			TileController topTile = hits[0].GetComponent<TileController>();
			if (topTile != null)
			{
				topTile.TrySelect();
			}
		}
	}
}
