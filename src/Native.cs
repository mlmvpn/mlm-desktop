using System;
using System.Runtime.InteropServices;
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
  internal static void PlaceOnDesktop(IntPtr hwnd){IntPtr desktop=Desktop();if(desktop==IntPtr.Zero)return;IntPtr above=GetWindow(desktop,3);if(above==hwnd)return;SetWindowPos(hwnd,above,0,0,0,0,0x13);}
 }
}
