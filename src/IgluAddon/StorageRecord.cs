using System;
using System.Text.Json;

namespace BurebistaFishingShelterFeatures
{
    public sealed class StorageRecord
    {
        public int Version { get; set; } = 1;
        public bool Built { get; set; }
        public string Contents { get; set; } = "";
        public string Encode() => JsonSerializer.Serialize(this);
        public static StorageRecord Decode(string value)
        {
            if (string.IsNullOrEmpty(value)) return new StorageRecord();
            var record = JsonSerializer.Deserialize<StorageRecord>(value);
            if (record == null || record.Version != 1 || record.Contents == null
                || (!record.Built && record.Contents.Length > 0) || (record.Built && record.Contents.Length == 0))
                throw new FormatException("Estado del almacen invalido; se conserva sin sobrescribir.");
            return record;
        }
    }
}
