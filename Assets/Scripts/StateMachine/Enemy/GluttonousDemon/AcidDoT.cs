using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AcidDoT : MonoBehaviour
{
    public int damagePerSecond = 5;
    public int tickRate = 1; // DoT (in seconds)

    private HashSet<GameObject> playersInRange = new();
    private Coroutine damageCoroutine;

    private void OnEnable()
    {
        // Reset state in case the object was reused from a pool
        playersInRange.Clear();

        if (damageCoroutine != null)
        {
            StopCoroutine(damageCoroutine);
            damageCoroutine = null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playersInRange.Add(other.gameObject);

            // Start the coroutine when the first player enters
            damageCoroutine ??= StartCoroutine(ApplyDamageOverTime());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playersInRange.Remove(other.gameObject);

            // Stop coroutine if no players are left in range
            if (playersInRange.Count == 0 && damageCoroutine != null)
            {
                StopCoroutine(damageCoroutine);
                damageCoroutine = null;
            }
        }
    }

    private IEnumerator ApplyDamageOverTime()
    {
        // Loop while there are players inside the hazard
        while (playersInRange.Count > 0)
        {
            foreach (GameObject player in playersInRange)
            {
                if (player != null)
                {
                    PlayerInformationManager playerManager = player.GetComponent<PlayerInformationManager>();
                    // Apply damage using the DecreaseHealth method
                    playerManager?.DecreaseHealth(damagePerSecond);
                }
            }

            yield return new WaitForSeconds(tickRate);
        }

        // Reset coroutine when no players are left in range
        damageCoroutine = null;
    }
}