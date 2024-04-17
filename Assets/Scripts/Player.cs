using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine;
using static ITEMS;
using static BLOCKS;
using StarterAssets;

public class Player : MonoBehaviour
{
    [SerializeField] TextAsset tower;
    [SerializeField] Transform towerPos;
    [SerializeField] Transform spineItemHolder;
    [SerializeField] Transform topDownTarget;
    //[SerializeField] ThirdPersonController thirdPersonController;

    public static UnityEvent<Player> onSpawn = new UnityEvent<Player>();

    public Transform SpineItemHolder => spineItemHolder;

    public Inventory inventory;

    private void Awake()
    {
        inventory = new(this);
    }

    private void Start()
    {
        EventsHolder.onPlayerSpawnAny?.Invoke(this);
        CameraStack.onCameraSwitch.AddListener(Camera_Switched);

        onSpawn?.Invoke(this);

        inventory.Close();

        //var jetpack = GameManager.Inst.ItemsData.Find(i => i.ID == JETPACK);
        //var item = jetpack.CreateItem();
        //inventory.AddItem(item);

    }



    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.E))
        {
            if (inventory.IsOpen)
            {
                inventory.Close();
            }
            else
            {
                inventory.Open();
            }
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            var pos = towerPos.position;//transform.position + (transform.forward * 3) + Vector3.up;
            BuildGenerator.Build(tower, pos, true);
        }

        

        //if (Input.GetKeyDown(KeyCode.T))
        //{
        //    var blockData = FindBlockSystem.Instance.GetNearBlockByUpPlane(transform.position);
        //    WorldGenerator.Inst.SetBlockAndUpdateChunck(blockData.pos, 8);
        //}
    }

    

    private void Camera_Switched(CameraStack.CameraType camType)
    {
        switch (camType)
        {
            case CameraStack.CameraType.Free:
                break;

            case CameraStack.CameraType.Third:
                //thirdPersonController.TopClamp = 70;
                //thirdPersonController.BottomClamp = -30;
                break;

            case CameraStack.CameraType.First:
                //thirdPersonController.TopClamp = 85;
                //thirdPersonController.BottomClamp = -50;
                break;

            case CameraStack.CameraType.TopDown:
                break;
        }
    }

    private void LateUpdate()
    {
        topDownTarget.position = transform.position + Vector3.up;
        topDownTarget.rotation = Quaternion.Euler(57, 0, 0);
    }
}
