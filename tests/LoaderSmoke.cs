using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

static class LoaderSmoke
{
    public static void Run(string dll, string game)
    {
        AssemblyLoadContext.Default.Resolving += (context, name) =>
        {
            foreach (string dir in new[]{"MelonLoader/Il2CppAssemblies", "MelonLoader/net6", "Mods"})
            {
                string path = Path.Combine(game, dir, name.Name + ".dll");
                if (File.Exists(path)) return context.LoadFromAssemblyPath(Path.GetFullPath(path));
            }
            return null;
        };
        Assembly assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(dll));
        Type ctx = assembly.GetType("BurebistaFishingShelter.B3D+Ctx", true);
        object state = Activator.CreateInstance(ctx, true); // Exact constructor in the reported failure.
        foreach (string field in new[]{"tex", "brushes"})
        {
            object value = ctx.GetField(field).GetValue(state);
            if (value.GetType().GetGenericTypeDefinition() != typeof(System.Collections.Generic.List<>))
                throw new Exception("Native list remains in " + field);
        }
        Console.WriteLine("PASS actual compiled B3D.Ctx constructor, without IL2CPP initialization");
        Type readerType = assembly.GetType("BurebistaFishingShelter.B3D+Reader", true);
        string temporary = Path.Combine(Path.GetTempPath(), "burebista-reader-" + Guid.NewGuid().ToString("N") + ".bin");
        try
        {
            File.WriteAllBytes(temporary, System.Text.Encoding.UTF8.GetBytes("snow03.jpg\0piel-á\0"));
            using (var reader = (IDisposable)Activator.CreateInstance(readerType, new object[]{temporary}))
            {
                MethodInfo str = readerType.GetMethod("Str");
                if ((string)str.Invoke(reader, null) != "snow03.jpg" || (string)str.Invoke(reader, null) != "piel-á")
                    throw new Exception("B3D string decoding failed");
            }
            Console.WriteLine("PASS actual compiled B3D.Reader.Str ASCII/UTF8, without IL2CPP initialization");
        }
        finally { File.Delete(temporary); }
    }
}
