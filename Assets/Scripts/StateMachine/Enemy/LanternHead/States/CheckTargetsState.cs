using System;
using Unity.VisualScripting;
using UnityEngine;

namespace LanternHeadStates
{
    public class CheckTargetsState : EnemyControllerState
    {
        public CheckTargetsState(EnemyControllerContext context, EnemyControllerStateMachine.EEnemyState estate) : base(context, estate)
        {
            EnemyControllerContext Context = context;
        }

        private bool foundPlayer, walkpointSet, stopLooking, _finished;
        private Vector3 destPoint;

        public override bool StateMachineActivitySetter() => Context.isStateMachineActive;
        public override void EnterState()
        {
            _finished = false;
            Context.utils.DebugOut($"------------{StateKey.ToString()} State Initiated.");
            Context.utils.DebugOut(Context.PrintPrerequesiteStateSequence());
            Context.utils.DebugOut(Context.PrintMainStateSequence());
            Context.SequenceBuffer.Clear();
            TimeTickSystem.OnTick += foundTarget;
            stopLooking = false;
            _finished = true;
        }
        public override void ExitState()
        {
            TimeTickSystem.OnTick -= foundTarget;
            Context.hidingSpots.Clear();
            Context.setLastState(StateKey);
        }
        public override void UpdateState()
        {
            if (!walkpointSet)
            {
                setNextTarget();
            }

            if (walkpointSet)
            {
                Debug.Log("Move to target");
                Context.Agent.SetDestination(destPoint);
            }

            if (Vector3.Distance(Context.Self.transform.position, destPoint) < 3) walkpointSet = false;
        }

        private void setNextTarget()
        {
            if (Context.hidingSpots.Count > 0)
            {
                Debug.Log("Targets left to check: " + Context.hidingSpots.Count);
                destPoint = Context.hidingSpots[0];
                Context.hidingSpots.RemoveAt(0);
                walkpointSet = true;
            }
            else
            {
                Debug.Log("No targets left to check, stop looking");
                stopLooking = true;
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
                }
            }
        }
        public override void OnCollisionStay(Collision collision)
        {
        }
        public override void OnCollisionExit(Collision collision)
        {
        }

        private void foundTarget(object sender, TimeTickSystem.OnTickEventArgs e)
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
                    Context.Rb.angularVelocity = Vector3.zero;
                    Context.PrerequisiteStateSequence.Add(EnemyControllerStateMachine.EEnemyState.Animation);
                    Context.MainStateSequence.Add(EnemyControllerStateMachine.EEnemyState.Chase);
                    foundPlayer = true;
                    Context.targetTransform = target;
                    Context.SetCurrentSide(Context.targetTransform.position);
                }
            }
        }
    }
}
