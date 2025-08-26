using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class RpcWithAckManager : NetworkBehaviour
{
    // ������ �������� ack'��: messageId -> TaskCompletionSource
    private static ConcurrentDictionary<long, TaskCompletionSource<bool>> s_pendingAcks = new();

    // ������ �������� ���� �����, ����� ��������� ������� "������" ClientRpc � ����� ack
    public Task<bool> SendImportantClientRpcAndWaitForAck(ulong clientId, byte[] payload, float timeoutSeconds = 3f)
    {
        var id = GenerateId();
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        s_pendingAcks[id] = tcs;

        // ���������� RPC (reliable �� ���������) � id � payload. ClientRpcMethod � ����.
        MethodClientRpc(id, payload, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } } });

        // �������
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        cts.Token.Register(() =>
        {
            if (s_pendingAcks.TryRemove(id, out var pending))
                pending.TrySetResult(false); // �� �������� ack
        });

        return tcs.Task;
    }

    // ���������� ��������� ��������� RPC
    [ClientRpc]
    private void MethodClientRpc(long messageId, byte[] payload, ClientRpcParams clientRpcParams = default)
    {
        // �������� payload � ���������
        ProcessPayload(payload);

        // ����� �������� ��������� ��� ack ����� �� ������
        // AckServerRpc ������������ �������� � �������
        AckServerRpc(messageId);
    }

    // ������ -> ������: ack RPC
    [ServerRpc(RequireOwnership = false)]
    private void AckServerRpc(long messageId, ServerRpcParams serverRpcParams = default)
    {
        // ������ �������� ack � �������� pending Task
        if (s_pendingAcks.TryRemove(messageId, out var tcs))
        {
            tcs.TrySetResult(true);
        }
    }

    // ����������� ������������ id
    private static long s_lastId = 1;
    private static long GenerateId() => Interlocked.Increment(ref s_lastId);

    private void ProcessPayload(byte[] payload)
    {
        // ��� ��������� ������
    }
}
