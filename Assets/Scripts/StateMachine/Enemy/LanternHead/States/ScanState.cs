using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

namespace LanternHeadStates
{
    public class ScanState : EnemyControllerState
    {
        public ScanState(EnemyControllerContext context, EnemyControllerStateMachine.EEnemyState estate) : base(context, estate)
        {
            EnemyControllerContext Context = context;
        }

        private bool foundHidingSpots, _finished;
        private string turnDirection;
        private Transform monsterTransform;
        private float totalCycles;
        private int cycleCount;
        private float rotationStep;
        private float turnSpeedModifier;
        float rotateAngle;
        public override bool StateMachineActivitySetter() => Context.isStateMachineActive;
        public override void EnterState()
        {
            _finished = false;
            Context.utils.DebugOut($"------------{StateKey.ToString()} State Initiated.");
            Context.utils.DebugOut(Context.PrintPrerequesiteStateSequence());
            Context.utils.DebugOut(Context.PrintMainStateSequence());
            Context.SequenceBuffer.Clear();
            Context.setIsScanning(true);
            TimeTickSystem.OnTick += foundTarget;
            monsterTransform = Context.Self.transform;
            Context.Agent.isStopped = true;
            Context.Rb.angularVelocity = Vector3.zero;
            Context.Rb.linearVelocity = Vector3.zero;
            Context.clearHidingSpots();
            turnDirection = Context.CurrentBodySide;

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
                monsterTransform.Rotate(0f, rotationStep, 0f, Space.Self);
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

                if (foundHidingSpots)
                {
                    Context.MainStateSequence.Add(EnemyControllerStateMachine.EEnemyState.CheckTargets);
                }
                else
                {
                    Context.MainStateSequence.Add(EnemyControllerStateMachine.EEnemyState.Interrupt);
                }

                _finished = true;
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
                foreach (RaycastHit hitData in Context._visibleTargets)
                {
                    if (hitData.transform != null)
                    {
                        if (hitData.transform.gameObject.layer == LayerMask.NameToLayer("Player"))
                        {
                            {
                                Context.Rb.angularVelocity = Vector3.zero;

                                Context.targetTransform = hitData.transform;
                                Context.lastKnownPos = hitData.transform.position;
                                Context.SetCurrentSide(Context.targetTransform.position);
                                Context.PrerequisiteStateSequence.Add(EnemyControllerStateMachine.EEnemyState.Animation);
                                Context.MainStateSequence.Add(EnemyControllerStateMachine.EEnemyState.Chase);
                                _finished = true;
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
}
