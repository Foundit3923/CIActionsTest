using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using System.Collections;
using static UnityEngine.UI.GridLayoutGroup;
using UnityServiceLocator;
using UnityEditorInternal.Profiling.Memory.Experimental.FileFormat;

public class PlayerCameraController : MonoBehaviour
{
    //--------- Dependencies
    private TargetDirectory _targetDir;
    TargetDirectory targetDir
    {
        get
        {
            if (_targetDir != null)
            {
                return _targetDir;
            }

            if (TryGetComponent<TargetDirectory>(out TargetDirectory dir))
            {
                return _targetDir = dir;
            }

            return null;
        }
    }

    [SerializeField]
    private TargetDirectory.TargetType _cameraTarget = TargetDirectory.TargetType.Camera;
    private CinemachineVirtualCamera __mainCam;
    private CinemachineVirtualCamera _mainCam
    {
        get
        {
            if (__mainCam != null)
            {
                return __mainCam;
            }

            if (targetDir != null)
            {
                TargetDirectory.Entry entry = targetDir.GetTarget(_cameraTarget);
                if (entry != null)
                {
                    foreach (Transform child in entry.target.transform)
                    {
                        if (entry.target.gameObject.TryGetComponent<CinemachineVirtualCamera>(out CinemachineVirtualCamera mainCam))
                        {
                            return __mainCam = mainCam;
                        }
                    }
                }
            }

            return null;
        }
    }

    private PlayerInformationManager __pim;
    private PlayerInformationManager _pim
    {
        get
        {
            if (__pim != null)
            {
                return __pim;
            }

            if (TryGetComponent<PlayerInformationManager>(out PlayerInformationManager pim))
            {
                return __pim = pim;
            }

            return null;
        }
    }

    private PlayerProperties _pp
    {
        get
        {
            if (_pim != null)
            {
                return _pim.playerProperties;
            }

            return null;
        }
    }

    Blackboard _blackboard;
    Blackboard blackboard
    {
        get
        {
            if (_blackboard != null)
            {
                return _blackboard;
            }

            return _blackboard = blackboardController.GetBlackboard();
        }
        set => _blackboard = value;
    }
    BlackboardController _blackboardController;
    BlackboardController blackboardController
    {
        get
        {
            if (_blackboardController != null)
            {
                return _blackboardController;
            }

            return _blackboardController = ServiceLocator.Global.Get<BlackboardController>();
        }
        set => _blackboardController = value;
    }

    private List<GameObject> __players;
    private List<GameObject> _players
    {
        get
        {
            if (__players != null)
            {
                return __players;
            }

            if (blackboard != null)
            {
                if (blackboard.TryGetValue(BlackboardController.BlackboardKeyStrings.Players, out List<GameObject> players))
                {
                    return __players = players;
                }
            }

            return null;
        }
    }

    //public CinemachineCamera mainCam;  // Assign in inspector or find dynamically
    public CinemachineComponentBase firstPersonRig;
    public CinemachineComponentBase thirdPersonRig;

    //isSpectating should be a playerproperties value
    private bool _shouldRefreshTargetList = false;
    private Transform originalFollowTarget;
    private Transform originalLookAtTarget;

    public List<Transform> spectateTargets = new();
    private int currentTargetIndex = 0;

    public LoadManager.State State;

    public delegate void PlayerStartsSpectatingTargetEvent(GameObject player, GameObject target);
    public static event PlayerStartsSpectatingTargetEvent OnPlayerStartsSpectatingTarget;

    public delegate void PlayerStopsSpectatingTargetEvent(GameObject player, GameObject target);
    public static event PlayerStopsSpectatingTargetEvent OnPlayerStopsSpectatingTarget;

    void Awake()
    {
        State = LoadManager.State.Awake;
        Init();
        PlayerInformationManager.OnPlayerDeath += EvalSpectatorMode;
        PlayerInformationManager.OnPlayerRevived += ExitSpectateMode;
    }

    private bool Init()
    {
        if (_pp != null && _mainCam != null && _players != null)
        {
            originalFollowTarget = _mainCam.Follow;
            originalLookAtTarget = _mainCam.LookAt;
            //firstPersonRig.enabled = true;
            //thirdPersonRig.enabled = false;
            RefreshSpectateTargets();
            State = LoadManager.State.Initialized;
            return true;
        }

        return false;
    }

    void Update()
    {
        if (State < LoadManager.State.Initialized)
        {
            if (!Init()) { return; }
        }

        if (_shouldRefreshTargetList)
        {
            RefreshSpectateTargets();
        }

        // Exit spectate mode when player respawns
        if (!_pp.isDead && _pp.isSpectating)
        {
            ExitSpectateMode();
        }
    }

    public void EvalSpectatorMode(PlayerInformationManager pim, GameObject killer)
    {
        if (pim.gameObject == this.gameObject)
        {
            InvokeRepeating("RefreshSpectateTargets", 0, 2);

            if (!_pp.isSpectating)
            {
                if (spectateTargets.Count > 0)
                {
                    currentTargetIndex = 0;
                    EnterSpectateMode(spectateTargets[currentTargetIndex]);
                }
            }
        }
    }

    public void EnterSpectateMode(Transform target)
    {
        if (_mainCam == null || target == null) { return; }

        _pp.isSpectating = true;
        _mainCam.Follow = target;
        _mainCam.LookAt = target;

        //firstPersonRig.enabled = false;
        //thirdPersonRig.enabled = true;

        OnPlayerStartsSpectatingTarget?.Invoke(this.gameObject, target.gameObject);
    }

    public void ExitSpectateMode(PlayerInformationManager pim = default, GameObject savior = default)
    {
        if (pim.gameObject == this.gameObject)
        {
            if (_mainCam == null) { return; }

            CancelInvoke("RefreshSpectateTargets");

            _pp.isSpectating = false;

            OnPlayerStopsSpectatingTarget?.Invoke(this.gameObject, _mainCam.Follow.gameObject);

            _mainCam.Follow = originalFollowTarget;
            _mainCam.LookAt = originalLookAtTarget;

            //thirdPersonRig.enabled = false;
            //firstPersonRig.enabled = true;
        }
    }

    public void SpectateNext()
    {
        if (spectateTargets.Count == 0) { return; }

        currentTargetIndex = (currentTargetIndex + 1) % spectateTargets.Count;
        EnterSpectateMode(spectateTargets[currentTargetIndex]);
    }

    public void SpectatePrevious()
    {
        if (spectateTargets.Count == 0) {return;}

        currentTargetIndex--;
        if (currentTargetIndex < 0) { currentTargetIndex = spectateTargets.Count - 1; }

        EnterSpectateMode(spectateTargets[currentTargetIndex]);
    }

    public void RefreshSpectateTargets()
    {
        spectateTargets.Clear();
        foreach (GameObject playerObj in _players)
        {
            if (playerObj != null && playerObj.GetComponent<PlayerInformationManager>().playerProperties.isDead != true && playerObj.transform != this.transform)
            {
                spectateTargets.Add(playerObj.transform);
            }
        }
    }
}
