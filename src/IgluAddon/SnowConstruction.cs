using System;
using System.Linq;
using System.Reflection;
using BurebistaFishingShelter;
namespace BurebistaFishingShelterFeatures;
internal static class SnowConstruction {
 static Type inventoryType;
 static Type InventoryType => inventoryType ??= AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a=>a.GetName().Name=="BurebistaSnowBlocks")?.GetType("BurebistaSnowBlocks.SnowInventory");
 static float nextCount;
 static int cachedCount;
 const BindingFlags Flags=BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public;
 internal static int Count(){if(UnityEngine.Time.realtimeSinceStartup<nextCount)return cachedCount;nextCount=UnityEngine.Time.realtimeSinceStartup+.5f;try{cachedCount=(int)(InventoryType?.GetMethod("WholeBlocks",Flags)?.Invoke(null,null)??0);}catch{cachedCount=0;}return cachedCount;}
 internal static object Reserve(){var type=InventoryType;if(type==null)throw new Exception(IgluText.Pick("Instala Burebista Snow Blocks 0.1.9 o posterior.","Install Burebista Snow Blocks 0.1.9 or later."));var plan=type.GetMethod("Plan",Flags).Invoke(null,new object[]{200f,true});Invoke(plan,"Apply");return plan;}
 internal static void Invoke(object plan,string method){if(plan!=null)plan.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).Invoke(plan,null);}
}
