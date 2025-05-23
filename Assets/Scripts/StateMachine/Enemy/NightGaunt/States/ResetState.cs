using UnityEngine;

namespace NightGauntStates
{
    public class ResetState : EnemyControllerState
    {
        public ResetState(EnemyControllerContext context, EnemyControllerStateMachine.EEnemyState estate) : base(context, estate)
        {
            EnemyControllerContext Context = context;
        }

        public override bool StateMachineActivitySetter() => Context.isStateMachineActive;
        bool foundTarget;

        public override void EnterState()
        {
            Debug.Log($"------------{Context.Self.name} | reset state initialized.");
            Context.shouldChase = false;
            Context.shouldFlee = false;
            Context.Agent.stoppingDistance = 1f;
            foundTarget = false;
            Context.SetNightGauntTarget();
            Debug.Log("Target Player found: " + (Context.TargetPlayer != null ? Context.TargetPlayer.name : "None"));
            if (Context.TargetPlayer != null)
            {
                foundTarget = true;
            }
        }
        public override void ExitState() => Context.setLastState(StateKey);
        public override void UpdateState()
        {
            if (foundTarget == true)
            {
                Context.shouldChase = true;
            }
        }
        public override EnemyControllerStateMachine.EEnemyState GetNextState()
        {
            if (Context.shouldKill == true)
            {
                return EnemyControllerStateMachine.EEnemyState.Death;
            }
            else if (Context.shouldChase == true)
            {
                return EnemyControllerStateMachine.EEnemyState.Chase;
            }
            else
            {
                return StateKey;
            }
        }
        public override void OnTriggerEnter(Collider other)
        {
        }
        public override void OnTriggerStay(Collider other)
        {
        }
        public override void OnTriggerExit(Collider other)
        {
        }
        public override void OnCollisionEnter(Collision collision)
        {
        }
        public override void OnCollisionStay(Collision collision)
        {
        }
        public override void OnCollisionExit(Collision collision)
        {
        }
    }
}
