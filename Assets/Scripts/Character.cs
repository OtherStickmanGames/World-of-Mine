using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine;


public class Character : MonoBehaviour
{
    [SerializeField] Transform spineItemHolder;
    [SerializeField] Transform topDownTarget;
    //[SerializeField] ThirdPersonController thirdPersonController;

    public static UnityEvent<Character> onSpawn = new UnityEvent<Character>();

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
