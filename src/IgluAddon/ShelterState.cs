using System;
using System.Globalization;
using System.Text.Json;

namespace BurebistaFishingShelterFeatures
{
    // No Unity dependency: validated independently in tests/StateTests.
    public sealed class ShelterState
    {
        public int Version { get; set; } = 2;
        public string Save { get; set; }
        public string Scene { get; set; }
        public float[] Position { get; set; }
        public float[] Rotation { get; set; }
        public float Scale { get; set; } = .08f;
        public int Variant { get; set; } = 1;
        public bool ClosedDoor { get; set; } = true;
        public bool Workbench { get; set; }
        public bool Rack { get; set; }
        public bool Removed { get; set; }
        public string LegacyContainer { get; set; } = "";

        public string Encode() => JsonSerializer.Serialize(this);
        public static ShelterState Decode(string text, string save, string scene)
        {
            ShelterState state;
            if (text.TrimStart().StartsWith("{")) state = JsonSerializer.Deserialize<ShelterState>(text);
            else
            {
                string[] p = text.Split('|');
                if (p.Length != 11) throw new FormatException("Estado v1 invalido");
                state = new ShelterState {
                    Save = p[0], Scene = p[1],
                    Position = new[] { Parse(p[2]), Parse(p[3]), Parse(p[4]) },
                    Rotation = new[] { Parse(p[5]), Parse(p[6]), Parse(p[7]), Parse(p[8]) },
                    Scale = Parse(p[9]), LegacyContainer = p[10]
                };
            }
            if (state == null || state.Version != 2) throw new FormatException("Version de estado desconocida");
            if (state.Save != save || state.Scene != scene) return null;
            if (state.Removed) return state;
            if (!FiniteArray(state.Position, 3) || !FiniteArray(state.Rotation, 4) ||
                !float.IsFinite(state.Scale) || state.Scale < .05f || state.Scale > 1f ||
                (state.Variant != 1 && state.Variant != 2)) throw new FormatException("Transformacion de refugio invalida");
            float q = 0; foreach (float v in state.Rotation) q += v * v;
            if (q < .5f || q > 1.5f) throw new FormatException("Rotacion de refugio invalida");
            return state;
        }
        private static float Parse(string text) => float.Parse(text, CultureInfo.InvariantCulture);
        private static bool FiniteArray(float[] a, int length)
        {
            if (a == null || a.Length != length) return false;
            foreach (float v in a) if (!float.IsFinite(v)) return false;
            return true;
        }
    }
}





