using Unity.Netcode;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
	public class StarterAssetsInputs : MonoBehaviour
	{
		[Header("Character Input Values")]
		public Vector2 move;
		public Vector2 look;
		public bool jump;
		public bool sprint;

		[Header("Movement Settings")]
		public bool analogMovement;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;

#if ENABLE_INPUT_SYSTEM
		public void OnMove(InputValue value)
		{
			if (InputLogic.Single.BlockPlayerControl)
				return;

			MoveInput(value.Get<Vector2>());
		}

		public void OnLook(InputValue value)
		{

			if (cursorInputForLook)
			{
				LookInput(value.Get<Vector2>());
			}
		}

		public void OnJump(InputValue value)
		{
			if (InputLogic.Single.BlockPlayerControl)
				return;

			JumpInput(value.isPressed);
		}

		public void OnSprint(InputValue value)
		{
		
			SprintInput(value.isPressed);
		}
#endif


		public void MoveInput(Vector2 newMoveDirection)
		{
			
			move = newMoveDirection;
		} 

		public void LookInput(Vector2 newLookDirection)
		{
			
			look = newLookDirection;
		}

		public void JumpInput(bool newJumpState)
		{
			
			jump = newJumpState;
		}

		public void SprintInput(bool newSprintState)
		{
			sprint = newSprintState;
		}
		
		private void OnApplicationFocus(bool hasFocus)
		{
			//SetCursorState(cursorLocked);
		}

		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.F))
			{
                switch (Cursor.lockState)
                {
                    case CursorLockMode.None:
						//SetCursorState(true);
						InputLogic.HideCursor();
						break;
                    case CursorLockMode.Locked:
						//SetCursorState(false);
						InputLogic.ShowCursor();
						break;
                    case CursorLockMode.Confined:
						print("opto");
						break;
                    
                }
            }
		}

	}
	
}