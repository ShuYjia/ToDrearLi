using Unity.Netcode.Components;
using UnityEngine;

[DisallowMultipleComponent]
public class ClientNetworkTransform : NetworkTransform
{
    // ⚡ 核心魔法：强制告诉 NGO，这个 Transform 的坐标由拥有者（客户端）说了算！
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}