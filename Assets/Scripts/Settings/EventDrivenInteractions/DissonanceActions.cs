using Dissonance;
using Dissonance.Audio.Capture;
using UnityEngine;
using UnityServiceLocator;

public class DissonanceActions : MonoBehaviour
{
    DissonanceComms comms;
    VoiceProximityBroadcastTrigger broadcastTrigger;
    BasicMicrophoneCapture micCapture;
    private bool validated = false;

    private Utility utils;
    public LoadManager.State state;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        state = LoadManager.State.Started;
        utils = ServiceLocator.Global.Get<Utility>();
        Settings.OnMicrophoneSettingsUpdated += UpdateMicrophoneSettings;
        TryGetComponent<BasicMicrophoneCapture>(out micCapture);
        TryGetComponent<VoiceProximityBroadcastTrigger>(out broadcastTrigger);
        TryGetComponent<DissonanceComms>(out comms);
        if (micCapture != null && broadcastTrigger != null && comms != null)
        {
            validated = true;
            state = LoadManager.State.Initialized;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!validated)
        {
            TryGetComponent<BasicMicrophoneCapture>(out micCapture);
            TryGetComponent<VoiceProximityBroadcastTrigger>(out broadcastTrigger);
            TryGetComponent<DissonanceComms>(out comms);
            if (micCapture != null && broadcastTrigger != null && comms != null)
            {
                validated = true;
                state = LoadManager.State.Initialized;
                utils.CatchupOnEventActions();
            }
        }
    }

    private void UpdateMicrophoneSettings(Settings settings)
    {
        utils.DebugOut("DissonanceActions UpdateMicrophoneSettings");
        if (utils.EvalInitState(state, () => UpdateMicrophoneSettings(settings))) { return; }

        Preconditions.CheckNotNull(settings);
        if (settings.micDeviceName.value is not null and not "")
        {
            comms.MicrophoneName = settings.micDeviceName.value;
        }
        else
        {
            utils.DebugOut("Error: No mic name is empty");
        }

        CommActivationMode mode = CommActivationMode.VoiceActivation;
        if (settings.pushToTalk.value)
        {
            mode = CommActivationMode.PushToTalk;
            broadcastTrigger.InputName = settings.ButtonsAndOverrides.GetBindingText("PushToTalk");
        }
        else
        {
            broadcastTrigger.InputName = null;
        }

        broadcastTrigger.Mode = mode;
    }
}
