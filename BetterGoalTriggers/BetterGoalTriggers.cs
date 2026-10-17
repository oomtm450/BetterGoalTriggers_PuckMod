using HarmonyLib;
using oomtm450PuckMod_BetterGoalTriggers.SystemFunc;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        private const string MOD_VERSION = "1.2.0";

        private const float BASE_GAME_GOAL_TRIGGER_X_SCALE = 1.08f;
        private const float BASE_GAME_GOAL_TRIGGER_Y_SCALE = 1.08f;
        private const float BASE_GAME_GOAL_TRIGGER_Z_SCALE = 0.96f;

        private const float BASE_GAME_GOAL_TRIGGER_X_POSITION = 0;
        private const float BASE_GAME_GOAL_TRIGGER_Y_POSITION = 0;
        private const float BASE_GAME_GOAL_TRIGGER_Z_POSITION = 0;

        private const float OFFSET_Z_FOR_EACH_M05_PUCK_SCALE = -0.0225f;
        #endregion

        #region Fields/Properties
        /// <summary>
        /// Harmony, harmony instance to patch the Puck's code.
        /// </summary>
        private static readonly Harmony _harmony = new Harmony(Constants.MOD_NAME);

        internal static Configs.ServerConfig ServerConfig = new Configs.ServerConfig();

        private static bool _triggersHaveBeenBettered = false;

        private static Vector3 _originalGoalTriggerScale = new Vector3(BASE_GAME_GOAL_TRIGGER_X_SCALE, BASE_GAME_GOAL_TRIGGER_Y_SCALE, BASE_GAME_GOAL_TRIGGER_Z_SCALE);

        private static Vector3 _originalGoalTriggerPosition = new Vector3(BASE_GAME_GOAL_TRIGGER_X_POSITION, BASE_GAME_GOAL_TRIGGER_Y_POSITION, BASE_GAME_GOAL_TRIGGER_Z_POSITION);

        private static Vector3 _scale = new Vector3(1.0319f, 1f, 1.0319f);

        private static Vector3 _offset = new Vector3(0, 0, 0);

        private static Vector3 _scaleGroundTrigger = Vector3.one;

        private static Vector3 _offsetGroundTrigger = new Vector3(0, -0.6574f, -0.73f);

        private static Vector3 _offsetGroundTriggerPuckScale = new Vector3(0, 0, 0);

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
                Puck puck = PuckManager.Instance.GetPuck();
                if (puck == null || !puck)
                    _offsetGroundTriggerPuckScale = Vector3.zero;
                else
                    _offsetGroundTriggerPuckScale = new Vector3(0, 0, ((1 - ((puck.transform.localScale.x + puck.transform.localScale.z) / 2f)) * 20f) * OFFSET_Z_FOR_EACH_M05_PUCK_SCALE);

                Transform goalTrigger = goal.transform.Find("Goal Trigger");

                // Resize current goal trigger.
                _originalGoalTriggerScale = goalTrigger.localScale;
                goalTrigger.localScale = new Vector3(goalTrigger.localScale.x * _scale.x, goalTrigger.localScale.y * _scale.y, goalTrigger.localScale.z * _scale.z);
                _originalGoalTriggerPosition = goalTrigger.localPosition;
                goalTrigger.localPosition = new Vector3(goalTrigger.localPosition.x + _offset.x, goalTrigger.localPosition.y + _offset.y, goalTrigger.localPosition.z + _offset.z);

                // Add a new goal trigger on the ground.
                GameObject groundGoalTrigger = UnityEngine.Object.Instantiate(goalTrigger.gameObject);
                groundGoalTrigger.name = "Goal Trigger Ground";
                groundGoalTrigger.transform.SetParent(goalTrigger.parent);
                groundGoalTrigger.transform.position = new Vector3(goalTrigger.transform.position.x, goalTrigger.transform.position.y + 1.45f, goalTrigger.transform.position.z);
                if (goal.name == "Goal Red")
                    groundGoalTrigger.transform.rotation = Quaternion.Euler(90, 180, 0);
                else
                    groundGoalTrigger.transform.rotation = Quaternion.Euler(90, 0, 0);
                groundGoalTrigger.transform.localScale = new Vector3((((_originalGoalTriggerScale.x - BASE_GAME_GOAL_TRIGGER_X_SCALE) * 0.873f) + 0.873f) * _scaleGroundTrigger.x, (((_originalGoalTriggerScale.y - BASE_GAME_GOAL_TRIGGER_Y_SCALE) * 0.712f) + 0.712f) * _scaleGroundTrigger.y, (((_originalGoalTriggerScale.z - BASE_GAME_GOAL_TRIGGER_Z_SCALE) * 1f) + 1f) * _scaleGroundTrigger.z);
                groundGoalTrigger.transform.localPosition = new Vector3(_offsetGroundTrigger.x + _offsetGroundTriggerPuckScale.x, _offsetGroundTrigger.y + _offsetGroundTriggerPuckScale.y, _offsetGroundTrigger.z + _offsetGroundTriggerPuckScale.z);

                _groundGoalTriggers.Add(groundGoalTrigger);

                /*// Add visual for ground trigger.
                MeshFilter meshFilter = null;
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
                }

                // Add visual for normal goal trigger.
                meshFilter = null;
                try {
                    meshFilter = goalTrigger.gameObject.GetComponent<MeshFilter>();
                    if (meshFilter == null)
                        meshFilter = goalTrigger.gameObject.AddComponent<MeshFilter>();
                }
                catch (Exception ex) {
                    Logging.LogError($"1 : {ex}", ServerConfig);
                }

                meshRenderer = null;
                try {
                    meshRenderer = goalTrigger.gameObject.GetComponent<MeshRenderer>();
                    if (meshRenderer == null)
                        meshRenderer = goalTrigger.gameObject.AddComponent<MeshRenderer>();
                }
                catch (Exception ex) {
                    Logging.LogError($"2 : {ex}", ServerConfig);
                }

                try {
                    MeshCollider meshCollider = goalTrigger.gameObject.GetComponent<MeshCollider>();
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
                    goalTrigger.localPosition = _originalGoalTriggerPosition;
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
        /// Class that patches the Client_SendChatMessage event from ChatManager.
        /// </summary>
        [HarmonyPatch(typeof(ChatManager), nameof(ChatManager.Client_SendChatMessage))]
        public class ChatManager_Client_SendChatMessage_Patch {
            [HarmonyPostfix]
            public static void Postfix(string content, bool isQuickChat, bool isTeamChat) {
                try {
                    if (content.StartsWith(@"/scalex")) {
                        content = content.Replace(@"/scalex", "").Trim();

                        if (!float.TryParse(content, NumberStyles.None, CultureInfo.InvariantCulture, out float x))
                            return;

                        _scale = new Vector3(x, _scale.y, _scale.z);
                        _scaleGroundTrigger = new Vector3(x, _scaleGroundTrigger.y, _scaleGroundTrigger.z);
                    }
                    else if (content.StartsWith(@"/scaley")) {
                        content = content.Replace(@"/scaley", "").Trim();

                        if (!float.TryParse(content, NumberStyles.None, CultureInfo.InvariantCulture, out float y))
                            return;

                        _scale = new Vector3(_scale.x, y, _scale.z);
                        _scaleGroundTrigger = new Vector3(_scaleGroundTrigger.x, y, _scaleGroundTrigger.z);
                    }
                    else if (content.StartsWith(@"/scalez")) {
                        content = content.Replace(@"/scalez", "").Trim();

                        if (!float.TryParse(content, NumberStyles.None, CultureInfo.InvariantCulture, out float z))
                            return;

                        _scale = new Vector3(_scale.x, _scale.y, z);
                        _scaleGroundTrigger = new Vector3(_scaleGroundTrigger.x, _scaleGroundTrigger.y, z);
                    }
                    else if (content.StartsWith(@"/offsetx")) {
                        content = content.Replace(@"/offsetx", "").Trim();

                        if (!float.TryParse(content, NumberStyles.None, CultureInfo.InvariantCulture, out float x))
                            return;

                        _offset = new Vector3(x, _offset.y, _offset.z);
                        _offsetGroundTrigger = new Vector3(x, _offsetGroundTrigger.y, _offsetGroundTrigger.z);
                    }
                    else if (content.StartsWith(@"/offsety")) {
                        content = content.Replace(@"/offsety", "").Trim();

                        if (!float.TryParse(content, NumberStyles.None, CultureInfo.InvariantCulture, out float y))
                            return;

                        _offset = new Vector3(_offset.x, y, _offset.z);
                        _offsetGroundTrigger = new Vector3(_offsetGroundTrigger.x, y, _offsetGroundTrigger.z);
                    }
                    else if (content.StartsWith(@"/offsetz")) {
                        content = content.Replace(@"/offsetz", "").Trim();

                        if (!float.TryParse(content, NumberStyles.None, CultureInfo.InvariantCulture, out float z))
                            return;

                        _offset = new Vector3(_offset.x, _offset.y, z);
                        _offsetGroundTrigger = new Vector3(_offsetGroundTrigger.x, _offsetGroundTrigger.y, z);
                    }

                    RevertBetterGoalTriggers();
                    BetterGoalTriggersFunc();
                }
                catch (Exception ex) {
                    Logging.LogError($"Error in {nameof(ChatManager_Client_SendChatMessage_Patch)} Postfix().\n{ex}", ServerConfig);
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
