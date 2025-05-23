using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

namespace GluttonousDemonStates
{
    public class EnvAtkState : EnemyControllerState
    {
        public EnvAtkState(EnemyControllerContext context, EnemyControllerStateMachine.EEnemyState estate) : base(context, estate)
        {
            EnemyControllerContext Context = context;
        }
        private bool shouldPatrol;
        private int lastIndex = 0;
        public override bool StateMachineActivitySetter() => Context.isStateMachineActive;

        public override void EnterState()
        {
            Debug.Log("------------EnvAtk State");
            Context.Agent.velocity = Vector3.zero;
            Context.Agent.angularSpeed = 0;
        }
        public override void ExitState() => shouldPatrol = false;

        public override void UpdateState()
        {
            SpawnPool();
            if (Context.CheckProximityToDeathItem(Context.itemCheckRadius) == true)
            {
                Context.isDead = true;
            }

            shouldPatrol = true;
        }
        private void SpawnPool()
        {
            GameObject poolToSpawn = null;
            if (lastIndex + 1 < Context.SpawnableGameObjectPool.Length) { lastIndex++; }
            else { lastIndex = 0; }

            poolToSpawn = Context.SpawnableGameObjectPool[lastIndex];
            if (poolToSpawn != null)
            {
                //Place pool 2 units in front of monster
                poolToSpawn.SetActive(false);
                Vector3 spawnPosition = Context.Self.transform.position + (Context.Self.transform.forward * 2f);
                poolToSpawn.transform.position = spawnPosition;

                //Activate the pool
                poolToSpawn.SetActive(true);
            }
            else
            {
                Debug.LogWarning("Acid Pool Pool initialized incorrectly");
            }
        }
        public override EnemyControllerStateMachine.EEnemyState GetNextState()
        {
            if (shouldPatrol)
            {
                return EnemyControllerStateMachine.EEnemyState.Patrol;
            }
            else if (Context.isDead)
            {
                return EnemyControllerStateMachine.EEnemyState.Death;
            }
            else
            {
                return StateKey;
            }
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
