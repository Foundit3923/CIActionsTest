using Unity.VisualScripting;
using UnityEngine;
using System.Collections.Generic;
using static UnityEngine.GraphicsBuffer;

namespace NightGauntStates
{
    public class ChaseState : EnemyControllerState
    {
        public ChaseState(EnemyControllerContext context, EnemyControllerStateMachine.EEnemyState estate) : base(context, estate)
        {
            EnemyControllerContext Context = context;
        }
        bool shouldAttack;

        public override bool StateMachineActivitySetter() => Context.isStateMachineActive;

        public override void EnterState()
        {
            shouldAttack = false;
            Debug.Log($"------------{Context.Self.name} | Chase State Initiated.");
        }
        public override void ExitState() => Context.setLastState(StateKey);
        public override void UpdateState()
        {
            float distanceToPlayer = Vector3.Distance(Context.Self.transform.position, Context.TargetPlayer.transform.position);
            if (!Context.Agent.isActiveAndEnabled) { Context.Agent.enabled = true; }

            Context.Agent.SetDestination(Context.TargetPlayer.transform.position);
            Debug.Log($"Distance to {Context.TargetPlayer.name}: " + distanceToPlayer);
            if (distanceToPlayer <= 2.75)
            {
                shouldAttack = true;
            }
        }
        public override EnemyControllerStateMachine.EEnemyState GetNextState()
        {
            if (Context.shouldKill == true)
            {
                return EnemyControllerStateMachine.EEnemyState.Death;
            }

            if (shouldAttack == true)
            {
                return EnemyControllerStateMachine.EEnemyState.Attack;
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
