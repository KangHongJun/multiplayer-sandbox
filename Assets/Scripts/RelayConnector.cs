using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

/// <summary>
/// 슬라이스2: Unity Relay로 호스트/조인.
/// 호스트는 할당을 만들고 join 코드를 생성, 클라는 코드로 접속한다.
/// 전송 계층(UnityTransport)에 Relay 데이터를 꽂은 뒤 NGO를 시작.
/// </summary>
public class RelayConnector : MonoBehaviour
{
    public static RelayConnector Instance { get; private set; }

    [SerializeField] private int maxPlayers = 10; // 호스트 포함 인원

    /// <summary>호스트가 만든(또는 클라가 입력한) 접속 코드.</summary>
    public string JoinCode { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public async Task<string> StartHostAsync()
    {
        // CreateAllocation의 인자는 "호스트를 제외한" 최대 연결 수
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
        JoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetRelayServerData(allocation.ToRelayServerData("dtls"));

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
        NetworkManager.Singleton.StartClient();
        Debug.Log($"[Relay] Client joining with code {code}");
    }
}
