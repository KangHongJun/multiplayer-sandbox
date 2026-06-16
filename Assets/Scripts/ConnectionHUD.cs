using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 로컬/MPPM 테스트용 임시 접속 버튼.
/// 빈 GameObject에 붙여두면 좌상단에 Host/Client/Server 버튼이 나온다.
/// (정식 방/접속 UI는 3단계 Relay+Lobby에서 교체)
/// </summary>
public class ConnectionHUD : MonoBehaviour
{
    [SerializeField] private int buttonWidth = 260;
    [SerializeField] private int buttonHeight = 70;
    [SerializeField] private int fontSize = 30;

    private void OnGUI()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        // 큰 버튼/라벨 스타일
        var btn = new GUIStyle(GUI.skin.button) { fontSize = fontSize, fixedHeight = buttonHeight };
        var lbl = new GUIStyle(GUI.skin.label) { fontSize = fontSize };

        GUILayout.BeginArea(new Rect(20, 20, buttonWidth, 400));

        if (!nm.IsClient && !nm.IsServer)
        {
            if (GUILayout.Button("Host", btn)) nm.StartHost();
            if (GUILayout.Button("Client", btn)) nm.StartClient();
            if (GUILayout.Button("Server", btn)) nm.StartServer();
        }
        else
        {
            GUILayout.Label($"Mode: {(nm.IsHost ? "Host" : nm.IsServer ? "Server" : "Client")}", lbl);
            GUILayout.Label($"Connected: {nm.ConnectedClients.Count}", lbl);
            if (GUILayout.Button("Disconnect", btn)) nm.Shutdown();
        }

        GUILayout.EndArea();
    }
}
