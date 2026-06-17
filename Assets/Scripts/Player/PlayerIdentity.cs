using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using Game.Networking;

namespace Game.Player
{
    /// <summary>
    /// 소유자가 자기 PlayerId/닉네임을 서버에 제출하면, 서버가 접속/퇴장을 콘솔에 로깅한다.
    /// (슬라이스 3의 색상/이름표 동기화가 이 제출 채널을 재사용할 예정)
    /// </summary>
    public class PlayerIdentity : NetworkBehaviour
    {
        // 서버에서만 채워짐. 퇴장 로그에서 이름/ID를 다시 쓰기 위해 보관.
        private FixedString64Bytes _playerId;
        private FixedString64Bytes _nickname;
        private bool _identityReceived;

        public override void OnNetworkSpawn()
        {
            // 자기 자신만 식별 정보를 서버로 제출
            if (!IsOwner) return;

            var boot = GameNetworkBootstrap.Instance;
            string id = boot != null ? boot.PlayerId : null;
            string nickname = boot != null ? boot.Nickname : null;

            // 길이 초과 시 throw 대신 잘라서 담음 (긴 닉네임 방어)
            var idFs = new FixedString64Bytes();
            idFs.CopyFromTruncated(id ?? "");
            var nameFs = new FixedString64Bytes();
            nameFs.CopyFromTruncated(nickname ?? "");

            SubmitIdentityServerRpc(idFs, nameFs);
        }

        [ServerRpc]
        private void SubmitIdentityServerRpc(FixedString64Bytes playerId, FixedString64Bytes nickname)
        {
            _playerId = playerId;
            _nickname = nickname;
            _identityReceived = true;
            Debug.Log($"[Connect] clientId={OwnerClientId} playerId={playerId} name={nickname}");
        }

        public override void OnNetworkDespawn()
        {
            // 서버에서만, 그리고 식별 정보를 받은 경우에만 퇴장 로그
            if (!IsServer || !_identityReceived) return;
            Debug.Log($"[Disconnect] clientId={OwnerClientId} playerId={_playerId} name={_nickname}");
        }
    }
}
