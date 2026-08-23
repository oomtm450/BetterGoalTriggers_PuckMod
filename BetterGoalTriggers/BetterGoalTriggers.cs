using HarmonyLib;
using oomtm450PuckMod_BetterGoalTriggers.SystemFunc;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace oomtm450PuckMod_BetterGoalTriggers {
    /// <summary>
    /// Class containing the main code for the BetterGoalTriggers patch.
    /// </summary>
    public class BetterGoalTriggers : IPuckPlugin {
        #region Constants
        /// <summary>
        /// Const string, version of the mod.
        /// </summary>
        private const string MOD_VERSION = "1.1.1";

        private const float BASE_GAME_GOAL_TRIGGER_X_SCALE = 1.08f;
        private const float BASE_GAME_GOAL_TRIGGER_Y_SCALE = 1.08f;
        private const float BASE_GAME_GOAL_TRIGGER_Z_SCALE = 0.96f;
        #endregion

        #region Fields/Properties
        /// <summary>
        /// Harmony, harmony instance to patch the Puck's code.
        /// </summary>
        private static readonly Harmony _harmony = new Harmony(Constants.MOD_NAME);

        internal static Configs.ServerConfig ServerConfig = new Configs.ServerConfig();

        private static bool _triggersHaveBeenBettered = false;

        private static Vector3 _originalGoalTriggerScale = new Vector3(BASE_GAME_GOAL_TRIGGER_X_SCALE, BASE_GAME_GOAL_TRIGGER_Y_SCALE, BASE_GAME_GOAL_TRIGGER_Z_SCALE);

        private static DateTime _lastTime_Server_OnPuckEnterGoal_WasCalled = DateTime.MinValue;

        private static readonly List<GameObject> _groundGoalTriggers = new List<GameObject>();
        #endregion

        /// <summary>
        /// Class that patches the Server_OnPuckEnterGoal event from Goal.
        /// </summary>
        [HarmonyPatch(typeof(Goal), nameof(Goal.Server_OnPuckEnterGoal))]
        public class Goal_Server_OnPuckEnterGoal_Patch {
            [HarmonyPrefix]
            public static bool Prefix(Puck puck) {
                if (GameManager.Instance.Phase != GamePhase.Play) {
                    if (GameManager.Instance.Phase == GamePhase.RedScore || GameManager.Instance.Phase == GamePhase.BlueScore)
                        return false;

                    return true;
                }

                try {
                    DateTime now = DateTime.UtcNow;

                    if ((now - _lastTime_Server_OnPuckEnterGoal_WasCalled).TotalMilliseconds < 1000)
                        return false;

                    _lastTime_Server_OnPuckEnterGoal_WasCalled = now;
                }
                catch (Exception ex) {
                    Logging.LogError($"Error in {nameof(Goal_Server_OnPuckEnterGoal_Patch)} Prefix().\n{ex}", ServerConfig);
                }

                return true;
            }
        }

        /// <summary>
        /// Method called when a client has connected (joined a server) on the server-side.
        /// Used to set server-sided stuff after the game has loaded.
        /// </summary>
        /// <param name="message">Dictionary of string and object, content of the event.</param>
        public static void Event_Everyone_OnClientConnected(Dictionary<string, object> message) {
            try {
                BetterGoalTriggersFunc();
            }
            catch (Exception ex) {
                Logging.LogError($"Error in {nameof(Event_Everyone_OnClientConnected)}.\n{ex}", ServerConfig);
            }
        }

        private static void BetterGoalTriggersFunc() {
            if (_triggersHaveBeenBettered)
                return;

            Logging.Log("Applying better goal triggers.", ServerConfig);

            GameObject levelObj = GameObject.Find("Level Default");
            for (int i = 0; i < levelObj.transform.childCount; i++) {
                Transform levelManagerChild = levelObj.transform.GetChild(i);
                if (levelManagerChild.gameObject.name != "Goal Blue" && levelManagerChild.gameObject.name != "Goal Red")
                    continue;

                BetterGoalTriggerFunc(levelManagerChild.gameObject);
            }

            _triggersHaveBeenBettered = true;
        }

        private static void BetterGoalTriggerFunc(GameObject goal) {
            try {
                Transform goalTrigger = goal.transform.Find("Goal Trigger");

                // Resize current goal trigger.
                _originalGoalTriggerScale = goalTrigger.localScale;
                goalTrigger.localScale = new Vector3(goalTrigger.localScale.x * 1.0319f, goalTrigger.localScale.y, goalTrigger.localScale.z * 1.0319f); // TODO : 1.0319f is for a 0.85 scaled puck, adjust depending on puck scale.

                // Add a new goal trigger on the ground.
                GameObject groundGoalTrigger = UnityEngine.Object.Instantiate(goalTrigger.gameObject);
                groundGoalTrigger.name = "Goal Trigger Ground";
                groundGoalTrigger.transform.SetParent(goalTrigger.parent);
                groundGoalTrigger.transform.position = new Vector3(goalTrigger.transform.position.x, goalTrigger.transform.position.y + 1.45f, goalTrigger.transform.position.z);
                if (goal.name == "Goal Red")
                    groundGoalTrigger.transform.rotation = Quaternion.Euler(90, 180, 0);
                else
                    groundGoalTrigger.transform.rotation = Quaternion.Euler(90, 0, 0);
                groundGoalTrigger.transform.localScale = new Vector3(((_originalGoalTriggerScale.x - BASE_GAME_GOAL_TRIGGER_X_SCALE) * 0.873f) + 0.873f, ((_originalGoalTriggerScale.y - BASE_GAME_GOAL_TRIGGER_Y_SCALE) * 0.712f) + 0.712f, ((_originalGoalTriggerScale.z - BASE_GAME_GOAL_TRIGGER_Z_SCALE) * 1f) + 1f);
                groundGoalTrigger.transform.localPosition = new Vector3(0, -0.6574f, -0.7f);

                _groundGoalTriggers.Add(groundGoalTrigger);

                /*MeshFilter meshFilter = null;
                try {
                    meshFilter = groundGoalTrigger.GetComponent<MeshFilter>();
                    if (meshFilter == null)
                        meshFilter = groundGoalTrigger.AddComponent<MeshFilter>();
                }
                catch (Exception ex) {
                    Logging.LogError($"1 : {ex}", ServerConfig);
                }

                MeshRenderer meshRenderer = null;
                try {
                    meshRenderer = groundGoalTrigger.GetComponent<MeshRenderer>();
                    if (meshRenderer == null)
                        meshRenderer = groundGoalTrigger.AddComponent<MeshRenderer>();
                }
                catch (Exception ex) {
                    Logging.LogError($"2 : {ex}", ServerConfig);
                }

                try {
                    MeshCollider meshCollider = groundGoalTrigger.GetComponent<MeshCollider>();
                    if (meshCollider == null)
                        Logging.LogError("meshCollider.sharedMesh is null !!!!!!!!!!", ServerConfig);
                    if (meshCollider.sharedMesh != null) {
                        // Duplicate or share the mesh data with the MeshFilter
                        meshFilter.sharedMesh = meshCollider.sharedMesh;
                    }
                    else
                        Logging.LogError("meshCollider.sharedMesh is null !!!!!!!!!!", ServerConfig);

                    // 3. Assign a default built-in material so it is visible
                    Shader testShader = Shader.Find("Universal Render Pipeline/Lit");
                    if (testShader == null)
                        testShader = Shader.Find("HDRP/Lit");
                    if (testShader == null)
                        Logging.LogError($"{nameof(testShader)} is null !!!!!!!!!!", ServerConfig);
                    meshRenderer.material = new Material(testShader);
                }
                catch (Exception ex) {
                    Logging.LogError($"3 : {ex}", ServerConfig);
                }*/
            }
            catch (Exception ex) {
                Logging.LogError($"Error in {nameof(BetterGoalTriggerFunc)}.\n{ex}", ServerConfig);
            }
        }

        private static void RevertBetterGoalTriggers() {
            try {
                if (!_triggersHaveBeenBettered)
                    return;

                Logging.Log("Reverting better goal triggers.", ServerConfig);

                GameObject levelObj = GameObject.Find("Level Default");
                for (int i = 0; i < levelObj.transform.childCount; i++) {
                    Transform levelManagerChild = levelObj.transform.GetChild(i);
                    if (levelManagerChild.gameObject.name != "Goal Blue" && levelManagerChild.gameObject.name != "Goal Red")
                        continue;

                    Transform goalTrigger = levelManagerChild.Find("Goal Trigger");
                    goalTrigger.localScale = _originalGoalTriggerScale;
                }

                foreach (GameObject groundGoalTrigger in _groundGoalTriggers)
                    GameObject.Destroy(groundGoalTrigger);

                _groundGoalTriggers.Clear();

                _triggersHaveBeenBettered = false;
            }
            catch (Exception ex) {
                Logging.LogError($"Error in {nameof(RevertBetterGoalTriggers)}.\n{ex}", ServerConfig);
            }
        }

        private static void Event_CompetitiveAdjustments_OnArenaSync(Dictionary<string, object> message) {
            try {
                RevertBetterGoalTriggers();
                BetterGoalTriggersFunc();
            }
            catch (Exception ex) {
                Logging.LogError($"Error in {nameof(Event_CompetitiveAdjustments_OnArenaSync)}.\n{ex}", ServerConfig);
            }
        }

        /*/// <summary>
        /// Class that patches the OnGameStateChanged event from BaseGameMode.
        /// </summary>
        [HarmonyPatch(typeof(BaseGameMode<BaseGameModeConfig>), "OnGameStateChanged")]
        public class BaseGameMode_OnGameStateChanged_Patch {
            [HarmonyPostfix]
            public static void Postfix(GameState oldGameState, GameState newGameState) {
                try {
                    // If this is not the server, do not use the patch.
                    if (oldGameState.Phase == newGameState.Phase)
                        return;

                    if (newGameState.Phase == GamePhase.Play)
                        BetterGoalTriggersFunc();
                }
                catch (Exception ex) {
                    Logging.LogError($"Error in {nameof(BaseGameMode_OnGameStateChanged_Patch)} Postfix().\n{ex}", ServerConfig);
                }
            }
        }*/

        /// <summary>
        /// Function that launches when the mod is being enabled.
        /// </summary>
        /// <returns>Bool, true if the mod successfully enabled.</returns>
        public bool OnEnable() {
            try {
                Logging.Log($"Enabling...", ServerConfig, true);

                if (Application.version != Constants.CURRENT_APPLICATION_VERSION)
                    Logging.LogWarning($"Server game version is {Application.version} and not {Constants.CURRENT_APPLICATION_VERSION} !", ServerConfig);

                _harmony.PatchAll();

                Logging.Log("Setting server sided config.", ServerConfig, true);
                ServerConfig = Configs.ServerConfig.ReadConfig();

                Logging.Log("Subscribing to events.", ServerConfig, true);

                EventManager.AddEventListener(nameof(Event_Everyone_OnClientConnected), Event_Everyone_OnClientConnected);
                EventManager.AddEventListener(nameof(Event_CompetitiveAdjustments_OnArenaSync), Event_CompetitiveAdjustments_OnArenaSync);

                Logging.Log($"Enabled.", ServerConfig, true);

                return true;
            }
            catch (Exception ex) {
                Logging.LogError($"Failed to enable.\n{ex}", ServerConfig);
                return false;
            }
        }

        /// <summary>
        /// Function that launches when the mod is being disabled.
        /// </summary>
        /// <returns>Bool, true if the mod successfully disabled.</returns>
        public bool OnDisable() {
            try {
                Logging.Log($"Disabling...", ServerConfig, true);

                Logging.Log("Unsubscribing from events.", ServerConfig, true);

                EventManager.RemoveEventListener(nameof(Event_Everyone_OnClientConnected), Event_Everyone_OnClientConnected);
                EventManager.RemoveEventListener(nameof(Event_CompetitiveAdjustments_OnArenaSync), Event_CompetitiveAdjustments_OnArenaSync);

                RevertBetterGoalTriggers();

                _harmony.UnpatchSelf();

                Logging.Log($"Disabled.", ServerConfig, true);
                return true;
            }
            catch (Exception ex) {
                Logging.LogError($"Failed to disable.\n{ex}", ServerConfig);
                return false;
            }
        }
    }
}
