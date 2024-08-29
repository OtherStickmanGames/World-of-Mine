using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class NoFog : MonoBehaviour
{
    bool doWeHaveFogInScene;
    Color origFogColor;

    private void Start()
    {
        doWeHaveFogInScene = RenderSettings.fog;
        origFogColor = RenderSettings.fogColor;

        RenderPipelineManager.endCameraRendering   += CameraEndRender;
        RenderPipelineManager.beginCameraRendering += CameraBeginRender;

        //RenderPipelineManager.endFrameRendering += (context, camera) =>
        //{
        //    print($"End Frame Working {camera}");
        //};
    }

    private void CameraBeginRender(ScriptableRenderContext context, Camera camera)
    {
        if (camera.gameObject == gameObject)
        {
            RenderSettings.fog = false;
        }
    }

    private void CameraEndRender(ScriptableRenderContext context, Camera camera)
    {

        if (camera.gameObject == gameObject)
        {
            RenderSettings.fog = doWeHaveFogInScene;
            //print($"End Camera Working {camera}");
            //RenderSettings.fogColor = Color.black;
        }

    }

    private void OnDestroy()
    {
        RenderPipelineManager.endCameraRendering   -= CameraEndRender;
        RenderPipelineManager.beginCameraRendering -= CameraBeginRender;
    }

    
}
