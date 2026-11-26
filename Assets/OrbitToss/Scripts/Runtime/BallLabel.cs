using TMPro;
using UnityEngine;

namespace OrbitToss
{
    /// <summary>
    /// Floating text above a ball (planet name, distance, "CLOSEST"). It follows the ball without rotating with it,
    /// and always turns to face the player.
    /// </summary>
    public class BallLabel : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private TMP_Text text;
        [SerializeField] private float heightAboveBall = 0.16f;

        public void Configure(Transform followTarget, TMP_Text label)
        {
            target = followTarget;
            text = label;
        }

        public void Show(string message)
        {
            if (text == null) return;
            text.text = message;
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);

        private void LateUpdate()
        {
            if (target == null) return;
            transform.position = target.position + Vector3.up * heightAboveBall;
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 away = transform.position - cam.transform.position;
                away.y = 0f;
                if (away.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(away);
            }
        }
    }
}
