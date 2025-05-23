using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;

public class LocalPlayerSounds : NetworkBehaviour
{
    private AudioSource audioSource;

    [Header("Sound Clip")]
    public AudioClip testClip;

    [Header("Ambient Sounds")]
    [SerializeField] private List<AudioClip> ambientClips = new();
    [SerializeField] private float minTimeBetween = 5f;
    [SerializeField] private float maxTimeBetween = 15f;

    [SerializeField] private bool ambientEnabled = true; // for testing purposes to easily toggle sound on / off

    private Vector3 front = new(0f, 5f, 5f);
    private Vector3 back = new(0f, 5f, -5f);
    private Vector3 left = new(-5f, 5f, 0f);
    private Vector3 right = new(5, 5f, 0f);
    private Vector3 head = new(0f, 10f, 0f);
    private Vector3 feet = new(0f, 0f, 0f);

    private List<Vector3> ambientDirections;
    private bool isRunningCoroutine = false; // Flag to check if coroutine is running

    void Start()
    {
        if (!isLocalPlayer) return;

        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = 1f; // Full 3D sound
        audioSource.playOnAwake = false;
        audioSource.loop = false;

        // Set up ambient directions
        ambientDirections = new List<Vector3> { front, back, left, right, head };

        // Start ambient loop if enabled
        if (ambientEnabled)
        {
            StartAmbientLoop();
        }
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        // Optionally toggle ambient sound on/off for testing
        if (Input.GetKeyDown(KeyCode.T))
        {
            ambientEnabled = !ambientEnabled;

            if (ambientEnabled)
            {
                StartAmbientLoop();
            }
            else
            {
                StopAmbientLoop();
            }
        }
    }

    private void StartAmbientLoop()
    {
        if (!isRunningCoroutine && ambientEnabled)
        {
            StartCoroutine(AmbientLoop());
            isRunningCoroutine = true;
        }
    }

    private void StopAmbientLoop()
    {
        if (isRunningCoroutine)
        {
            StopCoroutine(AmbientLoop());
            isRunningCoroutine = false;
        }
    }

    public void PlayDirectionalSound(AudioClip clip, Vector3 localOffset)
    {
        if (!isLocalPlayer || clip == null) return;

        audioSource.transform.localPosition = localOffset;

        audioSource.clip = clip;
        audioSource.Play();
    }

    // The ambient sound loop itself
    private IEnumerator AmbientLoop()
    {
        while (ambientEnabled)
        {
            yield return new WaitForSeconds(Random.Range(minTimeBetween, maxTimeBetween));

            if (ambientClips.Count == 0) continue;

            AudioClip clip = ambientClips[Random.Range(0, ambientClips.Count)];
            Vector3 dir = ambientDirections[Random.Range(0, ambientDirections.Count)];

            PlayDirectionalSound(clip, dir);
        }

        // Once we break out of the while loop, the coroutine has finished.
        isRunningCoroutine = false;
    }
}
