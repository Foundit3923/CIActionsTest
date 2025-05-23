using Mirror;
using UnityEngine;
using static EnemyControllerContext;

public class PlaySound : AnimationStateMonitor
{
    private MonsterSounds soundsManager;
    public override void OnEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        int currentState = animator.GetInteger("CurrentState");
        CmdPlaySound(currentState);
    }

    [Command(requiresAuthority = false)]
    public void CmdPlaySound(int currentStateInt)
    {
        EnemyControllerStateMachine.EEnemyState currentState = (EnemyControllerStateMachine.EEnemyState)currentStateInt;// EnemyProperties.gameObject.GetComponent<EnemyControllerStateMachine>().GetCurrentState();
        soundsManager = EnemyProperties.gameObject.GetComponent<MonsterSounds>();
        soundsManager.PlaySound(currentState);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
