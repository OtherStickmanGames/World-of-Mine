using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;
using Unity.Netcode;

public class IdGenerator
{
    private long _lastId = 0;

    // ¬озвращает уникальный id как ulong
    public ulong GenerateId()
    {
        long next = Interlocked.Increment(ref _lastId);
        return (ulong)next;
    }
}


// --- Sender side (server) ---
public class FragmentedSender : NetworkBehaviour
{
    // ѕараметры Ч подгон€й под свой MaxPayloadSize
    private const int ChunkSize = 1000; // байт на фрагмент (payload only)
    private const int AckTimeoutMs = 5000;
    private const int MaxRetries = 3;

    private ulong _nextMessageId = 1;

    private IdGenerator idGenerator = new();

    public void SendBuildingToClient(ulong clientId, BuildingServerData data)
    {
        var bytes = SerializeBuilding(data);
        var messageId = idGenerator.GenerateId(); // где idGenerator Ч экземпл€р класса выше
        SendFragmentsReliable(clientId, messageId, bytes);
        //можно стартовать ожидание ack и retry, но ниже мы делаем простую версию с retry
    }

    private void SendFragmentsReliable(ulong clientId, ulong messageId, byte[] bytes)
    {
        int total = (bytes.Length + ChunkSize - 1) / ChunkSize;
        for (int i = 0; i < total; i++)
        {
            int offset = i * ChunkSize;
            int len = Math.Min(ChunkSize, bytes.Length - offset);
            byte[] frag = new byte[len];
            Array.Copy(bytes, offset, frag, 0, len);

            // ќтправл€ем фрагмент клиенту
            BuildingFragmentClientRpc(messageId, i, total, frag,
                new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } } });
        }

        // «апустить Coroutine/таймер дл€ ожидани€ ack и retry Ч в реальном проекте об€зательно
        // ћожно хранить pending set и на AckServerRpc удал€ть.
    }

    private byte[] SerializeBuilding(BuildingServerData d)
    {
        using (var ms = new MemoryStream())
        using (var bw = new BinaryWriter(ms))
        {
            // positions
            if (d.positions != null)
            {
                bw.Write(d.positions.Length);
                foreach (var v in d.positions)
                {
                    bw.Write(v.x);
                    bw.Write(v.y);
                    bw.Write(v.z);
                }
            }
            else bw.Write(0);

            // blockIDs
            if (d.blockIDs != null)
            {
                bw.Write(d.blockIDs.Length);
                bw.Write(d.blockIDs);
            }
            else bw.Write(0);

            // strings (UTF8)
            WriteString(bw, d.nameBuilding);
            WriteString(bw, d.authorName);
            WriteString(bw, d.guid);

            bw.Write(d.countLikes);
            bw.Write(d.liked);

            bw.Flush();
            return ms.ToArray();
        }
    }

    private void WriteString(BinaryWriter bw, string s)
    {
        if (s == null) { bw.Write(0); return; }
        var bytes = System.Text.Encoding.UTF8.GetBytes(s);
        bw.Write(bytes.Length);
        bw.Write(bytes);
    }

    // RPC: фрагмент
    [ClientRpc]
    private void BuildingFragmentClientRpc(ulong messageId, int fragmentIndex, int totalFragments, byte[] fragmentData, ClientRpcParams clientRpcParams = default)
    {
        // Ётот метод выполн€етс€ на клиенте (см. клиентскую часть ниже)
    }

    // Server будет получать Ack от клиента:
    [ServerRpc(RequireOwnership = false)]
    public void FragmentAckServerRpc(ulong messageId, ServerRpcParams serverRpcParams = default)
    {
        // отметь messageId как доставленный Ч убери из pending, прекрати retry
        Debug.Log($"Server: got ACK for message {messageId} from client {serverRpcParams.Receive.SenderClientId}");
    }
}
