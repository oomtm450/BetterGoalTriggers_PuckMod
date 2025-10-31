using HarmonyLib;
using oomtm450PuckMod_BetterGoalTriggers.Configs;
using oomtm450PuckMod_BetterGoalTriggers.SystemFunc;
using System;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using static UnityEngine.GraphicsBuffer;

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
        /// <summary>
        /// Harmony, harmony instance to patch the Puck's code.
        /// </summary>
        private static readonly Harmony _harmony = new Harmony(Constants.MOD_NAME);

        internal static ServerConfig ServerConfig = new ServerConfig();
        #endregion

        // UIChat.Server_ChatMessageRpc Direct function
        [HarmonyPatch(typeof(UIChat), nameof(UIChat.Server_ChatMessageRpc))]
        public static class UIChat_Server_ChatMessageRpc {
            [HarmonyPrefix]
            public static bool Prefix(string message, RpcParams rpcParams) {
                return ProcessRPCHandlerServerChat(rpcParams.Receive.SenderClientId);
            }
        }

        /// <summary>
        /// Function that launches when the mod is being enabled.
        /// </summary>
        /// <returns>Bool, true if the mod successfully enabled.</returns>
        public bool OnEnable() {
            try {
                Logging.Log($"Enabling...", ServerConfig, true);

                _harmony.PatchAll();

                Logging.Log($"Enabled.", ServerConfig, true);

                Logging.Log("Setting server sided config.", ServerConfig, true);
                ServerConfig = ServerConfig.ReadConfig();

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
