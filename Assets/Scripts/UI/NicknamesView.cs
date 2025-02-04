using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NicknamesView : MonoBehaviour
{
    [SerializeField] NicknameView nicknamePrefab;
    public float distanceToView = 30f;
    public float trackedDamp = 10f;

    public List<Character> characters;
    readonly Dictionary<Character, string> nicknames = new();
    Dictionary<Character, NicknameView> nickViews = new();

    PlayerBehaviour player;
    Character waitingNickname;

    public void Init()
    {
        Character.onSpawn.AddListener(AnyCharacter_Spawned);
        Character.onDisable.AddListener(AnyCharacter_Disabled);
        PlayerBehaviour.onMineSpawn.AddListener(MinePlayer_Spawned);
    }

    private void AnyCharacter_Disabled(Character character)
    {
        characters.Remove(character);
        nicknames.Remove(character);
        if (nickViews.ContainsKey(character))
        {
            Destroy(nickViews[character].gameObject);
            nickViews.Remove(character);
        }
    }

    private void MinePlayer_Spawned(MonoBehaviour player)
    {
        this.player = player as PlayerBehaviour;
    }

    private void AnyCharacter_Spawned(Character character)
    {
        characters.Add(character);
    }

    private void LateUpdate()
    {
        if (!player)
            return;

        foreach (var character in characters)
        {
            if (character == player.Character)
                continue;

            var dist = Vector3.Distance(player.transform.position, character.transform.position);
            
            if (dist < distanceToView)
            {
                if (!IsVisible(character.transform))
                {
                    DestroyNickView(character);
                    continue;
                }

                if (nicknames.ContainsKey(character))
                {
                    var cam = CameraStack.Instance.Main;
                    var screenPos = cam.WorldToScreenPoint(character.transform.position + Vector3.up * 1.98f);

                    if (!nickViews.ContainsKey(character))
                    {
                        var view = Instantiate(nicknamePrefab, transform);
                        view.Init(new NicknameData() 
                        { 
                            nickname = nicknames[character] 
                        });
                        nickViews.Add(character, view);

                        var rect = view.transform as RectTransform;
                        var targetPos = screenPos * UI.ScaleFactor;
                        rect.anchoredPosition = targetPos;
                    }
                    else
                    {
                        var view = nickViews[character];

                        var rect = view.transform as RectTransform;
                        var targetPos = screenPos * UI.ScaleFactor;
                        rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, targetPos, Time.deltaTime * trackedDamp);
                        
                    }
                }
                else
                {
                    if (waitingNickname == character)
                    {
                        continue;
                    }

                    var ownerId = character.GetComponent<NetworkObject>().OwnerClientId;
                    NetworkUserManager.Instance.GetNicknameRequest(ownerId, ReceiveNickname);
                    waitingNickname = character;
                }

            }
            else
            {
                DestroyNickView(character);
            }
        }
    }

    private void DestroyNickView(Character character)
    {
        if (nickViews.ContainsKey(character))
        {
            Destroy(nickViews[character].gameObject);
            nickViews.Remove(character);
        }
    }

    private void ReceiveNickname(string nickname)
    {
        //print($"{waitingNickname} {nickname}");
        // TO DO странная хуйня надо буде тразобраться
        if (waitingNickname)
        {
            nicknames.Add(waitingNickname, nickname);
            waitingNickname = null;
        }
    }

    private bool IsVisible(Transform character)
    {
        Vector3 viewportPosition = CameraStack.Instance.Main.WorldToViewportPoint(character.position);

        if (viewportPosition.z > 0 && viewportPosition.x >= 0 && viewportPosition.x <= 1 && viewportPosition.y >= 0 && viewportPosition.y <= 1)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}
