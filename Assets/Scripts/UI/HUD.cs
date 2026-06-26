using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.AI.Navigation;

public class HUD : MonoBehaviour
{
    //[SerializeField] QuickSlotsView quickSlotsView;
    [SerializeField] EquipmentView equipmentView;
    [SerializeField] TMP_InputField inputField;

    Character player;

    public void Init(Character owner)
    {
        player = owner;

        //quickSlotsView.Init(owner);
        equipmentView.Init(owner);

        equipmentView.gameObject.SetActive(false);

        var nav = inputField.navigation;
        nav.mode = UnityEngine.UI.Navigation.Mode.None;
        inputField.navigation = nav;

        inputField.onSubmit.AddListener(Input_Submited);
        inputField.onSelect.AddListener(Input_Selected);
        inputField.onDeselect.AddListener(Input_Deselected);
    }

    private void Input_Selected(string value)
    {
        if (InputLogic.Single != null) InputLogic.Single.BlockPlayerControl = true;
    }

    private void Input_Deselected(string value)
    {
        if (InputLogic.Single != null) InputLogic.Single.BlockPlayerControl = false;
    }

    private void Input_Submited(string value)
    {
        var navMesh = FindObjectOfType<NavMeshSurface>();
        navMesh.tileSize = int.Parse(value);
        
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.E))
        {
            var state = !equipmentView.gameObject.activeSelf;
            equipmentView.gameObject.SetActive(state);
            //player.GetComponent<StarterAssetsInputs>().SetCursorState(!state);
        }
    }
}
