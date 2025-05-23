//TODO: Detatch from BlackboardSystem, implement event triggers

using System.Collections.Generic;
using System;
//using Dissonance;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityServiceLocator;
using Unity.VisualScripting;
using static IClientExpert;
using System.Linq;
using UnityEngine.Audio;
using Mirror;
using System.Dynamic;
using Unity.Cinemachine;

[RequireComponent(typeof(Settings))]
[RequireComponent(typeof(PlayerProperties))]
public class PlayerInformationManager : MonoBehaviour
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
    //pim related to User (PlayerDetails, Graphics, Audio, Controls, Gameplay)
    private Settings _settings;

    public Settings settings
    {
        get
        {
            if (_settings != null)
            {
                return _settings;
            }

            return _settings = this.GetOrAddComponent<Settings>();
        }
    }

    //In-game information about player character
    private PlayerProperties _playerProperties;
    public PlayerProperties playerProperties
    {
        get
        {
            if (_playerProperties != null)
            {
                return _playerProperties;
            }

            return _playerProperties = this.GetOrAddComponent<PlayerProperties>();
        }
    }

    private bool updateInProgress;

    private bool updateSettings;

    public delegate void PlayerCreatedEvent(PlayerInformationManager e);
    public static event PlayerCreatedEvent OnPlayerCreated;

    public delegate void PlayerDeathEvent(PlayerInformationManager e, GameObject killer = default);
    public static event PlayerDeathEvent OnPlayerDeath;

    public delegate void PlayerRevivedEvent(PlayerInformationManager e, GameObject savior = default);
    public static event PlayerRevivedEvent OnPlayerRevived;

    public delegate void PlayerDamagedEvent(PlayerInformationManager e, int damage = -1, GameObject attacker = default);
    public static event PlayerDamagedEvent OnPlayerDamaged;

    public delegate void PlayerHealedEvent(PlayerInformationManager e, int healing = -1, GameObject healer = default);
    public static event PlayerHealedEvent OnPlayerHealed;

    void Start()
    {
        updateInProgress = false;
        OnPlayerCreated?.Invoke(this);
    }

    private void OnEnable()
    {
        EnemyControllerContext.OnDamagePlayer += DecreaseHealth;
        EnemyControllerContext.OnHealPlayer += IncreaseHealth;
        Portal.OnPlayerEnterLevel += () => playerProperties.inLevel = true;
        Portal.OnPlayerExitLevel += () => playerProperties.inLevel = false;
    }

    private void OnDisable()
    {
        EnemyControllerContext.OnDamagePlayer -= DecreaseHealth;
        EnemyControllerContext.OnHealPlayer -= IncreaseHealth;
        Portal.OnPlayerEnterLevel -= () => playerProperties.inLevel = true;
        Portal.OnPlayerExitLevel -= () => playerProperties.inLevel = false;
    }

    void Update()
    {
        if (updateSettings && !updateInProgress)
        {
            updateSettings = false;
            updateInProgress = true;
            settings.SaveToDisk();
            updateInProgress = false;
        }
    }

    public void SetValues(PlayerInformationManager newManager)
    {
        settings.SetValues(newManager.settings);
        settings.SaveToDisk();
        playerProperties.SetValues(newManager.playerProperties);
        updateInProgress = false;
        updateSettings = false;
    }

    //Temporary, needs to be expanded for functionality
    public void SwitchMicrophone(string micName, int micIndex)
    {
        settings.micEnabled.value = true;
        settings.micDeviceIndex.value = micIndex;
        settings.micDeviceName.value = micName;
        updateSettings = true;
    }

    public void SetAsDead(GameObject killer = default)
    {
        bool death = !playerProperties.isDead;
        playerProperties.AddExperience(PlayerProperties.PlayerState.Dead);
        playerProperties.isDead = true;
        playerProperties.health = 0;
        settings.AnimatorRef.SetParam(AnimatorRef.PlayerAnimatorParams.B_Is_Dead, true);
        settings.AnimatorRef.SetParam(AnimatorRef.PlayerAnimatorParams.T_Kill_Player);
        if (death && OnPlayerDeath != null)
        {
            OnPlayerDeath(this);
        }
    }

    public void SetAsAlive(GameObject savior = default)
    {
        bool revival = playerProperties.isDead;
        playerProperties.AddExperience(PlayerProperties.PlayerState.Revived);
        playerProperties.isDead = false;
        settings.AnimatorRef.SetParam(AnimatorRef.PlayerAnimatorParams.B_Is_Dead, false);
        SetAttachedMonster(PlayerProperties.Persuer.None, true);
        if (revival && OnPlayerRevived != null)
        {
            OnPlayerRevived(this);
        }
    }

    public void DecreaseHealth(int damage, GameObject attacker = default)
    {
        playerProperties.health -= damage;
        playerProperties.AddExperience(PlayerProperties.PlayerState.Damaged);
        OnPlayerDamaged?.Invoke(this, damage, attacker);
        if (playerProperties.health <= 0)
        {
            playerProperties.health = 0;
            SetAsDead(attacker);
        }
    }

    public void IncreaseHealth(int healing, GameObject healer = default)
    {
        if (playerProperties.health < 100)
        {
            playerProperties.AddExperience(PlayerProperties.PlayerState.Healed);
            OnPlayerHealed?.Invoke(this, healing, healer);
            if (playerProperties.isDead)
            {
                if (100 - healing < healing)
                {
                    playerProperties.health = 100;
                }
                else
                {
                    playerProperties.health += healing;
                }

                if (playerProperties.health > 0)
                {
                    SetAsAlive(healer);
                }
                else
                {
                    playerProperties.AddExperience(PlayerProperties.PlayerState.FailedRevival);
                }
            }
            else
            {
                if (100 - healing < healing)
                {
                    playerProperties.health = 100;
                }
                else
                {
                    playerProperties.health += healing;
                }
            }
        }
    }

    public void RestoreHealthToFull(GameObject healer = default)
    {
        playerProperties.health = 100;
        playerProperties.AddExperience(PlayerProperties.PlayerState.FullHeal);
        OnPlayerHealed?.Invoke(this, 100, healer);
        if (playerProperties.isDead)
        {
            SetAsAlive(healer);
        }
    }

    public void SetAttachedMonster(PlayerProperties.Persuer monsterToAttach, bool attached)
    {
        settings.AnimatorRef.SetParam(AnimatorRef.PlayerAnimatorParams.I_Chosen_State, (int)monsterToAttach);
        playerProperties.SetPersuer(monsterToAttach);
        switch (monsterToAttach)
        {
            case PlayerProperties.Persuer.LanternHead:
                break;
            case PlayerProperties.Persuer.Sjena:
                break;
            case PlayerProperties.Persuer.NightGaunt:
                break;
            case PlayerProperties.Persuer.Cymothoa:
                break;
            default:
                break;
        }
    }

    public void ApplySettings()
    {
        utils.DebugOut("PIM: Apply Settings");
        Screen.fullScreen = settings.fullscreen.value;
        QualitySettings.vSyncCount = settings.vsyncCount.value == true ? 1 : 0;
        if (utils.isOnline)
        {
            settings.FollowCamera.Lens.FieldOfView = settings.fovValue.value;
        }

        settings.mixer.SetFloat(settings.volumeGroupName.value, settings.masterVolume.value);
        if (Microphone.devices.Contains(settings.micDeviceName.value))
        {
            if (settings.micEnabled.value)
            {
                Microphone.Start(settings.micDeviceName.value, true, 999, 44100);
            }
            else
            {
                Microphone.End(settings.micDeviceName.value);
            }
        }
        else
        {
            settings.micEnabled.value = false;
            settings.micDeviceIndex.value = 0;
        }
        //Screen.brightness = settings.brightness;
        if (settings.audioSource != null)
        {
            settings.audioSource.volume = settings.playerMicVolume.value;
        }

        Screen.brightness = settings.brightness.value;

        settings.ApplyBindings();
    }
}
