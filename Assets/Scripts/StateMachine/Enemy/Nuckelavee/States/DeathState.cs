using Unity.VisualScripting;
using UnityEngine;

namespace NuckelaveeStates
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

            Debug.Log("------------Death State");
            Context.setStateMachineActive(false);
            Context.Agent.isStopped = true;
            Context.Self.transform.rotation = Quaternion.identity;
            Vector3 pos = Context.Self.transform.position;
            Context.Self.transform.position = new Vector3(pos.x, pos.y - 0.5f, pos.z);
            Context.Rb.angularVelocity = Vector3.zero;
            Context.Rb.linearVelocity = Vector3.zero;
            Context.Agent.enabled = false;
            Context.Self.GetComponent<EnemyControllerStateMachine>().enabled = false;
        }
        public override void ExitState()
        {
        }
        public override void UpdateState()
        {
            //if (cycleCount <= totalCycles)
            //{
            //    //Debug.Log("Total turned: " + cycleCount * rotationStep);
            //    //Debug.Log("Cycles remaining: " + (totalCycles - cycleCount));
            //    monsterTransform.Rotate(0f, rotationStep, 0f, Space.Self);
            //    cycleCount += 1;
            //}
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
            ContactPoint contact;
            Debug.Log("Contact points available: " + collision.contactCount);
            if (collision.contactCount > 0)
            {
                contact = collision.GetContact(0);
                Debug.Log("------------Enemy Collided with: " + contact.otherCollider.name);
                if (contact.otherCollider.name == "Player")
                {

                    // Print how many points are colliding with this transform
                    Debug.Log("Points colliding: " + collision.contacts.Length);

                    // Print the normal of the first point in the collision.
                    Debug.Log("Normal of the first point: " + collision.contacts[0].normal);

                    // Draw a different colored ray for every normal in the collision
                    foreach (var item in collision.contacts)
                    {
                        if (item.point.y > 0)
                        {
                            Debug.Log("Location of point: " + item.point);
                            Debug.Log("Impulse of impact: " + item.impulse);
                            Debug.Log("Normal of impact: " + item.normal);
                            Debug.Log("Rb rotation: " + Context.Self.transform.rotation);
                            Vector3 test = item.normal;
                            Vector3 loc = Context.Self.transform.position;
                            Vector3 impactLoc = new(loc.x, loc.y + 0.5f, loc.z);
                            Context.Rb.useGravity = true;
                            collision.collider.attachedRigidbody.AddForceAtPosition(10 * new Vector3(test.x, 0f, test.z), impactLoc);
                            Debug.Log("Rb rotation after force applied: " + Context.Self.transform.rotation);
                            Debug.DrawRay(item.point, item.normal * 100, Color.red, 10f);
                        }
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
