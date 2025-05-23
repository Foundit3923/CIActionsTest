using System.Collections.Generic;
using System.Linq;
using GluttonousDemonStates;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityServiceLocator;

[RequireComponent(typeof(PlayerDetails))]
[RequireComponent(typeof(AnimatorRef))]
public class Settings : MonoBehaviour
{
    private Utility _utils;
    public Utility utils
    {
        get
        {
            if (_utils != null)
            {
                return _utils;
            }

            return _utils = ServiceLocator.Global.Get<Utility>();
        }
    }

    private AnimatorRef _animator;
    public AnimatorRef AnimatorRef
    {
        get
        {
            if (_animator != null)
            {
                return _animator;
            }

            this.gameObject.TryGetComponent<AnimatorRef>(out _animator);
            return _animator;
        }
    }

    public delegate void MicrophoneSettingsUpdatedEvent(Settings e);
    public static event MicrophoneSettingsUpdatedEvent OnMicrophoneSettingsUpdated;

    public class IntPrefProperty
    {
        public string key;
        public bool pendingChanges;
        public int defaultVal;
        public int pendingValue;
        public int value
        {
            get
            {
                int value = PlayerPrefs.GetInt(key, defaultVal);
                if (value < 0)
                {
                    value = defaultVal;
                }

                return value;
            }
            set
            {
                pendingChanges = true;
                pendingValue = value;
            }
        }
        public IntPrefProperty(string key, int defaultVal)
        {
            this.key = key;
            this.pendingChanges = false;
            this.defaultVal = defaultVal;
            this.pendingValue = defaultVal;
        }
        public void ResetProperty()
        {
            PlayerPrefs.DeleteKey(key);
            pendingChanges = true;
        }
    }

    public class StringPrefProperty
    {
        public string key;
        public bool pendingChanges;
        public string defaultVal;
        public string pendingValue;
        public string value
        {
            get
            {
                string value = PlayerPrefs.GetString(key, defaultVal);
                if (value == "")
                {
                    value = defaultVal;
                }

                return value;
            }
            set
            {
                pendingChanges = true;
                pendingValue = value;
            }
        }
        public StringPrefProperty(string key, string defaultVal)
        {
            this.key = key;
            this.pendingChanges = false;
            this.defaultVal = defaultVal;
            this.pendingValue = defaultVal;
        }
        public void ResetProperty()
        {
            PlayerPrefs.DeleteKey(key);
            pendingChanges = true;
        }
    }

    public class FloatPrefProperty
    {
        public string key;
        public bool pendingChanges;
        public float defaultVal;
        public float pendingValue;
        public float value
        {
            get
            {
                float value = PlayerPrefs.GetFloat(key, defaultVal);
                if (value < 0)
                {
                    value = defaultVal;
                }

                return value;
            }
            set
            {
                pendingChanges = true;
                pendingValue = value;
            }
        }
        public FloatPrefProperty(string key, float defaultVal)
        {
            this.key = key;
            this.pendingChanges = false;
            this.defaultVal = defaultVal;
            this.pendingValue = defaultVal;
        }
        public void ResetProperty()
        {
            PlayerPrefs.DeleteKey(key);
            pendingChanges = true;
        }
    }
    public class FloatRangePrefProperty
    {
        public string key;
        public bool pendingChanges;
        public float defaultVal;
        [Range(0f, 1f)]
        public float pendingValue;
        [Range(0f, 1f)]
        public float value
        {
            get => PlayerPrefs.GetFloat(key, defaultVal);
            set
            {
                pendingChanges = true;
                pendingValue = value;
            }
        }
        public FloatRangePrefProperty(string key, float defaultVal)
        {
            this.key = key;
            this.pendingChanges = false;
            this.defaultVal = defaultVal;
            this.pendingValue = defaultVal;
        }
        public void ResetProperty()
        {
            PlayerPrefs.DeleteKey(key);
            pendingChanges = true;
        }
    }

    public class BoolPrefProperty
    {
        public string key;
        public bool pendingChanges;
        public int defaultVal;
        [Range(0, 1)]
        public int pendingValue;
        [Range(0, 1)]
        public bool value
        {
            get => PlayerPrefs.GetInt(key, defaultVal) == 1 ? true : false;
            set
            {
                pendingChanges = true;
                pendingValue = value == true ? 1 : 0;
            }
        }
        public BoolPrefProperty(string key, bool defaultVal)
        {
            this.key = key;
            this.pendingChanges = false;
            this.defaultVal = defaultVal == true ? 1 : 0;
            this.pendingValue = defaultVal == true ? 1 : 0;
        }
        public void ResetProperty()
        {
            PlayerPrefs.DeleteKey(key);
            pendingChanges = true;
        }
    }

    //pim related to how users physically interact with the game
    private PlayerDetails _playerDetails;

    public PlayerDetails playerDetails
    {
        get
        {
            if (_playerDetails != null)
            {
                return _playerDetails;
            }

            return _playerDetails = this.GetOrAddComponent<PlayerDetails>();
        }
        set => _playerDetails = value;
    }

    private CinemachineCamera _followCameraPrefab;
    private CinemachineCamera _followCamera;
    public CinemachineCamera FollowCamera
    {
        get
        {
            if (utils.isOnline)
            {
                if (_followCamera != null)
                {
                    return _followCamera;
                }

                return _followCamera = this.GetComponent<TargetDirectory>().GetTarget(TargetDirectory.TargetType.Camera).target.gameObject.GetComponent<CinemachineCamera>();
            }
            else
            {
                return _followCameraPrefab;
            }
        }
        set => _followCameraPrefab = value;
    }

    private RenderTexture _activeTexture;
    public RenderTexture activeTexture
    {
        get
        {
            if (_activeTexture != null)
            {
                return _activeTexture;
            }

            activeTextureName.value = "default";
            return default(RenderTexture);
        }
        set
        {
            if (value == default(RenderTexture)) { activeTextureName.value = "default"; }
            else { activeTextureName.value = value.ToString(); }

            _activeTexture = value;
        }
    }

    private StringPrefProperty _activeTextureName;
    public StringPrefProperty activeTextureName
    {
        get
        {
            if (_activeTextureName == null)
            {
                return _activeTextureName = new StringPrefProperty("ActiveTextureName", "default");
            }

            return _activeTextureName;
        }
        set => _activeTextureName = value;
    }

    private FloatPrefProperty _pixelizationValue;
    public FloatPrefProperty pixelizationValue
    {
        get
        {
            if (_pixelizationValue == null)
            {
                return _pixelizationValue = new FloatPrefProperty("PixelizationValue", 0);
            }

            return _pixelizationValue;
        }
        set => _pixelizationValue = value;
    }

    private FloatPrefProperty _fovValue;
    public FloatPrefProperty fovValue
    {
        get
        {
            if(_fovValue == null)
            {
                return _fovValue = new FloatPrefProperty("FOVValue", 40f);
            }

            return _fovValue;
        }

        set => _fovValue = value;
    }
    //public string pixelizationValueKey = "PixelizationValue";
    //private float _pixelizationValue;
    //public float pixelizationValue
    //{
    //    get
    //    {
    //        return PlayerPrefs.GetFloat(pixelizationValueKey, 0);
    //    }
    //    set
    //    {
    //        _pendingChanges = true;
    //        _pixelizationValue = value;
    //    }
    //}

    private StringPrefProperty _language;
    public StringPrefProperty language
    {
        get
        {
            if (_language == null)
            {
                return _language = new StringPrefProperty("Language", "English");
            }

            return _language;
        }
        set => _language = value;
    }
    //public string languageKey = "Language";
    //private string _language;
    //public string language
    //{
    //    get
    //    {
    //        return PlayerPrefs.GetString(languageKey, "English");
    //    }
    //    set
    //    {
    //        _pendingChanges = true;
    //        _language = value;
    //    }
    //}

    private BoolPrefProperty _invertY;
    public BoolPrefProperty invertY
    {
        get
        {
            if (_invertY == null)
            {
                return _invertY = new BoolPrefProperty("InvertY", false);
            }

            return _invertY;
        }
        set => _invertY = value;
    }
    //public string invertYKey = "InvertY";
    //[Range(0, 1)]
    //private int _invertY;
    //[Range(0, 1)]
    //public int invertY
    //{
    //    get
    //    {
    //        return PlayerPrefs.GetInt(invertYKey, 0);
    //    }
    //    set
    //    {
    //        _pendingChanges = true;
    //        _invertY = value;
    //    }
    //}

    private BoolPrefProperty _invertX;
    public BoolPrefProperty invertX
    {
        get
        {
            if (_invertX == null)
            {
                return _invertX = new BoolPrefProperty("InvertX", false);
            }

            return _invertX;
        }
        set => _invertX = value;
    }
    //public string invertXKey = "InvertX";
    //[Range(0, 1)]
    //private int _invertX;
    //[Range(0, 1)]
    //public int invertX
    //{
    //    get
    //    {
    //        return PlayerPrefs.GetInt(invertXKey, 0);
    //    }
    //    set
    //    {
    //        _pendingChanges = true;
    //        _invertX = value;
    //    }
    //}

    private FloatPrefProperty _mouseSensitivity;
    public FloatPrefProperty mouseSensitivity
    {
        get
        {
            if (_mouseSensitivity == null)
            {
                return _mouseSensitivity = new FloatPrefProperty("MouseSens", 0.5f);
            }

            return _mouseSensitivity;
        }
        set => _mouseSensitivity = value;
    }
    //public string mouseSensitivityKey = "MouseSens";
    //private float _mouseSensitivity;
    //public float mouseSensitivity
    //{
    //    get
    //    {
    //        return PlayerPrefs.GetFloat(mouseSensitivityKey, 0.5f);
    //    }
    //    set
    //    {
    //        _pendingChanges = true;
    //        _mouseSensitivity = value;
    //    }
    //}

    public float sensitivityMultiplier = 7f;

    private BoolPrefProperty _vsyncCount;
    public BoolPrefProperty vsyncCount
    {
        get
        {
            if (_vsyncCount == null)
            {
                return _vsyncCount = new BoolPrefProperty("VsyncCount", false);
            }

            return _vsyncCount;
        }
        set => _vsyncCount = value;
    }
    //public string vsyncCountKey = "VsyncCount";
    //[Range(0, 1)]
    //private int _vsyncCount;
    //[Range(0, 1)]
    //public int vsyncCount
    //{
    //    get
    //    {
    //        return PlayerPrefs.GetInt(vsyncCountKey, 0);
    //    }
    //    set
    //    {
    //        _pendingChanges = true;
    //        _vsyncCount = value;
    //    }
    //}

    private BoolPrefProperty _fullscreen;
    public BoolPrefProperty fullscreen
    {
        get
        {
            if (_fullscreen == null)
            {
                return _fullscreen = new BoolPrefProperty("Fullscreen", true);
            }

            return _fullscreen;
        }
        set => _fullscreen = value;
    }
    //public string fullscreenKey = "Fullscreen";
    //[Range(0, 1)]
    //private int _fullscreen;
    //[Range(0, 1)]
    //public int fullscreen
    //{
    //    get
    //    {
    //        return PlayerPrefs.GetInt(fullscreenKey, 1);
    //    }
    //    set
    //    {
    //        _pendingChanges = true;
    //        _fullscreen = value;
    //    }
    //}

    private FloatPrefProperty _brightness;
    public FloatPrefProperty brightness
    {
        get
        {
            if (_brightness == null)
            {
                return _brightness = new FloatPrefProperty("Brightness", 0.5f);
            }

            return _brightness;
        }
        set => _brightness = value;
    }
    //public string brightnesKey = "Brightness";
    //private float _brightness;
    //public float brightness
    //{
    //    get
    //    {
    //        return PlayerPrefs.GetFloat(brightnesKey, 0.5f);
    //    }
    //    set
    //    {
    //        _pendingChanges = true;
    //        _brightness = value;
    //    }
    //}

    private IntPrefProperty _micDeviceIndex;
    public IntPrefProperty micDeviceIndex

    {
        get
        {
            if (_micDeviceIndex == null)
            {
                return _micDeviceIndex = new IntPrefProperty("Microphone", 0);
            }

            return _micDeviceIndex;
        }
        set => _micDeviceIndex = value;
    }
    //public string micDeviceIndexKey = "Microphone";
    //private int _micDeviceIndex;
    //public int micDeviceIndex
    //{
    //    get
    //    {
    //        return PlayerPrefs.GetInt(micDeviceIndexKey, 0);
    //    }
    //    set
    //    {
    //        _pendingChanges = true;
    //        _micDeviceIndex = value;
    //    }

    //}

    private StringPrefProperty _micDeviceName;
    public StringPrefProperty micDeviceName
    {
        get
        {
            if (_micDeviceName == null)
            {
                return _micDeviceName = new StringPrefProperty("MicName", Microphone.devices[0]);
            }

            return _micDeviceName;
        }
        set => _micDeviceName = value;
    }
    //public string micDeviceNameKey = "MicName";
    //private string _micDeviceName;
    //public string micDeviceName
    //{
    //    get
    //    {
    //        return PlayerPrefs.GetString(micDeviceNameKey, Microphone.devices[0]);
    //    }
    //    set
    //    {
    //        _pendingChanges = true;
    //        _micDeviceName = value;
    //    }
    //}

    public class ButtonNamesAndOverrides
    {
        public readonly Dictionary<string, string> _ButtonNameToOverridePath = new();

        public string this[InputAction key]
        {
            get => PlayerPrefs.GetString(key.name);

            set => _ButtonNameToOverridePath[key.name] = value;
        }

        public string this[string key]
        {
            get => PlayerPrefs.GetString(key);

            set => _ButtonNameToOverridePath[key] = value;
        }

        public string GetBindingText(string key)
        {
            string bindingText = this[key];
            var strings = bindingText.Split('/', 2);
            bindingText = strings[1];
            return bindingText;
        }

        public void SaveToPlayerPrefs()
        {
            foreach (string key in _ButtonNameToOverridePath.Keys)
            {
                PlayerPrefs.SetString(key, _ButtonNameToOverridePath[key]);
            }
        }

        public void ResetDict() => _ButtonNameToOverridePath.Clear();
    }

    public ButtonNamesAndOverrides ButtonsAndOverrides = new();

    private BoolPrefProperty _pushToTalk;
    public BoolPrefProperty pushToTalk
    {
        get
        {
            if (_pushToTalk == null)
            {
                return _pushToTalk = new BoolPrefProperty("PushToTalk", false);
            }

            return _pushToTalk;
        }
        set => _pushToTalk = value;
    }
    //[Range(0, 1)]
    //private int _pushToTalk = 0;
    //[Range(0, 1)]
    //public int pushToTalk
    //{
    //    get
    //    {
    //        return PlayerPrefs.GetInt("PushToTalk", 0);
    //    }
    //    set
    //    {
    //        _pendingChanges = true;
    //        _pushToTalk = value;
    //    }

    //}

    private BoolPrefProperty _micEnabled;
    public BoolPrefProperty micEnabled
    {
        get
        {
            if (_micEnabled == null)
            {
                return _micEnabled = new BoolPrefProperty("MicEnabled", false);
            }

            return _micEnabled;
        }
        set => _micEnabled = value;
    }
    //public string micEnabledKey = "MicEnabled";
    //[Range(0, 1)]
    //private int _micEnabled;
    //[Range(0, 1)]
    //public int micEnabled
    //{
    //    get
    //    {
    //        return PlayerPrefs.GetInt(micEnabledKey, 0);
    //    }
    //    set
    //    {
    //        _pendingChanges = true;
    //        _micEnabled = value;
    //    }

    //}

    private FloatPrefProperty _masterVolume;
    public FloatPrefProperty masterVolume
    {
        get
        {
            if (_masterVolume == null)
            {
                return _masterVolume = new FloatPrefProperty("Volume", 0.5f);
            }

            return _masterVolume;
        }
        set => _masterVolume = value;
    }
    //public string masterVolumeKey = "Volume";
    //[Range(0, 1)]
    //private float _masterVolume;
    //[Range(0, 1)]
    //public float masterVolume
    //{
    //    get
    //    {
    //        return PlayerPrefs.GetFloat(masterVolumeKey, 0.5f);
    //    }
    //    set
    //    {
    //        _pendingChanges = true;
    //        _masterVolume = value;
    //    }
    //}

    private FloatRangePrefProperty _playerMicVolume;
    public FloatRangePrefProperty playerMicVolume
    {
        get
        {
            if (_playerMicVolume == null)
            {
                return _playerMicVolume = new FloatRangePrefProperty("MicInputVolume", 0.5f);
            }

            return _playerMicVolume;
        }
        set => _playerMicVolume = value;
    }
    //public string playerMicVolumeKey = "MicInputVolume";
    //[Range(0, 1)]
    //private float _playerMicVolume;
    //[Range(0, 1)]
    //public float playerMicVolume
    //{
    //    get
    //    {
    //        return PlayerPrefs.GetFloat(playerMicVolumeKey, 0.5f);
    //    }
    //    set
    //    {
    //        _pendingChanges = true;
    //        _playerMicVolume = value;
    //    }
    //}

    private StringPrefProperty _volumeGroupName;
    public StringPrefProperty volumeGroupName
    {
        get
        {
            if (_volumeGroupName == null)
            {
                return _volumeGroupName = new StringPrefProperty("VolumeGroupName", "MasterVolume");
            }

            return _volumeGroupName;
        }
        set => _volumeGroupName = value;
    }
    //public string volumeGroupNameKey = "VolumeGroupName";
    //private string _volumeGroupName;
    //public string volumeGroupName
    //{
    //    get
    //    {
    //        return PlayerPrefs.GetString(volumeGroupNameKey, "MasterVolume");
    //    }
    //    set
    //    {
    //        _pendingChanges = true;
    //        _volumeGroupName = value;
    //    }
    //}

    public InputActionAsset InputActionAsset;

    private PlayerInput _playerInput;
    public PlayerInput playerInput
    {
        get
        {
            if (_playerInput != null)
            {
                return _playerInput;
            }

            return _playerInput = this.GetOrAddComponent<PlayerInput>();
        }
        set => _playerInput = value;
    }

    //private AudioMixer _audioMixer;
    //public AudioMixer audioMixer
    //{
    //    get
    //    {
    //        //if (_audioMixer != null)
    //        //{
    //        //    return _audioMixer;
    //        //}
    //        return _audioMixer;
    //    }
    //    set
    //    {
    //        _audioMixer = value;
    //    }
    //}

    public AudioMixer mixer;

    private AudioSource _audioSource;
    public AudioSource audioSource
    {
        get
        {
            if (_audioSource != null)
            {
                return _audioSource;
            }

            return _audioSource = this.GetOrAddComponent<AudioSource>();
        }
        set => _audioSource = value;
    }

    private Volume _universalVolume;
    public Volume universalVolume
    {
        get
        {
            if (_universalVolume != null)
            {
                return _universalVolume;
            }

            return _universalVolume = this.GetOrAddComponent<Volume>();
        }
        set => _universalVolume = value;
    }

    private bool _saving = false;

    public void SetValues(Settings newSettings)
    {
        utils.DebugOut("Settings: Apply Settings from source");
        _saving = true;
        micDeviceIndex = newSettings.micDeviceIndex != default ? newSettings.micDeviceIndex : micDeviceIndex;
        micDeviceName = newSettings.micDeviceName ?? micDeviceName;
        ButtonsAndOverrides = newSettings.ButtonsAndOverrides ?? ButtonsAndOverrides;
        pushToTalk = newSettings.pushToTalk != default ? newSettings.pushToTalk : pushToTalk;
        micEnabled = newSettings.micEnabled != default ? newSettings.micEnabled : micEnabled;
        masterVolume = newSettings.masterVolume != default ? newSettings.masterVolume : masterVolume;
        playerMicVolume = newSettings.playerMicVolume != default ? newSettings.playerMicVolume : playerMicVolume;
        InputActionAsset = newSettings.InputActionAsset ?? InputActionAsset;
        playerInput = newSettings.playerInput ?? playerInput;
        audioSource = newSettings.audioSource ?? audioSource;
        universalVolume = newSettings.universalVolume ?? universalVolume;
        brightness = newSettings.brightness != default ? newSettings.brightness : brightness;
        fullscreen = newSettings.fullscreen != default ? newSettings.fullscreen : fullscreen;
        vsyncCount = newSettings.vsyncCount != default ? newSettings.vsyncCount : vsyncCount;
        mouseSensitivity = newSettings.mouseSensitivity != default ? newSettings.mouseSensitivity : mouseSensitivity;
        invertX = newSettings.invertX != default ? newSettings.invertX : invertX;
        invertY = newSettings.invertY != default ? newSettings.invertY : invertY;
        language = newSettings.language ?? language;
        activeTexture = newSettings.activeTexture ?? activeTexture;
        activeTextureName = newSettings.activeTextureName ?? activeTextureName;
        pixelizationValue = newSettings.pixelizationValue != default ? newSettings.pixelizationValue : pixelizationValue;
        volumeGroupName = newSettings.volumeGroupName ?? volumeGroupName;
        sensitivityMultiplier = newSettings.sensitivityMultiplier != default ? newSettings.sensitivityMultiplier : sensitivityMultiplier;
        fovValue = newSettings.fovValue != default ? newSettings.fovValue : fovValue;
        //adding everything will flip the local _pendingChanges to true. The incoming _pendingSettings will indicate if there are any unsaved settings
        _saving = false;
    }

    public void SaveToDisk()
    {
        utils.DebugOut("Settings: Save Settings to Disk");
        if (micDeviceIndex.pendingChanges) { PlayerPrefs.SetInt("Microphone", micDeviceIndex.pendingValue); }

        if (micDeviceName.pendingChanges) { PlayerPrefs.SetString("MicName", micDeviceName.pendingValue); }

        if (micEnabled.pendingChanges) { PlayerPrefs.SetInt("MicEnabled", micEnabled.pendingValue); }

        if (masterVolume.pendingChanges) { PlayerPrefs.SetFloat("Volume", masterVolume.pendingValue); }

        if (playerMicVolume.pendingChanges) { PlayerPrefs.SetFloat("MicInputVolume", playerMicVolume.pendingValue); }

        if (volumeGroupName.pendingChanges) { PlayerPrefs.SetString("VolumeMixerName", volumeGroupName.pendingValue); }

        if (brightness.pendingChanges) { PlayerPrefs.SetFloat("Brightness", brightness.pendingValue); }

        if (fullscreen.pendingChanges) { PlayerPrefs.SetInt("Fullscreen", fullscreen.pendingValue); }

        if (vsyncCount.pendingChanges) { PlayerPrefs.SetInt("VsyncCount", vsyncCount.pendingValue); }

        if (mouseSensitivity.pendingChanges) { PlayerPrefs.SetFloat("MouseSens", mouseSensitivity.pendingValue); }

        if (invertX.pendingChanges) { PlayerPrefs.SetInt("InvertX", invertX.pendingValue); }

        if (invertY.pendingChanges) { PlayerPrefs.SetInt("InvertY", invertY.pendingValue); }

        if (language.pendingChanges) { PlayerPrefs.SetString("Language", language.pendingValue); }

        if (activeTextureName.pendingChanges) { PlayerPrefs.SetString("ActiveTextureName", activeTextureName.pendingValue); }

        if (pixelizationValue.pendingChanges) { PlayerPrefs.SetFloat("PixelizationValue", pixelizationValue.pendingValue); }

        if (fovValue.pendingChanges) { PlayerPrefs.SetFloat("FOVValue", fovValue.pendingValue); }

        ButtonsAndOverrides.SaveToPlayerPrefs();
        PlayerPrefs.Save();
        OnMicrophoneSettingsUpdated?.Invoke(this);
    }

    public void ClearUnsavedValues()
    {
        utils.DebugOut("Settings: Clear Unsaved Values");
        _micDeviceIndex.pendingValue = _micDeviceIndex.defaultVal;
        _micDeviceName.pendingValue = _micDeviceName.defaultVal;
        _micEnabled.pendingValue = _micEnabled.defaultVal;
        _masterVolume.pendingValue = _masterVolume.defaultVal;
        _playerMicVolume.pendingValue = _playerMicVolume.defaultVal;
        _volumeGroupName.pendingValue = _volumeGroupName.defaultVal;
        _brightness.pendingValue = _brightness.defaultVal;
        _fullscreen.pendingValue = _fullscreen.defaultVal;
        _vsyncCount.pendingValue = _vsyncCount.defaultVal;
        _mouseSensitivity.pendingValue = _mouseSensitivity.defaultVal;
        _invertX.pendingValue = _invertX.defaultVal;
        _invertY.pendingValue = _invertY.defaultVal;
        _language.pendingValue = _language.defaultVal;
        _activeTextureName.pendingValue = _activeTextureName.defaultVal;
        _pixelizationValue.pendingValue = _pixelizationValue.defaultVal;
        _fovValue.pendingValue = _fovValue.defaultVal;
        ButtonsAndOverrides = default;
    }

    private void Awake()
    {
        //OnAwake();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //OnStarting();
    }
    //protected abstract void OnStarting();
    //protected abstract void OnAwake();

    // Update is called once per frame
    void Update()
    {

    }

    public void ApplyBindings(string actionMapName = "Player")
    {
        if (actionMapName == "Player")
        {
            utils.DebugOut($"Settings: Apply bindings to {actionMapName}");
        }
        else if (actionMapName == "Default")
        {
            utils.DebugOut($"Settings: Reset bindings using {actionMapName}");
        }

        if (InputActionAsset == null) { InputActionAsset = playerInput.actions; }

        InputActionMap playerActionMap = InputActionAsset.actionMaps[actionMapName == "Default" ? 1 : 0];

        foreach (var action in playerActionMap.actions)
        {
            if (!action.bindings[0].effectivePath.Contains("Keyboard") || action.bindings[0].isPartOfComposite)
                continue;
            string overridePath = PlayerPrefs.GetString(action.name, action.bindings[0].path);
            if (overridePath == "")
            {
                overridePath = action.bindings[0].path;
                ButtonsAndOverrides[action] = overridePath;
            }

            action.ApplyBindingOverride(overridePath);
        }

        SaveToDisk();
    }

    public void ResetBindings()
    {
        utils.DebugOut("Settings: ResetBindings");
        ButtonsAndOverrides.ResetDict();
        ApplyBindings("Default");
    }
}
