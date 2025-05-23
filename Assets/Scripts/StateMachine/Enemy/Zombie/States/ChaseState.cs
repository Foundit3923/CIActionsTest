using Unity.VisualScripting;
using UnityEngine;
using System.Collections.Generic;
using static UnityEngine.GraphicsBuffer;

namespace ZombieStates
{
    public class ChaseState : EnemyControllerState
    {
        public ChaseState(EnemyControllerContext context, EnemyControllerStateMachine.EEnemyState estate) : base(context, estate)
        {
            EnemyControllerContext Context = context;
        }

        private Vector3 targetPos, adjustedPos;
        private bool targetPosSet, lostTarget, _finished;
        private int hasTriggered;

        public override bool StateMachineActivitySetter() => Context.isStateMachineActive;
        public override void EnterState()
        {
            _finished = false;
            Context.utils.DebugOut($"------------{StateKey.ToString()} State Initiated.");
            Context.utils.DebugOut(Context.PrintPrerequesiteStateSequence());
            Context.utils.DebugOut(Context.PrintMainStateSequence());
            Context.SequenceBuffer.Clear();
            TimeTickSystem.OnTick_2 += foundPlayer;
            //TimeTickSystem.OnTick_2 += facePlayer;
            Context.setIsRunning(true);
            Context.Animator.SetInteger("State", (int)StateKey);

            targetPosSet = true;
            if (Context.targetTransform != null)
            {
                targetPos = Context.targetTransform.position;
            }
            else
            {
                targetPos = Context.lastKnownPos;
            }

            adjustedPos = new Vector3(targetPos.x, Context.Agent.transform.position.y, targetPos.z);
            lostTarget = false;
        }
        public override void ExitState()
        {
            TimeTickSystem.OnTick_2 -= foundPlayer;
            //TimeTickSystem.OnTick_2 -= facePlayer;
            Context.targetTransform = null;
            Context.setLastState(StateKey);
        }
        public override void UpdateState()
        {
            if (targetPosSet)
            {
                Context.Agent.SetDestination(adjustedPos);
                //Context.Rb.transform.forward = (targetPos - Context.Rb.transform.position).normalized;
            }
        }
        public override EnemyControllerStateMachine.EEnemyState GetNextState()
        {
            if (Context.interrupted)
            {
                //When interrupted clear all future states and go to the interrupted state. can happen at any time.
                Context.PrerequisiteStateSequence.Clear();
                Context.MainStateSequence.Clear();
                return EnemyControllerStateMachine.EEnemyState.Interrupt;
            }

            if (Context.PrerequisiteStateSequence.Count > 0 && _finished)
            {
                EnemyControllerStateMachine.EEnemyState state = Context.PrerequisiteStateSequence[0];
                Context.PrerequisiteStateSequence.RemoveAt(0);
                return state;
            }

            if (Context.MainStateSequence.Count > 0 && _finished)
            {
                EnemyControllerStateMachine.EEnemyState state = Context.MainStateSequence[0];
                Context.MainStateSequence.RemoveAt(0);
                return state;
            }

            if (_finished)
            {
                //State finished but no preloaded states
                return EnemyControllerStateMachine.EEnemyState.Reset;
            }

            return StateKey;
            if (Context.interrupted)
            {
                return EnemyControllerStateMachine.EEnemyState.Interrupt;
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
                    //else
                    //{
                    //    if (!Context.isPlayerCaptured)
                    //    {
                    //        Context.storeCapturedPlayer(collision.gameObject);
                    //    }
                    //}
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
                    float distance = Vector3.Distance(Context.Agent.transform.position, target.position);
                    if (Vector3.Distance(Context.Agent.transform.position, target.position) <= 10)
                    {
                        if (!Context.isPlayerCaptured)
                        {
                            Context.PrerequisiteStateSequence.Add(EnemyControllerStateMachine.EEnemyState.Animation);
                            Context.MainStateSequence.Add(EnemyControllerStateMachine.EEnemyState.PlayerCapture);
                            Context.storeCapturedPlayer(target.gameObject);
                        }
                    }

                    targetPosSet = true;
                    Debug.Log("other: " + target.name);
                    Debug.Log("Visible Targets: " + Context._visibleTargets.ToCommaSeparatedString());
                    Context.setTargetPlayer(target);
                    Context.SetCurrentSide(target.position);
                    targetPos = target.position;
                    adjustedPos = new Vector3(targetPos.x, Context.Agent.transform.position.y, targetPos.z);
                }
            }
            else
            {
                //Stop moving, save the last known position
                targetPosSet = false;
                Context.lastKnownPos = targetPos;
                Context.Agent.SetDestination(Context.Self.transform.position);
                Context.SetCurrentSide(Context.TargetPlayer.transform.position);
                Context.PrerequisiteStateSequence.Add(EnemyControllerStateMachine.EEnemyState.Animation);
                Context.MainStateSequence.Add(EnemyControllerStateMachine.EEnemyState.Scan);
                lostTarget = true;
            }
        }

        //private void facePlayer(object sender, TimeTickSystem.OnTickEventArgs e)
        //{
        //    //Implement as slerp function
        //    Context.Rb.transform.forward = (targetPos - Context.Rb.transform.position).normalized;
        //}
    }
}
