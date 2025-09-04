using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ziruGpt
{
    // sender side (например сервер -> клиент или клиент -> сервер)
    // chunkSize: берем 1000 байт payload (под header)
    const int ChunkSize = 1000;

    // сериализация структуры в byte[]
    static byte[] SerializeBuilding(BuildingServerData data)
    {
        using (var ms = new System.IO.MemoryStream())
        using (var bw = new System.IO.BinaryWriter(ms))
        {
            // positions
            bw.Write(data.positions?.Length ?? 0);
            if (data.positions != null)
            {
                foreach (var v in data.positions)
                {
                    bw.Write(v.x);
                    bw.Write(v.y);
                    bw.Write(v.z);
                }
            }

            // blockIDs
            bw.Write(data.blockIDs?.Length ?? 0);
            if (data.blockIDs != null)
                bw.Write(data.blockIDs);

            // strings (write as UTF8 with length)
            void WriteString(string s)
            {
                if (string.IsNullOrEmpty(s)) { bw.Write(0); return; }
                var b = System.Text.Encoding.UTF8.GetBytes(s);
                bw.Write(b.Length);
                bw.Write(b);
            }
            WriteString(data.nameBuilding);
            WriteString(data.authorName);
            WriteString(data.guid);

            bw.Write(data.countLikes);
            bw.Write(data.liked);

            bw.Flush();
            return ms.ToArray();
        }
    }

    // отправка (например на сервере, шлём клиенту clientId)
    public async System.Threading.Tasks.Task SendBuildingFragmentedAsync(ulong clientId, BuildingServerData data)
    {
        var bytes = SerializeBuilding(data);
        var messageId = GenerateMessageId(); // ulong, атомарно
        int totalFragments = (bytes.Length + ChunkSize - 1) / ChunkSize;

        for (int i = 0; i < totalFragments; i++)
        {
            int offset = i * ChunkSize;
            int size = Mathf.Min(ChunkSize, bytes.Length - offset);

            // тут мы используем Reliable RPC; пример ClientRpc с FastBufferWriter
            using (var writer = new FastBufferWriter(8 + 4 + size, Unity.Collections.Allocator.Temp))
            {
                writer.WriteValueSafe(messageId);
                writer.WriteValueSafe((ushort)i);
                writer.WriteValueSafe((ushort)totalFragments);
                writer.WriteValueSafe(size);
                writer.WriteBytesSafe(bytes, offset, size);

                // Пример ClientRpc: отправляем конкретному клиенту
                SendFragmentClientRpc(writer, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } } });
            }

            // Опционально small delay to avoid spiking the transport:
            // await Task.Yield(); // или small Task.Delay(1)
        }
    }

    // уникальный id
    static long s_lastMessageId = 0;
    static ulong GenerateMessageId() => (ulong)System.Threading.Interlocked.Increment(ref s_lastMessageId);


    // структура для хранения промежуточных фрагментов
    class FragmentCollector
    {
        public readonly ushort totalFragments;
        public readonly Dictionary<ushort, byte[]> fragments = new Dictionary<ushort, byte[]>();
        public readonly System.DateTime created = System.DateTime.UtcNow;

        public FragmentCollector(ushort total)
        {
            totalFragments = total;
        }
    }

    static readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, FragmentCollector> s_collectors
        = new System.Collections.Concurrent.ConcurrentDictionary<ulong, FragmentCollector>();

    // вызывается из RPC, распарсив header + payload
    private void HandleIncomingFragment(FastBufferReader reader)
    {
        reader.ReadValueSafe(out ulong messageId);
        reader.ReadValueSafe(out ushort fragmentIndex);
        reader.ReadValueSafe(out ushort totalFragments);
        reader.ReadValueSafe(out int fragmentSize);

        var fragmentBytes = new byte[fragmentSize];
        reader.ReadBytesSafe(ref fragmentBytes, fragmentSize);

        var coll = s_collectors.GetOrAdd(messageId, id => new FragmentCollector(totalFragments));
        lock (coll) // блокируем на коллектор, т.к. фрагменты могут прийти параллельно
        {
            if (!coll.fragments.ContainsKey(fragmentIndex))
            {
                coll.fragments[fragmentIndex] = fragmentBytes;
            }

            // проверяем, все ли фрагменты пришли
            if (coll.fragments.Count == coll.totalFragments)
            {
                // собираем байты
                int totalSize = 0;
                for (ushort i = 0; i < coll.totalFragments; i++)
                    totalSize += coll.fragments[i].Length;

                var assembled = new byte[totalSize];
                int pos = 0;
                for (ushort i = 0; i < coll.totalFragments; i++)
                {
                    var f = coll.fragments[i];
                    System.Buffer.BlockCopy(f, 0, assembled, pos, f.Length);
                    pos += f.Length;
                }

                // удаляем collector
                s_collectors.TryRemove(messageId, out _);

                // десериализуем и вызываем обработчик
                var building = DeserializeBuilding(assembled);
                //OnCompleteBuildingReceived(building); // твоя логика
            }
        }
    }

    // десериализация, обратный SerializeBuilding
    static BuildingServerData DeserializeBuilding(byte[] bytes)
    {
        var data = new BuildingServerData();
        using (var ms = new System.IO.MemoryStream(bytes))
        using (var br = new System.IO.BinaryReader(ms))
        {
            int len = br.ReadInt32();
            data.positions = new Vector3[len];
            for (int i = 0; i < len; i++)
            {
                float x = br.ReadSingle();
                float y = br.ReadSingle();
                float z = br.ReadSingle();
                data.positions[i] = new Vector3(x, y, z);
            }

            int blockLen = br.ReadInt32();
            data.blockIDs = br.ReadBytes(blockLen);

            string ReadString()
            {
                int slen = br.ReadInt32();
                if (slen == 0) return string.Empty;
                var sb = br.ReadBytes(slen);
                return System.Text.Encoding.UTF8.GetString(sb);
            }
            data.nameBuilding = ReadString();
            data.authorName = ReadString();
            data.guid = ReadString();

            data.countLikes = br.ReadInt32();
            data.liked = br.ReadBoolean();
        }
        return data;
    }

    [ClientRpc]
    private void SendFragmentClientRpc(FastBufferWriter fragmentBuffer, ClientRpcParams rpcParams = default)
    {
        // fragmentBuffer уже доступен как FastBufferReader на клиенте в аргументах,
        // но если у тебя метод получает FastBufferWriter, то внутри нужно его прочитать:
        using (var reader = new FastBufferReader(fragmentBuffer.ToArray(), Unity.Collections.Allocator.Temp))
        {
            HandleIncomingFragment(reader);
        }
    }

    void CleanupOldCollectors()
    {
        var now = System.DateTime.UtcNow;
        foreach (var kv in s_collectors)
        {
            if ((now - kv.Value.created).TotalSeconds > 10) // 10s timeout
            {
                s_collectors.TryRemove(kv.Key, out _);
            }
        }
    }

}
