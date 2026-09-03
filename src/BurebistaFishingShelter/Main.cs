using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text;
using BurebistaFishingShelter;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Collections.Generic;
using MelonLoader;
using Microsoft.CodeAnalysis;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: MelonInfo(typeof(BurebistaFishingShelter.Main), "Burebista Fishing Shelter", "0.3", "Burebista", null)]
[assembly: MelonGame("Hinterland", "TheLongDark")]
[assembly: TargetFramework(".NETCoreApp,Version=v6.0", FrameworkDisplayName = ".NET 6.0")]
[assembly: AssemblyVersion("0.0.0.0")]
namespace Microsoft.CodeAnalysis
{
	[CompilerGenerated]
	[Microsoft.CodeAnalysis.Embedded]
	internal sealed class EmbeddedAttribute : Attribute
	{
	}
}
namespace BurebistaFishingShelter
{
	public class Main : MelonMod
	{
		private static GameObject shelterRoot;

		private static GameObject doorRoot;

		private static bool closedDoor = true;

		private static int variant = 1;

		private static float shelterScale = 0.08f;

		private static string assetDir;

		private static GUIStyle promptStyle;

		private static readonly System.Collections.Generic.Dictionary<string, Type> typeCache = new System.Collections.Generic.Dictionary<string, Type>(StringComparer.Ordinal);

		public override void OnInitializeMelon()
		{
			assetDir = Path.Combine(AppContext.BaseDirectory, "Mods", "BurebistaFishingShelter", "iceland");
			((MelonBase)this).LoggerInstance.Msg("Burebista Fishing Shelter v0.8 TRUE OPAQUE ICE + INDOOR loaded.");
			((MelonBase)this).LoggerInstance.Msg("F7 variant | F8 place shelter | F10 remove | E door when nearby");
		}

		public override void OnUpdate()
		{
			if (Playable())
			{
				if (Input.GetKeyDown((KeyCode)286))
				{
					AdjustScale(-0.05f);
				}
				if (Input.GetKeyDown((KeyCode)287))
				{
					AdjustScale(0.05f);
				}
				if (Input.GetKeyDown((KeyCode)288))
				{
					variant = ((variant != 1) ? 1 : 2);
					HUDMessage($"Fishing Shelter variant {variant}");
				}
				if (Input.GetKeyDown((KeyCode)289))
				{
					PlaceShelter();
				}
				if (Input.GetKeyDown((KeyCode)291))
				{
					RemoveShelter();
				}
				if ((Object)(object)shelterRoot != (Object)null && Input.GetKeyDown((KeyCode)101) && PlayerDistance() <= 3f)
				{
					ToggleDoor();
				}
			}
		}

		public override void OnGUI()
		{
			//IL_005d: Unknown result type (might be due to invalid IL or missing references)
			//IL_008f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0033: Unknown result type (might be due to invalid IL or missing references)
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			//IL_0040: Unknown result type (might be due to invalid IL or missing references)
			//IL_0047: Unknown result type (might be due to invalid IL or missing references)
			//IL_0053: Expected O, but got Unknown
			if (Playable() && !((Object)(object)shelterRoot == (Object)null) && !(PlayerDistance() > 3f))
			{
				if (promptStyle == null)
				{
					promptStyle = new GUIStyle(GUI.skin.label)
					{
						fontSize = 18,
						fontStyle = (FontStyle)1,
						alignment = (TextAnchor)4
					};
				}
				promptStyle.normal.textColor = Color.white;
				GUI.Label(new Rect((float)Screen.width / 2f - 180f, (float)(Screen.height - 170), 360f, 40f), closedDoor ? "E  OPEN SHELTER DOOR" : "E  CLOSE SHELTER DOOR", promptStyle);
			}
		}

		private static void AdjustScale(float delta)
		{
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			shelterScale = Mathf.Clamp(shelterScale + delta, 0.05f, 1f);
			if ((Object)(object)shelterRoot != (Object)null)
			{
				shelterRoot.transform.localScale = Vector3.one * shelterScale;
			}
			HUDMessage($"Fishing Shelter scale: {shelterScale:0.00}");
		}

		private static void PlaceShelter()
		{
			//IL_0052: Unknown result type (might be due to invalid IL or missing references)
			//IL_0058: Unknown result type (might be due to invalid IL or missing references)
			//IL_0062: Unknown result type (might be due to invalid IL or missing references)
			//IL_0067: Unknown result type (might be due to invalid IL or missing references)
			//IL_006c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0073: Unknown result type (might be due to invalid IL or missing references)
			//IL_0088: Unknown result type (might be due to invalid IL or missing references)
			//IL_008d: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
			//IL_00db: Unknown result type (might be due to invalid IL or missing references)
			Transform player = GetPlayer();
			if ((Object)(object)player == (Object)null)
			{
				HUDMessage("Player not found");
				return;
			}
			string text = Path.Combine(assetDir, (variant == 1) ? "iglu.b3d" : "iglu_2.b3d");
			if (!File.Exists(text))
			{
				HUDMessage("Fishing Shelter assets missing");
				return;
			}
			RemoveShelter();
			try
			{
				Vector3 position = player.position + player.forward * 3.5f;
				Quaternion rotation = Quaternion.Euler(0f, player.eulerAngles.y + 180f, 0f);
				shelterRoot = B3D.Load(text, assetDir, "BurebistaFishingShelter");
				shelterRoot.transform.localScale = Vector3.one * shelterScale;
				shelterRoot.transform.position = position;
				shelterRoot.transform.rotation = rotation;
				AddSimpleColliders(shelterRoot);
				closedDoor = true;
				SpawnDoor();
				HUDMessage($"Fishing Shelter variant {variant} placed");
			}
			catch (Exception ex)
			{
				MelonLogger.Error("Shelter load failed: " + ex);
				HUDMessage("Shelter model load failed - check MelonLoader log");
			}
		}

		private static void SpawnDoor()
		{
			if ((Object)(object)shelterRoot == (Object)null)
			{
				return;
			}
			if ((Object)(object)doorRoot != (Object)null)
			{
				Object.Destroy((Object)(object)doorRoot);
			}
			string text = Path.Combine(assetDir, closedDoor ? "shkura_closed.b3d" : "shkura.b3d");
			if (!File.Exists(text))
			{
				return;
			}
			try
			{
				doorRoot = B3D.Load(text, assetDir, "BurebistaFishingDoor");
				doorRoot.transform.SetParent(shelterRoot.transform, false);
				if (closedDoor)
				{
					AddSimpleColliders(doorRoot);
				}
			}
			catch (Exception ex)
			{
				MelonLogger.Warning("Door load failed: " + ex.Message);
			}
		}

		private static void ToggleDoor()
		{
			closedDoor = !closedDoor;
			SpawnDoor();
			HUDMessage(closedDoor ? "Shelter door closed" : "Shelter door opened");
		}

		private static void RemoveShelter()
		{
			if ((Object)(object)doorRoot != (Object)null)
			{
				Object.Destroy((Object)(object)doorRoot);
				doorRoot = null;
			}
			if ((Object)(object)shelterRoot != (Object)null)
			{
				Object.Destroy((Object)(object)shelterRoot);
				shelterRoot = null;
			}
		}

		private static float PlayerDistance()
		{
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			Transform player = GetPlayer();
			if (!((Object)(object)player == (Object)null) && !((Object)(object)shelterRoot == (Object)null))
			{
				return Vector3.Distance(player.position, shelterRoot.transform.position);
			}
			return 999f;
		}

		private static Transform GetPlayer()
		{
			try
			{
				object obj = StaticCall("GameManager", "GetPlayerTransform");
				return (Transform)((obj is Transform) ? obj : null);
			}
			catch
			{
				return null;
			}
		}

		private static object StaticCall(string typeName, string method)
		{
			Type type = FindType(typeName);
			if (type == null)
			{
				return null;
			}
			return type.GetMethod(method, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null)?.Invoke(null, null);
		}

		private static Type FindType(string n)
		{
			if (typeCache.TryGetValue(n, out var value))
			{
				return value;
			}
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (Assembly assembly in assemblies)
			{
				value = assembly.GetType(n, throwOnError: false) ?? assembly.GetType("Il2Cpp." + n, throwOnError: false);
				if (value != null)
				{
					break;
				}
			}
			typeCache[n] = value;
			return value;
		}

		private static bool Playable()
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			Scene activeScene = SceneManager.GetActiveScene();
			string text = activeScene.name ?? "";
			if (!string.IsNullOrEmpty(text) && !text.Contains("MainMenu", StringComparison.OrdinalIgnoreCase))
			{
				return !text.Contains("Boot", StringComparison.OrdinalIgnoreCase);
			}
			return false;
		}

		private static void HUDMessage(string text)
		{
			MelonLogger.Msg(text);
			try
			{
				(FindType("HUDMessage")?.GetMethod("AddMessage", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[1] { typeof(string) }, null))?.Invoke(null, new object[1] { text });
			}
			catch
			{
			}
		}

		private static void AddSimpleColliders(GameObject root)
		{
			try
			{
				foreach (MeshFilter componentsInChild in root.GetComponentsInChildren<MeshFilter>(true))
				{
					if (!((Object)(object)componentsInChild == (Object)null) && !((Object)(object)componentsInChild.sharedMesh == (Object)null) && !((Object)(object)((Component)componentsInChild).GetComponent<Collider>() != (Object)null))
					{
						MeshCollider obj = ((Component)componentsInChild).gameObject.AddComponent<MeshCollider>();
						obj.sharedMesh = componentsInChild.sharedMesh;
						obj.convex = false;
					}
				}
			}
			catch
			{
			}
		}
	}
	internal static class B3D
	{
		private sealed class Reader : IDisposable
		{
			public readonly BinaryReader R;

			public Reader(string file)
			{
				R = new BinaryReader(File.OpenRead(file));
			}

			public void Dispose()
			{
				R.Dispose();
			}

			public string Str()
			{
				List<byte> list = new List<byte>(32);
				byte item;
				while ((item = R.ReadByte()) != 0)
				{
					list.Add(item);
				}
				return Encoding.UTF8.GetString(list.ToArray());
			}

			public string Tag()
			{
				return Encoding.ASCII.GetString(R.ReadBytes(4));
			}
		}

		private sealed class Brush
		{
			public Color color = Color.white;

			public int tex = -1;
		}

		private sealed class Ctx
		{
			public readonly List<string> tex = new List<string>();

			public readonly List<Brush> brushes = new List<Brush>();

			public string dir;
		}

		public static GameObject Load(string file, string textureDir, string rootName)
		{
			//IL_0069: Unknown result type (might be due to invalid IL or missing references)
			//IL_0070: Expected O, but got Unknown
			using Reader reader = new Reader(file);
			if (reader.Tag() != "BB3D")
			{
				throw new Exception("Not a BB3D file: " + Path.GetFileName(file));
			}
			int num = reader.R.ReadInt32();
			long num2 = reader.R.BaseStream.Position + num;
			reader.R.ReadInt32();
			Ctx c = new Ctx
			{
				dir = textureDir
			};
			GameObject val = new GameObject(rootName);
			while (reader.R.BaseStream.Position < num2)
			{
				ReadTop(reader, c, val.transform);
			}
			return val;
		}

		private static void ReadTop(Reader r, Ctx c, Transform parent)
		{
			string text = r.Tag();
			int num = r.R.ReadInt32();
			long num2 = r.R.BaseStream.Position + num;
			switch (text)
			{
			case "TEXS":
				ReadTextures(r, c, num2);
				break;
			case "BRUS":
				ReadBrushes(r, c, num2);
				break;
			case "NODE":
				ReadNode(r, c, parent, num2);
				break;
			}
			r.R.BaseStream.Position = num2;
		}

		private static void ReadTextures(Reader r, Ctx c, long end)
		{
			while (r.R.BaseStream.Position < end)
			{
				c.tex.Add(r.Str());
				r.R.ReadInt32();
				r.R.ReadInt32();
				for (int i = 0; i < 5; i++)
				{
					r.R.ReadSingle();
				}
			}
		}

		private static void ReadBrushes(Reader r, Ctx c, long end)
		{
			//IL_004b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
			int num = r.R.ReadInt32();
			while (r.R.BaseStream.Position < end)
			{
				r.Str();
				Brush brush = new Brush();
				brush.color = new Color(r.R.ReadSingle(), r.R.ReadSingle(), r.R.ReadSingle(), r.R.ReadSingle());
				r.R.ReadSingle();
				r.R.ReadInt32();
				r.R.ReadInt32();
				for (int i = 0; i < num; i++)
				{
					int tex = r.R.ReadInt32();
					if (i == 0)
					{
						brush.tex = tex;
					}
				}
				c.brushes.Add(brush);
			}
		}

		private static void ReadNode(Reader r, Ctx c, Transform parent, long end)
		{
			//IL_009a: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a1: Expected O, but got Unknown
			//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00da: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
			string text = r.Str();
			Vector3 val = default(Vector3);
			val = new Vector3(r.R.ReadSingle(), r.R.ReadSingle(), r.R.ReadSingle());
			Vector3 localScale = default(Vector3);
			localScale = new Vector3(r.R.ReadSingle(), r.R.ReadSingle(), r.R.ReadSingle());
			float num = r.R.ReadSingle();
			float num2 = r.R.ReadSingle();
			float num3 = r.R.ReadSingle();
			float num4 = r.R.ReadSingle();
			GameObject val2 = new GameObject(string.IsNullOrWhiteSpace(text) ? "B3DNode" : text);
			val2.transform.SetParent(parent, false);
			val2.transform.localPosition = new Vector3(val.x, val.y, 0f - val.z);
			val2.transform.localScale = localScale;
			val2.transform.localRotation = new Quaternion(0f - num2, 0f - num3, num4, num);
			while (r.R.BaseStream.Position < end)
			{
				string text2 = r.Tag();
				int num5 = r.R.ReadInt32();
				long num6 = r.R.BaseStream.Position + num5;
				if (text2 == "MESH")
				{
					ReadMesh(r, c, val2.transform, num6);
				}
				else if (text2 == "NODE")
				{
					ReadNode(r, c, val2.transform, num6);
				}
				r.R.BaseStream.Position = num6;
			}
		}

		private static void ReadMesh(Reader r, Ctx c, Transform parent, long end)
		{
			//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cb: Expected O, but got Unknown
			//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e0: Expected O, but got Unknown
			//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
			//IL_013f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0181: Unknown result type (might be due to invalid IL or missing references)
			int num = r.R.ReadInt32();
			List<Vector3> list = new List<Vector3>();
			List<Vector3> list2 = new List<Vector3>();
			List<Vector2> list3 = new List<Vector2>();
			List<(int, List<int>)> list4 = new List<(int, List<int>)>();
			while (r.R.BaseStream.Position < end)
			{
				string text = r.Tag();
				int num2 = r.R.ReadInt32();
				long num3 = r.R.BaseStream.Position + num2;
				if (text == "VRTS")
				{
					ReadVerts(r, num3, list, list2, list3);
				}
				else if (text == "TRIS")
				{
					ReadTris(r, num3, list4);
				}
				r.R.BaseStream.Position = num3;
			}
			if (list.Count == 0 || list4.Count == 0)
			{
				return;
			}
			GameObject val = new GameObject("Mesh");
			val.transform.SetParent(parent, false);
			Mesh val2 = new Mesh();
			((Object)val2).name = "B3DMesh";
			List<Vector3> val3 = new List<Vector3>();
			for (int i = 0; i < list.Count; i++)
			{
				val3.Add(list[i]);
			}
			val2.SetVertices(val3);
			if (list2.Count == list.Count)
			{
				List<Vector3> val4 = new List<Vector3>();
				for (int j = 0; j < list2.Count; j++)
				{
					val4.Add(list2[j]);
				}
				val2.SetNormals(val4);
			}
			if (list3.Count == list.Count)
			{
				List<Vector2> val5 = new List<Vector2>();
				for (int k = 0; k < list3.Count; k++)
				{
					val5.Add(list3[k]);
				}
				val2.SetUVs(0, val5);
			}
			val2.subMeshCount = list4.Count;
			for (int l = 0; l < list4.Count; l++)
			{
				List<int> val6 = new List<int>();
				List<int> item = list4[l].Item2;
				for (int m = 0; m < item.Count; m++)
				{
					val6.Add(item[m]);
				}
				val2.SetTriangles(val6, l, true);
			}
			if (list2.Count != list.Count)
			{
				val2.RecalculateNormals();
			}
			val2.RecalculateBounds();
			val.AddComponent<MeshFilter>().sharedMesh = val2;
			MeshRenderer val7 = val.AddComponent<MeshRenderer>();
			Material[] array = (Material[])(object)new Material[list4.Count];
			for (int n = 0; n < array.Length; n++)
			{
				array[n] = MaterialFor(c, (list4[n].Item1 >= 0) ? list4[n].Item1 : num);
			}
			((Renderer)val7).sharedMaterials = array;
		}

		private static void ReadVerts(Reader r, long end, List<Vector3> v, List<Vector3> n, List<Vector2> uv)
		{
			//IL_0056: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
			//IL_0094: Unknown result type (might be due to invalid IL or missing references)
			//IL_013a: Unknown result type (might be due to invalid IL or missing references)
			int num = r.R.ReadInt32();
			int num2 = r.R.ReadInt32();
			int num3 = r.R.ReadInt32();
			while (r.R.BaseStream.Position < end)
			{
				float num4 = r.R.ReadSingle();
				float num5 = r.R.ReadSingle();
				float num6 = r.R.ReadSingle();
				v.Add(new Vector3(num4, num5, 0f - num6));
				if (((uint)num & (true ? 1u : 0u)) != 0)
				{
					float num7 = r.R.ReadSingle();
					float num8 = r.R.ReadSingle();
					float num9 = r.R.ReadSingle();
					n.Add(new Vector3(num7, num8, 0f - num9));
				}
				else
				{
					n.Add(Vector3.zero);
				}
				if (((uint)num & 2u) != 0)
				{
					r.R.ReadSingle();
					r.R.ReadSingle();
					r.R.ReadSingle();
					r.R.ReadSingle();
				}
				float num10 = 0f;
				float num11 = 0f;
				for (int i = 0; i < num2; i++)
				{
					for (int j = 0; j < num3; j++)
					{
						float num12 = r.R.ReadSingle();
						if (i == 0 && j == 0)
						{
							num10 = num12;
						}
						if (i == 0 && j == 1)
						{
							num11 = num12;
						}
					}
				}
				uv.Add(new Vector2(num10, num11));
			}
		}

		private static void ReadTris(Reader r, long end, List<(int brush, List<int> tris)> outp)
		{
			int item = r.R.ReadInt32();
			List<int> list = new List<int>();
			while (r.R.BaseStream.Position + 12 <= end)
			{
				int item2 = r.R.ReadInt32();
				int item3 = r.R.ReadInt32();
				int item4 = r.R.ReadInt32();
				list.Add(item2);
				list.Add(item4);
				list.Add(item3);
			}
			outp.Add((item, list));
		}

		private static Material MaterialFor(Ctx c, int brushId)
		{
			//IL_0060: Unknown result type (might be due to invalid IL or missing references)
			//IL_0066: Expected O, but got Unknown
			//IL_0072: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f0: Expected O, but got Unknown
			Brush brush = ((brushId >= 0 && brushId < c.brushes.Count) ? c.brushes[brushId] : new Brush());
			Shader obj = Shader.Find("Unlit/Texture") ?? Shader.Find("Legacy Shaders/Diffuse") ?? Shader.Find("Standard");
			if ((Object)(object)obj == (Object)null)
			{
				throw new Exception("No compatible Unity shader found");
			}
			Material val = new Material(obj);
			((Object)val).name = "BurebistaShelterMaterial";
			val.color = Color.white;
			Texture2D val2 = null;
			string text = null;
			if (brush.tex >= 0 && brush.tex < c.tex.Count)
			{
				text = Path.GetFileName(c.tex[brush.tex].Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar));
				string text2 = FindTexture(c.dir, text);
				if (text2 != null)
				{
					try
					{
						val2 = new Texture2D(2, 2, (TextureFormat)4, false);
						if (ImageConversion.LoadImage(val2, File.ReadAllBytes(text2)))
						{
							((Object)val2).name = "Burebista_" + Path.GetFileName(text2);
							((Texture)val2).wrapMode = (TextureWrapMode)0;
							((Texture)val2).filterMode = (FilterMode)1;
							((Texture)val2).anisoLevel = 2;
							val.mainTexture = (Texture)(object)val2;
							MelonLogger.Msg("[FishingShelter] Texture OK: " + Path.GetFileName(text2));
						}
						else
						{
							val2 = null;
						}
					}
					catch (Exception ex)
					{
						MelonLogger.Warning("[FishingShelter] Texture load failed " + text + ": " + ex.Message);
						val2 = null;
					}
				}
			}
			if ((Object)(object)val2 == (Object)null)
			{
				string text3 = (text ?? "").ToLowerInvariant();
				if (text3.Contains("wood") || text3.Contains("shkura"))
				{
					val.color = new Color(0.42f, 0.3f, 0.2f, 1f);
				}
				else
				{
					val.color = new Color(0.78f, 0.84f, 0.88f, 1f);
				}
				MelonLogger.Warning("[FishingShelter] Using visible fallback for: " + (text ?? "no texture"));
			}
			return val;
		}

		private static string FindTexture(string dir, string fileName)
		{
			if (string.IsNullOrWhiteSpace(fileName))
			{
				return null;
			}
			string text = Path.Combine(dir, fileName);
			if (File.Exists(text))
			{
				return text;
			}
			try
			{
				string[] files = Directory.GetFiles(dir);
				foreach (string text2 in files)
				{
					if (string.Equals(Path.GetFileName(text2), fileName, StringComparison.OrdinalIgnoreCase))
					{
						return text2;
					}
				}
			}
			catch
			{
			}
			return null;
		}
	}
}
