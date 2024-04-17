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

    Player player;

    public void Init(Player owner)
    {
        player = owner;

        //quickSlotsView.Init(owner);
        equipmentView.Init(owner);

        equipmentView.gameObject.SetActive(false);

        inputField.onSubmit.AddListener(Input_Submited);
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
