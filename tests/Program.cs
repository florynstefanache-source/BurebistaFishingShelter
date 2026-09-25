using System;
using System.Globalization;
using BurebistaFishingShelterFeatures;

class Program
{
    static int checks;
    static void Check(bool value, string name) { if (!value) throw new Exception(name); checks++; Console.WriteLine("PASS " + name); }
    static void Invalid(string text, string name)
    {
        try { ShelterState.Decode(text, "slotA", "lake"); }
        catch (Exception) { Check(true, name); return; }
        throw new Exception("Accepted invalid data: " + name);
    }
    static void Main(string[] args)
    {
        const string legacy = "slotA|lake|1.25|0|-3.5|0|0|0|1|0.08|bGVnYWN5";
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("es-ES");
        ShelterState a = ShelterState.Decode(legacy, "slotA", "lake");
        Check(a != null && !a.Workbench && !a.Rack, "v1 migration grants no free upgrades");
        Check(a.Position[0] == 1.25f && a.Scale == .08f, "invariant coordinates under Spanish locale");
        Check(a.LegacyContainer == "bGVnYWN5", "preserve old container payload without discarding it");
        Check(ShelterState.Decode(legacy, "slotB", "lake") == null, "isolate save slots");
        Check(ShelterState.Decode(legacy, "slotA", "coast") == null, "isolate scenes");
        foreach (bool bench in new[]{false,true}) foreach (bool rack in new[]{false,true})
        {
            a.Workbench = bench; a.Rack = rack; a.Variant = 2; a.ClosedDoor = false;
            ShelterState b = ShelterState.Decode(a.Encode(), "slotA", "lake");
            Check(b.Workbench == bench && b.Rack == rack && b.Variant == 2 && !b.ClosedDoor,
                "round trip independent upgrades " + bench + "/" + rack + " and variant/door");
        }
        a.Removed = true;
        Check(ShelterState.Decode(a.Encode(), "slotA", "lake").Removed, "removal tombstone prevents legacy resurrection");
        Invalid("slotA|lake|NaN|0|0|0|0|0|1|0.08|", "reject NaN");
        Invalid("slotA|lake|0|0|0|0|0|0|0|0.08|", "reject zero quaternion");
        Invalid("slotA|lake|0|0|0|0|0|0|1|0|", "reject zero scale");
        Invalid("slotA|lake|0|0|0|0|0|0|1|9|", "reject oversized scale");
        Invalid("slotA|lake|truncated", "reject truncated legacy state");
        Invalid("{\"Version\":3}", "reject unknown future version");
        a.Removed = false; a.Variant = 99;
        Invalid(a.Encode(), "reject invalid variant");
        var gate = new RestoreGate();
        Check(!gate.Observe(null, "Empty", false, 0), "empty screen does not trigger restore");
        // The active region remains ModMountainPass while these additive scenes load.
        foreach (string loaded in new[]{"ModMountainPass", "ModMountainPass_GEAR", "ModMountainPass_EXTRA", "ModMountainPass_WILDLIFE"})
            Check(!gate.Observe("sandbox1", "ModMountainPass", false, 1), "wait through additive load " + loaded);
        Check(!gate.Observe("sandbox1", "ModMountainPass", true, 10), "wait for stable native readiness");
        Check(gate.Observe("sandbox1", "ModMountainPass", true, 12), "restore primary region after all additive loads");
        Check(!gate.Observe("sandbox1", "ModMountainPass", true, 15), "restore only once in same session");
        gate.Observe("sandbox1", "ModMountainPass", false, 16);
        Check(!gate.Observe("sandbox1", "ModMountainPass", true, 20), "pause does not restore twice");
        gate.Reset();
        gate.Observe("sandbox1", "ModMountainPass", true, 30);
        Check(gate.Observe("sandbox1", "ModMountainPass", true, 32), "same save and region restore after menu reload");
        gate.Observe("sandbox1", "CoastalRegion", true, 40);
        Check(gate.Observe("sandbox1", "CoastalRegion", true, 42), "region change restores separately");
        gate.Observe("sandbox2", "CoastalRegion", true, 50);
        Check(gate.Observe("sandbox2", "CoastalRegion", true, 52), "save change restores separately");
        gate.Reset();
        gate.Observe("sandbox1", "ModMountainPass", true, 60);
        gate.Observe("sandbox1", "ModMountainPass", false, 61);
        Check(!gate.Observe("sandbox1", "ModMountainPass", true, 62), "interrupted initialization restarts wait");
        Check(gate.Observe("sandbox1", "ModMountainPass", true, 64), "slow load eventually restores");
        ShelterState recovery = ShelterState.Decode("sandbox1|ModMountainPass|1821.9154|44.299995|1310.7635|0|0.9381937|0|-0.34611076|0.38|", "sandbox1", "ModMountainPass");
        Check(recovery != null && recovery.Scale == .38f && recovery.Position[0] == 1821.9154f, "affected user's legacy shelter remains readable at original scale/location");
        foreach (string name in new[]{"GEAR_LeatherHide", "GEAR_WolfPelt(Clone)", "GEAR_Gut"})
            Check(RackPlacementRules.Kind(name) == 1, "accept fresh hide/gut: " + name);
        foreach (string name in new[]{"GEAR_SaplingBirch", "GEAR_SaplingMaple(Clone)"})
            Check(RackPlacementRules.Kind(name) == 2, "accept sapling: " + name);
        foreach (string name in new[]{"GEAR_GutDried", "GEAR_SaplingBirchDried", "GEAR_Stick", "GEAR_Cloth", ""})
            Check(RackPlacementRules.Kind(name) == 0, "exclude cured/unrelated gear: " + name);
        bool[] occupied = new bool[6];
        for (int i=0;i<4;i++) { int slot=RackPlacementRules.FreeSlot(1,occupied); Check(slot==i,"allocate hide slot " + i); occupied[slot]=true; }
        Check(RackPlacementRules.FreeSlot(1,occupied)==-1,"hide slots full preserve sapling slots");
        Check(RackPlacementRules.FreeSlot(2,occupied)==4,"sapling slot available independently");
        occupied[4]=occupied[5]=true;
        Check(RackPlacementRules.FreeSlot(2,occupied)==-1,"full sapling slots reject additional item");
        occupied[2]=false;
        Check(RackPlacementRules.FreeSlot(1,occupied)==2,"reuse picked-up item slot");
        Check(RackPlacementRules.FreeSlot(0,occupied)==-1,"ineligible item never assigned");
        Check(!StorageRecord.Decode(null).Built, "old saves grant no free storage");
        Check(!StorageRecord.Decode(new StorageRecord().Encode()).Built, "empty removal record round trip");
        string containerPayload = "{\"items\":[\"food\",\"tool\"],\"condition\":42.5}";
        var stored = StorageRecord.Decode(new StorageRecord { Built=true, Contents=containerPayload }.Encode());
        Check(stored.Built && stored.Contents==containerPayload, "preserve opaque native contents exactly");
        foreach(string bad in new[]{"{", "null", "{\"Version\":99}", "{\"Built\":true}", "{\"Contents\":null}", "{\"Built\":false,\"Contents\":\"items\"}"})
        {
            bool rejected=false;
            try { StorageRecord.Decode(bad); } catch { rejected=true; }
            Check(rejected,"reject corrupt/incompatible storage without replacing with empty");
        }
        Console.WriteLine(checks + " checks passed.");
        if (args.Length == 2) LoaderSmoke.Run(args[0], args[1]);
    }
}
