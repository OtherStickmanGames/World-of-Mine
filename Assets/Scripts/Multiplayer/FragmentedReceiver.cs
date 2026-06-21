using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;

public class FragmentedReceiver : NetworkBehaviour
{
    class PendingTransfer
    {
        public int TotalFragments;
        public byte[][] Parts;
        public int ReceivedCount;
        public float LastReceivedTime;
    }

    private Dictionary<ulong, PendingTransfer> _pendingTransfers = new Dictionary<ulong, PendingTransfer>();
    private List<ulong> _keysToRemove = new List<ulong>();

    [HideInInspector] public UnityEvent<byte[]> onDataReceive = new UnityEvent<byte[]>();

    public FragmentedSender sender;

    private void Awake()
    {
        if (sender == null) sender = GetComponent<FragmentedSender>();
    }

    [ClientRpc(RequireOwnership = false)]
    public void ReceiveFragmentClientRpc(ulong transferId, int fragmentIndex, int totalFragments, byte[] fragmentData, ClientRpcParams clientRpcParams = default)
    {
        if (sender == null)
        {
            Debug.LogError($"[FragmentedReceiver] Ошибка: Компонент FragmentedSender не привязан!");
            return;
        }

        if (!_pendingTransfers.TryGetValue(transferId, out var pending))
        {
            pending = new PendingTransfer { 
                TotalFragments = totalFragments,
                Parts = new byte[totalFragments][],
                ReceivedCount = 0
            };
            _pendingTransfers.Add(transferId, pending);
        }

        pending.LastReceivedTime = Time.unscaledTime;

        if (pending.Parts[fragmentIndex] == null)
        {
            pending.Parts[fragmentIndex] = fragmentData;
            pending.ReceivedCount++;
        }

        sender.FragmentAckServerRpc(transferId, fragmentIndex);

        if (pending.ReceivedCount == pending.TotalFragments)
        {
            int fullSize = 0;
            for (int i = 0; i < pending.TotalFragments; i++)
            {
                fullSize += pending.Parts[i].Length;
            }

            byte[] completeData = new byte[fullSize];
            int offset = 0;
            for (int i = 0; i < pending.TotalFragments; i++)
            {
                byte[] part = pending.Parts[i];
                Buffer.BlockCopy(part, 0, completeData, offset, part.Length);
                offset += part.Length;
            }

            _pendingTransfers.Remove(transferId);
            
            Debug.Log($"[FragmentedReceiver] Передача {transferId} успешно собрана на клиенте. Размер: {completeData.Length} байт.");
            onDataReceive.Invoke(completeData);
        }
    }
    
    private void Update()
    {
        if (_pendingTransfers.Count > 0)
        {
            _keysToRemove.Clear();
            foreach (var kvp in _pendingTransfers)
            {
                if ((Time.unscaledTime - kvp.Value.LastReceivedTime) > 30f)
                {
                    _keysToRemove.Add(kvp.Key);
                }
            }

            for (int i = 0; i < _keysToRemove.Count; i++)
            {
                _pendingTransfers.Remove(_keysToRemove[i]);
                Debug.LogError($"[FragmentedReceiver] Таймаут передачи {_keysToRemove[i]} на клиенте. Очистка незаконченных фрагментов.");
            }
        }
    }
}