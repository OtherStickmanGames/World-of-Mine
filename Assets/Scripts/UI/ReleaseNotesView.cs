using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReleaseNotesView : MonoBehaviour
{
    [SerializeField] Transform notesParent;
    [SerializeField] ReseaseNoteView noteViewPrefab;
    [SerializeField] GameObject notesView;

    public void Init()
    {
        //ReleaseNotesHandler.onNewsReceive.AddListener(NewsData_Received);
        ReleaseNotesHandler.onNoteReceive.AddListener(NoteDate_Received);

        Clear();
    }

    private void NoteDate_Received(NetworkNewsData data)
    {
        var view = Instantiate(noteViewPrefab, notesParent);
        view.Init();
        view.Fill(data.title, data.name.Replace("_", "."), data.text);
    }

    private void NewsData_Received(List<NetworkNewsData> newsData)
    {
        Clear();

        if (newsData == null || newsData.Count == 0)
        {
            notesView.SetActive(false);
            return;
        }

        notesView.SetActive(true);

        foreach (var data in newsData)
        {
            var view = Instantiate(noteViewPrefab, notesParent);
            view.Init();
            view.Fill(data.title, data.name.Replace("_", "."), data.text);
            //print(data.date);
        }
    }

    private void Clear()
    {
        foreach (Transform item in notesParent)
        {
            Destroy(item.gameObject);
        }
    }
}
