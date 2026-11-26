using System;
using System.Collections;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEngine;

namespace OrbitToss
{
    /// <summary>
    /// Bridges the Meta Interaction SDK grab components to Boccia gameplay.
    ///
    /// The ISDK <see cref="Grabbable"/> (with "Throw When Unselected" enabled) moves the ball while held and, on
    /// release, gives it the controller/hand release velocity. This script then:
    ///   - multiplies and CLAMPS that velocity (prevents unrealistic launches and motion discomfort),
    ///   - decides whether the release was a real throw or an accidental drop,
    ///   - disables the ball's grab interactables after a throw so it cannot be re-grabbed (no duplicate throws),
    ///   - plays a short haptic pulse on the controller that released the ball (if enabled in Settings).
    /// </summary>
    [RequireComponent(typeof(BocciaBall))]
    public class BallGrabThrow : MonoBehaviour
    {
        [Header("Meta Interaction SDK references (auto-filled if empty)")]
        [SerializeField] private Grabbable grabbable;

        [Header("Throw tuning")]
        [Tooltip("Scales the release velocity measured by the Interaction SDK.")]
        [SerializeField] private float throwMultiplier = 1.0f;
        [Tooltip("Maximum ball speed after release (m/s). A gentle underarm Boccia throw to 7.5 m needs ~3-4 m/s.")]
        [SerializeField] private float maxThrowSpeed = 7f;
        [Tooltip("Maximum spin after release (rad/s).")]
        [SerializeField] private float maxAngularSpeed = 120f;
        [Tooltip("Releases slower than this (m/s) count as an accidental drop: the ball returns to the rack and is not scored.")]
        [SerializeField] private float minThrowSpeed = 0.4f;
        [Tooltip("Upward launch speed is limited to this (m/s) to keep throws low and readable.")]
        [SerializeField] private float maxUpwardSpeed = 3f;

        [Header("Haptics")]
        [SerializeField, Range(0f, 1f)] private float grabHapticAmplitude = 0.25f;
        [SerializeField, Range(0f, 1f)] private float releaseHapticAmplitude = 0.5f;
        [SerializeField] private float hapticDuration = 0.08f;

        public event Action<BallGrabThrow> Grabbed;
        public event Action<BallGrabThrow, Vector3> Thrown;
        public event Action<BallGrabThrow> Dropped;

        public BocciaBall Ball { get; private set; }
        public bool IsHeld { get; private set; }

        /// <summary>Global switch set by SettingsManager.</summary>
        public static bool HapticsEnabled = true;

        private MonoBehaviour[] interactables;
        private bool releasePending;
        private static OVRCameraRig cachedRig;

        private void Awake()
        {
            Ball = GetComponent<BocciaBall>();
            if (grabbable == null) grabbable = GetComponent<Grabbable>();

            var grabInteractables = GetComponentsInChildren<GrabInteractable>(true);
            var handGrabInteractables = GetComponentsInChildren<HandGrabInteractable>(true);
            interactables = new MonoBehaviour[grabInteractables.Length + handGrabInteractables.Length];
            grabInteractables.CopyTo(interactables, 0);
            handGrabInteractables.CopyTo(interactables, grabInteractables.Length);
        }

        private void OnEnable()
        {
            if (grabbable != null) grabbable.WhenPointerEventRaised += HandlePointerEvent;
        }

        private void OnDisable()
        {
            if (grabbable != null) grabbable.WhenPointerEventRaised -= HandlePointerEvent;
        }

        /// <summary>Enable/disable grabbing (used when the ball has been thrown or the round is not active).</summary>
        public void SetGrabEnabled(bool enabledState)
        {
            foreach (var interactable in interactables)
                if (interactable != null) interactable.enabled = enabledState;
        }

        private void HandlePointerEvent(PointerEvent evt)
        {
            switch (evt.Type)
            {
                case PointerEventType.Select:
                    if (!IsHeld && Ball.State == BocciaBall.BallState.Ready)
                    {
                        IsHeld = true;
                        releasePending = false;
                        Ball.MarkHeld();
                        Pulse(grabHapticAmplitude);
                        Grabbed?.Invoke(this);
                    }
                    break;

                case PointerEventType.Unselect:
                case PointerEventType.Cancel:
                    // Only treat it as a release once no hand/controller is still holding the ball.
                    if (IsHeld && grabbable.SelectingPointsCount == 0)
                    {
                        IsHeld = false;
                        // The ISDK applies its release velocity during this same event; read it on the next physics step.
                        releasePending = true;
                    }
                    break;
            }
        }

        private void FixedUpdate()
        {
            if (!releasePending) return;
            releasePending = false;
            ProcessRelease();
        }

        private void ProcessRelease()
        {
            Rigidbody body = Ball.Body;
            Vector3 v = body.isKinematic ? Vector3.zero : body.linearVelocity * throwMultiplier;
            v.y = Mathf.Min(v.y, maxUpwardSpeed);
            v = Vector3.ClampMagnitude(v, maxThrowSpeed);

            if (!body.isKinematic)
            {
                body.linearVelocity = v;
                body.angularVelocity = Vector3.ClampMagnitude(body.angularVelocity, maxAngularSpeed);
            }

            if (v.magnitude < minThrowSpeed)
            {
                Ball.MarkDropped();
                Dropped?.Invoke(this);
                return;
            }

            // A real throw: lock out further grabs until the round is reset.
            SetGrabEnabled(false);
            Ball.MarkThrown();
            Pulse(releaseHapticAmplitude);
            Thrown?.Invoke(this, v);
        }

        // ---------------------------------------------------------------- haptics

        private void Pulse(float amplitude)
        {
            if (!HapticsEnabled || amplitude <= 0f) return;
            OVRInput.Controller controller = NearestController();
            if (controller == OVRInput.Controller.None) return;
            StartCoroutine(PulseRoutine(controller, amplitude));
        }

        private IEnumerator PulseRoutine(OVRInput.Controller controller, float amplitude)
        {
            OVRInput.SetControllerVibration(0.5f, amplitude, controller);
            yield return new WaitForSeconds(hapticDuration);
            OVRInput.SetControllerVibration(0f, 0f, controller);
        }

        private OVRInput.Controller NearestController()
        {
            if (cachedRig == null) cachedRig = FindAnyObjectByType<OVRCameraRig>();
            if (cachedRig == null) return OVRInput.Controller.None;
            float left = Vector3.Distance(transform.position, cachedRig.leftHandAnchor.position);
            float right = Vector3.Distance(transform.position, cachedRig.rightHandAnchor.position);
            return left < right ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
        }
    }
}
