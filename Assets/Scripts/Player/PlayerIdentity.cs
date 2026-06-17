using Unity.Collections;
using Unity.Netcode;
using TMPro;
using UnityEngine;
using Game.Networking;

namespace Game.Player
{
    /// <summary>
    /// 플레이어 식별: 소유자가 PlayerId/닉네임을 서버에 제출 → 서버가 고유 색상 배정 + 접속/퇴장 로깅.
    /// 색상/닉네임은 NetworkVariable로 전원 동기화되어 SpriteRenderer 색 + 머리 위 이름표에 적용된다.
    /// </summary>
    public class PlayerIdentity : NetworkBehaviour
    {
        [SerializeField] private SpriteRenderer sprite;   // 색상 적용 대상(보통 루트 SpriteRenderer)
        [SerializeField] private TMP_Text nameLabel;      // 머리 위 이름표(TMP 월드 텍스트)

        // 기본 권한: 서버 쓰기 / 전원 읽기 — 정확히 우리가 원하는 것
        private readonly NetworkVariable<Color32> _color = new();
        private readonly NetworkVariable<FixedString64Bytes> _name = new();

        // 서버에서만 보관(퇴장 로그용)
        private FixedString64Bytes _playerId;
        private bool _identityReceived;

        public override void OnNetworkSpawn()
        {
            // 전원: 동기화된 현재값 적용 + 이후 변경 구독 (늦게 들어온 클라도 즉시 반영)
            _color.OnValueChanged += OnColorChanged;
            _name.OnValueChanged += OnNameChanged;
            ApplyColor(_color.Value);
            ApplyName(_name.Value);

            // 서버: 접속 즉시 고유 색 배정(클라 정보 불필요)
            if (IsServer)
            {
                var assigner = ColorAssigner.Instance;
                if (assigner != null) _color.Value = assigner.Acquire(OwnerClientId);
            }

            // 소유자: 자기 식별 정보 제출
            if (IsOwner)
            {
                var boot = GameNetworkBootstrap.Instance;
                var idFs = new FixedString64Bytes();
                idFs.CopyFromTruncated(boot != null ? (boot.PlayerId ?? "") : "");
                var nameFs = new FixedString64Bytes();
                nameFs.CopyFromTruncated(boot != null ? (boot.Nickname ?? "") : "");
                SubmitIdentityServerRpc(idFs, nameFs);
            }
        }

        public override void OnNetworkDespawn()
        {
            _color.OnValueChanged -= OnColorChanged;
            _name.OnValueChanged -= OnNameChanged;

            if (!IsServer) return;
            ColorAssigner.Instance?.Release(OwnerClientId);
            if (_identityReceived)
                Debug.Log($"[Disconnect] clientId={OwnerClientId} playerId={_playerId} name={_name.Value}");
        }

        [ServerRpc]
        private void SubmitIdentityServerRpc(FixedString64Bytes playerId, FixedString64Bytes nickname)
        {
            _playerId = playerId;
            _identityReceived = true;
            _name.Value = nickname;   // 전원에 동기화 → 이름표 갱신
            Debug.Log($"[Connect] clientId={OwnerClientId} playerId={playerId} name={nickname}");
        }

        private void OnColorChanged(Color32 _, Color32 next) => ApplyColor(next);
        private void OnNameChanged(FixedString64Bytes _, FixedString64Bytes next) => ApplyName(next);

        private void ApplyColor(Color32 c)
        {
            // 기본값(투명) 단계에선 적용하지 않아 스폰 직후 깜빡임 방지(팔레트 색은 a=255)
            if (sprite != null && c.a != 0) sprite.color = c;
        }

        private void ApplyName(FixedString64Bytes n)
        {
            if (nameLabel != null) nameLabel.text = n.ToString();
        }
    }
}
