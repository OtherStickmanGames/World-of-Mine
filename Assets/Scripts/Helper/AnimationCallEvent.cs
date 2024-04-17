using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationCallEvent : MonoBehaviour
{
    ThirdPersonController thirdPersonController;

    private void Start()
    {
        thirdPersonController = GetComponentInParent<ThirdPersonController>();
    }

    private void OnFootstep(AnimationEvent animationEvent)
    {
        thirdPersonController?.OnFootstep(animationEvent);
    }

    private void OnLand(AnimationEvent animationEvent)
    {
        thirdPersonController?.OnLand(animationEvent);
    }


}
