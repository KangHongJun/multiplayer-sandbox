using Unity.Collections;
using Unity.Netcode;
using TMPro;
using UnityEngine;
using Game.Networking;

namespace Game.Player
{
    /// <summary>
    /// 플레이어 표시 + 로깅: 닉네임은 접속 승인 단계(RelayConnector)에서 이미 예약·검증됨.
    /// 서버가 스폰 시 고유 색상 배정 + 예약된 닉네임을 NetworkVariable로 세팅 → 전원 동기화되어
    /// SpriteRenderer 색 + 머리 위 이름표에 적용된다. 접속/퇴장도 서버가 로깅.
    /// </summary>
    public class PlayerIdentity : NetworkBehaviour
    {
        [SerializeField] private SpriteRenderer sprite;   // 색상 적용 대상(보통 루트 SpriteRenderer)
        [SerializeField] private TMP_Text nameLabel;      // 머리 위 이름표(TMP 월드 텍스트)

        // 기본 권한: 서버 쓰기 / 전원 읽기
        private readonly NetworkVariable<Color32> _color = new();
        private readonly NetworkVariable<FixedString64Bytes> _name = new();

        private bool _connectLogged;

        public override void OnNetworkSpawn()
        {
            // 전원: 동기화된 현재값 적용 + 변경 구독(늦게 들어온 클라도 즉시 반영)
            _color.OnValueChanged += OnColorChanged;
            _name.OnValueChanged += OnNameChanged;
            ApplyColor(_color.Value);
            ApplyName(_name.Value);

            if (!IsServer) return;

            // 색상 배정
            var assigner = ColorAssigner.Instance;
            if (assigner != null) _color.Value = assigner.Acquire(OwnerClientId);

            // 승인 단계에서 예약된 닉네임 적용
            var registry = NicknameRegistry.Instance;
            string display = registry != null ? registry.GetName(OwnerClientId) : "";
            if (!string.IsNullOrEmpty(display))
            {
                var fs = new FixedString64Bytes();
                fs.CopyFromTruncated(display);
                _name.Value = fs;
            }

            string playerId = registry != null ? registry.GetPlayerId(OwnerClientId) : "";
            _connectLogged = true;
            Debug.Log($"[Connect] clientId={OwnerClientId} playerId={playerId} name={display}");
        }

        public override void OnNetworkDespawn()
        {
            _color.OnValueChanged -= OnColorChanged;
            _name.OnValueChanged -= OnNameChanged;

            if (!IsServer) return;

            var registry = NicknameRegistry.Instance;
            string display = registry != null ? registry.GetName(OwnerClientId) : _name.Value.ToString();
            string playerId = registry != null ? registry.GetPlayerId(OwnerClientId) : "";
            if (_connectLogged)
                Debug.Log($"[Disconnect] clientId={OwnerClientId} playerId={playerId} name={display}");

            ColorAssigner.Instance?.Release(OwnerClientId);
            registry?.Release(OwnerClientId);
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
