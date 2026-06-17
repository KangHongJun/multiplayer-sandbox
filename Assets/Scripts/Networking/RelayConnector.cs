using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace Game.Networking
{
    /// <summary>
    /// 슬라이스2: Unity Relay로 호스트/조인 + 접속 승인(닉네임 중복 차단).
    /// 접속 시 닉네임/PlayerId를 페이로드로 보내고, 서버가 승인 콜백에서 중복이면 거부한다.
    /// </summary>
    public class RelayConnector : MonoBehaviour
    {
        public static RelayConnector Instance { get; private set; }

        [SerializeField] private int maxPlayers = 10; // 호스트 포함 인원

        /// <summary>호스트가 만든(또는 클라가 입력한) 접속 코드.</summary>
        public string JoinCode { get; private set; }

        [System.Serializable]
        private struct JoinPayload { public string id; public string name; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public async Task<string> StartHostAsync()
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
            JoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(allocation.ToRelayServerData("dtls"));

            PrepareConnection();
            NetworkManager.Singleton.StartHost();
            Debug.Log($"[Relay] Host started. JoinCode={JoinCode}");
            return JoinCode;
        }

        public async Task JoinAsync(string joinCode)
        {
            string code = joinCode.Trim();
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(code);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(allocation.ToRelayServerData("dtls"));

            JoinCode = code;
            PrepareConnection();
            NetworkManager.Singleton.StartClient();
            Debug.Log($"[Relay] Client joining with code {code}");
        }

        // 접속 승인 활성화 + 닉네임/PlayerId 페이로드 설정 (StartHost/StartClient 직전)
        private void PrepareConnection()
        {
            var nm = NetworkManager.Singleton;
            nm.NetworkConfig.ConnectionApproval = true;
            nm.ConnectionApprovalCallback = ApprovalCheck;

            var boot = GameNetworkBootstrap.Instance;
            var payload = new JoinPayload
            {
                id = boot != null ? (boot.PlayerId ?? "") : "",
                name = boot != null ? (boot.Nickname ?? "") : "",
            };
            nm.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));
        }

        // 서버에서만 호출됨. 닉네임이 비었거나 중복이면 접속 거부.
        private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request,
                                   NetworkManager.ConnectionApprovalResponse response)
        {
            string name = "", id = "";
            try
            {
                var p = JsonUtility.FromJson<JoinPayload>(Encoding.UTF8.GetString(request.Payload));
                name = p.name; id = p.id;
            }
            catch { /* 페이로드 파싱 실패 → 빈 닉네임으로 취급 */ }

            var registry = NicknameRegistry.Instance;
            bool ok = registry != null
                ? registry.TryReserve(request.ClientNetworkId, name, id, out _)
                : !string.IsNullOrWhiteSpace(name);

            response.Approved = ok;
            response.CreatePlayerObject = ok;
            if (!ok)
                response.Reason = string.IsNullOrWhiteSpace(name) ? "빈 닉네임" : "이미 사용 중인 닉네임";
        }
    }
}
