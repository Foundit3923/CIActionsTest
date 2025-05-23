using System;
using Unity.VisualScripting;
using UnityEngine;

namespace NuckelaveeStates
{
    public class CheckTargetsState : EnemyControllerState
    {
        public CheckTargetsState(EnemyControllerContext context, EnemyControllerStateMachine.EEnemyState estate) : base(context, estate)
        {
            EnemyControllerContext Context = context;
        }

        private bool foundPlayer, walkpointSet, stopLooking;
        private Vector3 destPoint;

        public override bool StateMachineActivitySetter() => Context.isStateMachineActive;
        public override void EnterState()
        {
            Debug.Log("------------Cheking Targets");
            TimeTickSystem.OnTick += foundTarget;
            stopLooking = false;
        }
        public override void ExitState()
        {
            TimeTickSystem.OnTick -= foundTarget;
            Context.hidingSpots.Clear();
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
                return EnemyControllerStateMachine.EEnemyState.Interrupt;
            }

            if (foundPlayer)
            {
                return EnemyControllerStateMachine.EEnemyState.ChaseAndThrow;
            }

            if (stopLooking)
            {
                return EnemyControllerStateMachine.EEnemyState.Reset;
            }

            return StateKey;
        }
        public override void OnTriggerEnter(Collider other)
        {
            //if (other.gameObject.name == "Player")
            //{
            //    Debug.Log("Found Player");
            //    Context.Rb.angularVelocity = Vector3.zero;
            //    foundPlayer = true;
            //    GetClosestPointOnCollider(other);
            //    Context.SetCurrentSide(Context.targetTransform.position);
            //}
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
                    foundPlayer = true;
                    Context.targetTransform = target;
                    Context.SetCurrentSide(Context.targetTransform.position);
                }
            }
        }
    }
}
