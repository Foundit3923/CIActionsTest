using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

namespace NuckelaveeStates
{
    public class ScanState : EnemyControllerState
    {
        public ScanState(EnemyControllerContext context, EnemyControllerStateMachine.EEnemyState estate) : base(context, estate)
        {
            EnemyControllerContext Context = context;
        }

        private bool foundPlayer, foundHidingSpots, scanFinished;
        private string turnDirection;
        private Transform enemyTransform;
        private float totalCycles;
        private int cycleCount;
        private float rotationStep;
        private float turnSpeedModifier;
        float rotateAngle;

        public override bool StateMachineActivitySetter() => Context.isStateMachineActive;
        public override void EnterState()
        {
            //Stop moving
            //Lift head up
            //Turn in circle
            Debug.Log("------------Scanning");
            Context.setIsScanning(true);
            TimeTickSystem.OnTick += foundTarget;
            enemyTransform = Context.Self.transform;
            Context.Agent.isStopped = true;
            Context.Rb.angularVelocity = Vector3.zero;
            Context.Rb.linearVelocity = Vector3.zero;
            //float wingspan = Context.RootCollider.height;
            //Context.BoxCollider.size = new Vector3(wingspan * 2f, wingspan, wingspan * 7);
            //Context.BoxCollider.center = new Vector3(Context.RootCollider.center.x, Context.RootCollider.center.y + (.25f * wingspan), Context.RootCollider.center.z + (3.5f * wingspan));
            Context.clearHidingSpots();
            foundPlayer = false;
            turnDirection = Context.CurrentBodySide;
            scanFinished = false;

            cycleCount = 0;
            rotateAngle = 359.0f;
            turnSpeedModifier = 10;
            rotationStep = rotateAngle / turnSpeedModifier * Time.deltaTime * Context.Agent.speed;
            totalCycles = rotateAngle / rotationStep;
            if (totalCycles - Mathf.Floor(totalCycles) > 0)
            {
                totalCycles = Mathf.Floor(totalCycles) + 1;
            }

            if (turnDirection == "left")
            {
                rotationStep = -rotationStep;
            }

            Debug.Log("rotation step: " + rotationStep);
            Debug.Log("Total cycles: " + totalCycles);
            Debug.Log("--------Turn direction: " + turnDirection);
        }

        public override void ExitState()
        {
            Debug.Log("Leaving Scan State");
            TimeTickSystem.OnTick -= foundTarget;
            Context.setIsScanning(false);
            Context.setLastState(StateKey);
            if (Context.Agent.isStopped)
            {
                Context.Agent.isStopped = false;
            }
        }

        public override void UpdateState()
        {
            if (cycleCount <= totalCycles)
            {
                //Debug.Log("Total turned: " + cycleCount * rotationStep);
                //Debug.Log("Cycles remaining: " + (totalCycles - cycleCount));
                enemyTransform.Rotate(0f, rotationStep, 0f, Space.Self);
                cycleCount += 1;
                //monsterTransform.rotation = Quaternion.AngleAxis(360f, monsterTransform.up);
            }

            if (cycleCount > totalCycles)
            {
                Debug.Log("Scan is finished");
                Debug.Log("CycleCount: " + cycleCount);
                Debug.Log("TotalCycles: " + totalCycles);
                if (Context.hidingSpots.Count > 0)
                {
                    Debug.Log("Moving to CheckTargetsState");
                    foundHidingSpots = true;
                }

                scanFinished = true;
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

            if (scanFinished && foundHidingSpots)
            {
                Debug.Log("Scan is finished, targets found, no players found");
                return EnemyControllerStateMachine.EEnemyState.CheckTargets;
            }

            if (scanFinished && !foundPlayer && !foundHidingSpots)
            {
                Debug.Log("Scan is finished, no targets or players found");
                return EnemyControllerStateMachine.EEnemyState.Interrupt;
            }
            else
            {
                return StateKey;
            }
        }
        public override void OnTriggerEnter(Collider other)
        {
            //string name = other.gameObject.name;
            //Vector3 pos = other.gameObject.transform.position;
            //if (name == "Player")
            //{
            //    //Context.BoxCollider.size = Vector3.zero;
            //    //If a player is found record, stop opening, and save last known position
            //    Context.Rb.angularVelocity = Vector3.zero;
            //    foundPlayer = true;
            //    GetClosestPointOnCollider(other);
            //    Context.SetCurrentSide(Context.targetTransform.position);
            //}
            //else if (name != "Floor")
            //{
            //    Debug.Log("Enemy Saw with: " + name);
            //    if (Context.hidingSpots != null)
            //    {
            //        if (!Context.hidingSpots.Contains(pos))
            //        {
            //            //Only store unique hiding spots
            //            Context.storeHidingSpot(pos);
            //        }
            //    }
            //    else
            //    {
            //        Debug.Log("Hiding Spots was not initialized properly.");
            //    }
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
                foreach (RaycastHit hitData in Context._visibleTargets)
                {
                    if (hitData.transform.name.Contains("Player"))
                    {
                        {
                            Context.Rb.angularVelocity = Vector3.zero;
                            foundPlayer = true;
                            Context.targetTransform = hitData.transform;
                            Context.lastKnownPos = hitData.transform.position;
                            Context.SetCurrentSide(Context.targetTransform.position);
                        }
                    }
                    else
                    {
                        if (!Context.hidingSpots.Contains(hitData.transform.position))
                        {
                            Context.storeHidingSpot(hitData.transform.position);
                        }
                    }
                }
            }
        }
    }
}
