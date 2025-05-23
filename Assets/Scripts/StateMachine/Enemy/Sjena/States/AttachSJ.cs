using System;
using UnityEngine;

namespace SjenaStates
{
    public class AttachState : EnemyControllerState
    {
        private bool _finished;
        public AttachState(EnemyControllerContext context, EnemyControllerStateMachine.EEnemyState estate) : base(context, estate)
        {
            EnemyControllerContext Context = context;
        }
        public override bool StateMachineActivitySetter() => Context.isStateMachineActive;

        public override void EnterState()
        {
            _finished = false;
            Context.utils.DebugOut($"------------{StateKey.ToString()} State Initiated.");
            Context.utils.DebugOut(Context.PrintPrerequesiteStateSequence());
            Context.utils.DebugOut(Context.PrintMainStateSequence());
            Context.SequenceBuffer.Clear();
            Context.PinToTarget(Context.TargetPlayer, TargetDirectory.TargetType.Sjena);
            EnemyControllerContext.OnTimerFinished += KillPlayer;
            Context.SetTimer(1, 0, 0);

        }

        public void KillPlayer(GameObject monster)
        {
            if (monster == Context.Self.gameObject)
            {
                EnemyControllerContext.OnTimerFinished -= KillPlayer;
                Context.PrerequisiteStateSequence.Add(EnemyControllerStateMachine.EEnemyState.Animation);
                Context.MainStateSequence.Add(EnemyControllerStateMachine.EEnemyState.Kill);
                _finished = true;
            }
        }
        public override void ExitState() => Context.setLastState(StateKey);

        public override void UpdateState()
        {
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
        }
        public override void OnCollisionStay(Collision collision)
        {
        }
        public override void OnCollisionExit(Collision collision)
        {
        }
    }
}
