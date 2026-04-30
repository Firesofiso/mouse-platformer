using System.Collections;
using UnityEngine;

public static class MotionUtils
{
    public static IEnumerator BezierMove(Transform target, Vector3 controlOffset, Vector3 endOffset, float duration, float easePower = 2f)
    {
        var p0 = target.position;
        var p1 = p0 + controlOffset;
        var p2 = p0 + endOffset;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float e = Mathf.Pow(t, easePower);
            float u = 1f - e;
            target.position = u * u * p0 + 2f * u * e * p1 + e * e * p2;
            yield return null;
        }
    }
}
