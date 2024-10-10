using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine;
using System;
using TMPro;

public class CraftableItemView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] TMP_Text title;
    [SerializeField] Button btn;

    [HideInInspector] public UnityEvent<CraftableItemView> onClick;

    public ItemCraftableData Data { get; private set; }

    Color originColor;

   
    public void Init(ItemCraftableData data)
    {
        Data = data;
        originColor = title.color;

        title.SetText(data.name);

        btn.onClick.AddListener(Item_Clicked);
    }

    private void Item_Clicked()
    {
        onClick?.Invoke(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        title.color = Color.white;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        title.color = originColor;
    }
}
