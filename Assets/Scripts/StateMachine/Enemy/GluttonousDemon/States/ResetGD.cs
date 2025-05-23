using System;
using UnityEngine;

namespace GluttonousDemonStates
{
    public class ResetState : EnemyControllerState
    {
        public ResetState(EnemyControllerContext context, EnemyControllerStateMachine.EEnemyState estate) : base(context, estate)
        {
            
        }
        private bool shouldPatrol = false;
        private bool poolInitialization = false;
        private bool poolz = false;
        private string GluttonousDemon = "GluttonousDemon";
        private bool spawnDeathItem = true;
        private FizzyNetworkManager networkManager;

        private bool test = false;
        public override bool StateMachineActivitySetter() => Context.isStateMachineActive;

        public override void EnterState()
        {
            Debug.Log("------------Reset State");
            Context.Rb.linearVelocity = Vector3.zero;
            shouldPatrol = true;
            if (!poolz)
            {
                poolz = true;
                Context.SetMaxAttackCount(20);
                //Context.PersonalizedInit(GluttonousDemon);
            }

            Context.FindNetworkManager(ref networkManager);


        }
        public override void ExitState() => shouldPatrol = false;

        public override void UpdateState()
        {
            if (Context.CheckProximityToDeathItem(Context.itemCheckRadius) == true)
            {
                Context.isDead = true;
            }
        }

        public override EnemyControllerStateMachine.EEnemyState GetNextState()
        {
            if (shouldPatrol)
            {
                return EnemyControllerStateMachine.EEnemyState.Patrol;
            }
            else if (test)
            {
                return EnemyControllerStateMachine.EEnemyState.Death;
            }
            else
            {
                return StateKey;
            }
        }

        private void SpawnDeathItem(GameObject DeathItem, Vector3 position)
        {
            spawnDeathItem = false;  //set flag to only run this once.

            //NetworkingServer.Spawn(DeathItem);
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
