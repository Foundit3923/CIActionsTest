using UnityEngine;

namespace ChenqueStates
{
    public class ChenChaseState : EnemyControllerState
    {
        public ChenChaseState(EnemyControllerContext context, EnemyControllerStateMachine.EEnemyState estate) : base(context, estate)
        {
            EnemyControllerContext Context = context;
        }
        private bool _finished;

        public override bool StateMachineActivitySetter() => Context.isStateMachineActive;
        public override void EnterState()
        {
            _finished = false;
            Context.utils.DebugOut($"------------{StateKey.ToString()} State Initiated.");
            Context.utils.DebugOut(Context.PrintPrerequesiteStateSequence());
            Context.utils.DebugOut(Context.PrintMainStateSequence());
            Context.SequenceBuffer.Clear();
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
                Context.PrerequisiteStateSequence.Add(EnemyControllerStateMachine.EEnemyState.Animation);
                Context.MainStateSequence.Add(EnemyControllerStateMachine.EEnemyState.Attack);
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
    }
}
