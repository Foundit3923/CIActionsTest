using Unity.VisualScripting;
using UnityEngine;

namespace NuckelaveeStates
{
    public class PatrolState : EnemyControllerState
    {
        public PatrolState(EnemyControllerContext context, EnemyControllerStateMachine.EEnemyState estate) : base(context, estate)
        {
            EnemyControllerContext Context = context;
        }

        private Vector3 destPoint;
        private bool walkpointSet, chase;

        public override bool StateMachineActivitySetter() => Context.isStateMachineActive;
        public override void EnterState()
        {
            Debug.Log("Patroling");
            chase = false;
            TimeTickSystem.OnTick_5 += foundPlayer;

        }
        public override void ExitState()
        {
            Debug.Log("Exiting Patrol State");
            TimeTickSystem.OnTick_5 -= foundPlayer;
            if (chase)
            {
                Context.Agent.SetDestination(destPoint);
            }

            walkpointSet = false;
            destPoint = new Vector3(0, 0, 0);
            Context.setLastState(StateKey);
        }
        public override void UpdateState()
        {
            //Debug.Log("Update EnemyPatrolState");
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
                return EnemyControllerStateMachine.EEnemyState.Interrupt;
            }

            if (chase)
            {
                chase = false;
                return EnemyControllerStateMachine.EEnemyState.ChaseAndThrow;
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

            // Draw a different colored ray for every normal in the collision
            foreach (var item in collision.contacts)
            {
                Debug.DrawRay(item.point, item.normal * 100, Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f), 10f);
            }

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
            float z = Random.Range(-Context.range, Context.range);
            float x = Random.Range(-Context.range, Context.range);

            destPoint = new Vector3(Context.Self.transform.position.x + x, Context.Self.transform.position.y, Context.Self.transform.position.z + z);

            if (Physics.Raycast(destPoint, Vector3.down, Context.groundLayer))
            {
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
                    chase = true;
                    walkpointSet = true;
                    destPoint = target.gameObject.transform.position;

                }
            }
        }
    }
}
