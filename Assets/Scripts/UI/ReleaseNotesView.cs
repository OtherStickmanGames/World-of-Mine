using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReleaseNotesView : MonoBehaviour
{
    [SerializeField] Transform notesParent;
    [SerializeField] ReseaseNoteView noteViewPrefab

    public void Init()
    {
        ReleaseNotesHandler.onNewsReceive.AddListener(NewsData_Received);
    }

    private void NewsData_Received(List<NetworkNewsData> newsData)
    {
        Clear();
    }

    private void Clear()
    {
        foreach (Transform item in notesParent)
        {
            Destroy(item.gameObject);
        }
    }
}
