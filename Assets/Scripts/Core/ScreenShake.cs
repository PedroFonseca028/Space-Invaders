using System.Collections;
using UnityEngine;

namespace SpaceInvaders
{
    // Attached to the main camera; nudges it briefly on hits/explosions.
    public class ScreenShake : MonoBehaviour
    {
        private Vector3 originalLocalPos;
        private Coroutine current;

        private void Awake()
        {
            originalLocalPos = transform.localPosition;
        }

        public void Shake(float duration, float magnitude)
        {
            if (current != null) StopCoroutine(current);
            current = StartCoroutine(ShakeRoutine(duration, magnitude));
        }

        private IEnumerator ShakeRoutine(float duration, float magnitude)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float damper = 1f - t / duration;
                Vector2 offset = Random.insideUnitCircle * magnitude * damper;
                transform.localPosition = originalLocalPos + new Vector3(offset.x, offset.y, 0f);
                yield return null;
            }
            transform.localPosition = originalLocalPos;
            current = null;
        }
    }
}
