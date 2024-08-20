using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class BuildingPreviewItem : MonoBehaviour
{
    [SerializeField] Transform parent;
    [SerializeField] TMP_Text title;
    [SerializeField] TMP_Text txtNameCreator;
    [SerializeField] TMP_Text txtCountLikes;
    [SerializeField] Button btnLike;

    public void Init(BuildPreviewData preview, BuildingServerData serverData)
    {
        title.SetText(serverData.nameBuilding);

        preview.view.layer = LayerMask.NameToLayer("UI");
        preview.view.transform.SetParent(parent);

        var rectTransform = transform as RectTransform;
        var size = rectTransform.sizeDelta;

        var widthScreenSpace = 1.5f;
        var scaleX = (size.x ) / (preview.width * widthScreenSpace);
        var scaleY = (size.y ) / (preview.height * widthScreenSpace);
        var scaleZ = (size.x ) / (preview.length * widthScreenSpace);
        preview.view.transform.localScale = Vector3.one * Mathf.Min(scaleX, scaleY, scaleZ);
        preview.ShiftPosition();
    }
}
