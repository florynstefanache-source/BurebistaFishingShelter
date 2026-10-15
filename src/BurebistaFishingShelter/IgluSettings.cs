using System;
using System.Reflection;
using System.Collections.Generic;
using HarmonyLib;
using Il2Cpp;
using ModSettings;
using UnityEngine;
namespace BurebistaFishingShelter;
public sealed class IgluSettings : JsonModSettings {
 public static IgluSettings Current {get;private set;}
 [Section("BUREIGLU_KEYS",Localize=true)]
 [Description("BUREIGLU_HELP",Localize=true)]
 [Name("BUREIGLU_BUILD",Localize=true)] public KeyCode Build = KeyCode.F8;
 [Name("BUREIGLU_HINTS",Localize=true)] public KeyCode Hints = KeyCode.F9;
 [Name("BUREIGLU_VARIANT",Localize=true)] public KeyCode Variant = KeyCode.F7;
 [Name("BUREIGLU_SMALL",Localize=true)] public KeyCode Smaller = KeyCode.F5;
 [Name("BUREIGLU_LARGE",Localize=true)] public KeyCode Larger = KeyCode.F6;
 [Name("BUREIGLU_REMOVE",Localize=true)] public KeyCode Remove = KeyCode.F10;
 [Section("BUREIGLU_SCREEN",Localize=true)]
 [Name("BUREIGLU_ICON",Localize=true)] public bool ShowIglooIcon = true;
 public IgluSettings():base("BurebistaFishingShelter-keys.json") {}
 public static void Initialize(){Current=new IgluSettings();Current.Validate();Current.AddToModSettings("Burebista · Igloo / Fishing Shelter");}
 static bool Invalid(KeyCode key)=>key==KeyCode.None || key==KeyCode.Escape || key==KeyCode.W || key==KeyCode.A || key==KeyCode.S || key==KeyCode.D || key==KeyCode.Space || key==KeyCode.Tab || key==KeyCode.E || key==KeyCode.B || key==KeyCode.T || key==KeyCode.G || key==KeyCode.U || key==KeyCode.LeftControl || key==KeyCode.RightControl || key==KeyCode.LeftAlt || key==KeyCode.RightAlt || key==KeyCode.LeftShift || key==KeyCode.RightShift || (key>=KeyCode.Mouse0 && key<=KeyCode.Mouse6);
 void Validate(){var used=new HashSet<KeyCode>();foreach(var f in typeof(IgluSettings).GetFields(BindingFlags.Public|BindingFlags.Instance)){if(f.FieldType!=typeof(KeyCode))continue;var key=(KeyCode)f.GetValue(this);if(Invalid(key)||!used.Add(key)){var defaults=new[]{KeyCode.F8,KeyCode.F9,KeyCode.F7,KeyCode.F5,KeyCode.F6,KeyCode.F10,KeyCode.Insert,KeyCode.Home,KeyCode.End};foreach(var fallback in defaults)if(used.Add(fallback)){f.SetValue(this,fallback);break;}}}}
 protected override void OnChange(FieldInfo field,object oldValue,object newValue){if(field.FieldType!=typeof(KeyCode))return;var key=(KeyCode)newValue;bool duplicate=false;foreach(var f in typeof(IgluSettings).GetFields(BindingFlags.Public|BindingFlags.Instance))if(f!=field&&f.FieldType==typeof(KeyCode)&&(KeyCode)f.GetValue(this)==key)duplicate=true;if(Invalid(key)||duplicate){field.SetValue(this,oldValue);HUDMessage.AddMessage(IgluText.Pick("Tecla reservada o repetida. Elige otra tecla.","Reserved or duplicate key. Choose another key."));RefreshGUI();}}
 public static bool Pressed(KeyCode key)=>Input.GetKeyDown(key)&&!Input.GetKey(KeyCode.LeftControl)&&!Input.GetKey(KeyCode.RightControl)&&!Input.GetKey(KeyCode.LeftAlt)&&!Input.GetKey(KeyCode.RightAlt)&&!Input.GetKey(KeyCode.LeftShift)&&!Input.GetKey(KeyCode.RightShift);
}
public static class IgluText {
 public static bool English {get {try{return (Localization.Language??"").StartsWith("English",StringComparison.OrdinalIgnoreCase);}catch{return false;}}}
 public static string Pick(string es,string en)=>English?en:es;
 internal static readonly Dictionary<string,string[]> Settings=new(){
 {"BUREIGLU_KEYS",new[]{"CONTROLES DEL IGLÚ","IGLOO CONTROLS"}},
 {"BUREIGLU_SCREEN",new[]{"EN PANTALLA","ON SCREEN"}},
 {"BUREIGLU_ICON",new[]{"Mostrar icono del iglú y su tecla","Show igloo icon and its key"}},
 {"BUREIGLU_HELP",new[]{"Pulsa el botón de la tecla y elige otra. No repitas teclas. Evita controles de otros mods. Confirma los cambios para guardarlos.","Click the key button and choose another key. Do not reuse keys. Avoid controls used by other mods. Confirm your changes to save them."}},
 {"BUREIGLU_BUILD",new[]{"Construir / colocar / mover iglú","Build / place / move igloo"}},
 {"BUREIGLU_HINTS",new[]{"Mostrar / ocultar ayudas","Show / hide hints"}},
 {"BUREIGLU_VARIANT",new[]{"Cambiar modelo","Change model"}},
 {"BUREIGLU_SMALL",new[]{"Reducir tamaño","Decrease size"}},
 {"BUREIGLU_LARGE",new[]{"Aumentar tamaño","Increase size"}},
 {"BUREIGLU_REMOVE",new[]{"Desmontar iglú","Dismantle igloo"}}};
}
[HarmonyPatch(typeof(Localization),nameof(Localization.Get),new[]{typeof(string)})]
internal static class IgluSettingsLanguage {
 static bool Prefix(string __0,ref string __result){if(__0==null||!IgluText.Settings.TryGetValue(__0,out var pair))return true;__result=pair[IgluText.English?1:0];return false;}
}
