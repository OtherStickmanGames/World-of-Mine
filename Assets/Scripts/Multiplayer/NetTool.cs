using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public static class NetTool
{
    public static ClientRpcParams GetTargetClientParams(ServerRpcParams serverRpcParams)
    {
        ClientRpcParams clientRpcParams = default;
        clientRpcParams.Send.TargetClientIds = new ulong[] { serverRpcParams.Receive.SenderClientId };

        return clientRpcParams;
    }

    public static ClientRpcParams GetTargetClientParams(params ulong[] clientId)
    {
        ClientRpcParams clientRpcParams = default;
        clientRpcParams.Send.TargetClientIds = clientId;

        return clientRpcParams;
    }


    public static ClientRpcParams GetTargetClientParams(ulong clientId)
    {
        ClientRpcParams clientRpcParams = default;
        clientRpcParams.Send.TargetClientIds = new ulong[] { clientId };

        return clientRpcParams;
    }
}
