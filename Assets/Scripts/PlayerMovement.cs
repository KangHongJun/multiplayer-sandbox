using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 1단계: 서버 권한 이동 동기화.
/// 소유자 클라이언트는 입력만 읽어 서버로 보내고,
/// 서버가 실제로 위치를 움직인다. NetworkTransform이 전원에게 복제.
/// (물리/예측은 2단계에서 추가)
/// </summary>
public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    // 소유자 클라가 매 프레임 읽는 입력
    private Vector2 _ownerInput;
    // 서버가 보관하는 최신 입력 (이동 적용에 사용)
    private Vector2 _serverInput;

    private void Update()
    {
        // 입력은 자기 캐릭터를 소유한 클라만 읽는다
        if (!IsOwner) return;
        _ownerInput = ReadInput();
    }

    private void FixedUpdate()
    {
        // 소유자: 고정 틱마다 입력을 서버로 전송
        if (IsOwner) SubmitInputRpc(_ownerInput);

        // 서버: 실제 이동 처리 (권한)
        if (IsServer)
        {
            Vector3 delta = (Vector3)(_serverInput * (moveSpeed * Time.fixedDeltaTime));
            transform.position += delta;
        }
    }

    // 서버로만 전송되는 RPC (NGO 2.x 통합 RPC)
    [Rpc(SendTo.Server)]
    private void SubmitInputRpc(Vector2 input)
    {
        _serverInput = input;
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
