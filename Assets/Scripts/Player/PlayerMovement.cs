using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    /// <summary>
    /// 소유자 권한(owner-authoritative) 이동.
    /// 각 플레이어는 자기 소유 캐릭터를 로컬에서 직접 움직이고,
    /// NetworkTransform(권한 = Owner)이 그 위치를 전원에게 복제한다.
    /// ※ NetworkTransform의 Authority Mode를 반드시 Owner로 둬야 함.
    /// </summary>
    public class PlayerMovement : NetworkBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;

        private void Update()
        {
            // 자기 캐릭터만 조종 (소유자가 직접 이동 → NetworkTransform이 복제)
            if (!IsOwner) return;

            Vector2 input = ReadInput();
            transform.position += (Vector3)(input * (moveSpeed * Time.deltaTime));
        }

        private static Vector2 ReadInput()
        {
            var kb = Keyboard.current;
            if (kb == null) return Vector2.zero; // Input System이 활성화돼 있어야 함

            Vector2 v = Vector2.zero;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v.y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v.y -= 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) v.x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) v.x += 1f;
            return v.normalized;
        }
    }
}
