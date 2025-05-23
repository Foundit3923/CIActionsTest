using System.Collections.Generic;
using UnityEngine;
using System;
using BaseState;

namespace StateMachine
{
    public abstract class StateMachine<EState> : MonoBehaviour where EState : Enum
    {
        protected Dictionary<EState, BaseState<EState>> States = new();

        protected BaseState<EState> CurrentState;

        protected bool isTransitioningState = false;

        protected bool isStateMachineActive = true;

        protected bool _finishedSetup = false;

        protected bool _forceNextState = false;

        private void Awake()
        {
            Debug.Log("Awake Cehck");
            States = new Dictionary<EState, BaseState<EState>>();
            OnAwake();
        }

        void Start() => OnStart();

        void Update()
        {
            if (isStateMachineActive)
            {
                Draw();
                if (CurrentState.StateMachineActivitySetter())
                {
                    EState nextStateKey = CurrentState.GetNextState();

                    if (nextStateKey.Equals(CurrentState.StateKey))
                    {
                        CurrentState.UpdateState();
                    }
                    else if (!isTransitioningState)
                    {
                        TransitionToState(nextStateKey);
                    }

                    OnUpdate();
                }
            }
        }

        public void TransitionToState(EState stateKey)
        {
            isTransitioningState = true;
            CurrentState.ExitState();
            CurrentState = States[stateKey];
            CurrentState.EnterState();
            isTransitioningState = false;
        }
        public bool isSetupFinished() => _finishedSetup;
        public abstract void Draw();
        public abstract void OnAwake();
        public abstract void OnStart();
        public abstract void OnUpdate();

        void OnTriggerEnter(Collider other)
        {
            CurrentState?.OnTriggerEnter(other);
        }

        void OnTriggerStay(Collider other)
        {
            CurrentState?.OnTriggerStay(other);
        }

        void OnTriggerExit(Collider other)
        {
            CurrentState?.OnTriggerExit(other);
        }

        void OnCollisionEnter(Collision collision)
        {
            CurrentState?.OnCollisionEnter(collision);
        }

        void OnCollisionStay(Collision collision)
        {
            CurrentState?.OnCollisionStay(collision);
        }

        void OnCollisionExit(Collision collision)
        {
            CurrentState?.OnCollisionEnter(collision);
        }
    }
}
