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
        private bool isDangerNearby = false;
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
                isDangerNearby = DetectActiveThreats();
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
            timer = 0f;
        }

        private bool DetectActiveThreats()
        {
            // Find all BaseAi components (wolves, bears, etc.)
            var animals = GameObject.FindObjectsOfType<BaseAi>();
            if (animals == null) return false;

            Vector3 playerPos = GameManager.GetPlayerTransform().position;

            foreach (var ai in animals)
            {
                if (ai == null) continue;

                // Matches Assembly-CSharp: BaseAi.GetAiMode(); death is AiMode.Dead (no IsDead())
                AiMode currentMode = ai.GetAiMode();
                if (currentMode == AiMode.Dead) continue;

                // Stalking / smell investigation / attack (Scent was never an enum value; use InvestigateSmell)
                if (currentMode == AiMode.Stalking || currentMode == AiMode.InvestigateSmell || currentMode == AiMode.Attack)
                {
                    // Check distance to ensure they are actually 'watching' or tracking us
                    float distance = Vector3.Distance(ai.transform.position, playerPos);

                    // 100 meters is the typical 'active' range for stalking/scent tracking
                    if (distance < 100f)
                    {
                        return true;
                    }
                }
            }

            return false;
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

                Color textColor = isDangerNearby ? Color.red : Color.white;
                textColor.a = alpha;
                style.normal.textColor = textColor;

                string message = isDangerNearby
                    ? "I'm being hunted!"
                    : "I'm safe, for now...";

                GUI.Label(new Rect(0, Screen.height * 0.22f, Screen.width, 50f), message, style);
            }
        }
    }
}