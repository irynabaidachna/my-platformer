
using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    private Vector3 basePosition;
    private Coroutine current;

    private void Awake()
    {
        basePosition = transform.localPosition;
    }

    public void Shake(float duration = 0.12f, float strength = 0.15f)
    {
        // Не дозволяємо двом тряскам накладатися
        if (current != null)
            StopCoroutine(current);

        current = StartCoroutine(ShakeRoutine(duration, strength));
    }

    private IEnumerator ShakeRoutine(float duration, float strength)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // Поступове затухання тряски
            float damper = 1f - (elapsed / duration);

            transform.localPosition = basePosition +
                (Vector3)(Random.insideUnitCircle * strength * damper);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // Повертаємо камеру в початкову позицію
        transform.localPosition = basePosition;
        current = null;
    }

}
