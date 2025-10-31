using oomtm450PuckMod_BetterGoalTriggers.Configs;
using oomtm450PuckMod_BetterGoalTriggers.SystemFunc;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace oomtm450PuckMod_BetterGoalTriggers {
    /// <summary>
    /// Class containing the main code for the BetterGoalTriggers patch.
    /// </summary>
    public class BetterGoalTriggers : IPuckMod {
        #region Constants
        /// <summary>
        /// Const string, version of the mod.
        /// </summary>
        private const string MOD_VERSION = "1.0.0DEV";
        #endregion

        #region Fields/Properties
        /*/// <summary>
        /// Harmony, harmony instance to patch the Puck's code.
        /// </summary>
        private static readonly Harmony _harmony = new Harmony(Constants.MOD_NAME);*/

        internal static ServerConfig ServerConfig = new ServerConfig();
        #endregion

        /// <summary>
        /// Method called when a scene has being loaded by Unity.
        /// Used to load assets.
        /// </summary>
        /// <param name="message">Dictionary of string and object, content of the event.</param>
        public static void Event_OnSceneLoaded(Dictionary<string, object> message) {
            try {
                // If this is the server, do not use the patch.
                if (!ServerFunc.IsDedicatedServer())
                    return;

                Logging.Log("Event_OnSceneLoaded", ServerConfig, true); // TODO : Remove debug logs.

                Scene scene = (Scene)message["scene"];
                Logging.Log("scene.buildIndex : " + scene.buildIndex, ServerConfig, true); // TODO : Remove debug logs.
                if (scene == null || scene.buildIndex != 2)
                    return;

            }
            catch (Exception ex) {
                Logging.LogError($"Error in {nameof(Event_OnSceneLoaded)}.\n{ex}", ServerConfig);
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
                    EventManager.Instance.AddEventListener("Event_OnSceneLoaded", Event_OnSceneLoaded);
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
                    EventManager.Instance.RemoveEventListener("Event_OnSceneLoaded", Event_OnSceneLoaded);
                }

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
