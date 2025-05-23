using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;

namespace ChenqueStates
{
    public class ChenqueAnimationState : EnemyControllerState
    {
        public ChenqueAnimationState(EnemyControllerContext context, EnemyControllerStateMachine.EEnemyState estate) : base(context, estate)
        {
            EnemyControllerContext Context = context;
        }
        private bool _finished, syncDeath;
        private int _layerIndex;
        private EnemyControllerStateMachine.EEnemyState killState = EnemyControllerStateMachine.EEnemyState.Kill;
        private EnemyControllerStateMachine.EEnemyState deathState = EnemyControllerStateMachine.EEnemyState.Death;
        private EnemyControllerStateMachine.EEnemyState idleState = EnemyControllerStateMachine.EEnemyState.SearchForHost;
        private EnemyControllerStateMachine.EEnemyState attackState = EnemyControllerStateMachine.EEnemyState.Attack;
        private EnemyControllerStateMachine.EEnemyState chaseState = EnemyControllerStateMachine.EEnemyState.Chase;
        //private AnimatorState currentAnimationState;
        //private AnimatorStateMachine animatorStateMachine;
        //ChildAnimatorState[] states;
        private float clipLenDelta;
        public override bool StateMachineActivitySetter() => Context.isStateMachineActive;

        public override void EnterState()
        {
            _finished = false;
            syncDeath = false;
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
            if (Context.Animator != null)
            {
                if (Context.MainStateSequence.FirstOrDefault() == killState)
                {
                    PositionConstraint constraint = Context.Self.GetComponent<PositionConstraint>();
                    if (constraint is not null)
                    {
                        constraint.weight = 0f;
                        constraint.constraintActive = false;
                    }

                    RotationConstraint rotationConstraint = Context.Self.GetComponent<RotationConstraint>();
                    if (rotationConstraint is not null)
                    {
                        rotationConstraint.weight = 0f;
                        rotationConstraint.constraintActive = false;
                    }

                    Context.Self.gameObject.transform.position = Context.TargetPlayer.transform.position;
                    Context.Self.gameObject.transform.rotation = Context.TargetPlayer.transform.rotation;

                    PlayerInformationManager.OnPlayerDeath += StartKillAnimation;

                    if (Context.TargetPlayer.GetComponent<PlayerInformationManager>().playerProperties.isDead is false) { Context.KillTargetPlayer(); }

                    syncDeath = true;
                }
                else if (Context.MainStateSequence.FirstOrDefault() == deathState)
                {
                    Context.Animator.SetBool("Death", true);
                }
                else if (Context.MainStateSequence.FirstOrDefault() == idleState)
                {
                    Context.Animator.SetBool("Death", false);
                    Context.Animator.SetBool("Kill", false);
                    Context.Animator.SetBool("Idle", true);
                }
                else if (Context.MainStateSequence.FirstOrDefault() == attackState)
                {
                    Context.Animator.SetBool("Death", false);
                    Context.Animator.SetBool("Kill", false);
                    Context.Animator.SetBool("Idle", false);

                }

                Context.Animator.SetInteger("CurrentState", (int)Context.MainStateSequence.FirstOrDefault());
            }
            else
            {
                _finished = true;
            }
        }

        public void StartKillAnimation(PlayerInformationManager player, GameObject killer)
        {
            if (player.gameObject == Context.TargetPlayer)
            {
                Context.Animator.SetBool("Death", false);
                Context.Animator.SetBool("Kill", true);
                Context.Animator.SetBool("Idle", false);
            }
        }

        public override void ExitState() => Context.setLastState(StateKey);

        public override void UpdateState()
        {
            //if (AnimatorInCorrectState())
            //{
            //    if (finishAnimationBeforeContinuing)
            //    {
            //        if (!AnimatorIsPlaying())
            //        {
            //            _finished = true;
            //        }
            //    }
            //    else
            //    {
            //        _finished = true;
            //    }
            //}
            if (syncDeath)
            {
                if (AnimatorInCorrectState())
                {
                    if (!AnimatorIsPlaying()) { _finished = true; }
                }
            }
            else if (!AnimatorIsPlaying() || AnimatorInCorrectState())
            {
                _finished = true;
            }
        }

        public bool AnimatorIsPlaying()
        {
            //Cut looping clips short
            return Context.Animator.GetCurrentAnimatorStateInfo(0).length >
                   Context.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        }

        public bool AnimatorInCorrectState()
        {
            if (Context.Animator.GetCurrentAnimatorStateInfo(0).IsName(Context.enemyProperties.Name + "_" + Context.MainStateSequence.FirstOrDefault().ToString()))
            {

                return true;
            }

            return false;// Context.Animator.GetCurrentAnimatorStateInfo(0).IsName(Context.enemyProperties.Name + "." + Context.MainStateSequence.FirstOrDefault().ToString());
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
