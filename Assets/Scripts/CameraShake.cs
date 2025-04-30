using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public Animator animator;

    public void Shake(float duration)
    {
        StartCoroutine(ShakeCoroutine(duration));
    }
    public IEnumerator ShakeCoroutine(float duration)
    {
        animator.SetBool("IsShaking", true);
        yield return new WaitForSeconds(duration);
        animator.SetBool("IsShaking", false);
    }
}
