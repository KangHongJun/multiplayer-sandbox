using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace Game.Networking
{
    /// <summary>
    /// 슬라이스1: Unity Gaming Services 초기화 + 익명 로그인.
    /// 로그인하면 기기별 고정 PlayerId를 얻는다 (Relay/식별의 기반).
    /// 씬에 빈 오브젝트 하나에 붙여두면 시작 시 자동 로그인.
    /// </summary>
    public class GameNetworkBootstrap : MonoBehaviour
    {
        public static GameNetworkBootstrap Instance { get; private set; }

        /// <summary>접속 시 사용할 닉네임 (UI에서 설정).</summary>
        public string Nickname { get; set; } = "Player";

        /// <summary>로그인 후 채워지는 고정 식별자.</summary>
        public string PlayerId { get; private set; }

        public bool IsSignedIn =>
            UnityServices.State == ServicesInitializationState.Initialized &&
            AuthenticationService.Instance.IsSignedIn;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private async void Start()
        {
            await SignInAsync();
        }

        public async Task SignInAsync()
        {
            try
            {
                if (UnityServices.State == ServicesInitializationState.Uninitialized)
                {
                    var options = new InitializationOptions();

                    // MPPM 가상 플레이어마다 다른 PlayerId를 받기 위해 프로필 분리
                    string profile = GetEditorProfileName();
                    if (!string.IsNullOrEmpty(profile)) options.SetProfile(profile);

                    await UnityServices.InitializeAsync(options);
                }

                if (!AuthenticationService.Instance.IsSignedIn)
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();

                PlayerId = AuthenticationService.Instance.PlayerId;
                Debug.Log($"[Bootstrap] Signed in. PlayerId={PlayerId} Profile={AuthenticationService.Instance.Profile}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Bootstrap] Sign-in failed: {e.Message}");
            }
        }

        // MPPM 태그를 인증 프로필로 사용 → 가상 플레이어마다 고유 PlayerId 확보.
        // (에디터 전용, MPPM 미설치/미사용이면 기본 프로필)
        private static string GetEditorProfileName()
        {
#if UNITY_EDITOR
            try
            {
                var tags = Unity.Multiplayer.PlayMode.CurrentPlayer.Tags;
                if (tags != null && tags.Count > 0)
                    return Sanitize(tags[0]);
            }
            catch { /* MPPM API 미가용 시 무시 */ }
#endif
            return null;
        }

        // 인증 프로필 허용 문자(영숫자/_/-)만, 최대 30자
        private static string Sanitize(string s)
        {
            var arr = s.ToCharArray();
            for (int i = 0; i < arr.Length; i++)
                if (!char.IsLetterOrDigit(arr[i]) && arr[i] != '_' && arr[i] != '-') arr[i] = '_';
            var r = new string(arr);
            return r.Length > 30 ? r.Substring(0, 30) : r;
        }
    }
}
