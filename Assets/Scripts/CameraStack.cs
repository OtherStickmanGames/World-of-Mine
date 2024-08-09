using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine;
using Cinemachine;

public class CameraStack : MonoBehaviour
{
    [SerializeField] CinemachineVirtualCamera thirdPersonCamera;
    [SerializeField] CinemachineVirtualCamera firstPersonCamera;
    [SerializeField] CinemachineVirtualCamera freeTopDownCamera;
    [SerializeField] CinemachineVirtualCamera topDownCamera;
    [SerializeField] CinemachineVirtualCamera saveBuildingCamera;

    [SerializeField] GameObject freeFlyCamera;
    [SerializeField] Transform freeTopDownCameraTarget;
    [SerializeField] float speedTarget = 5f;

    public CameraType CurrentType { get; set; } = CameraType.Third;
    public Camera Main;
    public List<CinemachineVirtualCamera> cameras;

    public static CameraStack Instance;
    public static UnityEvent<CameraType> onCameraSwitch = new UnityEvent<CameraType>();

    CinemachineBrain cinemachineBrain;
    PlayerBehaviour player;

    Vector3 targetDirection;
    Vector3 targetPos;

    int activePriority = 10;
    int deactivePriority = 8;


    private void Start()
    {
        Init();
    }

    public void Init()
    {
        Instance = this;

        cameras = new List<CinemachineVirtualCamera>();

        cinemachineBrain = FindObjectOfType<CinemachineBrain>();

        AddCamera
        (
            thirdPersonCamera,
            firstPersonCamera,
            freeTopDownCamera,
            topDownCamera,
            saveBuildingCamera
        );

        PlayerBehaviour.onMineSpawn.AddListener(OwnerPlayer_Spawned);
    }

    private void OwnerPlayer_Spawned(MonoBehaviour owner)
    {
        player = owner.GetComponent<PlayerBehaviour>();

        if (thirdPersonCamera)
        {
            thirdPersonCamera.Follow = player.cameraTarget;
        }
        if (firstPersonCamera)
        {
            firstPersonCamera.Follow = player.cameraTarget;
        }
    }

    public void SwitchToThirdPerson()
    {
        if (topDownCamera)
        {
            topDownCamera.Priority = deactivePriority;
        }
        if (freeTopDownCamera)
        {
            freeTopDownCamera.Priority = deactivePriority;
        }
        if (firstPersonCamera)
        {
            firstPersonCamera.Priority = deactivePriority;
        }
        
        thirdPersonCamera.Priority = 10;
        
        CurrentType = CameraType.Third;

        onCameraSwitch?.Invoke(CurrentType);
    }

    public void SwitchToFirstPerson()
    {
        if (topDownCamera)
        {
            topDownCamera.Priority = deactivePriority;
        }
        if (freeTopDownCamera)
        {
            freeTopDownCamera.Priority = deactivePriority;
        }
        if (thirdPersonCamera)
        {
            thirdPersonCamera.Priority = deactivePriority;
        }

        firstPersonCamera.Priority = 10;

        CurrentType = CameraType.First;

        onCameraSwitch?.Invoke(CurrentType);
    }

    public void SwitchToTopDown()
    {
        if (freeTopDownCamera)
        {
            freeTopDownCamera.Priority = deactivePriority;
        }
        if (firstPersonCamera)
        {
            firstPersonCamera.Priority = deactivePriority;
        }
        topDownCamera.Priority = 10;
        
        
        thirdPersonCamera.Priority = 8;
        CurrentType = CameraType.TopDown;

        onCameraSwitch?.Invoke(CurrentType);
    }

    public void SwitchToFreeTopDown()
    {
        SetPriorityAllCams(deactivePriority);

        freeTopDownCamera.Priority = 10;

        CurrentType = CameraType.FreeTopDown;

        onCameraSwitch?.Invoke(CurrentType);
    }

    private void TopDownZoom()
    {
        if (Input.mouseScrollDelta.y == 0)
            return;

        if (topDownCamera)
        {
            var topDownComponent = topDownCamera.GetCinemachineComponent(0) as Cinemachine3rdPersonFollow;
            topDownComponent.CameraDistance -= Input.mouseScrollDelta.y;
            topDownComponent.CameraDistance = Mathf.Clamp(topDownComponent.CameraDistance, 1, 18);
        }

        if (freeTopDownCamera)
        {
            var component = freeTopDownCamera.GetCinemachineComponent(0) as Cinemachine3rdPersonFollow;
            component.CameraDistance -= Input.mouseScrollDelta.y;
            component.CameraDistance = Mathf.Clamp(component.CameraDistance, 1, 15);

        }
    }

    private void Update()
    {
        TopDownZoom();
        TopDownTargetMovenment();
    }

    private void TopDownTargetMovenment()
    {
        if(!freeTopDownCamera || freeTopDownCamera.Priority == deactivePriority)
        {
            return;
        }

        targetDirection.x = Input.GetAxis("Horizontal");
        targetDirection.z = Input.GetAxis("Vertical");

        freeTopDownCameraTarget.position += speedTarget * Time.deltaTime * targetDirection;

        for (int y = 100; y > 0; y--)
        {
            targetPos = freeTopDownCameraTarget.position;
            targetPos.y = y;

            if (WorldGenerator.Inst.GetBlockID(targetPos) > 0)
            {
                targetPos.y += 3;

                var delta = Time.deltaTime * 10;
                var result = Vector3.MoveTowards(freeTopDownCameraTarget.position, targetPos, delta);
                result.x = freeTopDownCameraTarget.position.x;
                result.z = freeTopDownCameraTarget.position.z;
                freeTopDownCameraTarget.position = result;

                //freeTopDownCameraTarget.position = targetPos;
                break;
            }
        }
    }

    public void SetShoulderOffset(Vector3 offset)
    {
        var component = thirdPersonCamera.GetCinemachineComponent(0) as Cinemachine3rdPersonFollow;
        component.ShoulderOffset = offset;
    }

    public Vector3 GetShoulderOffset()
    {
        var component = thirdPersonCamera.GetCinemachineComponent(0) as Cinemachine3rdPersonFollow;
        return component.ShoulderOffset;
    }

    public void SaveBuilding(SelectionMode selectionMode, Vector3 camPos)
    {
        if (selectionMode == SelectionMode.Horizontal)
        {
            SetPriorityAllCams(deactivePriority);

            var blendDuration = 1f;
            var originTime = cinemachineBrain.m_DefaultBlend.m_Time;
            cinemachineBrain.m_DefaultBlend.m_Time = blendDuration;

            LeanTween.delayedCall(blendDuration, () => cinemachineBrain.m_DefaultBlend.m_Time = originTime);

            saveBuildingCamera.Priority = activePriority;
            saveBuildingCamera.transform.position = camPos; 
            saveBuildingCamera.transform.rotation = Quaternion.Euler(90, 0, 0);
        }
        if (selectionMode == SelectionMode.Vertical)
        {
            LeanTween.move(saveBuildingCamera.gameObject, camPos, 1f);
            LeanTween.rotate(saveBuildingCamera.gameObject, Vector3.zero, 1f);
        }
    }

    public void SetPriorityAllCams(int priority)
    {
        foreach (var cam in cameras) cam.Priority = priority;
    }

    public void AddCamera(params CinemachineVirtualCamera[] cincaCamera)
    {
        foreach (var cam in cincaCamera)
        {
            if (!cam || cameras.Contains(cam))
                continue;

            cameras.Add(cam);
        }
    }

    public enum CameraType
    {
        Free,
        Third,
        First,
        TopDown,
        FreeTopDown,
        SaveBuilding,
    }
}
