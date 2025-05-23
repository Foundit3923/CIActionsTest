using UnityEngine;

public class LightTracking : MonoBehaviour
{
    public float detectionRadius = 10f; // Range of light detection
    public float lightThreshold = 0.5f; // The threshold below which Sjena will attach to the player

    private Light[] lightsInRange; // Array to hold the lights within detection radius
    public float currentLightLevel = 0f; // The combined light level from all lights in range
    private Transform player; // Reference to the player(s) (could be set dynamically)

    public delegate void LightTrackingEvent();
    public static event LightTrackingEvent OnLightLevelTripped;

    void Start()
        // Find the player in the scene if it's not assigned manually
        => player = GameObject.FindWithTag("Player").transform;

    void Update() => TrackLightLevels();

    void TrackLightLevels()
    {
        // Get all lights in the scene
        lightsInRange = GetComponentInParent<Light[]>();

        currentLightLevel = 0f; // Reset the light level for each check

        // Iterate over all lights and check if they are within range of the player
        foreach (Light light in lightsInRange)
        {
            if (Vector3.Distance(light.transform.position, player.position) <= detectionRadius)
            {
                currentLightLevel += light.intensity; // Add light intensity if within range
            }
        }

        // Debugging: Print out the current light level
        Debug.Log("Current Light Level for Sjena (Player " + player.name + "): " + currentLightLevel);

        // Check if light level is below the threshold to allow attaching
        if (currentLightLevel < lightThreshold)
        {
            // Trigger action to attach Sjena to the player (this would interact with your state machine)
            Debug.Log("Sjena can attach to the player - Light level is low!");
        }
        else
        {
            Debug.Log("Sjena cannot attach - Light level is too high.");
        }
    }
}