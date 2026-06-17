using System.Collections.Generic;
using UnityEngine;

namespace Game.Networking
{
    /// <summary>
    /// 서버 전용 고유 색상 배정기. 고정 팔레트에서 미사용 색을 나눠주고 퇴장 시 회수한다.
    /// 씬에 하나만 두면 됨(RelayConnector와 같은 오브젝트에 붙여도 OK). 서버에서만 호출.
    /// </summary>
    public class ColorAssigner : MonoBehaviour
    {
        public static ColorAssigner Instance { get; private set; }

        // 서로 잘 구분되는 고정 팔레트
        private static readonly Color32[] Palette =
        {
            new Color32(231, 76, 60, 255),   // red
            new Color32(52, 152, 219, 255),  // blue
            new Color32(46, 204, 113, 255),  // green
            new Color32(241, 196, 15, 255),  // yellow
            new Color32(155, 89, 182, 255),  // purple
            new Color32(230, 126, 34, 255),  // orange
            new Color32(26, 188, 156, 255),  // teal
            new Color32(236, 240, 241, 255), // white
            new Color32(52, 73, 94, 255),    // navy
            new Color32(244, 143, 177, 255), // pink
            new Color32(149, 165, 166, 255), // gray
            new Color32(120, 224, 143, 255), // lime
        };

        private readonly Dictionary<ulong, int> _assigned = new();
        private readonly HashSet<int> _used = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>해당 클라에 미사용 색을 배정. 이미 배정돼 있으면 기존 색 반환.</summary>
        public Color32 Acquire(ulong clientId)
        {
            if (_assigned.TryGetValue(clientId, out int existing))
                return Palette[existing];

            int idx = -1;
            for (int i = 0; i < Palette.Length; i++)
                if (!_used.Contains(i)) { idx = i; break; }

            // 팔레트 소진 시 clientId 기반으로 회전 폴백(중복 허용)
            if (idx < 0) idx = (int)(clientId % (ulong)Palette.Length);

            _assigned[clientId] = idx;
            _used.Add(idx);
            return Palette[idx];
        }

        /// <summary>퇴장 시 색 회수.</summary>
        public void Release(ulong clientId)
        {
            if (_assigned.TryGetValue(clientId, out int idx))
            {
                _assigned.Remove(clientId);
                _used.Remove(idx);
            }
        }
    }
}
