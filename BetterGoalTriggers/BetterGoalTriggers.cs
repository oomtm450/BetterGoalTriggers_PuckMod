using oomtm450PuckMod_BetterGoalTriggers.Configs;
using oomtm450PuckMod_BetterGoalTriggers.SystemFunc;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace oomtm450PuckMod_BetterGoalTriggers {
    /// <summary>
    /// Class containing the main code for the BetterGoalTriggers patch.
    /// </summary>
    public class BetterGoalTriggers : IPuckMod {
        #region Constants
        /// <summary>
        /// Const string, version of the mod.
        /// </summary>
        private const string MOD_VERSION = "1.0.1";
        #endregion

        #region Fields/Properties
        /*/// <summary>
        /// Harmony, harmony instance to patch the Puck's code.
        /// </summary>
        private static readonly Harmony _harmony = new Harmony(Constants.MOD_NAME);*/

        internal static ServerConfig ServerConfig = new ServerConfig();

        private static bool _triggersHaveBeenBettered = false;
        #endregion

        /// <summary>
        /// Method called when a client has connected (joined a server) on the server-side.
        /// Used to set server-sided stuff after the game has loaded.
        /// </summary>
        /// <param name="message">Dictionary of string and object, content of the event.</param>
        public static void Event_OnClientConnected(Dictionary<string, object> message) {
            if (!ServerFunc.IsDedicatedServer())
                return;

            if (_triggersHaveBeenBettered)
                return;

            try {
                for (int i = 0; i < LevelManager.Instance.gameObject.transform.childCount; i++) {
                    Transform levelManagerChild = LevelManager.Instance.gameObject.transform.GetChild(i);
                    if (levelManagerChild.gameObject.name != "Goal Blue" && levelManagerChild.gameObject.name != "Goal Red")
                        continue;

                    for (int j = 0; j < levelManagerChild.childCount; j++) {
                        Transform goalChild = levelManagerChild.GetChild(j);
                        if (goalChild.gameObject.name == "Goal Trigger") {
                            // Resize current goal trigger.
                            goalChild.localScale = new Vector3(goalChild.localScale.x + 0.02f, goalChild.localScale.y, goalChild.localScale.z + 0.02f);

                            // Add a new goal trigger on the ground.
                            float teamMod = 1f;
                            if (levelManagerChild.gameObject.name == "Goal Red")
                                teamMod = -1f;

                            GameObject groundGoalTrigger = UnityEngine.Object.Instantiate(goalChild.gameObject);
                            groundGoalTrigger.name = "Goal Trigger Ground";
                            groundGoalTrigger.transform.SetParent(goalChild.parent);
                            groundGoalTrigger.transform.position = new Vector3(0, 0, 40.92f * teamMod);
                            groundGoalTrigger.transform.rotation = Quaternion.Euler(0, 0, 0);
                            groundGoalTrigger.transform.localScale = new Vector3(0.86f, 1f, 0.73f);
                            groundGoalTrigger.transform.localPosition = new Vector3(0, -0.6574f, -0.5833f * teamMod);
                            break;
                        }
                    }
                }

                _triggersHaveBeenBettered = true;
            }
            catch (Exception ex) {
                Logging.LogError($"Error in {nameof(Event_OnClientConnected)}.\n{ex}", ServerConfig);
            }
        }

        /// <summary>
        /// Function that launches when the mod is being enabled.
        /// </summary>
        /// <returns>Bool, true if the mod successfully enabled.</returns>
        public bool OnEnable() {
            try {
                Logging.Log($"Enabling...", ServerConfig, true);

                //_harmony.PatchAll();

                Logging.Log("Setting server sided config.", ServerConfig, true);
                ServerConfig = ServerConfig.ReadConfig();

                Logging.Log("Subscribing to events.", ServerConfig, true);

                if (ServerFunc.IsDedicatedServer()) {
                    EventManager.Instance.AddEventListener("Event_OnClientConnected", Event_OnClientConnected);
                }

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

                if (ServerFunc.IsDedicatedServer()) {
                    EventManager.Instance.RemoveEventListener("Event_OnClientConnected", Event_OnClientConnected);
                }

                _triggersHaveBeenBettered = false;

                //_harmony.UnpatchSelf();

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
