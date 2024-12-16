using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragDropItems : MonoBehaviour
{
    [SerializeField] Transform dragDropSpace;
    [SerializeField] InventorySlot dragableSlot;


    Item dragable;
    InventorySlot startSlot;

    private void Start()
    {
        dragableSlot.Init();
    }

    private void Update()
    {
        DragDropInventory();
    }

    void DragDropInventory()
    {
        if (Input.GetMouseButtonDown(0))
        {
            foreach (var hit in GetRaycasts())
            {
                var slot = hit.gameObject.GetComponent<InventorySlot>();
                if (slot && slot.Item != null)
                {
                    startSlot = slot;
                    dragable = slot.Item;

                    dragableSlot.SetItem(dragable);
                    slot.RemoveItem();
                }
            }
        }

        if (Input.GetMouseButton(0))
        {
            if (dragable != null)
            {
                //var offset = new Vector3(Screen.width / 2, Screen.height / 2);
                //var uiTransform = dragableSlot.transform as RectTransform;
                //uiTransform.position = Input.mousePosition - offset;
                //var loc = uiTransform.localPosition;
                //loc.z = 
                //uiTransform.localPosition = loc;

                var scaleFactor = 1080f / Screen.height;
                var mousePos = Input.mousePosition;
                Vector2 anchoredPos = mousePos * scaleFactor;
                var uiTransform = dragableSlot.transform as RectTransform;
                uiTransform.anchoredPosition = anchoredPos;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (dragable != null)
            {
                var results = GetRaycasts();

                if (results.Count > 0)
                {
                    bool itemSwap = false;

                    foreach (var hit in results)
                    {
                        var slot = hit.gameObject.GetComponent<InventorySlot>();
                        if (slot && slot != startSlot)
                        {
                            //print(slot.SlotType);
                            if (slot.Item != null)
                            {
                                var item = slot.Item;
                                slot.SetItemToSwap(dragable);
                                //print(item.view + " -=-=-=-=-=-");
                                //slot.RemoveItem();
                                //DelayPrint(startSlot, slot);
                                startSlot.SetItemWithoutUpdate(item);
                               
                                startSlot.UpdateView();

                                slot.UpdateView();

                                dragable = null;
                                startSlot = null;
                                dragableSlot.RemoveItem();

                                itemSwap = true;
                            }
                            else
                            {
                                slot.SetItem(dragable);
                                dragableSlot.RemoveItem();

                                dragable = null;
                                startSlot = null;
                            }

                            break;
                        }
                    }

                    if (dragableSlot.Item != null && !itemSwap)
                    {
                        ResetDrag();
                    }
                }
                else
                {
                    ResetDrag();
                }
            }
        }

        void ResetDrag()
        {
            startSlot.SetItem(dragable);
            dragableSlot.RemoveItem();
            dragable = null;
            startSlot = null;
        }
    }

    void DelayPrint(InventorySlot slot, InventorySlot slot2)
    {
        print(slot.Item?.view + " уруруру");

        StartCoroutine(Delay());

        IEnumerator Delay()
        {
            for (int i = 0; i < 3; i++)
            {
                print(slot.Item?.view + " эээ");
                yield return null;
            }

            slot.UpdateView();
            slot2.UpdateView();
        }
    }

    List<RaycastResult> GetRaycasts()
    {
        PointerEventData ped = new(EventSystem.current)
        {
            position = Input.mousePosition
        };
        List<RaycastResult> results = new();
        EventSystem.current.RaycastAll(ped, results);

        return results;
    }
}
