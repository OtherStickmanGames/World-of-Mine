using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine;
using Cinemachine;

public class CameraStack : MonoBehaviour
{
    [SerializeField] CinemachineVirtualCamera startCamera;
    [SerializeField] CinemachineVirtualCamera thirdPersonCamera;
    [SerializeField] CinemachineVirtualCamera firstPersonCamera;
    [SerializeField] CinemachineVirtualCamera freeTopDownCamera;
    [SerializeField] CinemachineVirtualCamera topDownCamera;
    [SerializeField] CinemachineVirtualCamera saveBuildingCamera;

    [SerializeField] GameObject freeFlyCamera;
    [SerializeField] Transform freeTopDownCameraTarget;
    [SerializeField] float speedTarget = 5f;

    public CameraType CurrentType { get; set; } = CameraType.Third;
    [field:SerializeField] public CameraType PreviousType { get; set; }
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
            startCamera,
            thirdPersonCamera,
            firstPersonCamera,
            freeTopDownCamera,
            topDownCamera,
            saveBuildingCamera
        );

        PlayerBehaviour.onMineSpawn.AddListener(OwnerPlayer_Spawned);
    }

    public void SwitchCamera()
    {
        if (CurrentType is CameraType.Third)
        {
            SwitchToFirstPerson();
        }
        else if (CurrentType is CameraType.First)
        {
            SwitchToThirdPerson();
            //SwitchToTopDown();
        }
        //else if (CurrentType is CameraType.TopDown)
        //{
        //    SwitchToThirdPerson();
        //}
    }

    private void OwnerPlayer_Spawned(MonoBehaviour owner)
    {
        player = owner.GetComponent<PlayerBehaviour>();
        //player.onStartAllowGravity.AddListener(StartGravity_Allowed);

        if (thirdPersonCamera)
        {
            thirdPersonCamera.Follow = player.cameraTarget;
        }
        if (firstPersonCamera)
        {
            firstPersonCamera.Follow = player.cameraTarget;
        }

        if (!GameManager.IsTutorialScene())
        {
            startCamera.Follow = player.cameraTarget;

            SetPriorityAllCams(deactivePriority);
            startCamera.Priority = activePriority;
            player.thirdPersonController.AllowCameraRotation = false;
            player.cameraTarget.rotation = Quaternion.Euler(5, -150, 0);
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

        PreviousType = CurrentType;
        CurrentType = CameraType.Third;

        onCameraSwitch?.Invoke(CurrentType);
    }

    public void SwitchToFirstPerson()
    {
        SetPriorityAllCams(deactivePriority);

        firstPersonCamera.Priority = 10;

        PreviousType = CurrentType;
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

    public void SwitchToPreviousCamera()
    {
        switch (PreviousType)
        {
            case CameraType.Loading:
                break;
            case CameraType.Free:
                break;
            case CameraType.Third:
                SwitchToThirdPerson();
                break;
            case CameraType.First:
                SwitchToFirstPerson();
                break;
            case CameraType.TopDown:
                break;
            case CameraType.FreeTopDown:
                break;
            case CameraType.SaveBuilding:
                break;
        }
    }

    private void TopDownZoom()
    {
        if (Input.mouseScrollDelta.y == 0)
            return;

        if (!InputLogic.Single.AvailableMouseScrollWorld)
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

    public void ZoomThirdPersonCamera(float value)
    {
        ZoomCamera(thirdPersonCamera, value, 28f);
    }

    public void ZoomCamera(CinemachineVirtualCamera camera, float zoomValue, float maxDistance)
    {
        var component = camera.GetCinemachineComponent(0) as Cinemachine3rdPersonFollow;
        component.CameraDistance -= zoomValue;
        component.CameraDistance = Mathf.Clamp(component.CameraDistance, 1f, maxDistance);
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

    public void SaveBuilding(AcceptMode selectionMode, Vector3 camPos)
    {
        if (selectionMode == AcceptMode.Horizontal)
        {
            PreviousType = CurrentType;
            CurrentType = CameraType.SaveBuilding;
            SetPriorityAllCams(deactivePriority);

            var blendDuration = 1f;
            var originTime = cinemachineBrain.m_DefaultBlend.m_Time;
            cinemachineBrain.m_DefaultBlend.m_Time = blendDuration;

            LeanTween.delayedCall(blendDuration, 
                () =>
                {
                    cinemachineBrain.m_DefaultBlend.m_Time = originTime;
                    onCameraSwitch?.Invoke(CameraType.SaveBuilding);
                });

            saveBuildingCamera.Priority = activePriority;
            saveBuildingCamera.transform.position = camPos; 
            saveBuildingCamera.transform.rotation = Quaternion.Euler(90, 0, 0);
        }
        if (selectionMode == AcceptMode.Vertical)
        {
            LeanTween.move(saveBuildingCamera.gameObject, camPos, 0.5f)
                .setEaseInOutQuad()
                .setOnComplete(Rotate);

            void Rotate()
            {
                LeanTween.rotate(saveBuildingCamera.gameObject, Vector3.zero, 1f)
                    .setEaseInOutQuad()
                    .setOnComplete(() => onCameraSwitch?.Invoke(CameraType.SaveBuilding));
            }
        }
    }

    public void SaveBuildingCameraChangeZoom(float value)
    {
        var curZoom = saveBuildingCamera.m_Lens.OrthographicSize;
        var tarZoom = saveBuildingCamera.m_Lens.OrthographicSize + value;
        var velocity = 0f;
        var result = Mathf.SmoothDamp(curZoom, tarZoom, ref velocity, Time.deltaTime);
        saveBuildingCamera.m_Lens.OrthographicSize = result;
    }

    public void SaveBuildingCamSetZoom(float value)
    {
        var curZoom = saveBuildingCamera.m_Lens.OrthographicSize;
        LeanTween.value(gameObject, z =>
        {
            saveBuildingCamera.m_Lens.OrthographicSize = z;
        }, curZoom, value, 0.5f).setEaseInOutQuad();
    }

    public void SaveBuildingCamMove(Vector3 dir)
    {
        saveBuildingCamera.transform.position += dir * Time.deltaTime;
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

    public void SetSaveBuildingCamPos(Vector3 pos)
    {
        saveBuildingCamera.transform.position = pos;
    }

    public float GetSaveBuildingCamZoomValue() => saveBuildingCamera.m_Lens.OrthographicSize;

    public Vector3 GetSaveBuildingCameraPosition() => saveBuildingCamera.transform.position;

    public enum CameraType
    {
        Loading,
        Free,
        Third,
        First,
        TopDown,
        FreeTopDown,
        SaveBuilding,
    }
}
