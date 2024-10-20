using System.Collections;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;

[assembly: MelonInfo(typeof(SimpleMissileMod.Core), "SimpleMissileMod", "0.1.0", "HerrTom", null)]
[assembly: MelonGame("Stonext Games", "Flyout")]

namespace SimpleMissileMod
{
    public class Core : MelonMod
    {
        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("Initialized SimpleMissleMod.");
        }
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName == "PlanetScene2") { MelonCoroutines.Start(WaitForGameObjects()); }
            base.OnSceneWasLoaded(buildIndex, sceneName);
        }
        private IEnumerator WaitForGameObjects()
        {
            Il2Cpp.Craft craft = null;

            // Wait until the object you want is found
            while (craft == null)
            {
                craft = UnityEngine.Object.FindObjectOfType<Il2Cpp.Craft>();

                // Log if still waiting
                if (craft == null)
                    // Yield until next frame to check again
                    yield return null;
            }

            // Once the object is found, execute your code
            MelonLogger.Msg("Craft found! Executing logic...");
            ProcessMissilesInScene();
        }
        private void ProcessMissilesInScene()
        {
            // Find all Missile objects in the scene
            var missiles = UnityEngine.Object.FindObjectsOfType<Il2Cpp.Missile>();

            // If no missiles are found, log and exit the function
            if (missiles == null || missiles.Length == 0)
            {
                MelonLogger.Msg($"No missiles found in scene.");
                return;
            }

            // Iterate through each missile
            foreach (var missile in missiles)
            {
                string missileName = missile.Part.DisplayName;
                MelonLogger.Msg($"Processing missile: {missileName}");

                // Split the missile name to check prefix and get the specific missile identifier
                var parts = missileName.Split('.');

                // Construct the configuration file path
                string cfgFile = parts[1] + ".cfg";
                string cfgPath = Path.Combine(MelonEnvironment.ModsDirectory, "SimpleMissileMod", cfgFile);

                if (!File.Exists(cfgPath))
                {
                    MelonLogger.Error($"Failed to load config for missile '{missileName}'. File not found: {cfgPath}");
                    continue;
                }

                try
                {
                    // Read all lines from the config file
                    var lines = File.ReadAllLines(cfgPath);
                    var config = new MissileConfig();

                    // Parse each line and assign to the config object
                    foreach (var line in lines)
                    {
                        // Ignore empty lines and comments
                        if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                            continue;

                        var keyValue = line.Split(new char[] { '=' }, 2);
                        if (keyValue.Length != 2)
                            continue;

                        string key = keyValue[0].Trim();
                        string value = keyValue[1].Trim();

                        switch (key.ToLower())
                        {
                            case "burntime":
                                config.BurnTime = float.Parse(value);
                                break;
                            case "turnrate":
                                config.TurnRate = float.Parse(value);
                                break;
                            case "thrust":
                                config.Thrust = float.Parse(value);
                                break;
                            case "guidancetime":
                                config.GuidanceTime = float.Parse(value);
                                break;
                            case "fuel":
                                config.Fuel = float.Parse(value);
                                break;
                            case "explosionscale":
                                config.ExplosionScale = float.Parse(value);
                                break;
                            case "explosionradius":
                                config.ExplosionRadius = float.Parse(value);
                                break;
                            case "dragcoeffs":
                                // Expecting format: a,b,c
                                var dragValues = value.Split(',');
                                if (dragValues.Length == 3)
                                {
                                    config.DragCoeffs = new Vector3(
                                        float.Parse(dragValues[0]),
                                        float.Parse(dragValues[1]),
                                        float.Parse(dragValues[2])
                                    );
                                }
                                else
                                {
                                    MelonLogger.Warning($"Invalid format for 'dragCoeffs' in file '{cfgPath}'. Expected 'a,b,c'.");
                                }
                                break;
                            case "gimbalrange":
                                config.GimbalRange = float.Parse(value);
                                break;
                            case "lockthreshold":
                                config.LockThreshold = float.Parse(value);
                                break;
                            case "signalstrength":
                                config.SignalStrength = float.Parse(value);
                                break;
                            default:
                                MelonLogger.Warning($"Unknown config key '{key}' in file '{cfgPath}'.");
                                break;
                        }
                    }

                    // Apply the config to the missile
                    ApplyConfigToMissile(missile, config);
                    MelonLogger.Msg($"Successfully applied config to missile '{missileName}'.");
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"Error processing config file '{cfgPath}': {ex.Message}");
                }
            }
        }
        private void ApplyConfigToMissile(Il2Cpp.Missile missile, MissileConfig config)
        {
            // Apply missile parameters
            missile.burnTime = config.BurnTime;
            missile.turnRate = config.TurnRate;
            missile.thrust = config.Thrust;
            missile.guidanceTime = config.GuidanceTime;
            missile.fuel = config.Fuel;
            missile.explosionScale = config.ExplosionScale;
            missile.radius = config.ExplosionRadius;
            missile.dragCoeffs = config.DragCoeffs;

            // Apply seeker parameters if the seeker exists
            var part = missile.gameObject;
            var seeker = part.GetComponent<Il2Cpp.IRSeeker>();
            if (seeker != null)
            {
                seeker.gimbalRange = config.GimbalRange;
                seeker.lockThreshold = config.LockThreshold;
                seeker.signalStrength = config.SignalStrength;
            }
            else
            {
                MelonLogger.Warning($"Missile '{missile.Part.DisplayName}' does not have a Seeker component.");
            }
        }
        private class MissileConfig
        {
            // Missile parameters
            public float BurnTime { get; set; } = 0f;
            public float TurnRate { get; set; } = 0f;
            public float Thrust { get; set; } = 0f;
            public float GuidanceTime { get; set; } = 0f;
            public float Fuel { get; set; } = 0f;
            public float ExplosionScale { get; set; } = 0f;
            public float ExplosionRadius { get; set; } = 0f;
            public Vector3 DragCoeffs { get; set; } = Vector3.zero;

            // Seeker parameters
            public float GimbalRange { get; set; } = 0f;
            public float LockThreshold { get; set; } = 0f;
            public float SignalStrength { get; set; } = 0f;
        }
    }
}