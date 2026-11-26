using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OrbitToss
{
    /// <summary>
    /// Single-player PRACTICE round flow:
    ///   Main menu -> Play -> grab a planet ball from the rack -> throw at the fixed white Jack ->
    ///   ball settles -> distance + feedback + practice points -> repeat for all balls -> round summary ->
    ///   Reset (new round) or Menu.
    ///
    /// "Practice points" are a motivational accuracy score, NOT an official Boccia match score. Official two-side
    /// scoring lives in <see cref="BocciaScoring.ScoreEnd"/> for a future opponent mode.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public enum GameState { Menu, Playing, RoundOver }

        [Header("Scene references")]
        [SerializeField] private JackController jack;
        [SerializeField] private List<BocciaBall> balls = new List<BocciaBall>();
        [SerializeField] private List<BallLabel> ballLabels = new List<BallLabel>();
        [SerializeField] private BallRack rack;
        [SerializeField] private ThrowFeedbackUI hud;
        [SerializeField] private VRMenuController menu;
        [SerializeField] private AudioManager audioManager;
        [SerializeField] private SettingsManager settings;

        [Header("Feedback thresholds (practice only, not official decisions)")]
        [SerializeField] private FeedbackThresholds thresholds = new FeedbackThresholds();

        public GameState State { get; private set; } = GameState.Menu;
        public int Round { get; private set; }
        public int PracticePoints { get; private set; }
        public int RemainingBalls { get; private set; }
        public IReadOnlyList<BocciaBall> Balls => balls;
        public JackController Jack => jack;
        public FeedbackThresholds Thresholds => thresholds;

        private readonly Dictionary<BocciaBall, float> measured = new Dictionary<BocciaBall, float>();
        private readonly Dictionary<BocciaBall, bool> inPlay = new Dictionary<BocciaBall, bool>();

        public void Configure(JackController jackController, List<BocciaBall> ballList, List<BallLabel> labels, BallRack ballRack,
            ThrowFeedbackUI feedbackUI, VRMenuController menuController, AudioManager audio, SettingsManager settingsManager)
        {
            jack = jackController; balls = ballList; ballLabels = labels; rack = ballRack; hud = feedbackUI;
            menu = menuController; audioManager = audio; settings = settingsManager;
        }

        private void OnEnable()
        {
            foreach (var ball in balls)
            {
                ball.Settled += HandleSettled;
                ball.Impact += HandleImpact;
                var grab = ball.GetComponent<BallGrabThrow>();
                if (grab == null) continue;
                grab.Grabbed += HandleGrabbed;
                grab.Thrown += HandleThrown;
                grab.Dropped += HandleDropped;
            }
            if (settings != null) settings.Changed += HandleSettingsChanged;
        }

        private void OnDisable()
        {
            foreach (var ball in balls)
            {
                if (ball == null) continue;
                ball.Settled -= HandleSettled;
                ball.Impact -= HandleImpact;
                var grab = ball.GetComponent<BallGrabThrow>();
                if (grab == null) continue;
                grab.Grabbed -= HandleGrabbed;
                grab.Thrown -= HandleThrown;
                grab.Dropped -= HandleDropped;
            }
            if (settings != null) settings.Changed -= HandleSettingsChanged;
        }

        private void Start()
        {
            PlaceBallsOnRack();
            SetGrabbing(false);
            HideLabels();
            ReturnToMenu();
        }

        // ---------------------------------------------------------------- flow (wired to UI buttons)

        public void StartPractice()
        {
            Round++;
            PracticePoints = 0;
            measured.Clear();
            inPlay.Clear();
            RemainingBalls = balls.Count;

            if (jack != null) jack.ResetPosition();
            PlaceBallsOnRack();
            HideLabels();
            SetGrabbing(true);

            State = GameState.Playing;
            if (menu != null) menu.ShowHud();
            if (hud != null)
            {
                hud.SetRound(Round, false);
                hud.SetRemaining(RemainingBalls, balls.Count);
                hud.SetLastDistance("--");
                hud.SetBestDistance("--");
                hud.SetScore(0);
                hud.HideClosestMarker();
                hud.ClearFeedback();
            }
        }

        /// <summary>Reset button: starts a fresh practice round with all balls back on the rack.</summary>
        public void ResetPractice() => StartPractice();

        public void ReturnToMenu()
        {
            State = GameState.Menu;
            SetGrabbing(false);
            PlaceBallsOnRack();
            HideLabels();
            if (hud != null) hud.HideClosestMarker();
            if (menu != null) menu.ShowMainMenu();
        }

        // ---------------------------------------------------------------- ball events

        private void HandleGrabbed(BallGrabThrow grab)
        {
            if (audioManager != null) audioManager.PlayGrab();
        }

        private void HandleThrown(BallGrabThrow grab, Vector3 velocity)
        {
            if (State != GameState.Playing) return;
            RemainingBalls = Mathf.Max(0, RemainingBalls - 1);
            if (audioManager != null) audioManager.PlayThrow();
            if (hud != null)
            {
                hud.SetRemaining(RemainingBalls, balls.Count);
                hud.ShowInfo($"{grab.Ball.DisplayName} is rolling...");
            }
        }

        private void HandleDropped(BallGrabThrow grab)
        {
            if (hud != null) hud.ShowInfo($"{grab.Ball.DisplayName} was dropped gently - it will return to the rack.");
        }

        private void HandleImpact(BocciaBall ball, float speed, bool hitBall)
        {
            if (audioManager != null) audioManager.PlayKnock(ball.transform.position, hitBall ? speed * 1.5f : speed);
        }

        private void HandleSettled(BocciaBall ball)
        {
            if (!ball.CountsAsThrow)
            {
                // Accidental drop: put it back so the player can try again (prototype accessibility simplification).
                ball.ResetToHome();
                var grab = ball.GetComponent<BallGrabThrow>();
                if (grab != null) grab.SetGrabEnabled(State == GameState.Playing);
                return;
            }
            if (State != GameState.Playing) return;
            ScoreBall(ball);
        }

        private void ScoreBall(BocciaBall ball)
        {
            bool valid = !ball.HasFallenOff && CourtDimensions.IsInsidePlayingArea(ball.transform.position);
            float distance = DistanceCalculator.CourtPlaneDistance(ball.transform.position, jack.Position);
            measured[ball] = distance;
            inPlay[ball] = valid;

            PracticeFeedback feedback = BocciaScoring.EvaluatePractice(distance, valid, thresholds);
            PracticePoints += feedback.PracticePoints;

            string distanceText = valid ? DistanceCalculator.Format(distance) : "out of court";
            if (audioManager != null) audioManager.PlayFeedback(feedback.Kind);
            if (hud != null)
            {
                hud.SetLastDistance($"{ball.DisplayName} {distanceText}");
                hud.SetScore(PracticePoints);
                hud.ShowFeedback(feedback.Kind, feedback.Label, $"{ball.DisplayName}: {distanceText} from the Jack");
            }

            UpdateClosest();

            if (RemainingBalls == 0 && AllThrownBallsSettled()) FinishRound();
        }

        private void UpdateClosest()
        {
            var list = new List<BallMeasurement>();
            var order = new List<BocciaBall>();
            foreach (var pair in measured)
            {
                list.Add(new BallMeasurement(pair.Key.Side, pair.Value, inPlay[pair.Key], pair.Key.DisplayName));
                order.Add(pair.Key);
            }
            int closest = BocciaScoring.IndexOfClosest(list);

            for (int i = 0; i < order.Count; i++)
            {
                BallLabel label = LabelFor(order[i]);
                if (label == null) continue;
                string text = !list[i].IsValid
                    ? $"{order[i].DisplayName}\nOUT"
                    : $"{(i == closest ? "CLOSEST\n" : "")}{order[i].DisplayName}\n{DistanceCalculator.Format(list[i].Distance)}";
                label.Show(text);
            }

            if (hud == null) return;
            if (closest >= 0)
            {
                hud.SetBestDistance($"{order[closest].DisplayName} {DistanceCalculator.Format(list[closest].Distance)}");
                hud.ShowClosestMarker(order[closest].transform.position);
            }
            else
            {
                hud.SetBestDistance("--");
                hud.HideClosestMarker();
            }
        }

        private void FinishRound()
        {
            State = GameState.RoundOver;
            SetGrabbing(false);
            if (hud != null)
            {
                hud.SetRound(Round, true);
                int max = balls.Count * thresholds.perfectPoints;
                hud.ShowInfo($"Round complete! Practice points: {PracticePoints} / {max}.\nPress RESET to play again or MENU to exit.");
            }
            StartCoroutine(ShowLeaderboardEntryAfterDelay());
        }

        [Tooltip("Seconds to show the last throw's feedback before the round-complete / leaderboard screen opens.")]
        [SerializeField] private float leaderboardDelay = 3f;

        private IEnumerator ShowLeaderboardEntryAfterDelay()
        {
            int round = Round;
            yield return new WaitForSeconds(leaderboardDelay);
            // Skip if the player already pressed Reset or Menu during the delay.
            if (State != GameState.RoundOver || Round != round || menu == null) yield break;
            menu.ShowRoundComplete(PracticePoints, BestDistance());
        }

        /// <summary>Closest in-play ball of this round in metres, or -1 if none stayed in play.</summary>
        private float BestDistance()
        {
            float best = -1f;
            foreach (var pair in measured)
                if (inPlay[pair.Key] && (best < 0f || pair.Value < best)) best = pair.Value;
            return best;
        }

        private bool AllThrownBallsSettled()
        {
            foreach (var b in balls)
                if (b.State == BocciaBall.BallState.Rolling || b.State == BocciaBall.BallState.Held || b.State == BocciaBall.BallState.Returning)
                    return false;
            return true;
        }

        // ---------------------------------------------------------------- helpers

        private void HandleSettingsChanged()
        {
            // Rack height/side changes only move balls that are still waiting on the rack.
            if (rack == null) return;
            rack.Reposition();
            rack.AssignHomes(balls);
            foreach (var b in balls) if (b.State == BocciaBall.BallState.Ready) b.ResetToHome();
        }

        private void PlaceBallsOnRack()
        {
            if (rack != null)
            {
                rack.Reposition();
                rack.AssignHomes(balls);
            }
            foreach (var b in balls) b.ResetToHome();
        }

        private void SetGrabbing(bool allowed)
        {
            foreach (var b in balls)
            {
                var grab = b.GetComponent<BallGrabThrow>();
                if (grab != null) grab.SetGrabEnabled(allowed);
            }
        }

        private void HideLabels()
        {
            foreach (var l in ballLabels) if (l != null) l.Hide();
        }

        private BallLabel LabelFor(BocciaBall ball)
        {
            int i = balls.IndexOf(ball);
            return i >= 0 && i < ballLabels.Count ? ballLabels[i] : null;
        }
    }
}
