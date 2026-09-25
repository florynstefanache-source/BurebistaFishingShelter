namespace BurebistaFishingShelterFeatures
{
    // Readiness is observed from the active gameplay session, not additive scene callbacks.
    public sealed class RestoreGate
    {
        private string save, scene;
        private float readySince = -1;
        private bool attempted;
        public bool Attempted => attempted;
        public void Reset() { save = null; scene = null; readySince = -1; attempted = false; }
        public bool Observe(string currentSave, string currentScene, bool ready, float now)
        {
            if (string.IsNullOrEmpty(currentSave) || string.IsNullOrEmpty(currentScene)) { Reset(); return false; }
            if (save != currentSave || scene != currentScene)
            {
                Reset(); save = currentSave; scene = currentScene;
            }
            if (attempted) return false;
            if (!ready) { readySince = -1; return false; }
            if (readySince < 0) { readySince = now; return false; }
            if (now - readySince < 2f) return false;
            attempted = true;
            return true;
        }
    }
}





