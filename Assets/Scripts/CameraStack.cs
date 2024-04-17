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

    [SerializeField] GameObject freeFlyCamera;
    [SerializeField] Transform freeTopDownCameraTarget;
    [SerializeField] float speedTarget = 5f;

    public CameraType CurrentType { get; set; } = CameraType.Third;
    public Camera Main;

    public static CameraStack Instance;
    public static UnityEvent<CameraType> onCameraSwitch = new UnityEvent<CameraType>();

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

        Main = Camera.main;
    }

    public void SwitchToThirdPerson()
    {
        topDownCamera.Priority = 8;
        freeTopDownCamera.Priority = 8;
        firstPersonCamera.Priority = 8;
        thirdPersonCamera.Priority = 10;
        Main = Camera.main;
        CurrentType = CameraType.Third;

        onCameraSwitch?.Invoke(CurrentType);
    }

    public void SwitchToFirstPerson()
    {
        topDownCamera.Priority = 8;
        freeTopDownCamera.Priority = 8;
        firstPersonCamera.Priority = 10;
        thirdPersonCamera.Priority = 8;
        Main = Camera.main;
        CurrentType = CameraType.First;

        onCameraSwitch?.Invoke(CurrentType);
    }

    public void SwitchToTopDown()
    {
        topDownCamera.Priority = 10;
        freeTopDownCamera.Priority = 8;
        firstPersonCamera.Priority = 8;
        thirdPersonCamera.Priority = 8;
        Main = Camera.main;
        CurrentType = CameraType.TopDown;

        onCameraSwitch?.Invoke(CurrentType);
    }

    public void SwitchToFreeTopDown()
    {
        topDownCamera.Priority = 8;
        freeTopDownCamera.Priority = 10;
        firstPersonCamera.Priority = 8;
        thirdPersonCamera.Priority = 8;
        Main = Camera.main;
        CurrentType = CameraType.FreeTopDown;

        onCameraSwitch?.Invoke(CurrentType);
    }

    private void TopDownZoom()
    {
        if (Input.mouseScrollDelta.y == 0)
            return;

        var topDownComponent = topDownCamera.GetCinemachineComponent(0) as Cinemachine3rdPersonFollow;
        topDownComponent.CameraDistance -= Input.mouseScrollDelta.y;
        topDownComponent.CameraDistance = Mathf.Clamp(topDownComponent.CameraDistance, 1, 18);

        var component = freeTopDownCamera.GetCinemachineComponent(0) as Cinemachine3rdPersonFollow;
        component.CameraDistance -= Input.mouseScrollDelta.y;
        component.CameraDistance = Mathf.Clamp(component.CameraDistance, 1, 15);
    }

    private void Update()
    {
        TopDownZoom();
        TopDownTargetMovenment();
    }

    private void TopDownTargetMovenment()
    {
        if(freeTopDownCamera.Priority == deactivePriority)
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

    public enum CameraType
    {
        Free,
        Third,
        First,
        TopDown,
        FreeTopDown,
    }
}
