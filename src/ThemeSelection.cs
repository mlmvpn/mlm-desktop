using System;using System.IO;using System.Runtime.InteropServices;using Microsoft.Win32;
namespace MlmDesktop {
 [ComImport,Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
 interface IDesktopWallpaper {
  void SetWallpaper([MarshalAs(UnmanagedType.LPWStr)]string monitor,[MarshalAs(UnmanagedType.LPWStr)]string wallpaper);
  [return:MarshalAs(UnmanagedType.LPWStr)]string GetWallpaper([MarshalAs(UnmanagedType.LPWStr)]string monitor);
  [return:MarshalAs(UnmanagedType.LPWStr)]string GetMonitorDevicePathAt(uint index);
  uint GetMonitorDevicePathCount();
  void GetMonitorRECT([MarshalAs(UnmanagedType.LPWStr)]string monitor,out MonitorRect rect);
 }
 [StructLayout(LayoutKind.Sequential)]struct MonitorRect {public int Left,Top,Right,Bottom;}
 internal static class ThemeSelection {
  internal static string ThemePath {get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Microsoft","Windows","Themes","MLM Desktop.theme");}}
  // Preserve an image still used on any monitor, including copied/custom themes.
  internal static bool WallpaperInUse(string wallpaper){
   IDesktopWallpaper desktop=null;try{
    desktop=(IDesktopWallpaper)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("C2CF3110-460E-4FC1-B9D0-8A1C0C9CC4BD")));
    for(uint i=0;i<desktop.GetMonitorDevicePathCount();i++)if(string.Equals(desktop.GetWallpaper(desktop.GetMonitorDevicePathAt(i)),wallpaper,StringComparison.OrdinalIgnoreCase))return true;
    return false;
   }catch{return true;}finally{if(desktop!=null)Marshal.FinalReleaseComObject(desktop);}
  }
  internal static bool IsSelected(string wallpaper){
   string currentTheme=null;bool picture=true;
   using(var key=Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes")){if(key!=null)currentTheme=key.GetValue("CurrentTheme") as string;}
   using(var key=Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Wallpapers")){if(key!=null)picture=Convert.ToInt32(key.GetValue("BackgroundType",0))==0;}
   if(!picture || string.IsNullOrEmpty(currentTheme))return false;
   IDesktopWallpaper desktop=null;try{
    desktop=(IDesktopWallpaper)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("C2CF3110-460E-4FC1-B9D0-8A1C0C9CC4BD")));
    string path=null;uint count=desktop.GetMonitorDevicePathCount();for(uint i=0;i<count;i++){string id=desktop.GetMonitorDevicePathAt(i);MonitorRect r;desktop.GetMonitorRECT(id,out r);if(r.Left==0&&r.Top==0){path=desktop.GetWallpaper(id);break;}}
    return DesktopMode.Active(currentTheme,ThemePath,path,wallpaper,picture);
   }finally{if(desktop!=null)Marshal.FinalReleaseComObject(desktop);}
  }
 }
}
