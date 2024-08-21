using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class BuildingPreviewItem : MonoBehaviour
{
    [SerializeField] Transform parent;
    [SerializeField] TMP_Text title;
    [SerializeField] TMP_Text txtNameCreator;
    [SerializeField] TMP_Text txtCountLikes;
    [SerializeField] FixedTouchField touchLook;
    [SerializeField] Button btnLike;

    [Space]

    public float sensitivityMouseY = 1.5f;
    public float CameraAngleOverrideX = 0.0f;
    public float CameraAngleOverrideY = 0.0f;
    public float rotateSensitibity = 5;

    public bool IsPreviewRotating { get; set; }

    public void Init(BuildPreviewData preview, BuildingServerData serverData)
    {
        title.SetText(serverData.nameBuilding);

        preview.view.layer = LayerMask.NameToLayer("UI");
        preview.view.transform.SetParent(parent);

        var rectTransform = transform as RectTransform;
        var size = rectTransform.sizeDelta;

        var widthScreenSpace = 1.5f;
        var scaleX = (size.x ) / (preview.width * widthScreenSpace);
        var scaleY = (size.y ) / (preview.height * widthScreenSpace);
        var scaleZ = (size.x ) / (preview.length * widthScreenSpace);
        preview.view.transform.localScale = Vector3.one * Mathf.Min(scaleX, scaleY, scaleZ);
        preview.ShiftPosition();
    }

    private void Update()
    {
        
    }

    private void LateUpdate()
    {
        IsPreviewRotating = touchLook.Pressed;

        BuildingPreviewRotate();

    }

    float _cinemachineTargetYaw;
    float _cinemachineTargetPitch;
    Vector2 prevMp, lookDirection, currentVelocity;
    void BuildingPreviewRotate()
    {
        var look = touchLook.TouchDist;
        if (!Application.isMobilePlatform && touchLook.gameObject == UI.CurrentUIObject)
        {
            if (Input.GetMouseButtonDown(0))
            {
                prevMp = Input.mousePosition;
            }
            if (Input.GetMouseButton(0))
            {
                look   = (Vector2)Input.mousePosition - prevMp;
                prevMp = (Vector2)Input.mousePosition;
            }
        }

        look.x *= -1;
        look *= rotateSensitibity;
        lookDirection = Vector2.SmoothDamp(lookDirection, look, ref currentVelocity, Time.deltaTime * 1.38f);


        if (lookDirection.sqrMagnitude >= 0.01f)
        {
            float deltaTimeMultiplier = Time.deltaTime;

            _cinemachineTargetYaw += lookDirection.x * deltaTimeMultiplier;
            _cinemachineTargetPitch += lookDirection.y * deltaTimeMultiplier * sensitivityMouseY;
        }

        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, -90, 90);

        parent.transform.rotation = Quaternion.Euler
        (
            _cinemachineTargetPitch + CameraAngleOverrideX,
            _cinemachineTargetYaw + CameraAngleOverrideY,
            0.0f
        );

    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }
}
