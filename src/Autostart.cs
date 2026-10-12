using System;using System.Diagnostics;using System.IO;using System.Security;using System.Security.Principal;using System.Text;using System.Threading;using Microsoft.Win32;
namespace MlmDesktop {
 // Keeps the invisible theme watcher alive without admin rights: a per-user Run entry
 // (logon) plus a hidden per-user scheduled task (logon + every minute). A duplicate
 // launch exits immediately on the single-instance mutex, so selecting the theme
 // brings the rug back within a minute even if the watcher was closed or never started.
 public static class Autostart {
  const string TaskName="MLM Desktop Watcher",RunKey="Software\\Microsoft\\Windows\\CurrentVersion\\Run",RunValue="MLM Desktop";
  public static string InstalledExe{get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Programs","MLM Desktop","MLMDesktop.exe");}}
  static int Schtasks(string args,out string output){
   var info=new ProcessStartInfo(Path.Combine(Environment.SystemDirectory,"schtasks.exe"),args){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
   using(var p=Process.Start(info)){output=p.StandardOutput.ReadToEnd();p.StandardError.ReadToEnd();if(!p.WaitForExit(15000)){try{p.Kill();}catch{}return -1;}return p.ExitCode;}
  }
  static string TaskXml(string exe){
   string user=SecurityElement.Escape(WindowsIdentity.GetCurrent().Name),path=SecurityElement.Escape(exe);
   return "<?xml version=\"1.0\" encoding=\"UTF-16\"?><Task version=\"1.2\" xmlns=\"http://schemas.microsoft.com/windows/2004/02/mit/task\"><Triggers>"
    +"<LogonTrigger><Enabled>true</Enabled><UserId>"+user+"</UserId></LogonTrigger>"
    +"<TimeTrigger><StartBoundary>2020-01-01T00:00:00</StartBoundary><Enabled>true</Enabled><Repetition><Interval>PT1M</Interval></Repetition></TimeTrigger></Triggers>"
    +"<Principals><Principal id=\"Author\"><UserId>"+user+"</UserId><LogonType>InteractiveToken</LogonType><RunLevel>LeastPrivilege</RunLevel></Principal></Principals>"
    +"<Settings><MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy><DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries><StopIfGoingOnBatteries>false</StopIfGoingOnBatteries><StartWhenAvailable>true</StartWhenAvailable><ExecutionTimeLimit>PT0S</ExecutionTimeLimit><Hidden>true</Hidden><Enabled>true</Enabled></Settings>"
    +"<Actions Context=\"Author\"><Exec><Command>"+path+"</Command></Exec></Actions></Task>";
  }
  // Idempotent; only touches the installed copy so dev builds never register themselves.
  public static void Ensure(string exe){
   if(!string.Equals(Path.GetFullPath(exe),InstalledExe,StringComparison.OrdinalIgnoreCase))return;
   using(var key=Registry.CurrentUser.CreateSubKey(RunKey)){string want="\""+exe+"\"";if(!string.Equals(key.GetValue(RunValue) as string,want,StringComparison.OrdinalIgnoreCase))key.SetValue(RunValue,want);}
   string xml;if(Schtasks("/Query /TN \""+TaskName+"\" /XML",out xml)==0&&xml.IndexOf(SecurityElement.Escape(exe),StringComparison.OrdinalIgnoreCase)>=0&&xml.IndexOf("PT1M",StringComparison.Ordinal)>=0)return;
   string file=Path.Combine(Path.GetTempPath(),"MLMDesktop-task-"+Guid.NewGuid()+".xml");
   try{File.WriteAllText(file,TaskXml(exe),Encoding.Unicode);string o;if(Schtasks("/Create /TN \""+TaskName+"\" /XML \""+file+"\" /F",out o)!=0)throw new IOException("Could not register the MLM Desktop watcher task. "+o);}
   finally{try{File.Delete(file);}catch{}}
  }
  public static void EnsureInBackground(string exe,Action<Exception> log){var t=new Thread(delegate(){try{Ensure(exe);}catch(Exception e){log(e);}}){IsBackground=true};t.Start();}
  public static void Remove(){
   string o;Schtasks("/Delete /TN \""+TaskName+"\" /F",out o);
   using(var k=Registry.CurrentUser.OpenSubKey(RunKey,true)){if(k!=null){var v=k.GetValue(RunValue) as string;if(v!=null&&v.IndexOf(InstalledExe,StringComparison.OrdinalIgnoreCase)>=0)k.DeleteValue(RunValue,false);}}
  }
 }
}
