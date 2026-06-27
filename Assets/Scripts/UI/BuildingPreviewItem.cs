using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
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
    [SerializeField] Image iconLike;
    [SerializeField] RectTransform loader;
    [SerializeField] TMP_Text loadingPercent;

    [Space]

    public float sensitivityMouseY = 1.5f;
    public float CameraAngleOverrideX = 0.0f;
    public float CameraAngleOverrideY = 0.0f;
    public float rotateSensitibity = 5;

    public bool IsPreviewRotating { get; set; }
    public string Guid { get; private set; }

    [HideInInspector] public UnityEvent<BuildingPreviewItem> onLikeClick;
    [HideInInspector] public UnityEvent<BuildingPreviewItem> onBuildingClick;

    InteractableStateTracker lookTouchTracker;
    Color unlikedColor;
    bool liked;
    int countLikes;

    public void Init(BuildPreviewData preview, BuildingServerData serverData)
    {
        lookTouchTracker = touchLook.GetComponent<InteractableStateTracker>();
        unlikedColor = iconLike.color;
        countLikes = serverData.countLikes;
        liked = serverData.liked;

        title.SetText(serverData.nameBuilding);
        txtNameCreator.SetText(serverData.authorName);
        txtCountLikes.SetText($"{countLikes}");

        Guid = serverData.guid;

        btnLike.onClick.AddListener(Like_Clicked);
        lookTouchTracker.onPointerUp.AddListener(LookTouch_Uped);

        UpdateBtnLikeView();

        if (preview == null)
        {
            loader.gameObject.SetActive(true);
            loadingPercent.gameObject.SetActive(true);
            loadingPercent.SetText("Загружено 0%");
            LeanTween.rotateAroundLocal(loader.gameObject, Vector3.forward, -360f, 1f).setLoopClamp();
            
            BuildingManager.Singleton.onBuildingLoadProgress.AddListener(OnProgressChanged);
        }
        else
        {
            loader.gameObject.SetActive(false);
            loadingPercent.gameObject.SetActive(false);
            PrepareBuildingMesh(preview);
        }
    }

    private void OnProgressChanged(string guid, float progress)
    {
        if (Guid != guid) return;

        loadingPercent.SetText($"Загружено {Mathf.RoundToInt(progress * 100)}%");
    }

    public void ApplyMesh(BuildPreviewData preview)
    {
        LeanTween.cancel(loader.gameObject);
        loader.gameObject.SetActive(false);
        loadingPercent.gameObject.SetActive(false);
        BuildingManager.Singleton.onBuildingLoadProgress.RemoveListener(OnProgressChanged);
        PrepareBuildingMesh(preview);
    }

    private void OnDestroy()
    {
        if (BuildingManager.Singleton.onBuildingLoadProgress != null)
        {
            BuildingManager.Singleton.onBuildingLoadProgress.RemoveListener(OnProgressChanged);
        }
    }

    private void LookTouch_Uped()
    {
        if (lookTouchTracker.touchTime < 0.18f)
        {
            onBuildingClick?.Invoke(this);
        }
    }

    private void Like_Clicked()
    {
        if (liked)
        {
            countLikes--;
        }
        else
        {
            countLikes++;
        }
        liked = !liked;

        UpdateBtnLikeView();

        onLikeClick?.Invoke(this);
    }

    private void UpdateBtnLikeView()
    {
        if (liked)
        {
            iconLike.color = Color.white;
        }
        else
        {
            iconLike.color = unlikedColor;
        }

        txtCountLikes.SetText($"{countLikes}");
    }

    private void PrepareBuildingMesh(BuildPreviewData preview)
    {
        preview.view.layer = LayerMask.NameToLayer("UI");
        preview.view.transform.SetParent(parent, false);
        preview.view.transform.localPosition = Vector3.zero;
        preview.view.transform.localRotation = Quaternion.identity;

        var rectTransform = transform as RectTransform;
        var size = rectTransform.sizeDelta - (Vector2.up * 100);
        var widthScreenSpace = 1.58f;
        var scaleX = size.x / (preview.width  * widthScreenSpace);
        var scaleY = size.y / (preview.height * widthScreenSpace);
        var scaleZ = size.x / (preview.length * widthScreenSpace);
        preview.view.transform.localScale = Vector3.one * Mathf.Min(scaleX, scaleY, scaleZ);
        preview.ShiftPosition();
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
            _cinemachineTargetYaw += lookDirection.x;
            _cinemachineTargetPitch += lookDirection.y * sensitivityMouseY;
        }

        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, -90, 90);

        parent.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverrideX,
            _cinemachineTargetYaw + CameraAngleOverrideY, 0.0f);
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }
}