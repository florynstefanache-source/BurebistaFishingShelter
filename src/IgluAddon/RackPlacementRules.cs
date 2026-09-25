using System;

namespace BurebistaFishingShelterFeatures
{
    internal static class RackPlacementRules
    {
        // Require the native curing component separately; these names only divide slots.
        internal static int Kind(string name)
        {
            if (string.IsNullOrEmpty(name) || name.IndexOf("Dried", StringComparison.OrdinalIgnoreCase) >= 0) return 0;
            if (name.IndexOf("Sapling", StringComparison.OrdinalIgnoreCase) >= 0) return 2;
            return name.IndexOf("Hide", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Pelt", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Gut", StringComparison.OrdinalIgnoreCase) >= 0 ? 1 : 0;
        }
        internal static int FreeSlot(int kind, bool[] occupied)
        {
            if (kind != 1 && kind != 2) return -1;
            for (int i = kind == 2 ? 4 : 0; i < (kind == 2 ? 6 : 4); i++)
                if (!occupied[i]) return i;
            return -1;
        }
    }
}
