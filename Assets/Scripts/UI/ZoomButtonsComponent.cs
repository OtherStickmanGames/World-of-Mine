using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.Events;

public class ZoomButtonsComponent : MonoBehaviour
{
    [SerializeField] InteractableStateTracker btnPlus;
    [SerializeField] InteractableStateTracker btnMinus;

    [ReadOnlyField] public UnityEvent<float> onZoomValue = new UnityEvent<float>();


    private void Update()
    {
        if (btnPlus.Pressed)
        {
            onZoomValue?.Invoke(1f);
        }

        if (btnMinus.Pressed)
        {
            onZoomValue?.Invoke(-1f);
        }
    }
}
