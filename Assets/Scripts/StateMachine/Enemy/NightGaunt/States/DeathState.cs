using Unity.VisualScripting;
using UnityEngine;
using System.Collections.Generic;
using static UnityEngine.GraphicsBuffer;

namespace NightGauntStates
{
    public class DeathState : EnemyControllerState
    {
        public DeathState(EnemyControllerContext context, EnemyControllerStateMachine.EEnemyState estate) : base(context, estate)
        {
            EnemyControllerContext Context = context;
        }
        public override bool StateMachineActivitySetter() => Context.isStateMachineActive;

        public override void EnterState()
        {
            Debug.Log($"-----------{Context.Self.name} | Death State Initiated.");
            //Animation
            UnityEngine.Object.Destroy(Context.Self.gameObject);
        }

        public override void ExitState()
        {
            //Exit is death
        }
        public override void UpdateState()
        {
            //NA
        }
        public override EnemyControllerStateMachine.EEnemyState GetNextState() => StateKey;
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
