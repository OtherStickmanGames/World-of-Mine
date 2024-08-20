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
    }
}
