using System;using System.IO;using System.IO.Compression;using System.Diagnostics;using System.Drawing;using System.Reflection;using System.Threading;using System.Windows.Forms;using Microsoft.Win32;
[assembly:AssemblyTitle("MLM Desktop Setup")]
[assembly:AssemblyVersion("0.2.1.0")]
namespace MlmSetup {
 static class Setup {
  internal static readonly string InstallDir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Programs","MLM Desktop");
  internal static readonly string ThemePath=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Microsoft","Windows","Themes","MLM Desktop.theme");
  const string Key="Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\MLMDesktop";
  internal static readonly string[] Files={"MLMDesktop.exe","MLMDesktop.exe.config","assets/rug.png","assets/MLM Desktop.png","assets/app.ico"};
  [STAThread]static int Main(string[] args){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);try{
   if(args.Length==1&&args[0]=="--install"){Install();return 0;}
   if(args.Length==2&&args[0]=="--extract"){Extract(args[1]);return 0;}
   if(args.Length>0&&args[0]=="--uninstall"){
    if(MessageBox.Show("Remove MLM Desktop and its theme? Your desktop files and icons will stay in place.","MLM Desktop",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return 0;
    string temp=Path.Combine(Path.GetTempPath(),"MLMDesktop-Uninstall-"+Guid.NewGuid()+".exe");File.Copy(Assembly.GetExecutingAssembly().Location,temp);Process.Start(new ProcessStartInfo(temp,"--remove"){UseShellExecute=true});return 0;
   }
   if(args.Length>0&&args[0]=="--remove"){Uninstall();MessageBox.Show("MLM Desktop was removed.","MLM Desktop");return 0;}
   Application.Run(new SetupForm());return 0;
  }catch(Exception e){MessageBox.Show(e.Message,"MLM Desktop Setup",MessageBoxButtons.OK,MessageBoxIcon.Error);return 1;}}
  internal static void Extract(string directory){
   directory=Path.GetFullPath(directory);Directory.CreateDirectory(directory);
   using(var resource=Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.zip"))using(var zip=new ZipArchive(resource,ZipArchiveMode.Read)){
    foreach(string file in Files){var entry=zip.GetEntry(file);if(entry==null)throw new InvalidDataException("Missing package file: "+file);string dest=Path.GetFullPath(Path.Combine(directory,file));if(!dest.StartsWith(directory.TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Invalid package path");Directory.CreateDirectory(Path.GetDirectoryName(dest));using(var input=entry.Open())using(var output=File.Create(dest))input.CopyTo(output);}
   }
  }
  static void StopApp(){
   try{using(var ev=EventWaitHandle.OpenExisting("Local\\MLMDesktop.Exit"))ev.Set();}catch(WaitHandleCannotBeOpenedException){}
   foreach(var p in Process.GetProcessesByName("MLMDesktop")){using(p){string path;try{path=p.MainModule.FileName;}catch{continue;}if(!string.Equals(path,Path.Combine(InstallDir,"MLMDesktop.exe"),StringComparison.OrdinalIgnoreCase))continue;if(!p.WaitForExit(7000))throw new IOException("MLM Desktop is still shutting down. Please retry in a moment.");}}
  }
  internal static void Install(){
   if(!Environment.Is64BitOperatingSystem)throw new PlatformNotSupportedException("MLM Desktop needs 64-bit Windows 10 or 11.");
   using(var net=Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full")){if(net==null||Convert.ToInt32(net.GetValue("Release",0))<528040)throw new PlatformNotSupportedException("Please install Microsoft .NET Framework 4.8, then run Setup again.");}
   MlmDesktop.Autostart.Remove();StopApp();Extract(InstallDir);
   File.Copy(Assembly.GetExecutingAssembly().Location,Path.Combine(InstallDir,"Uninstall.exe"),true);
   Directory.CreateDirectory(Path.GetDirectoryName(ThemePath));
   string theme;using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("MLMDesktop.theme"))using(var reader=new StreamReader(stream)){theme=reader.ReadToEnd();}
   string systemMode="Dark",appMode="Dark";
   using(var prefs=Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize")){if(prefs!=null){systemMode=Convert.ToInt32(prefs.GetValue("SystemUsesLightTheme",0))==0?"Dark":"Light";appMode=Convert.ToInt32(prefs.GetValue("AppsUseLightTheme",0))==0?"Dark":"Light";}}
   theme=theme.Replace("{{WALLPAPER}}",Path.Combine(InstallDir,"assets","MLM Desktop.png")).Replace("{{SYSTEM_MODE}}",systemMode).Replace("{{APP_MODE}}",appMode);
   File.WriteAllText(ThemePath,theme,System.Text.Encoding.Unicode);
   using(var key=Registry.CurrentUser.CreateSubKey(Key)){key.SetValue("DisplayName","MLM Desktop");key.SetValue("DisplayVersion","0.2.1");key.SetValue("Publisher","mlmvpn");key.SetValue("InstallLocation",InstallDir);key.SetValue("DisplayIcon",Path.Combine(InstallDir,"MLMDesktop.exe"));key.SetValue("UninstallString","\""+Path.Combine(InstallDir,"Uninstall.exe")+"\" --uninstall");key.SetValue("NoModify",1);key.SetValue("NoRepair",1);key.SetValue("EstimatedSize",18000);}
   MlmDesktop.Autostart.Ensure(Path.Combine(InstallDir,"MLMDesktop.exe"));
   Process.Start(new ProcessStartInfo(Path.Combine(InstallDir,"MLMDesktop.exe")){UseShellExecute=false,CreateNoWindow=true});
  }
  internal static void OpenThemes(){Process.Start(new ProcessStartInfo("ms-settings:themes"){UseShellExecute=true});}
  static void Uninstall(){
   using(var key=Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes")){if(key!=null&&string.Equals(key.GetValue("CurrentTheme") as string,ThemePath,StringComparison.OrdinalIgnoreCase)){OpenThemes();throw new IOException("Choose another theme in Windows Settings, then run uninstall again.");}}
   MlmDesktop.Autostart.Remove();StopApp();
   bool preserveWallpaper=MlmDesktop.ThemeSelection.WallpaperInUse(Path.Combine(InstallDir,"assets","MLM Desktop.png"));
   foreach(var name in Files){if(preserveWallpaper && name=="assets/MLM Desktop.png")continue;string f=Path.Combine(InstallDir,name);if(File.Exists(f))File.Delete(f);}
   if(File.Exists(ThemePath))File.Delete(ThemePath);
   string uninstall=Path.Combine(InstallDir,"Uninstall.exe");for(int retry=0;retry<20;retry++){try{if(File.Exists(uninstall))File.Delete(uninstall);break;}catch(IOException){if(retry==19)throw;Thread.Sleep(100);}}
   foreach(var d in new[]{Path.Combine(InstallDir,"assets"),InstallDir})if(Directory.Exists(d)&&Directory.GetFileSystemEntries(d).Length==0)Directory.Delete(d);
   Registry.CurrentUser.DeleteSubKeyTree(Key,false);
  }
 }
 sealed class SetupForm:Form {
  Button install;Label status;
  internal SetupForm(){Text="MLM Desktop · Setup";ClientSize=new Size(620,410);StartPosition=FormStartPosition.CenterScreen;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;BackColor=Color.FromArgb(246,240,228);Font=new Font("Segoe UI",10);
   Controls.Add(new Label{Text="A little room\nfor your desktop.",Font=new Font("Segoe UI Semibold",29),ForeColor=Color.FromArgb(80,30,35),Location=new Point(34,28),Size=new Size(550,110)});
   Controls.Add(new Label{Text="MLM DESKTOP  /  WINDOWS THEME",ForeColor=Color.FromArgb(80,67,55),Location=new Point(38,150),Size=new Size(540,25)});
   Controls.Add(new Label{Text="After installation, choose MLM Desktop in Windows Settings → Themes.\nChoose a different theme to turn the rug off.\n\nNo Start or desktop shortcuts. No taskbar or tray icon.\nA small background process follows your selected theme.",Location=new Point(38,192),Size=new Size(545,110)});
   status=new Label{Text=Setup.InstallDir,Location=new Point(38,307),Size=new Size(545,45),Font=new Font("Segoe UI",8)};Controls.Add(status);
   install=new Button{Text="Install theme",Location=new Point(412,360),Size=new Size(168,36),BackColor=Color.FromArgb(102,35,41),ForeColor=Color.White,FlatStyle=FlatStyle.Flat};install.Click+=delegate{try{install.Enabled=false;status.Text="Installing theme…";Refresh();Setup.Install();Setup.OpenThemes();Close();}catch(Exception e){status.Text=e.Message;install.Enabled=true;}};Controls.Add(install);AcceptButton=install;
  }
 }
}
