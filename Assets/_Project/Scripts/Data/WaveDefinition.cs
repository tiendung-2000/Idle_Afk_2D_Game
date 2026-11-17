using System;

namespace PixelImmortal.Data
{
    /// <summary>One normal wave of a map. Completion: every group spawned and killed (elites live in the final group).</summary>
    [Serializable]
    public class WaveDefinition
    {
        public int groupCount = 3;
        public int enemiesPerGroup = 3;
        public int eliteCountInFinalGroup = 1;
        /// <summary>Additive difficulty on top of the cycle/map factor.</summary>
        public float difficultyOffset;
        [UnityEngine.Tooltip("Assumed seconds to clear this wave while farming. Offline rewards = wave reward / this value.")]
        public float estimatedClearSeconds = 18f;
    }
}
