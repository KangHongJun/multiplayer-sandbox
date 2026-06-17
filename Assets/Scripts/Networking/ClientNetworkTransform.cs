using Unity.Netcode.Components;
using UnityEngine;

namespace Game.Networking
{
    /// <summary>
    /// 소유자 권한(owner-authoritative) NetworkTransform.
    /// 기본 NetworkTransform은 서버 권한이라 소유자의 로컬 이동이 복제되지 않는다.
    /// 이 컴포넌트는 소유자를 권한으로 만들어, 각자 자기 캐릭터를 움직이면
    /// 그 위치가 서버를 거쳐 다른 클라에게 복제된다.
    /// 사용: 플레이어 프리팹에서 NetworkTransform 제거 후 이 컴포넌트 추가.
    /// </summary>
    [DisallowMultipleComponent]
    public class ClientNetworkTransform : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative() => false;
    }
}
