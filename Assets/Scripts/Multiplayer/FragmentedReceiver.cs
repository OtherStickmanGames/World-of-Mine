using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.Netcode;

public class FragmentedReceiver : MonoBehaviour
{
    // Класс для сборки
    class Pending
    {
        public int Total;
        public Dictionary<int, byte[]> Parts = new Dictionary<int, byte[]>();
        public DateTime FirstReceived = DateTime.UtcNow;
        public int ReceivedCount => Parts.Count;
    }

    private readonly Dictionary<ulong, Pending> _pendingByMessage = new Dictionary<ulong, Pending>();
    private readonly object _lock = new object();

    // Этот метод должен совпадать с RPC подписью на сервере
    // Помести его в скрипт на объекте, который слушает RPC'ы (NetworkBehaviour или обычный MonoBehaviour с регистрации вызова)
    public void OnFragmentReceived(ulong messageId, int fragmentIndex, int totalFragments, byte[] fragmentData)
    {
        lock (_lock)
        {
            if (!_pendingByMessage.TryGetValue(messageId, out var p))
            {
                p = new Pending { Total = totalFragments };
                _pendingByMessage[messageId] = p;
            }

            // игнорируем дубликаты
            if (!p.Parts.ContainsKey(fragmentIndex))
            {
                p.Parts[fragmentIndex] = fragmentData;
            }

            if (p.ReceivedCount == p.Total)
            {
                // Собираем все в порядке индексов
                int fullSize = 0;
                for (int i = 0; i < p.Total; i++) fullSize += p.Parts[i].Length;
                var all = new byte[fullSize];
                int pos = 0;
                for (int i = 0; i < p.Total; i++)
                {
                    var part = p.Parts[i];
                    Buffer.BlockCopy(part, 0, all, pos, part.Length);
                    pos += part.Length;
                }

                // Удаляем pending
                _pendingByMessage.Remove(messageId);

                // Десериализуем структуру
                var building = DeserializeBuilding(all);

                // Отправляем ACK на сервер (через NetworkBehaviour/registered RPC)
                var rpcOwner = NetworkManager.Singleton.LocalClientId;
                // предполагаем, что есть сетевой объект с методом FragmentAckServerRpc
                var ackSender = FindObjectOfType<FragmentedSender>(); // у тебя архитектура может быть другая
                if (ackSender != null)
                {
                    ackSender.FragmentAckServerRpc(messageId);
                }

                // Делай что нужно с building: добавить в мир, сохранить и т.д.
                Debug.Log($"Client: received building '{building.nameBuilding}', positions: {building.positions?.Length}");
            }
        }
    }

    private BuildingServerData DeserializeBuilding(byte[] bytes)
    {
        var d = new BuildingServerData();
        using (var ms = new MemoryStream(bytes))
        using (var br = new BinaryReader(ms))
        {
            int posCount = br.ReadInt32();
            if (posCount > 0)
            {
                d.positions = new Vector3[posCount];
                for (int i = 0; i < posCount; i++)
                {
                    float x = br.ReadSingle();
                    float y = br.ReadSingle();
                    float z = br.ReadSingle();
                    d.positions[i] = new Vector3(x, y, z);
                }
            }
            else d.positions = Array.Empty<Vector3>();

            int blockLen = br.ReadInt32();
            if (blockLen > 0) d.blockIDs = br.ReadBytes(blockLen);
            else d.blockIDs = Array.Empty<byte>();

            d.nameBuilding = ReadString(br);
            d.authorName = ReadString(br);
            d.guid = ReadString(br);
            d.countLikes = br.ReadInt32();
            d.liked = br.ReadBoolean();
        }
        return d;
    }

    private string ReadString(BinaryReader br)
    {
        int len = br.ReadInt32();
        if (len == 0) return string.Empty;
        var bytes = br.ReadBytes(len);
        return System.Text.Encoding.UTF8.GetString(bytes);
    }
}
