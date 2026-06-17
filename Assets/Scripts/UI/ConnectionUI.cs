using System;
using Unity.Netcode;
using UnityEngine;
using Game.Networking;

namespace Game.UI
{
    /// <summary>
    /// 슬라이스2: 접속 UI (IMGUI, 씬 와이어링 불필요).
    /// 닉네임 입력 + Host/Join 버튼 + join 코드 표시/복사.
    /// 닉네임 중복으로 접속이 거부되면 사유를 표시 → 이름 바꿔 다시 시도.
    /// </summary>
    public class ConnectionUI : MonoBehaviour
    {
        [SerializeField] private int fontSize = 24;

        private string _joinCodeInput = "";
        private string _status = "";
        private bool _hooked;

        private void OnGUI()
        {
            var nm = NetworkManager.Singleton;
            var boot = GameNetworkBootstrap.Instance;
            var relay = RelayConnector.Instance;
            if (nm == null || boot == null || relay == null) return;

            if (!_hooked)
            {
                nm.OnClientDisconnectCallback += OnDisconnect;
                _hooked = true;
            }

            var btn = new GUIStyle(GUI.skin.button) { fontSize = fontSize, fixedHeight = fontSize * 2.2f };
            var lbl = new GUIStyle(GUI.skin.label) { fontSize = fontSize };
            var fld = new GUIStyle(GUI.skin.textField) { fontSize = fontSize, fixedHeight = fontSize * 2.0f };

            GUILayout.BeginArea(new Rect(20, 20, 460, 600));

            if (!nm.IsClient && !nm.IsServer)
            {
                GUILayout.Label(boot.IsSignedIn
                    ? $"ID: {Short(boot.PlayerId)}"
                    : "로그인 중...", lbl);

                GUILayout.Label("닉네임", lbl);
                boot.Nickname = GUILayout.TextField(boot.Nickname, fld);

                GUI.enabled = boot.IsSignedIn;
                if (GUILayout.Button("Host", btn)) HostFlow();

                GUILayout.Space(10);
                GUILayout.Label("Join 코드", lbl);
                _joinCodeInput = GUILayout.TextField(_joinCodeInput, fld);
                if (GUILayout.Button("Join", btn)) JoinFlow();
                GUI.enabled = true;
            }
            else
            {
                string mode = nm.IsHost ? "Host" : nm.IsServer ? "Server" : "Client";
                GUILayout.Label($"Mode: {mode}", lbl);

                if (!string.IsNullOrEmpty(relay.JoinCode))
                {
                    GUILayout.Label($"Code: {relay.JoinCode}", lbl);
                    if (GUILayout.Button("코드 복사", btn))
                        GUIUtility.systemCopyBuffer = relay.JoinCode;
                }

                GUILayout.Label($"Connected: {nm.ConnectedClients.Count}", lbl);
                if (GUILayout.Button("Disconnect", btn)) nm.Shutdown();
            }

            if (!string.IsNullOrEmpty(_status))
                GUILayout.Label(_status, lbl);

            GUILayout.EndArea();
        }

        // 서버가 거부/종료 사유를 보냈으면 표시 (닉네임 중복 등)
        private void OnDisconnect(ulong clientId)
        {
            var nm = NetworkManager.Singleton;
            if (nm != null && !string.IsNullOrEmpty(nm.DisconnectReason))
                _status = $"접속 거부: {nm.DisconnectReason}";
        }

        private void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnDisconnect;
        }

        private async void HostFlow()
        {
            _status = "호스트 생성 중...";
            try
            {
                string code = await RelayConnector.Instance.StartHostAsync();
                _status = $"호스트 시작. 코드: {code}";
            }
            catch (Exception e)
            {
                _status = $"호스트 실패: {e.Message}";
                Debug.LogError($"[ConnectionUI] Host failed: {e}");
            }
        }

        private async void JoinFlow()
        {
            if (string.IsNullOrWhiteSpace(_joinCodeInput))
            {
                _status = "코드를 입력하세요";
                return;
            }
            _status = "접속 중...";
            try
            {
                await RelayConnector.Instance.JoinAsync(_joinCodeInput);
                _status = "접속 요청 완료";
            }
            catch (Exception e)
            {
                _status = $"접속 실패: {e.Message}";
                Debug.LogError($"[ConnectionUI] Join failed: {e}");
            }
        }

        private static string Short(string s) =>
            string.IsNullOrEmpty(s) ? "-" : (s.Length > 8 ? s.Substring(0, 8) : s);
    }
}
