using System;
using System.Runtime.InteropServices;
using System.Text;
namespace MlmDesktop {
 internal static class Native {
  internal delegate bool EnumWindowsProc(IntPtr hwnd,IntPtr param);
  [DllImport("user32.dll")]internal static extern bool EnumWindows(EnumWindowsProc callback,IntPtr param);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)]internal static extern IntPtr FindWindowEx(IntPtr parent,IntPtr after,string cls,string name);
  [DllImport("user32.dll")]internal static extern IntPtr GetWindow(IntPtr hwnd,uint command);
  [DllImport("user32.dll")]internal static extern bool SetWindowPos(IntPtr hwnd,IntPtr after,int x,int y,int w,int h,uint flags);
  [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")]internal static extern IntPtr GetWindowLongPtr(IntPtr hwnd,int index);
  [DllImport("user32.dll",EntryPoint="SetWindowLongPtrW")]internal static extern IntPtr SetWindowLongPtr(IntPtr hwnd,int index,IntPtr value);
  internal static IntPtr Desktop(){IntPtr found=IntPtr.Zero;EnumWindows(delegate(IntPtr h,IntPtr p){if(FindWindowEx(h,IntPtr.Zero,"SHELLDLL_DefView",null)!=IntPtr.Zero){found=h;return false;}return true;},IntPtr.Zero);return found;}
  [DllImport("user32.dll")]static extern bool IsWindowVisible(IntPtr hwnd);
  [DllImport("user32.dll")]static extern bool IsIconic(IntPtr hwnd);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetClassName(IntPtr hwnd,StringBuilder name,int count);
  [DllImport("user32.dll")]static extern bool GetWindowRect(IntPtr hwnd,out Rect rect);
  [StructLayout(LayoutKind.Sequential)]struct Rect {internal int Left,Top,Right,Bottom;}
  [StructLayout(LayoutKind.Sequential)]internal struct WindowPosition {internal IntPtr Hwnd,After;internal int X,Y,Width,Height;internal uint Flags;}
  static bool placing;
  internal static IntPtr PlacementAnchor(IntPtr hwnd,IntPtr desktop,IntPtr predecessor,bool belowApps){
   // NULL is HWND_TOP, never a safe fallback for a desktop overlay.
   if(desktop==IntPtr.Zero||predecessor==IntPtr.Zero||!belowApps)return new IntPtr(1); // HWND_BOTTOM
   return predecessor==hwnd?IntPtr.Zero:predecessor;
  }
  internal static void KeepZOrder(ref WindowPosition position){if(!placing)position.Flags|=0x4;}
  internal static void ProtectZOrder(IntPtr address){if(address==IntPtr.Zero)return;var position=(WindowPosition)Marshal.PtrToStructure(address,typeof(WindowPosition));KeepZOrder(ref position);Marshal.StructureToPtr(position,address,false);}
  static bool BelowApplications(IntPtr desktop,IntPtr overlay){
   // Explorer can briefly raise its desktop host while opening/restoring a
   // folder. Never follow that host above an ordinary application window.
   int guard=0;for(IntPtr h=GetWindow(desktop,2);h!=IntPtr.Zero&&guard++<4096;h=GetWindow(h,2)){
    if(h==overlay||!IsWindowVisible(h)||IsIconic(h))continue;var name=new StringBuilder(64);GetClassName(h,name,name.Capacity);string cls=name.ToString();if(cls=="WorkerW"||cls=="Progman")continue;
    Rect rect;if(GetWindowRect(h,out rect)&&rect.Right>rect.Left&&rect.Bottom>rect.Top)return false;
   }
   return guard<4096;
  }
  internal static void PlaceOnDesktop(IntPtr hwnd){
   IntPtr desktop=Desktop(),above=desktop==IntPtr.Zero?IntPtr.Zero:GetWindow(desktop,3);
   IntPtr anchor=PlacementAnchor(hwnd,desktop,above,desktop!=IntPtr.Zero&&BelowApplications(desktop,hwnd));if(anchor==IntPtr.Zero)return;
   placing=true;try{SetWindowPos(hwnd,anchor,0,0,0,0,0x213);}finally{placing=false;}
  }
 }
}
