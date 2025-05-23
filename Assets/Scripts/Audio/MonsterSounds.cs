using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class MonsterSounds : MonoBehaviour
{
    public AudioSource Source;
    public AudioClip deathClip;
    public AudioClip killClip;
    public AudioClip attachClip;
    public AudioClip idleClip;
    public AudioClip attachedClip1;
    public AudioClip attachedClip2;
    public float soundTimer;
    public float timeDelay;

    private bool shouldPlayRepeatedSounds = false;

    public void PlaySound(EnemyControllerStateMachine.EEnemyState state)
    {
        shouldPlayRepeatedSounds = false;
        switch (state)
        {
            case EnemyControllerStateMachine.EEnemyState.Attach:
                if (attachClip != null)
                {
                    Source.clip = attachClip;
                    Source.Play();
                    soundTimer = Source.clip.length;
                }

                shouldPlayRepeatedSounds = true;
                break;
            case EnemyControllerStateMachine.EEnemyState.Kill:
                if (killClip != null)
                {
                    Source.clip = killClip;
                    Source.Play();
                    soundTimer = Source.clip.length;
                }

                shouldPlayRepeatedSounds = false;
                break;
            case EnemyControllerStateMachine.EEnemyState.Death:
                if (deathClip != null)
                {
                    Source.clip = deathClip;
                    Source.Play();
                    soundTimer = Source.clip.length;
                }

                shouldPlayRepeatedSounds = false;
                break;
            case EnemyControllerStateMachine.EEnemyState.Idle:
                if (idleClip != null)
                {
                    Source.clip = idleClip;
                    Source.Play();
                }

                shouldPlayRepeatedSounds = false;
                break;
            default:
                break;
        }
    }

    private void Update()
    {
        if (Source.isPlaying)
        {
            soundTimer -= Time.deltaTime;
        }

        if (shouldPlayRepeatedSounds)
        {
            if (soundTimer <= 0)
            {
                if (Source.clip != attachedClip1)
                {
                    Source.clip = attachedClip1;
                    Source.Play();
                    soundTimer = attachClip.length;
                }
                else
                {
                    Source.clip = attachedClip2;
                    Source.Play();
                    soundTimer = attachClip.length;
                }
            }
        }
    }
}
