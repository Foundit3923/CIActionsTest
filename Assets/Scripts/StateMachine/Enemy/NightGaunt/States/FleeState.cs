using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using static UnityEngine.GraphicsBuffer;

namespace NightGauntStates
{
    public class FleeState : EnemyControllerState
    {
        public FleeState(EnemyControllerContext context, EnemyControllerStateMachine.EEnemyState estate) : base(context, estate)
        {
            EnemyControllerContext Context = context;
        }
        private Vector3 targetPos;
        private bool targetPosSet, shouldReset;
        private float fleeDuration = 3f, fleeTimer;
        public override bool StateMachineActivitySetter() => Context.isStateMachineActive;

        public override void EnterState()
        {
            shouldReset = false;
            Debug.Log($"------------{Context.Self.name} | Flee State Initiated.");
            Context.Agent.stoppingDistance = 8f;
            Context.Agent.autoBraking = false;
            fleeTimer = fleeDuration;

            Vector3 fleeDirection = Context.GetRandomFleeDirection();
            Context.Agent.SetDestination(fleeDirection);
            //Debug.Log($"Fleeing towards: {fleeDirection}");
            Context.Agent.speed = Context.fleeSpeed;
        }
        public override void ExitState()
        {
            Context.targetTransform = null;
            //Context.ResetFleeCooldownTimer();                                         
            Context.Agent.speed = Context.chaseSpeed;  // Reset speed to normal chase speed
            Context.Agent.velocity = Vector3.zero;     // Reset velocity to 0 to ensure proper nav
            Context.setLastState(StateKey);
        }
        public override void UpdateState()
        {
            if (targetPosSet)
            {
                Context.Agent.SetDestination(targetPos);
            }

            if (Context.Agent.pathPending) return;
            if (!Context.Agent.hasPath || Context.Agent.isPathStale)
            {
                Vector3 newFleeDirection = Context.GetRandomFleeDirection();
                Context.Agent.SetDestination(newFleeDirection);
            }

            if (fleeTimer > 0)
            {
                fleeTimer -= Time.unscaledDeltaTime;
            }
            else
            {
                shouldReset = true;

                //if (Context.Agent.velocity.magnitude < 0.1f)  //debug for Gaunt getting stuck on Navmesh
                //{
                //Debug.LogWarning("NightGaunt is stuck.");
                //shouldReset = true;
                //}              
            }
        }
        public override EnemyControllerStateMachine.EEnemyState GetNextState()
        {
            if (Context.shouldKill == true)
            {
                //Context.shouldKill = false;
                return EnemyControllerStateMachine.EEnemyState.Death;
            }
            else if (shouldReset == true)
            {
                return EnemyControllerStateMachine.EEnemyState.Reset;
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
