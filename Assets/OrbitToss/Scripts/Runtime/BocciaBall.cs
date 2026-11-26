using System;
using UnityEngine;

namespace OrbitToss
{
    /// <summary>
    /// Physics and state of one planet-themed Boccia ball: Rigidbody/SphereCollider setup, rolling resistance,
    /// and "settled" detection (the ball must be nearly stationary for a while before its distance is scored).
    /// Grabbing is handled separately by <see cref="BallGrabThrow"/>.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public class BocciaBall : MonoBehaviour
    {
        public enum BallState
        {
            Ready,     // waiting on the ball rack
            Held,      // in the player's hand
            Rolling,   // thrown, still moving
            Settled,   // stopped - distance can be measured
            Returning  // dropped by accident, will go back to the rack
        }

        [Header("Identity")]
        [SerializeField] private string displayName = "Mars";
        [Tooltip("0 = side A (player), 1 = side B (reserved for a future opponent).")]
        [SerializeField] private int side;

        [Header("Physics (official ball: 275 g, 270 mm circumference)")]
        [SerializeField] private float mass = CourtDimensions.BallMassKg;
        [SerializeField] private float linearDamping = 0.05f;
        [SerializeField] private float angularDamping = 0.1f;
        [Tooltip("Extra deceleration (m/s²) while rolling on the floor. Boccia balls are soft leather and roll short distances.")]
        [SerializeField] private float rollingResistance = 0.45f;
        [SerializeField] private float maxAngularVelocity = 200f; // rolling at 5 m/s with r = 4.3 cm needs ~116 rad/s

        [Header("Settling")]
        [SerializeField] private float settleLinearSpeed = 0.03f;
        [SerializeField] private float settleAngularSpeed = 0.6f;
        [Tooltip("Seconds the ball must stay below the settle speeds.")]
        [SerializeField] private float settleHoldTime = 0.6f;
        [Tooltip("Safety net: force-settle after this many seconds of rolling.")]
        [SerializeField] private float maxRollTime = 20f;
        [Tooltip("If the ball falls below this height it is treated as settled (out of play).")]
        [SerializeField] private float fallLimitY = -2f;

        public event Action<BocciaBall> Settled;
        /// <summary>Raised on impacts: (ball, impact speed m/s, is the other object a ball/jack).</summary>
        public event Action<BocciaBall, float, bool> Impact;

        public string DisplayName => displayName;
        public int Side => side;
        public BallState State { get; private set; } = BallState.Ready;
        public Rigidbody Body { get; private set; }
        public float Radius => sphere != null ? sphere.radius * MaxScale() : CourtDimensions.BallRadius;
        /// <summary>True if this throw counts as a played ball (false for accidental drops).</summary>
        public bool CountsAsThrow { get; private set; }
        public bool HasFallenOff { get; private set; }

        private SphereCollider sphere;
        private Vector3 homePosition;
        private Quaternion homeRotation;
        private bool hasHome;
        private float belowSpeedTimer;
        private float rollTimer;
        private bool grounded;

        private void Awake()
        {
            Body = GetComponent<Rigidbody>();
            sphere = GetComponent<SphereCollider>();
            ApplyPhysicsSettings();
            if (!hasHome) SetHome(transform.position, transform.rotation);
        }

        private void OnValidate()
        {
            if (Body == null) Body = GetComponent<Rigidbody>();
            if (Body != null) ApplyPhysicsSettings();
        }

        public void ApplyPhysicsSettings()
        {
            Body.mass = mass;
            Body.linearDamping = linearDamping;
            Body.angularDamping = angularDamping;
            Body.maxAngularVelocity = maxAngularVelocity;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Body.useGravity = true;
        }

        public void Configure(string newName, int newSide)
        {
            displayName = newName;
            side = newSide;
        }

        /// <summary>Where the ball returns to when the round resets (its rack slot).</summary>
        public void SetHome(Vector3 position, Quaternion rotation)
        {
            homePosition = position;
            homeRotation = rotation;
            hasHome = true;
        }

        /// <summary>Puts the ball back on the rack, frozen in place until grabbed.</summary>
        public void ResetToHome()
        {
            State = BallState.Ready;
            CountsAsThrow = false;
            HasFallenOff = false;
            belowSpeedTimer = 0f;
            rollTimer = 0f;

            if (!Body.isKinematic)
            {
                Body.linearVelocity = Vector3.zero;
                Body.angularVelocity = Vector3.zero;
            }
            // Teleporting is used only to reset the rack, never as the throwing mechanic.
            Body.position = homePosition;
            Body.rotation = homeRotation;
            transform.SetPositionAndRotation(homePosition, homeRotation);
            Body.constraints = RigidbodyConstraints.FreezeAll; // rests in its slot without rolling away
        }

        /// <summary>Called when a hand/controller grabs the ball.</summary>
        public void MarkHeld()
        {
            State = BallState.Held;
            Body.constraints = RigidbodyConstraints.None;
        }

        /// <summary>Called when the ball is released as a real throw.</summary>
        public void MarkThrown()
        {
            Body.constraints = RigidbodyConstraints.None;
            State = BallState.Rolling;
            CountsAsThrow = true;
            belowSpeedTimer = 0f;
            rollTimer = 0f;
        }

        /// <summary>Called when the ball is released too gently to be a throw (accidental drop).</summary>
        public void MarkDropped()
        {
            Body.constraints = RigidbodyConstraints.None;
            State = BallState.Returning;
            CountsAsThrow = false;
            belowSpeedTimer = 0f;
            rollTimer = 0f;
        }

        private void FixedUpdate()
        {
            if (State != BallState.Rolling && State != BallState.Returning) return;

            rollTimer += Time.fixedDeltaTime;

            if (transform.position.y < fallLimitY)
            {
                HasFallenOff = true;
                FinishSettling();
                return;
            }

            ApplyRollingResistance();

            bool slow = Body.linearVelocity.magnitude < settleLinearSpeed && Body.angularVelocity.magnitude < settleAngularSpeed;
            belowSpeedTimer = slow ? belowSpeedTimer + Time.fixedDeltaTime : 0f;

            if (belowSpeedTimer >= settleHoldTime || rollTimer >= maxRollTime)
            {
                FinishSettling();
            }
            grounded = false; // refreshed by OnCollisionStay each physics step
        }

        private void ApplyRollingResistance()
        {
            if (!grounded || rollingResistance <= 0f) return;
            Vector3 v = Body.linearVelocity;
            Vector3 horizontal = new Vector3(v.x, 0f, v.z);
            float speed = horizontal.magnitude;
            if (speed < 0.0001f) return;
            float deltaV = Mathf.Min(rollingResistance * Time.fixedDeltaTime, speed);
            Body.AddForce(-horizontal / speed * deltaV, ForceMode.VelocityChange);
            // Slow the spin proportionally so the ball does not keep rotating in place.
            Body.angularVelocity *= Mathf.Clamp01(1f - deltaV / Mathf.Max(speed, 0.0001f));
        }

        private void FinishSettling()
        {
            Body.linearVelocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
            Body.Sleep();
            State = BallState.Settled;
            Settled?.Invoke(this);
        }

        private void OnCollisionStay(Collision collision)
        {
            for (int i = 0; i < collision.contactCount; i++)
            {
                if (collision.GetContact(i).normal.y > 0.7f) { grounded = true; return; }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (State == BallState.Ready) return;
            bool hitBall = collision.rigidbody != null &&
                           (collision.rigidbody.GetComponent<BocciaBall>() != null || collision.rigidbody.GetComponent<JackController>() != null);
            Impact?.Invoke(this, collision.relativeVelocity.magnitude, hitBall);
        }

        private float MaxScale()
        {
            Vector3 s = transform.lossyScale;
            return Mathf.Max(Mathf.Abs(s.x), Mathf.Max(Mathf.Abs(s.y), Mathf.Abs(s.z)));
        }
    }
}
