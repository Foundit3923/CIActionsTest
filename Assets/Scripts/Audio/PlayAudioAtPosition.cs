using UnityEngine;
using UnityServiceLocator;

public class PlayAudioAtPosition : MonoBehaviour
{
    const int FREQUENCY = 44100;
    int lastPos, pos;
    AudioClip mic;
    AudioSource audio;
    bool isRecording = false;
    string microphone;
    internal void PlayerSpeech(Transform player, PlayerInformationManager inGamePlayerSettings, bool status)
    {
        microphone = inGamePlayerSettings.settings.micDeviceName.value;
        //var audio = inGamePlayerSettings.audioSource;
        if (status && !Microphone.IsRecording(inGamePlayerSettings.settings.micDeviceName.value))
        {
            isRecording = true;
            mic = Microphone.Start(inGamePlayerSettings.settings.micDeviceName.value, true, 10, FREQUENCY);

            audio.clip = AudioClip.Create("test", 10 * FREQUENCY, mic.channels, FREQUENCY, false);
            audio.loop = true;
        }
        else
        {
            isRecording = false;
            Microphone.End(inGamePlayerSettings.settings.micDeviceName.value);
            audio.loop = true;
            audio.Stop();
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        ServiceLocator.Global.Register(this);
        audio = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log($"RecordingInProgress: {Microphone.IsRecording(microphone)}");
        Debug.Log($"Audio is playing: {audio.isPlaying}");
        if ((pos = Microphone.GetPosition(microphone)) > 0)
        {
            if (lastPos > pos) lastPos = 0;

            if (pos - lastPos > 0)
            {
                // Allocate the space for the sample.
                float[] sample = new float[(pos - lastPos) * mic.channels];

                // Get the data from microphone.
                mic.GetData(sample, lastPos);

                // Put the data in the audio source.
                audio.clip.SetData(sample, lastPos);

                if (!audio.isPlaying) audio.Play();

                lastPos = pos;
            }
        }
    }
}
