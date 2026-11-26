using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

/// <summary>
/// First-person movement for the XR Origin.
/// - With a VR headset: left thumbstick moves (relative to where you look), right thumbstick snap-turns.
/// - Without a headset (testing in the Editor): WASD / arrows to move, Shift to walk faster,
///   click the Game view to capture the mouse and look around, Esc to release it.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class FirstPersonRig : MonoBehaviour
{
    [Header("Rig parts")]
    public Transform cameraOffset;
    public Transform head;
    [Tooltip("Objects shown only when a headset is active (e.g. controller hands).")]
    public GameObject[] xrOnlyObjects;

    [Header("Movement")]
    public float moveSpeed = 2f;
    public float snapTurnDegrees = 45f;

    [Header("Desktop fallback")]
    public float desktopEyeHeight = 1.7f;
    public float mouseSensitivity = 0.1f;

    InputAction _moveAction;
    InputAction _turnAction;
    CharacterController _controller;
    bool _turnReady = true;
    float _pitch;
    float _verticalVelocity;

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _moveAction = new InputAction("Move", InputActionType.Value, "<XRController>{LeftHand}/{Primary2DAxis}");
        _turnAction = new InputAction("Turn", InputActionType.Value, "<XRController>{RightHand}/{Primary2DAxis}");
    }

    void OnEnable()
    {
        _moveAction.Enable();
        _turnAction.Enable();
    }

    void OnDisable()
    {
        _moveAction.Disable();
        _turnAction.Disable();
        Cursor.lockState = CursorLockMode.None;
    }

    void Update()
    {
        bool xr = XRSettings.isDeviceActive;
        foreach (var go in xrOnlyObjects)
            if (go != null && go.activeSelf != xr) go.SetActive(xr);

        Vector2 move;
        if (xr)
        {
            move = _moveAction.ReadValue<Vector2>();
            HandleSnapTurn(_turnAction.ReadValue<Vector2>().x);
        }
        else
        {
            // No headset: put the camera at standing eye height and drive it with keyboard + mouse.
            head.localPosition = new Vector3(0f, desktopEyeHeight - cameraOffset.localPosition.y, 0f);
            move = DesktopMove();
            DesktopLook();
        }

        // Keep the collision capsule under the player's head (they can walk around in room-scale VR).
        Vector3 headLocal = transform.InverseTransformPoint(head.position);
        _controller.center = new Vector3(headLocal.x, _controller.height / 2f + _controller.skinWidth, headLocal.z);

        Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.001f) forward = transform.forward;
        forward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, forward);

        Vector3 velocity = (forward * move.y + right * move.x) * moveSpeed;
        _verticalVelocity = _controller.isGrounded ? -1f : _verticalVelocity + Physics.gravity.y * Time.deltaTime;
        velocity.y = _verticalVelocity;
        _controller.Move(velocity * Time.deltaTime);
    }

    void HandleSnapTurn(float x)
    {
        if (_turnReady && Mathf.Abs(x) > 0.7f)
        {
            transform.RotateAround(head.position, Vector3.up, Mathf.Sign(x) * snapTurnDegrees);
            _turnReady = false;
        }
        else if (Mathf.Abs(x) < 0.3f)
        {
            _turnReady = true;
        }
    }

    static Vector2 DesktopMove()
    {
        var k = Keyboard.current;
        if (k == null) return Vector2.zero;

        Vector2 m = Vector2.zero;
        if (k.wKey.isPressed || k.upArrowKey.isPressed) m.y += 1f;
        if (k.sKey.isPressed || k.downArrowKey.isPressed) m.y -= 1f;
        if (k.dKey.isPressed || k.rightArrowKey.isPressed) m.x += 1f;
        if (k.aKey.isPressed || k.leftArrowKey.isPressed) m.x -= 1f;
        m = Vector2.ClampMagnitude(m, 1f);
        return k.leftShiftKey.isPressed ? m * 2f : m;
    }

    void DesktopLook()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame) Cursor.lockState = CursorLockMode.Locked;
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Cursor.lockState = CursorLockMode.None;
        if (Cursor.lockState != CursorLockMode.Locked) return;

        Vector2 delta = mouse.delta.ReadValue() * mouseSensitivity;
        transform.Rotate(0f, delta.x, 0f);
        _pitch = Mathf.Clamp(_pitch - delta.y, -80f, 80f);
        head.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }
}
