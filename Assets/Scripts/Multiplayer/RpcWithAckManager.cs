using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class RpcWithAckManager : NetworkBehaviour
{
    // Храним ожидания ack'ов: messageId -> TaskCompletionSource
    private static ConcurrentDictionary<long, TaskCompletionSource<bool>> s_pendingAcks = new();

    // Сервер вызывает этот метод, чтобы отправить клиенту "важный" ClientRpc и ждать ack
    public Task<bool> SendImportantClientRpcAndWaitForAck(ulong clientId, byte[] payload, float timeoutSeconds = 3f)
    {
        var id = GenerateId();
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        s_pendingAcks[id] = tcs;

        // Отправляем RPC (reliable по умолчанию) с id и payload. ClientRpcMethod — ниже.
        MethodClientRpc(id, payload, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } } });

        // таймаут
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        cts.Token.Register(() =>
        {
            if (s_pendingAcks.TryRemove(id, out var pending))
                pending.TrySetResult(false); // не получили ack
        });

        return tcs.Task;
    }

    // Клиентская обработка входящего RPC
    [ClientRpc]
    private void MethodClientRpc(long messageId, byte[] payload, ClientRpcParams clientRpcParams = default)
    {
        // распарсь payload и обработай
        ProcessPayload(payload);

        // после успешной обработки шлём ack назад на сервер
        // AckServerRpc отправляется клиентом к серверу
        AckServerRpc(messageId);
    }

    // Клиент -> сервер: ack RPC
    [ServerRpc(RequireOwnership = false)]
    private void AckServerRpc(long messageId, ServerRpcParams serverRpcParams = default)
    {
        // Сервер получает ack — резолвим pending Task
        if (s_pendingAcks.TryRemove(messageId, out var tcs))
        {
            tcs.TrySetResult(true);
        }
    }

    // Гарантируем уникальность id
    private static long s_lastId = 1;
    private static long GenerateId() => Interlocked.Increment(ref s_lastId);

    private void ProcessPayload(byte[] payload)
    {
        // код обработки данных
    }
}
