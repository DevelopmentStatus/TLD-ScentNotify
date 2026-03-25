using UnityEngine;
using MelonLoader;
using Il2Cpp;
using Il2CppTLD.AI;

[assembly: MelonInfo(typeof(ScentMod.ScentNotify), "Scent Notify", "1.0.0", "Krusty")]
[assembly: MelonGame("Hinterland", "TheLongDark")]

namespace ScentMod
{
    public class ScentNotify : MelonMod
    {
        private bool showNotification = false;
        private AiMode? threatMode = null;
        private float timer = 0f;
        private float displayDuration = 3.5f;
        private float fadeOutDuration = 1.15f;

        public override void OnUpdate()
        {
            if (!IsLoadedGameplayReady())
            {
                DismissNotification();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                threatMode = DetectActiveThreats();
                showNotification = true;
                timer = displayDuration;
            }

            if (showNotification)
            {
                timer -= Time.deltaTime;
                if (timer <= 0) DismissNotification();
            }
        }

        private static bool IsLoadedGameplayReady()
        {
            if (GameManager.IsMainMenuActive()
                || GameManager.IsBootSceneActive()
                || GameManager.IsEmptySceneActive())
                return false;

            if (InterfaceManager.IsPanelLoadingEnabledOrLoading())
                return false;

            if (SaveGameSystem.IsRestoreInProgress() || SaveGameSystem.IsSceneRestoreInProgress())
                return false;

            if (GameManager.GetPlayerTransform() == null)
                return false;

            UnityEngine.SceneManagement.Scene active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            return active.IsValid() && active.isLoaded;
        }

        private void DismissNotification()
        {
            showNotification = false;
            threatMode = null;
            timer = 0f;
        }

        /// If several animals qualify, the worst mode wins: Attack, then Stalking, then InvestigateSmell.
        private AiMode? DetectActiveThreats()
        {
            var animals = GameObject.FindObjectsOfType<BaseAi>();
            if (animals == null) return null;

            Vector3 playerPos = GameManager.GetPlayerTransform().position;
            int bestPriority = 0;
            AiMode? bestMode = null;

            foreach (var ai in animals)
            {
                if (ai == null) continue;

                AiMode currentMode = ai.GetAiMode();
                if (currentMode == AiMode.Dead) continue;

                if (currentMode != AiMode.Stalking && currentMode != AiMode.InvestigateSmell && currentMode != AiMode.Attack)
                    continue;

                float distance = Vector3.Distance(ai.transform.position, playerPos);
                if (distance >= 100f) continue;

                int priority = currentMode == AiMode.Attack ? 3 : currentMode == AiMode.Stalking ? 2 : 1;
                if (priority > bestPriority)
                {
                    bestPriority = priority;
                    bestMode = currentMode;
                }
            }

            return bestMode;
        }

        private static string ThreatMessage(AiMode mode)
        {
            if (mode == AiMode.InvestigateSmell)
                return "Something caught my scent.";
            if (mode == AiMode.Stalking)
                return "I'm being stalked.";
            if (mode == AiMode.Attack)
                return "I'm under attack!";
            return "I'm being hunted!";
        }

        public override void OnGUI()
        {
            if (showNotification && IsLoadedGameplayReady())
            {
                float fadeWindow = Mathf.Min(fadeOutDuration, displayDuration);
                float alpha = timer >= fadeWindow ? 1f : Mathf.Clamp01(timer / fadeWindow);

                GUIStyle style = new GUIStyle();
                style.fontSize = 28;
                style.alignment = TextAnchor.MiddleCenter;

                bool danger = threatMode.HasValue;
                Color textColor = danger ? Color.red : Color.white;
                textColor.a = alpha;
                style.normal.textColor = textColor;

                string message = danger ? ThreatMessage(threatMode.Value) : "I'm safe, for now...";

                GUI.Label(new Rect(0, Screen.height * 0.22f, Screen.width, 50f), message, style);
            }
        }
    }
}
