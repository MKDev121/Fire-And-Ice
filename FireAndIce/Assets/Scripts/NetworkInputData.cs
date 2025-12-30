using Fusion;
using UnityEngine;

enum MyButtons
{
    Jump = 0,
    Attack = 1
}
public struct NetworkInputData : INetworkInput
{
    public float direction;
    public NetworkButtons buttons;
}
