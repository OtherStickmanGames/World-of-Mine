using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;
using System.Linq;

public class DragDropSlots : MonoBehaviour
{
    //SlotView startSlot;
    //InventoryItem dragable;
    //Vector3 itemHolderOriginPos;
    //Transform originParent;


    //private void Update()
    //{
    //    if (Input.GetMouseButtonDown(0))
    //    {
    //        PointerEventData ped = new(EventSystem.current) { position = Input.mousePosition };
    //        List<RaycastResult> hits = new();
    //        EventSystem.current.RaycastAll(ped, hits);

    //        var hit = hits.Find(h => h.gameObject.GetComponent<SlotView>());
    //        if(hit.gameObject)
    //        {
    //            var slot = hit.gameObject.GetComponent<SlotView>();
    //            //print(slot);
    //            if (slot && slot.Item != null)
    //            {
    //                originParent = slot.transform;
    //                startSlot = slot;
    //                itemHolderOriginPos = slot.Item.view.transform.parent.localPosition;
    //                dragable = slot.Item;
    //                dragable.view.transform.parent.SetParent(transform, false);
    //            }
    //        }
    //    }

    //    if(dragable != null)
    //    {
    //        var scaleFactor = (float)1600 / (float)Screen.width;
    //        var mousePos = Input.mousePosition;
    //        Vector2 anchoredPos = (mousePos - new Vector3(Screen.width / 2, Screen.height / 2)) * scaleFactor;
    //        var uiHolder = dragable.view.transform.parent as RectTransform;
    //        //anchoredPos -= uiHolder.sizeDelta / 2;
    //        uiHolder.anchoredPosition = anchoredPos;

    //        //var ScreenScale = transform.root.lossyScale.x;
    //        //var ebobob = dragable.view.transform.parent.parent.parent.lossyScale.x;
    //        //float k = ebobob;//transform.lossyScale.x / ScreenScale;
    //        //var t = dragable.view.transform.parent;
    //        //float x = (Input.mousePosition.x * k);// - ((Screen.width / 2) * k);
    //        //float y = (Input.mousePosition.y * k);// - ((Screen.height / 2) * k);
    //        //float z = t.position.z;
    //        //t.position = new Vector3(x, y, z);
    //    }

    //    if (Input.GetMouseButtonUp(0))
    //    {
    //        if(dragable != null)
    //        {
    //            dragable.view.transform.parent.localPosition = itemHolderOriginPos;
    //            dragable.view.transform.parent.SetParent(originParent, false);

    //            PointerEventData ped = new(EventSystem.current) { position = Input.mousePosition };
    //            List<RaycastResult> hits = new();
    //            EventSystem.current.RaycastAll(ped, hits);

    //            foreach (var hit in hits)
    //            {
    //                if (hit.gameObject)
    //                {
    //                    var slot = hit.gameObject.GetComponent<SlotView>();
                        
    //                    if(slot && slot != startSlot)
    //                    {
    //                        // Проверяем что мы переместили в слот экепировки
    //                        if(slot is EquipmentSlot)
    //                        {
    //                            var equipSlot = slot as EquipmentSlot;
    //                            if(equipSlot.Purpose != dragable.itemPurpose)
    //                            {
    //                                break;
    //                            }
    //                        }

    //                        slot.SetItem(dragable);
    //                        startSlot.RemoveItem();
    //                        break;
    //                    }
    //                }
    //            }

    //            dragable = null;
    //            startSlot = null;
    //            originParent = null;
    //        }
    //    }
    //}
}
