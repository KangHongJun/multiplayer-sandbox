using System.Collections.Generic;
using UnityEngine;

namespace Game.Networking
{
    /// <summary>
    /// 서버 전용 닉네임 중복 방지 + 접속자 식별 정보 보관.
    /// 접속 승인 단계에서 예약하고(중복이면 실패), 퇴장 시 회수한다.
    /// 씬에 하나만 두면 됨(ColorAssigner와 같은 오브젝트에 붙여도 OK). 서버에서만 호출.
    /// </summary>
    public class NicknameRegistry : MonoBehaviour
    {
        public static NicknameRegistry Instance { get; private set; }

        private const int MaxLen = 16;

        private struct Entry { public string Display; public string PlayerId; public string Key; }

        private readonly Dictionary<string, ulong> _usedKeys = new();  // 정규화 키 -> clientId
        private readonly Dictionary<ulong, Entry> _byClient = new();   // clientId -> 식별 정보

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>
        /// 닉네임 예약 시도. 성공 시 true와 정리된 표시용 이름(display) 반환.
        /// 다른 클라가 같은(대소문자 무시) 이름을 쓰고 있으면 실패 → 접속 거부 근거가 됨.
        /// </summary>
        public bool TryReserve(ulong clientId, string requested, string playerId, out string display)
        {
            display = (requested ?? "").Trim();
            if (display.Length > MaxLen) display = display.Substring(0, MaxLen);
            if (display.Length == 0) return false;   // 빈 닉네임 거부

            string key = display.ToLowerInvariant();

            if (_byClient.TryGetValue(clientId, out var prev) && prev.Key == key)
                return true;   // 같은 클라가 같은 이름 재요청

            if (_usedKeys.TryGetValue(key, out var owner) && owner != clientId)
                return false;  // 다른 클라가 사용 중

            if (_byClient.TryGetValue(clientId, out var old)) _usedKeys.Remove(old.Key);
            _usedKeys[key] = clientId;
            _byClient[clientId] = new Entry { Display = display, PlayerId = playerId ?? "", Key = key };
            return true;
        }

        public string GetName(ulong clientId) => _byClient.TryGetValue(clientId, out var e) ? e.Display : "";
        public string GetPlayerId(ulong clientId) => _byClient.TryGetValue(clientId, out var e) ? e.PlayerId : "";

        public void Release(ulong clientId)
        {
            if (_byClient.TryGetValue(clientId, out var e))
            {
                _byClient.Remove(clientId);
                _usedKeys.Remove(e.Key);
            }
        }
    }
}
