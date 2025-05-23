using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    //this is for reference for now. Will update when I start working on the player controller. current date and time = 02/12/25 8:15 pm EST
    [Range(0, 200)] public int startHealth = 100, currentHealth;
    public static bool isDead = false;

    private void Start()
        //set the player health  at start up. 
        => currentHealth = startHealth;

    private void Update()
    {
        if (currentHealth <= 0 && !isDead)
        {
            isDead = true;
            Debug.Log("Player has died.");
        }
    }

    public void TakeDamage(int amount)
    {
        Debug.Log($"Player took damage from monster for {amount} of damage");
        currentHealth -= amount;
    }
}
