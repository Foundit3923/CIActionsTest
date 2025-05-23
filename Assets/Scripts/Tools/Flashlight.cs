using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.UI;

public class Flashlight : InteractableNetworkedFlickeringLightEffect
{
    [SerializeField]
    private SpotLight _spotLight;
    [SerializeField]
    private float _range = 50f;
    public GameObject Owner;
    [SerializeField]
    private int _damageToSjena = 1;

    private int _layerMask;

    [SyncVar(hook = nameof(SetState))] bool isTurnedOn;

    TargetDirectory targetDir;

    [SerializeField]
    TargetDirectory.TargetType To;
    [SerializeField]
    TargetDirectory.TargetType From;

    private Transform _rightIndex2
    {
        get
        {
            if (owner != null)
            {
                return targetDir.GetTarget(To).target.transform;
            }

            return null;
        }
    }
    private Transform _rightPinky2
    {
        get
        {
            if (owner != null)
            {
                return targetDir.GetTarget(From).target.transform;
            }

            return null;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public override void OnStart()
    {
        base.Init();
        this.isTurnedOn = false;
        light.type = UnityEngine.LightType.Spot;
        light.range = _range;
        ShouldFlicker = false;
        ShouldSpark = false;

        PlayerMenuInteractions.OnPlayerInteractWithTool += CmdPlayerInteraction;
        Debug.Log("Intensity: " + light.intensity);
        light.spotAngle = 90;
        light.innerSpotAngle = 45;

        _layerMask = LayerMask.GetMask("Sjena", "Obstacle");
    }
    public override void OnUpdate()
    {
        base.OnUpdate();
        CmdUpdateForward();
        RayCheckForSjena();
    }

    [Command(requiresAuthority = false)]
    void CmdUpdateForward()
    {
        if (owner != null)
        {
            this.transform.up = -(_rightIndex2.position - _rightPinky2.position).normalized;
        }
    }

    public override void OnOwnerChanged(GameObject old, GameObject @new)
    {
        mountpoint = @new?.GetComponent<MountPointDirectory>().GetTarget(MountPointDirectory.MountPointTargetType.Grip).target;
        targetDir = @new?.GetComponent<TargetDirectory>();
        //_rightIndex2 = targetDir?.GetTarget(To).target.transform;
        //_rightPinky2 = targetDir?.GetTarget(From).target.transform;
    }

    private void SetState(bool old, bool @new)
    {
        if (@new)
        {
            TurnOn();
        }
        else
        {
            TurnOff();
        }
    }

    [Command]
    public override void CmdPlayerInteraction(PlayerMenuInteractions menuInteractions) 
    {
        if (owner != null) { this.isTurnedOn = !this.isTurnedOn; }
    }

    private void RayCheckForSjena()
    {
        if (this.isTurnedOn)
        {
            if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out RaycastHit hit, 48, _layerMask))
            {
                if (hit.collider.gameObject.layer == LayerMask.GetMask("Sjena"))
                {
                    //hit
                    hit.collider.gameObject.GetComponent<EnemyControllerStateMachine>().ApplyDamage(_damageToSjena);
                }
            }
        }
    }
}
