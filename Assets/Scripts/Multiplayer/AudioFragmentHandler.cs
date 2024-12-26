using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class AudioFragmentHandler : NetworkBehaviour
{
    private const int FragmentSize = 1000; // Размер одного фрагмента в байтах

    // Словарь для хранения фрагментов данных на клиенте
    private Dictionary<int, List<byte[]>> receivedFragments = new Dictionary<int, List<byte[]>>();

    // Метод для отправки большого массива данных
    public void SendLargeData(byte[] data, int dataId, ulong clientID)
    {
        List<byte[]> fragments = SplitIntoFragments(data);

        StartCoroutine(Async());

        IEnumerator Async()
        {
            for (int i = 0; i < fragments.Count; i++)
            {
                SendFragment(dataId, fragments[i], i, fragments.Count, clientID);

                if (i % 80 == 0)
                {
                    yield return null;
                }
            }
        }
    }

    // Разбивает данные на фрагменты
    private List<byte[]> SplitIntoFragments(byte[] data)
    {
        List<byte[]> fragments = new List<byte[]>();
        int offset = 0;

        while (offset < data.Length)
        {
            int size = Mathf.Min(FragmentSize, data.Length - offset);
            byte[] fragment = new byte[size];
            System.Array.Copy(data, offset, fragment, 0, size);
            fragments.Add(fragment);
            offset += size;
        }

        return fragments;
    }

    // Серверный RPC для получения фрагментов
    //[ServerRpc(RequireOwnership = false)]
    private void SendFragment(int dataId, byte[] fragment, int index, int totalFragments, ulong clientID)
    {
        //Debug.Log($"Отправил {index} из {totalFragments}");
        ReceiveFragmentClientRpc(dataId, fragment, index, totalFragments, GetTargetClientParams(clientID));
    }

    // Клиентский RPC для получения фрагментов
    [ClientRpc(RequireOwnership = false)]
    private void ReceiveFragmentClientRpc(int dataId, byte[] fragment, int index, int totalFragments, ClientRpcParams clientRpcParams = default)
    {
        if (!receivedFragments.ContainsKey(dataId))
        {
            receivedFragments[dataId] = new List<byte[]>(new byte[totalFragments][]);
        }

        // Сохраняем фрагмент
        receivedFragments[dataId][index] = fragment;

        // Проверяем, все ли фрагменты получены
        if (IsAllFragmentsReceived(receivedFragments[dataId], totalFragments))
        {
            byte[] completeData = CombineFragments(receivedFragments[dataId]);
            receivedFragments.Remove(dataId); // Очищаем данные после сборки

            Debug.Log($"Data {dataId} successfully reconstructed with size: {completeData.Length}");

            // Пример: обработка собранных данных
            ProcessReceivedData(completeData);
        }
    }

    // Проверка, все ли фрагменты получены
    private bool IsAllFragmentsReceived(List<byte[]> fragments, int totalFragments)
    {
        foreach (var fragment in fragments)
        {
            if (fragment == null)
                return false;
        }
        return true;
    }

    // Собираем все фрагменты в единый массив
    private byte[] CombineFragments(List<byte[]> fragments)
    {
        int totalSize = 0;
        foreach (var fragment in fragments)
        {
            totalSize += fragment.Length;
        }

        byte[] completeData = new byte[totalSize];
        int offset = 0;

        foreach (var fragment in fragments)
        {
            System.Array.Copy(fragment, 0, completeData, offset, fragment.Length);
            offset += fragment.Length;
        }

        return completeData;
    }

    public UnityEvent<byte[]> onDataReceive;

    // Пример обработки полученных данных
    private void ProcessReceivedData(byte[] data)
    {
        // Например, восстановление AudioClip из данных
        Debug.Log($"Processing received data of size {data.Length}");
        onDataReceive?.Invoke(data);
    }


    private ClientRpcParams GetTargetClientParams(ServerRpcParams serverRpcParams)
    {
        ClientRpcParams clientRpcParams = default;
        clientRpcParams.Send.TargetClientIds = new ulong[] { serverRpcParams.Receive.SenderClientId };

        return clientRpcParams;
    }

    private ClientRpcParams GetTargetClientParams(ulong clientId)
    {
        ClientRpcParams clientRpcParams = default;
        clientRpcParams.Send.TargetClientIds = new ulong[] { clientId };

        return clientRpcParams;
    }
}
