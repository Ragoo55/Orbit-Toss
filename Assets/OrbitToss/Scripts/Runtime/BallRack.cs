using System.Collections.Generic;
using UnityEngine;

namespace OrbitToss
{
    /// <summary>
    /// The tray that holds the player's balls within comfortable seated reach (below shoulder height, beside the player).
    /// Height and side (left/right hand) are adjustable from Settings.
    /// </summary>
    public class BallRack : MonoBehaviour
    {
        [Tooltip("Slots where balls rest. Children of this rack, laid out by the builder.")]
        [SerializeField] private List<Transform> slots = new List<Transform>();
        [Tooltip("Player's seated position (usually the XR rig root). The rack is placed relative to it.")]
        [SerializeField] private Transform playerAnchor;
        [SerializeField] private SettingsManager settings;

        [Header("Placement relative to the player (metres)")]
        [Tooltip("Rack surface height for Low / Medium / High settings (seated hand height).")]
        [SerializeField] private float[] heights = { 0.55f, 0.70f, 0.85f };
        [SerializeField] private float sideOffset = 0.38f;
        [SerializeField] private float forwardOffset = 0.30f;

        public IReadOnlyList<Transform> Slots => slots;

        public void Configure(List<Transform> rackSlots, Transform anchor, SettingsManager settingsManager)
        {
            slots = rackSlots;
            playerAnchor = anchor;
            settings = settingsManager;
        }

        private void OnEnable() { if (settings != null) settings.Changed += Reposition; }
        private void OnDisable() { if (settings != null) settings.Changed -= Reposition; }

        /// <summary>Moves the rack for the current settings. Call ResetToHome on balls afterwards.</summary>
        public void Reposition()
        {
            if (playerAnchor == null) return;
            int h = settings != null ? (int)settings.CurrentRackHeight : 1;
            bool right = settings == null || settings.RackOnRight;
            Vector3 offset = new Vector3(right ? sideOffset : -sideOffset, heights[Mathf.Clamp(h, 0, heights.Length - 1)], forwardOffset);
            transform.position = playerAnchor.TransformPoint(new Vector3(offset.x, 0f, offset.z)) + Vector3.up * offset.y;
            transform.rotation = Quaternion.Euler(0f, playerAnchor.eulerAngles.y, 0f);
        }

        /// <summary>Assigns each ball's home to a slot (ball centre sits just above the tray surface).</summary>
        public void AssignHomes(IReadOnlyList<BocciaBall> balls)
        {
            for (int i = 0; i < balls.Count && i < slots.Count; i++)
            {
                Vector3 p = slots[i].position + Vector3.up * balls[i].Radius;
                balls[i].SetHome(p, Quaternion.identity);
            }
        }
    }
}
