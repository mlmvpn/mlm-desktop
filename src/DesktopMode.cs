using System;using System.IO;
namespace MlmDesktop {
 public static class DesktopMode {
  static bool SamePath(string a,string b){if(string.IsNullOrWhiteSpace(a)||string.IsNullOrWhiteSpace(b))return false;try{return string.Equals(Path.GetFullPath(Environment.ExpandEnvironmentVariables(a.Trim('"'))),Path.GetFullPath(Environment.ExpandEnvironmentVariables(b.Trim('"'))),StringComparison.OrdinalIgnoreCase);}catch(ArgumentException){return false;}}
  public static bool Active(string theme,string expectedTheme,string wallpaper,string expectedWallpaper,bool picture){return picture&&SamePath(theme,expectedTheme)&&SamePath(wallpaper,expectedWallpaper);}
  public static bool MoveWhole(double u,double v){return u>.25&&u<.75&&v>.25&&v<.75;}
 }
}
