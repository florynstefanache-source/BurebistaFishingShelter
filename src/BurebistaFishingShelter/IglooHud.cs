using UnityEngine;
using Il2Cpp;
namespace BurebistaFishingShelter;
// Passive reminder: repaint only. Never captures input or changes cursor state.
internal static class IglooHud {
 static Texture2D icon;
 static GUIStyle keyStyle, captionStyle;
 internal static void Draw(bool placed){
  if(IgluSettings.Current==null || !IgluSettings.Current.ShowIglooIcon)return;
  if(InterfaceManager.GetInstance()==null || InterfaceManager.GetInstance().AnyOverlayPanelEnabled() || GameManager.GetPlayerManagerComponent()?.GetControlMode()!=PlayerControlMode.Normal)return;
  if(icon==null)CreateIcon();
  if(keyStyle==null){keyStyle=new GUIStyle(GUI.skin.label){fontSize=15,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};keyStyle.normal.textColor=new Color(.92f,.95f,.95f,.9f);captionStyle=new GUIStyle(keyStyle){fontSize=12,fontStyle=FontStyle.Normal};}
  // Upper right, away from the central view and the lower survival meters.
  float scale=Mathf.Clamp(Screen.height/1080f,.75f,1.4f);
  float width=100f*scale, x=Screen.width-width-24f*scale, y=92f*scale;
  Color previous=GUI.color;
  GUI.color=new Color(1,1,1,.78f);
  GUI.DrawTexture(new Rect(x+16f*scale,y,68f*scale,46f*scale),icon);
  GUI.color=previous;
  GUI.Label(new Rect(x,y+47f*scale,width,23f*scale),"[ "+IgluSettings.Current.Build+" ]",keyStyle);
  GUI.Label(new Rect(x,y+69f*scale,width,20f*scale),IgluText.Pick(placed?"Mover iglú":"Construir iglú",placed?"Move igloo":"Build igloo"),captionStyle);
 }
 static void CreateIcon(){
  icon=new Texture2D(96,64,TextureFormat.RGBA32,false);
  icon.hideFlags=HideFlags.HideAndDontSave;
  icon.filterMode=FilterMode.Bilinear;
  for(int y=0;y<64;y++)for(int x=0;x<96;x++){
   float dx=(x-43f)/40f,dy=(y-8f)/48f;
   bool dome=y>=8&&dx*dx+dy*dy<=1;
   bool edge=dome&&(dx*dx+dy*dy>.91f||y<11);
   bool seam=dome&&(y==23||y==37||y==49 || (y<23&&(x==19||x==42||x==65)) || (y>=23&&y<37&&(x==29||x==56)) || (y>=37&&y<49&&(x==23||x==44||x==65)) || (y>=49&&x==43));
   bool door=x>=39&&x<=60&&y>=8&&(y<=23 || ((x-49.5f)*(x-49.5f)+(y-23f)*(y-23f)<=110));
   Color color=new Color(0,0,0,0);
   if(dome)color=edge?new Color(.94f,.97f,.97f,1):seam?new Color(.38f,.47f,.49f,.8f):new Color(.81f,.87f,.87f,.92f);
   if(door)color=new Color(.1f,.16f,.18f,.9f);
   icon.SetPixel(x,y,color);
  }
  icon.Apply(false,true);
 }
 internal static void Dispose(){if(icon!=null)Object.Destroy(icon);icon=null;keyStyle=null;captionStyle=null;}
}
