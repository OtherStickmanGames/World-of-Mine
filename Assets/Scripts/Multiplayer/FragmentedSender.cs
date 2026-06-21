using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Unity.Netcode;

public class IdGenerator
{
    private long _lastId = 0;

    public ulong GenerateId()
    {
        long next = Interlocked.Increment(ref _lastId);
        return (ulong)next;
    }
}

public class FragmentedSender : NetworkBehaviour
{
    [SerializeField] private int FragmentSize = 1024;
    [SerializeField] private float AckTimeoutSeconds = 5f;
    [SerializeField] private int MaxRetries = 3;

    private IdGenerator idGenerator = new IdGenerator();
    private Dictionary<ulong, int> _acknowledgedFragments = new Dictionary<ulong, int>();

    public FragmentedReceiver receiver;

    private void Awake()
    {
        if (receiver == null) receiver = GetComponent<FragmentedReceiver>();
    }

    public void SendLargeData(byte[] data, ulong clientId)
    {
        ulong transferId = idGenerator.GenerateId();
        _acknowledgedFragments.Add(transferId, -1);
        StartCoroutine(SendFragmentsReliableCoroutine(clientId, transferId, data));
    }

    private IEnumerator SendFragmentsReliableCoroutine(ulong clientId, ulong transferId, byte[] data)
    {
        if (receiver == null)
        {
            Debug.LogError($"[FragmentedSender] Ошибка: Компонент FragmentedReceiver не привязан!");
            yield break;
        }

        int totalFragments = (data.Length + FragmentSize - 1) / FragmentSize;
        Debug.Log($"[FragmentedSender] Начинаем передачу {transferId} клиенту {clientId}. Всего фрагментов: {totalFragments}");
        
        for (int i = 0; i < totalFragments; i++)
        {
            int offset = i * FragmentSize;
            int length = Math.Min(FragmentSize, data.Length - offset);
            byte[] fragmentData = new byte[length];
            Array.Copy(data, offset, fragmentData, 0, length);

            bool fragmentAcked = false;
            int retryCount = 0;

            while (!fragmentAcked && retryCount < MaxRetries)
            {
                var rpcParams = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } } };
                receiver.ReceiveFragmentClientRpc(transferId, i, totalFragments, fragmentData, rpcParams);

                float timer = 0f;
                while (timer < AckTimeoutSeconds)
                {
                    if (_acknowledgedFragments[transferId] == i)
                    {
                        fragmentAcked = true;
                        break;
                    }
                    timer += Time.deltaTime;
                    yield return null;
                }

                if (!fragmentAcked)
                {
                    retryCount++;
                    Debug.Log($"[FragmentedSender] Таймаут ожидания подтверждения передачи {transferId}, фрагмент {i}. Попытка {retryCount} из {MaxRetries}");
                }
            }

            if (!fragmentAcked)
            {
                Debug.LogError($"[FragmentedSender] Не удалось отправить фрагмент {i} передачи {transferId} клиенту {clientId} после {MaxRetries} попыток. Передача прервана.");
                _acknowledgedFragments.Remove(transferId);
                yield break;
            }
        }

        _acknowledgedFragments.Remove(transferId);
        Debug.Log($"[FragmentedSender] Передача {transferId} клиенту {clientId} успешно завершена.");
    }

    [ServerRpc(RequireOwnership = false)]
    public void FragmentAckServerRpc(ulong transferId, int fragmentIndex, ServerRpcParams serverRpcParams = default)
    {
        if (_acknowledgedFragments.ContainsKey(transferId))
        {
            _acknowledgedFragments[transferId] = fragmentIndex;
        }
    }
}