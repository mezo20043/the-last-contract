using UnityEngine;

namespace TLC.Core
{
    /// <summary>
    /// وصول عام لحالة اللعبة من أي سكربت في العالم (hotspots، أزرار، أنظمة صوت)
    /// بدون ربط مراجع يدوي في الـ Inspector.
    /// </summary>
    public static class GameStateBus
    {
        public static GameState Current { get; private set; }

        public static void Register(GameState state) => Current = state;
        public static void Unregister(GameState state) { if (Current == state) Current = null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay() => Current = null;
    }
}
