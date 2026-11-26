using TMPro;
using UnityEngine;

namespace OrbitToss
{
    /// <summary>
    /// In-game HUD: round indicator, remaining balls, latest and best distance, practice score, and the throw
    /// feedback message. Feedback is shown as TEXT + a SYMBOL (never colour alone) and fades gently - no flashing.
    /// </summary>
    public class ThrowFeedbackUI : MonoBehaviour
    {
        [Header("HUD panel texts")]
        [SerializeField] private TMP_Text roundText;
        [SerializeField] private TMP_Text remainingText;
        [SerializeField] private TMP_Text lastDistanceText;
        [SerializeField] private TMP_Text bestDistanceText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text feedbackText;

        [Header("Large floating message above the court")]
        [SerializeField] private TMP_Text courtMessageText;
        [Tooltip("Seconds the floating message stays fully visible before fading.")]
        [SerializeField] private float messageHoldTime = 2.5f;
        [SerializeField] private float messageFadeTime = 1.0f;

        [Header("Closest-ball marker")]
        [SerializeField] private Transform closestMarker;

        private float messageTimer = -1f;

        public void Configure(TMP_Text round, TMP_Text remaining, TMP_Text last, TMP_Text best, TMP_Text score,
            TMP_Text feedback, TMP_Text courtMessage, Transform marker)
        {
            roundText = round; remainingText = remaining; lastDistanceText = last; bestDistanceText = best;
            scoreText = score; feedbackText = feedback; courtMessageText = courtMessage; closestMarker = marker;
        }

        public void SetRound(int round, bool finished)
        {
            Set(roundText, finished ? $"Practice round {round} - END" : $"Practice round {round}");
        }

        public void SetRemaining(int remaining, int total) => Set(remainingText, $"Balls left: {remaining} / {total}");
        public void SetLastDistance(string value) => Set(lastDistanceText, $"Last ball: {value}");
        public void SetBestDistance(string value) => Set(bestDistanceText, $"Closest ball: {value}");
        public void SetScore(int practicePoints) => Set(scoreText, $"Practice points: {practicePoints}");

        public void ShowFeedback(FeedbackKind kind, string label, string detail)
        {
            string message = $"{Symbol(kind)} {label}";
            Set(feedbackText, string.IsNullOrEmpty(detail) ? message : $"{message}\n{detail}");
            if (courtMessageText != null)
            {
                courtMessageText.text = string.IsNullOrEmpty(detail) ? message : $"{message}\n<size=60%>{detail}</size>";
                SetAlpha(courtMessageText, 1f);
                courtMessageText.gameObject.SetActive(true);
                messageTimer = 0f;
            }
        }

        public void ShowInfo(string message)
        {
            Set(feedbackText, message);
        }

        public void ClearFeedback()
        {
            Set(feedbackText, "Grab a planet ball and throw it towards the white Jack.");
            if (courtMessageText != null) courtMessageText.gameObject.SetActive(false);
            messageTimer = -1f;
        }

        public void ShowClosestMarker(Vector3 ballPosition)
        {
            if (closestMarker == null) return;
            closestMarker.position = new Vector3(ballPosition.x, 0.003f, ballPosition.z);
            closestMarker.gameObject.SetActive(true);
        }

        public void HideClosestMarker()
        {
            if (closestMarker != null) closestMarker.gameObject.SetActive(false);
        }

        /// <summary>
        /// Text symbols so feedback does not depend on colour. Plain ASCII so every font (including the default
        /// TextMeshPro font) can render them.
        /// </summary>
        public static string Symbol(FeedbackKind kind)
        {
            switch (kind)
            {
                case FeedbackKind.Perfect: return "[***]";
                case FeedbackKind.Good: return "[**]";
                case FeedbackKind.Close: return "[*]";
                case FeedbackKind.Out: return "[X]";
                default: return "[~]";
            }
        }

        private void Update()
        {
            if (messageTimer < 0f || courtMessageText == null) return;
            messageTimer += Time.deltaTime;
            if (messageTimer > messageHoldTime)
            {
                float a = 1f - Mathf.Clamp01((messageTimer - messageHoldTime) / messageFadeTime);
                SetAlpha(courtMessageText, a);
                if (a <= 0f) { courtMessageText.gameObject.SetActive(false); messageTimer = -1f; }
            }
        }

        private static void Set(TMP_Text t, string value) { if (t != null) t.text = value; }

        private static void SetAlpha(TMP_Text t, float a)
        {
            Color c = t.color; c.a = a; t.color = c;
        }
    }
}
