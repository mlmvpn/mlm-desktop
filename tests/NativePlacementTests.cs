using System;using MlmDesktop;
class NativePlacementTests {
 static int failures;static void Check(bool ok,string name){Console.WriteLine((ok?"PASS ":"FAIL ")+name);if(!ok)failures++;}
 static int Main(){IntPtr rug=new IntPtr(100),desktop=new IntPtr(200),app=new IntPtr(300);
 Check(Native.PlacementAnchor(rug,desktop,IntPtr.Zero,true)==new IntPtr(1),"desktop without predecessor lowers overlay instead of HWND_TOP");
 Check(Native.PlacementAnchor(rug,IntPtr.Zero,IntPtr.Zero,false)==new IntPtr(1),"missing Explorer host never raises overlay");
 Check(Native.PlacementAnchor(rug,desktop,app,false)==new IntPtr(1),"transient desktop above application cannot raise rug over that application");
 Check(Native.PlacementAnchor(rug,desktop,rug,true)==IntPtr.Zero,"correct desktop placement needs no z-order change");
 Check(Native.PlacementAnchor(rug,desktop,app,true)==app,"stable desktop positions rug below application");
 var position=new Native.WindowPosition{Hwnd=rug,After=IntPtr.Zero,Flags=0x40};Native.KeepZOrder(ref position);Check((position.Flags&0x4)!=0&&(position.Flags&0x40)!=0,"show and restore preserve z-order while retaining visibility flags");
 return failures==0?0:1;
 }
}