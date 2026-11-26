using UnityEngine;

namespace OrbitToss
{
    /// <summary>
    /// The white Jack (target ball). In this prototype it is FIXED on the target cross so development can focus on
    /// throwing the planet balls (prototype simplification - in a real match the Jack is thrown first and must cross
    /// the V line). It has no grab components and a kinematic Rigidbody: balls bounce off it, but it never moves.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public class JackController : MonoBehaviour
    {
        [Tooltip("Keep the Jack locked in place during practice.")]
        [SerializeField] private bool lockInPlace = true;
        [Tooltip("Optional soft glow/halo object (decoration only).")]
        [SerializeField] private Transform halo;

        private Rigidbody body;
        private Vector3 fixedPosition;

        public Vector3 Position => transform.position;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            fixedPosition = transform.position;
            ApplyLock();
        }

        private void ApplyLock()
        {
            body.isKinematic = lockInPlace;
            body.useGravity = !lockInPlace;
            body.interpolation = RigidbodyInterpolation.None;
        }

        /// <summary>Places the Jack on the centre of the target cross, resting on the court.</summary>
        public void PlaceOnCross()
        {
            float radius = GetComponent<SphereCollider>().radius * transform.lossyScale.x;
            fixedPosition = CourtDimensions.CrossCentre + Vector3.up * radius;
            transform.position = fixedPosition;
        }

        public void ResetPosition()
        {
            transform.position = fixedPosition;
            if (body != null) body.position = fixedPosition;
        }

        private void LateUpdate()
        {
            // Defensive: nothing in the prototype should move the Jack, but snap it back if anything does.
            if (lockInPlace && (transform.position - fixedPosition).sqrMagnitude > 1e-8f) ResetPosition();
            if (halo != null && Camera.main != null)
            {
                halo.rotation = Quaternion.LookRotation(halo.position - Camera.main.transform.position);
            }
        }
    }
}
