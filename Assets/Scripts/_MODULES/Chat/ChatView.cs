using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine;
using TMPro;
using System;

public class ChatView : NetworkBehaviour
{
    [SerializeField] MessageView messagePrefab;
    [SerializeField] Transform messagesParent;
    [SerializeField] TMP_InputField messageInput;
    [SerializeField] Button btnSend;
    [SerializeField] GameObject view;

    Dictionary<ulong, string> usernames;

    bool inited;

    public void Init(string username)
    {
        if (inited)
            return;

        inited = true;

        Clear();

        if (IsServer)
        {
            usernames = new Dictionary<ulong, string>();
        }
        else
        {
            Show();

            messageInput.onSubmit.AddListener(Message_Submited);
            SendUsernameServerRpc(username);

            if (Application.isMobilePlatform)
            {
                btnSend.onClick.AddListener(BtnSend_Clicked);
            }
            else
            {
                btnSend.gameObject.SetActive(false);
            }

        }

    }

    private void BtnSend_Clicked()
    {
        Message_Submited(messageInput.text);
    }

    private void Message_Submited(string msg)
    {
        SendMessageServerRpc(msg);

        messageInput.SetTextWithoutNotify(string.Empty);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendUsernameServerRpc(string username, ServerRpcParams serverRpcParams = default)
    {
        usernames.Add(serverRpcParams.Receive.SenderClientId, username);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendMessageServerRpc(string message, ServerRpcParams serverRpcParams = default)
    {
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
