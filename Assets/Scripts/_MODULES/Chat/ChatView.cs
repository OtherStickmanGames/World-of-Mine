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
            NetworkManager.OnClientDisconnectCallback += Client_Disconnected;
        }
        else
        {
            Show();

            messageInput.onSubmit.AddListener(Message_Submited);
            messageInput.onValueChanged.AddListener(InputValue_Changed);

            if (Application.isMobilePlatform)
            {
                btnSend.onClick.AddListener(BtnSend_Clicked);
            }

            btnSend.gameObject.SetActive(false);
        }

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
        usernames.Add(serverRpcParams.Receive.SenderClientId, username);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendMessageServerRpc(string message, ServerRpcParams serverRpcParams = default)
    {
        print($"пришел мсг {message}");
        var clientID = serverRpcParams.Receive.SenderClientId;
        var username = "Кусок Травы";
        if (usernames.ContainsKey(clientID))
        {
            username = usernames[clientID];
        }

        ReceiveMessageClientRpc(username, message);
    }

    [ClientRpc(RequireOwnership = false)]
    private void ReceiveMessageClientRpc(string username, string message, ClientRpcParams clientRpcParams = default)
    {
        var view = Instantiate(messagePrefab, messagesParent);
        view.Init(username, message);
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
