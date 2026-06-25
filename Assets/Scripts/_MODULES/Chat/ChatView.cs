using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine;
using TMPro;

public class ChatView : NetworkBehaviour
{
    [SerializeField] MessageView messagePrefab;
    [SerializeField] Transform messagesParent;
    [SerializeField] TMP_InputField messageInput;
    [SerializeField] Scrollbar scrollbar;
    [SerializeField] Button btnSend;
    [SerializeField] GameObject view;

    Dictionary<ulong, string> usernames;
    List<ChatMessage> history;

    public struct ChatMessage : INetworkSerializable
    {
        public string Username;
        public string Message;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Username);
            serializer.SerializeValue(ref Message);
        }
    }

    bool inited;

    public void Init(string username)
    {
        if (IsClient)
        {
            SendUsernameServerRpc(username);
        }

        if (inited)
            return;

        inited = true;

        Clear();

        if (IsServer)
        {
            usernames = new Dictionary<ulong, string>();
            history = new List<ChatMessage>();
            NetworkManager.OnClientDisconnectCallback += Client_Disconnected;
        }
        else
        {
            Show();

            messageInput.onSubmit.AddListener(Message_Submited);
            messageInput.onValueChanged.AddListener(InputValue_Changed);
            messageInput.onSelect.AddListener(Input_Selected);
            messageInput.onDeselect.AddListener(Input_Deselected);


            if (Application.isMobilePlatform)
            {
                btnSend.onClick.AddListener(BtnSend_Clicked);
            }

            btnSend.gameObject.SetActive(false);
        }

    }

    private void Input_Deselected(string inputValue)
    {
        InputLogic.Single.BlockPlayerControl = false;
        InputLogic.HideCursor();
        InputLogic.LockPlayerDigging();
    }

    private void Input_Selected(string inputValue)
    {
        InputLogic.Single.BlockPlayerControl = true;
        InputLogic.LockPlayerDigging();
    }

    private void InputValue_Changed(string value)
    {
        if (Application.isMobilePlatform)
        {
            btnSend.gameObject.SetActive(value.Length > 0);
        }
    }

    private void Client_Disconnected(ulong clientID)
    {
        if (usernames.ContainsKey(clientID))
        {
            usernames.Remove(clientID);
        }
    }

    private void BtnSend_Clicked()
    {
        Message_Submited(messageInput.text);
    }

    private void Message_Submited(string msg)
    {
        if (msg.Length == 0)
            return;

        SendMessageServerRpc(msg);

        messageInput.SetTextWithoutNotify(string.Empty);
        btnSend.gameObject.SetActive(false);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendUsernameServerRpc(string username, ServerRpcParams serverRpcParams = default)
    {
        if (usernames == null)
            throw new System.NullReferenceException($"{nameof(usernames)} dictionary is not initialized. Ensure ChatView.Init is called on the server.");

        // Оставляем только проверку на null, разрешая пустые строки или пробелы по желанию игрока
        if (username == null)
            throw new System.ArgumentNullException(nameof(username), "Username cannot be null.");

        var clientID = serverRpcParams.Receive.SenderClientId;
        usernames[clientID] = username;

        // Синхронизируем последние 20 сообщений для нового игрока
        if (history != null && history.Count > 0)
        {
            int count = Mathf.Min(history.Count, 20);
            ChatMessage[] lastMessages = history.GetRange(history.Count - count, count).ToArray();
            SyncHistoryClientRpc(lastMessages, new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { clientID } }
            });
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendMessageServerRpc(string message, ServerRpcParams serverRpcParams = default)
    {
        if (history == null)
            throw new System.NullReferenceException($"{nameof(history)} is not initialized. Ensure ChatView.Init is called on the server.");

        print($"сообщение чат {message}");
        var clientID = serverRpcParams.Receive.SenderClientId;
        var username = "Житель мира";
        if (usernames.ContainsKey(clientID))
        {
            username = usernames[clientID];
        }

        // Сохраняем в историю сервера (максимум 100)
        history.Add(new ChatMessage { Username = username, Message = message });
        if (history.Count > 100)
        {
            history.RemoveAt(0);
        }

        ReceiveMessageClientRpc(username, message);
    }

    [ClientRpc]
    private void SyncHistoryClientRpc(ChatMessage[] messages, ClientRpcParams clientRpcParams = default)
    {
        foreach (var msg in messages)
        {
            AddMessageToUI(msg.Username, msg.Message);
        }
        ScrollToBottom();
    }

    [ClientRpc(RequireOwnership = false)]
    private void ReceiveMessageClientRpc(string username, string message, ClientRpcParams clientRpcParams = default)
    {
        AddMessageToUI(username, message);
        ScrollToBottom();
    }

    private void AddMessageToUI(string username, string message)
    {
        var msgView = Instantiate(messagePrefab, messagesParent);
        msgView.Init(username, message);
    }

    private void ScrollToBottom()
    {
        LeanTween.value(gameObject, v =>
        {
            scrollbar.value = v;
        }, scrollbar.value, 0, 0.3f).setEaseOutQuad();
    }

    private void Clear()
    {
        foreach (Transform item in messagesParent)
        {
            Destroy(item.gameObject);
        }
    }

    public void Hide()
    {
        view.SetActive(false);
    }

    public void Show()
    {
        view.SetActive(true);
    }
}
