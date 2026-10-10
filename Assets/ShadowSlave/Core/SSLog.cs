using UnityEngine;

namespace ShadowSlave.Core
{
    /// <summary>
    /// Thin Debug.Log wrapper with Shadow Slave log categories (UE LogShadowSlave channels).
    /// </summary>
    public static class SSLog
    {
        public const string CategoryCore = "ShadowSlave.Core";
        public const string CategoryAttributes = "ShadowSlave.Attributes";
        public const string CategoryCharacters = "ShadowSlave.Characters";
        public const string CategoryCombat = "ShadowSlave.Combat";
        public const string CategoryAspects = "ShadowSlave.Aspects";
        public const string CategoryStatusEffects = "ShadowSlave.StatusEffects";
        public const string CategoryItems = "ShadowSlave.Items";
        public const string CategoryEquipment = "ShadowSlave.Equipment";

        public static void Log(string category, string message)
        {
            Debug.Log($"[{category}] {message}");
        }

        public static void Warning(string category, string message)
        {
            Debug.LogWarning($"[{category}] {message}");
        }

        public static void Error(string category, string message)
        {
            Debug.LogError($"[{category}] {message}");
        }

        public static void LogAttributes(string message) => Log(CategoryAttributes, message);
        public static void LogCharacters(string message) => Log(CategoryCharacters, message);
        public static void LogCombat(string message) => Log(CategoryCombat, message);
        public static void LogAspects(string message) => Log(CategoryAspects, message);
        public static void LogStatusEffects(string message) => Log(CategoryStatusEffects, message);
        public static void LogItems(string message) => Log(CategoryItems, message);
        public static void LogEquipment(string message) => Log(CategoryEquipment, message);
    }
}
