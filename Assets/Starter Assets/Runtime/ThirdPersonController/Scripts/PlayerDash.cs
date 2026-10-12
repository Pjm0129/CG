using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StarterAssets
{
    public class PlayerDash : MonoBehaviour
    {
        [Header("Dash Settings")]
        [SerializeField] private float dashSpeed = 20f;
        [SerializeField] private float dashDuration = 0.18f;
        [SerializeField] private float dashCooldown = 0.6f;

        private CharacterController controller;
        private bool canDash = true;
        private bool isDashing;

        public bool IsDashing => isDashing;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();

            if (controller == null)
            {
                Debug.LogError("PlayerDash: CharacterController가 필요합니다.", this);
            }
        }

        private void Update()
        {
            if (!canDash || isDashing || controller == null)
                return;

            if (Mouse.current != null &&
                Mouse.current.rightButton.wasPressedThisFrame)
            {
                StartCoroutine(Dash());
            }
        }

        private IEnumerator Dash()
        {
            canDash = false;
            isDashing = true;

            Vector3 dashDirection = GetDashDirection();
            float timer = 0f;

            while (timer < dashDuration)
            {
                controller.Move(
                    dashDirection * dashSpeed * Time.deltaTime
                );

                timer += Time.deltaTime;
                yield return null;
            }

            isDashing = false;

            yield return new WaitForSeconds(dashCooldown);

            canDash = true;
        }

        private Vector3 GetDashDirection()
        {
            Vector2 input = Vector2.zero;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed) input.y += 1f;
                if (Keyboard.current.sKey.isPressed) input.y -= 1f;
                if (Keyboard.current.dKey.isPressed) input.x += 1f;
                if (Keyboard.current.aKey.isPressed) input.x -= 1f;
            }

            Camera mainCamera = Camera.main;

            if (mainCamera == null)
                return transform.forward;

            Transform cam = mainCamera.transform;

            Vector3 forward = cam.forward;
            Vector3 right = cam.right;

            forward.y = 0f;
            right.y = 0f;

            forward.Normalize();
            right.Normalize();

            Vector3 direction =
                forward * input.y +
                right * input.x;

            if (direction.sqrMagnitude < 0.01f)
                direction = transform.forward;

            return direction.normalized;
        }
    }
}
