using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ProjectGatefront.Core
{
    public static class GameLog
    {
        private const string Prefix = "[Project Gatefront]";

        public static bool VerboseEnabled { get; set; } = Debug.isDebugBuild;

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void Verbose(string message, Object context = null)
        {
            if (VerboseEnabled)
            {
                Debug.Log($"{Prefix} {message}", context);
            }
        }

        public static void Warning(string message, Object context = null)
        {
            Debug.LogWarning($"{Prefix} {message}", context);
        }

        public static void Error(string message, Object context = null)
        {
            Debug.LogError($"{Prefix} {message}", context);
        }
    }
}
