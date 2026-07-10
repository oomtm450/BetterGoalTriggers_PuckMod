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
        private const string MOD_VERSION = "1.0.11";
        #endregion

        #region Fields/Properties
        /// <summary>
        /// Harmony, harmony instance to patch the Puck's code.
        /// </summary>
        private static readonly Harmony _harmony = new Harmony(Constants.MOD_NAME);

        internal static Configs.ServerConfig ServerConfig = new Configs.ServerConfig();

        private static bool _triggersHaveBeenBettered = false;

        private static DateTime _lastTime_Server_OnPuckEnterGoal_WasCalled = DateTime.MinValue;
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
            if (_triggersHaveBeenBettered)
                return;

            try {
                GameObject levelObj = GameObject.Find("Level Default");
                for (int i = 0; i < levelObj.transform.childCount; i++) {
                    Transform levelManagerChild = levelObj.transform.GetChild(i);
                    if (levelManagerChild.gameObject.name != "Goal Blue" && levelManagerChild.gameObject.name != "Goal Red")
                        continue;

                    for (int j = 0; j < levelManagerChild.childCount; j++) {
                        Transform goalChild = levelManagerChild.GetChild(j);
                        if (goalChild.gameObject.name == "Goal Trigger") {
                            // Resize current goal trigger.
                            goalChild.localScale = new Vector3(1.0319f, goalChild.localScale.y, 1.0319f);

                            // Add a new goal trigger on the ground.
                            float teamMod = 1f;
                            if (levelManagerChild.gameObject.name == "Goal Red")
                                teamMod = -1f;

                            GameObject groundGoalTrigger = UnityEngine.Object.Instantiate(goalChild.gameObject);
                            groundGoalTrigger.name = "Goal Trigger Ground";
                            groundGoalTrigger.transform.SetParent(goalChild.parent);
                            groundGoalTrigger.transform.position = new Vector3(0, 0, 40.92f * teamMod);
                            if (levelManagerChild.gameObject.name == "Goal Red")
                                groundGoalTrigger.transform.rotation = Quaternion.Euler(90, 180, 0);
                            else
                                groundGoalTrigger.transform.rotation = Quaternion.Euler(90, 0, 0);
                            groundGoalTrigger.transform.localScale = new Vector3(0.873f, 0.712f, 1f);
                            groundGoalTrigger.transform.localPosition = new Vector3(0, -0.6574f, -0.7f);

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
                                meshRenderer.enabled = true;
                            }
                            catch (Exception ex) {
                                Logging.LogError($"2 : {ex}", ServerConfig);
                            }

                            try {
                                MeshCollider meshCollider = groundGoalTrigger.GetComponent<MeshCollider>();
                                if (meshCollider.sharedMesh != null) {
                                    // Duplicate or share the mesh data with the MeshFilter
                                    meshFilter.sharedMesh = meshCollider.sharedMesh;
                                }

                                // 3. Assign a default built-in material so it is visible
                                Shader testShader = Shader.Find("Universal Render Pipeline/Lit");
                                if (testShader == null)
                                    testShader = Shader.Find("HDRP/Lit");
                                meshRenderer.material = new Material(testShader);
                            }
                            catch (Exception ex) {
                                Logging.LogError($"3 : {ex}", ServerConfig);
                            }*/
                            break;
                        }
                    }
                }

                _triggersHaveBeenBettered = true;
            }
            catch (Exception ex) {
                Logging.LogError($"Error in {nameof(Event_Everyone_OnClientConnected)}.\n{ex}", ServerConfig);
            }
        }

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

                EventManager.AddEventListener("Event_Everyone_OnClientConnected", Event_Everyone_OnClientConnected);

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

                EventManager.RemoveEventListener("Event_Everyone_OnClientConnected", Event_Everyone_OnClientConnected);

                _triggersHaveBeenBettered = false;

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
