using UnityEngine;
using System.Collections;

public class ExplosiveProjectile : MonoBehaviour
{
    /// <summary>
    /// Coroutine to move the object along a parabolic arc to the target destination.
    /// </summary>
    /// <param name="targetPosition">The world position to travel to.</param>
    /// <param name="duration">The total time the travel should take.</param>
    /// <param name="arcHeight">The maximum height of the arc.</param>
    /// <returns>IEnumerator for the coroutine.</returns>
    public IEnumerator TravelToTarget(Vector3 targetPosition, float duration, float arcHeight)
    {
        Vector3 startPosition = transform.position;
        float timer = 0.0f;

        while (timer < duration)
        {
            // Calculate the current position along the arc
            float t = timer / duration;
            // This formula creates a parabolic curve for the y-axis
            float yOffset = arcHeight * 4 * (t - t * t);
            transform.position = Vector3.Lerp(startPosition, targetPosition, t) + new Vector3(0, yOffset, 0);

            timer += Time.deltaTime;
            yield return null;
        }

        // Ensure the projectile ends exactly at the target position
        transform.position = targetPosition;
    }
}
