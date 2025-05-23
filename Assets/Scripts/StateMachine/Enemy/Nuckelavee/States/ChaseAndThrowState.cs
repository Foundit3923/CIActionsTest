using Unity.VisualScripting;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using static UnityEngine.GraphicsBuffer;
//using Mono.CSharp;

namespace NuckelaveeStates
{
    public class ChaseAndThrowState : EnemyControllerState
    {
        public ChaseAndThrowState(EnemyControllerContext context, EnemyControllerStateMachine.EEnemyState estate) : base(context, estate)
        {
            EnemyControllerContext Context = context;
        }

        EnemyThrow Throw;

        private Vector3 targetPos;
        private bool targetPosSet, lostTarget, stopLooking;
        private int cooldown = 0;
        private int chargeTime = 0;

        public override bool StateMachineActivitySetter() => Context.isStateMachineActive;

        public override void EnterState()
        {
            Debug.Log("------------Chase State Initiated.");
            Throw = Context.Self.GetComponent<EnemyThrow>();
            TimeTickSystem.OnTick += ThrowSpearCycle;
            TimeTickSystem.OnTick += foundPlayer;
            Context.setIsRunning(true);
            targetPosSet = true;
            targetPos = Context.targetTransform.position;
            lostTarget = false;
        }
        public override void ExitState()
        {
            TimeTickSystem.OnTick -= ThrowSpearCycle;
            TimeTickSystem.OnTick -= foundPlayer;
            Context.targetTransform = null;
            Context.setLastState(StateKey);
        }
        public override void UpdateState()
        {
            if (targetPosSet)
            {
                Context.Agent.SetDestination(targetPos);
            }
        }
        public override EnemyControllerStateMachine.EEnemyState GetNextState()
        {
            if (Context.interrupted)
            {
                return EnemyControllerStateMachine.EEnemyState.Interrupt;
            }

            if (stopLooking)
            {
                return EnemyControllerStateMachine.EEnemyState.Reset;
            }
            else if (Context.isPlayerCaptured)
            {
                return EnemyControllerStateMachine.EEnemyState.PlayerCapture;
            }
            else if (lostTarget)
            {
                return EnemyControllerStateMachine.EEnemyState.Scan;
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
            ContactPoint contact;
            Debug.Log("Contact points available: " + collision.contactCount);
            if (collision.contactCount > 0)
            {
                contact = collision.GetContact(0);
                Debug.Log("------------Enemy Collided with: " + contact.otherCollider.name);
                if (contact.otherCollider.name == "Player")
                {
                    Vector3 collisionDirection = collision.transform.position - Context.Self.transform.position;
                    collisionDirection.Normalize();
                    float dotProduct = Vector3.Dot(Context.Self.transform.forward, collisionDirection);

                    Debug.Log("Dot Product: " + dotProduct);

                    if (dotProduct < -0.9f)
                    {
                        Context.setInterrupted(true);
                        Context.setInterruptTag(collision.gameObject.name);
                        Context.setDead(true);
                    }
                    else
                    {
                        if (!Context.isPlayerCaptured)
                        {
                            Context.storeCapturedPlayer(collision.gameObject);
                        }
                    }
                }
            }
        }
        public override void OnCollisionStay(Collision collision)
        {
        }
        public override void OnCollisionExit(Collision collision)
        {
        }

        private void foundPlayer(object sender, TimeTickSystem.OnTickEventArgs e)
        {
            if (Context._visibleTargets.Count > 0)
            {
                //Target is in range
                Debug.Log("Looking for target");
                int rand = UnityEngine.Random.Range(0, Context._visibleTargets.Count);
                RaycastHit val = Context._visibleTargets[rand];
                Transform target = val.transform;
                if (target != null)
                {
                    targetPosSet = true;
                    Debug.Log("other: " + target.name);
                    Debug.Log("Visible Targets: " + Context._visibleTargets.ToCommaSeparatedString());
                    Context.setTargetPlayer(target);
                    Context.SetCurrentSide(target.position);
                    targetPos = target.position;
                    Throw.SetTarget(target);
                }
            }
            else
            {
                //Stop moving, save the last known position
                targetPosSet = false;
                Context.lastKnownPos = targetPos;
                Context.Agent.SetDestination(Context.Self.transform.position);
                Context.SetCurrentSide(Context.TargetPlayer.transform.position);
                lostTarget = true;
            }
        }

        private void ThrowSpearCycle(object sender, TimeTickSystem.OnTickEventArgs e)
        {
            cooldown--;
            if (!Throw.isSpawned)
            {
                Throw.Spawn();
            }

            if (Context._visibleTargets.Count > 0)
            {
                chargeTime--;
                Throw.FocusOnTarget();
                if (!Throw.readyToThrow && !Throw.isThrown)
                {
                    Throw.Charge();
                    chargeTime = 4;
                }

                if (cooldown <= 0 && chargeTime <= 0 && Throw.readyToThrow && Throw.isSpawned && !Throw.isThrown)
                {
                    Throw.ThrowObject();
                    cooldown = 1;
                }
            }
        }
    }
}