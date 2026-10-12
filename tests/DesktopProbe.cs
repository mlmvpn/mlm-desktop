using System;using System.Diagnostics;using System.Runtime.InteropServices;using MlmDesktop;
class DesktopProbe {
 [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);
 [DllImport("user32.dll")]static extern bool IsWindowVisible(IntPtr h);
 [STAThread]static int Main(string[] args){bool expected=args.Length>0&&args[0]=="on";int count=0;bool styles=true;
  foreach(var process in Process.GetProcessesByName("MLMDesktop")){int id=process.Id;Native.EnumWindows(delegate(IntPtr h,IntPtr unused){uint owner;GetWindowThreadProcessId(h,out owner);if(owner==id&&IsWindowVisible(h)){count++;long ex=Native.GetWindowLongPtr(h,-20).ToInt64();styles&=(ex&0x08000080)==0x08000080&&(ex&0x40000)==0;Console.WriteLine("Overlay extended style: 0x"+ex.ToString("X"));}return true;},IntPtr.Zero);}
  Console.WriteLine("Visible overlays: "+count+"; toolwindow/noactivate/no-appwindow: "+styles);
  return (expected?count==1&&styles:count==0)?0:1;
 }
}
