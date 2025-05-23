using System;
using System.Collections;
using System.Xml.XPath;
using Unity.VisualScripting;
using UnityEngine;

namespace GluttonousDemonStates
{
    public class DeathState : EnemyControllerState
    {
        public DeathState(EnemyControllerContext context, EnemyControllerStateMachine.EEnemyState estate) : base(context, estate)
        {
            EnemyControllerContext Context = context;
        }
        private float explosionRadius = 1f;
        public override bool StateMachineActivitySetter() => Context.isStateMachineActive;

        public override void EnterState()
        {
            Debug.Log("------------Death State");
            Context.Agent.velocity = Vector3.zero;
            Context.Agent.angularSpeed = 0;
            //play death animation with CrossFade()
        }
        public override void ExitState()
        {
            //destroy
        }

        public override void UpdateState() => TriggerAcidExplosion();

        private void TriggerAcidExplosion()
        {
            for (int i = 0; i < Context.SpawnableGameObjectPool.Length; i++)
            {
                Vector3 randomPosition = GetRandomPositionAroundBloat();

                Context.SpawnableGameObjectPool[i].transform.position = randomPosition;
                Context.SpawnableGameObjectPool[i].SetActive(true);

                //create some sort of delay for animations to go off.
            }
        }

        private Vector3 GetRandomPositionAroundBloat()
        {
            float angle = UnityEngine.Random.Range(0f, 360f);
            float distance = UnityEngine.Random.Range(0f, explosionRadius);

            // ConvertPlayerToZombie polar coordinates to Cartesian coordinates
            float x = Context.transform.position.x + (distance * Mathf.Cos(angle * Mathf.Deg2Rad));
            float z = Context.transform.position.z + (distance * Mathf.Sin(angle * Mathf.Deg2Rad));

            return new Vector3(x, Context.transform.position.y, z); // Keep the y-coordinate constant
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
