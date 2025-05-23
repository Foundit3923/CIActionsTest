using UnityEngine;
using UnityEngine.Events;

namespace TriggerDelegator
{
    public class OnTrigger3DDelegator : MonoBehaviour
    {
        private Collider caller;

        private void Awake() => caller = GetComponent<Collider>();

        [Tooltip("Which function should be called when trigger was entered.")]
        public UnityEvent<OnTriggerDelegation> Enter;

        [Tooltip("Which function should be called while the trigger is occupied.")]
        public UnityEvent<OnTriggerDelegation> Stay;

        [Tooltip("Which function should be called when trigger was exited.")]
        public UnityEvent<OnTriggerDelegation> Exit;

        void OnTriggerEnter(Collider other) => Enter.Invoke(new OnTriggerDelegation(caller, other));
        void OnTriggerStay(Collider other) => Stay.Invoke(new OnTriggerDelegation(caller, other));
        void OnTriggerExit(Collider other) => Exit.Invoke(new OnTriggerDelegation(caller, other));
    }

    /// <summary>
    /// Stores which collider triggered this call and which collider belongs to the other object.
    /// </summary>
    public struct OnTriggerDelegation
    {

        /// <summary>
        /// Creates an OnTriggerDelegation struct.
        /// Stores which collider triggered this call and which collider belongs to the other object.
        /// </summary>
        /// <param name="caller">The trigger collider which triggered the call.</param>
        /// <param name="other">The collider which belongs to the other object.</param>
        public OnTriggerDelegation(Collider caller, Collider other)
        {
            Caller = caller;
            Other = other;
        }

        /// <summary>
        /// The trigger collider which triggered the call.
        /// </summary>
        public Collider Caller
        {
            get; private set;
        }

        /// <summary>
        /// The other collider.
        /// </summary>
        public Collider Other
        {
            get; private set;
        }
    }
}
