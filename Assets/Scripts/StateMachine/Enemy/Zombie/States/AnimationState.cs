using System;
using System.Linq;
using UnityEngine;

namespace ZombieStates
{
    public class AnimationState : EnemyControllerState
    {
        public AnimationState(EnemyControllerContext context, EnemyControllerStateMachine.EEnemyState estate) : base(context, estate)
        {
            EnemyControllerContext Context = context;
        }
        private bool _finished, shouldWait;
        private int _layerIndex;
        //private AnimatorState currentAnimationState;
        //private AnimatorStateMachine animatorStateMachine;
        //ChildAnimatorState[] states;
        private float clipLenDelta;
        public override bool StateMachineActivitySetter() => Context.isStateMachineActive;

        public override void EnterState()
        {
            _finished = false;
            Context.utils.DebugOut($"------------{StateKey.ToString()} State Initiated.");
            Context.utils.DebugOut(Context.PrintPrerequesiteStateSequence());
            Context.utils.DebugOut(Context.PrintMainStateSequence());
            Context.SequenceBuffer.Clear();
            //_layerIndex = Context.Animator.GetLayerIndex("Base");
            //states = Context.AnimatorController.layers[_layerIndex].stateMachine.states;
            //clipLenDelta = 0f;
            //finishAnimationBeforeContinuing = false;
            //TODO: change the State parameter in the Animator to the appropriate Value
            //If the Value doesn't correlate to anything in the animation state machine the current state should be looping which will set _finished to true immediately
            //
            Context.Animator.SetInteger("State", (int)Context.MainStateSequence.FirstOrDefault());
        }
        public override void ExitState() => Context.setLastState(StateKey);

        public override void UpdateState()
        {
            if (Context.IsSequenceComplete)
            {
                _finished = true;
            }
            //if (clipLenDelta <= 0f)
            //{
            //    AnimatorStateInfo info = Context.Animator.GetCurrentAnimatorStateInfo(_layerIndex);
            //    finishAnimationBeforeContinuing = false;
            //    foreach (ChildAnimatorState state in states)
            //    {
            //        if (state.state.nameHash == info.shortNameHash)
            //        {
            //            currentAnimationState = state.state;
            //            String name = currentAnimationState.name;
            //            float currentProgress = Context.Animator.GetCurrentAnimatorStateInfo(_layerIndex).normalizedTime;
            //            float totalDuration = Context.Animator.GetCurrentAnimatorClipInfo(_layerIndex)[0].clip.length;
            //            clipLenDelta = totalDuration - currentProgress;
            //            AnimatorStateTransition[] transitions = state.state.transitions;
            //            foreach (AnimatorStateTransition transition in transitions)
            //            {
            //                if (finishAnimationBeforeContinuing)
            //                {
            //                    break;
            //                }
            //                foreach (AnimatorCondition condition in transition.conditions)
            //                {
            //                    if (finishAnimationBeforeContinuing)
            //                    {
            //                        break;
            //                    }
            //                    if (condition.parameter == "State")
            //                    {
            //                        int Value = (int)Context.MainStateSequence.FirstOrDefault();
            //                        switch (condition.mode)
            //                        {
            //                            case AnimatorConditionMode.Equals:
            //                                if (condition.threshold == Value) { finishAnimationBeforeContinuing = true; }
            //                                break;
            //                            case AnimatorConditionMode.Less:
            //                                if (Value < condition.threshold) { finishAnimationBeforeContinuing = true; }
            //                                break;
            //                            case AnimatorConditionMode.Greater:
            //                                if (Value > condition.threshold) { finishAnimationBeforeContinuing = true; }
            //                                break;
            //                            case AnimatorConditionMode.NotEqual:
            //                                if (Value != condition.threshold) { finishAnimationBeforeContinuing = true; }
            //                                break;
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //    }
            //    if (!finishAnimationBeforeContinuing)
            //    {
            //        _finished = true;
            //    }
            //}
            //clipLenDelta -= Time.deltaTime;
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
