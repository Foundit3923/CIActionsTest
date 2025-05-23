using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;

namespace LanternHeadStates
{
    public class PatrolState : EnemyControllerState
    {
        public PatrolState(EnemyControllerContext context, EnemyControllerStateMachine.EEnemyState estate) : base(context, estate)
        {
            EnemyControllerContext Context = context;
        }

        private Vector3 destPoint;
        private bool walkpointSet, chase, _finished;

        public override bool StateMachineActivitySetter() => Context.isStateMachineActive;
        public override void EnterState()
        {
            _finished = false;
            Context.utils.DebugOut($"------------{StateKey.ToString()} State Initiated.");
            Context.utils.DebugOut(Context.PrintPrerequesiteStateSequence());
            Context.utils.DebugOut(Context.PrintMainStateSequence());
            Context.SequenceBuffer.Clear();
            chase = false;
            TimeTickSystem.OnTick_5 += foundPlayer;

        }
        public override void ExitState()
        {
            TimeTickSystem.OnTick_5 -= foundPlayer;
            Context.lastKnownPos = destPoint;

            walkpointSet = false;
            destPoint = new Vector3(0, 0, 0);
            Context.setLastState(StateKey);
        }
        public override void UpdateState()
        {
            //Debug.Log("Update EnemyPatrolState");
            if (chase) { return; }

            if (!walkpointSet)
            {
                SearchForDest();
            }

            if (walkpointSet)
            {
                Context.Agent.SetDestination(destPoint);
            }

            if (Vector3.Distance(Context.Self.transform.position, destPoint) < 10) walkpointSet = false;
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

            // Print how many points are colliding with this transform
            Debug.Log("Points colliding: " + collision.contacts.Length);

            // Print the normal of the first point in the collision.
            //Debug.Log("Normal of the first point: " + collision.contacts[0].normal);
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

        private void SearchForDest()
        {
            (bool, Vector3) potentialDest = Context.GetRandomWalkablePosInRange(10f, Context.range);
            if (potentialDest.Item1)
            {
                destPoint = potentialDest.Item2;
                walkpointSet = true;
            }
        }

        private void foundPlayer(object sender, TimeTickSystem.OnTickEventArgs e)
        {
            if (Context._visibleTargets.Count > 0)
            {
                Debug.Log("Looking for target");
                int rand = UnityEngine.Random.Range(0, Context._visibleTargets.Count);
                RaycastHit val = Context._visibleTargets[rand];
                Transform target = val.transform;
                if (target != null)
                {
                    Debug.Log("other: " + target.name);
                    Debug.Log("Visible Targets: " + Context._visibleTargets.ToCommaSeparatedString());
                    Context.targetTransform = target;
                    Context.SetCurrentSide(Context.targetTransform.position);
                    Context.PrerequisiteStateSequence.Add(EnemyControllerStateMachine.EEnemyState.Animation);
                    Context.MainStateSequence.Add(EnemyControllerStateMachine.EEnemyState.Chase);
                    chase = true;
                    _finished = true;
                    walkpointSet = true;
                    destPoint = target.gameObject.transform.position;
                }
            }
        }
    }
}
