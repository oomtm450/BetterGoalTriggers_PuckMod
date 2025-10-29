using Newtonsoft.Json;
using oomtm450PuckMod_Template.SystemFunc;
using System;
using System.IO;

namespace oomtm450PuckMod_Template.Configs {
    /// <summary>
    /// Class containing the configuration from oomtm450_template_clientconfig.json used for this mod.
    /// </summary>
    public class ClientConfig : IConfig {
        #region Fields and Properties
        /// <summary>
        /// String, full path for the config file.
        /// </summary>
        [JsonIgnore]
        private readonly string _configPath = Path.Combine(Path.GetFullPath("."), Constants.MOD_NAME + "_serverconfig.json");

        /// <summary>
        /// Bool, true if the info logs must be printed.
        /// </summary>
        public bool LogInfo { get; set; } = true;

        /// <summary>
        /// String, name of the mod.
        /// </summary>
        [JsonIgnore]
        public string ModName { get; } = Constants.MOD_NAME;
        #endregion

        /// <summary>
        /// Function that serialize the ClientConfig object.
        /// </summary>
        /// <returns>String, serialized ClientConfig.</returns>
        public override string ToString() {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>
        /// Function that unserialize a ClientConfig.
        /// </summary>
        /// <param name="json">String, JSON that is the serialized ClientConfig.</param>
        /// <returns>ClientConfig, unserialized ClientConfig.</returns>
        internal static ClientConfig SetConfig(string json) {
            return JsonConvert.DeserializeObject<ClientConfig>(json);
        }

        /// <summary>
        /// Function that reads the config file for the mod and create a ClientConfig object with it.
        /// Also creates the file with the default values, if it doesn't exists.
        /// </summary>
        /// <returns>ClientConfig, parsed config.</returns>
        internal static ClientConfig ReadConfig() {
            ClientConfig config = new ClientConfig();

            try {
                if (File.Exists(config._configPath)) {
                    string configFileContent = File.ReadAllText(config._configPath);
                    config = SetConfig(configFileContent);
                    Logging.Log($"Client config read.", config, true);
                }

                try {
                    File.WriteAllText(config._configPath, config.ToString());
                }
                catch (Exception ex) {
                    Logging.LogError($"Can't write the client config file. (Permission error ?)\n{ex}", config);
                }

                Logging.Log($"Wrote client config : {config}", config);
            }
            catch (Exception ex) {
                Logging.LogError($"Can't read the client config file/folder. (Permission error ?)\n{ex}", config);
            }

            return config;
        }
    }
}
