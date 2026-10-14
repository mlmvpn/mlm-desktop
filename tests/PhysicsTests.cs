using System;using MlmDesktop;
class PhysicsTests {
 static int failures;
 static void Check(bool value,string name){Console.WriteLine((value?"PASS ":"FAIL ")+name);if(!value)failures++;}
 static double MaxStretch(Cloth c){double worst=0;for(int y=0;y<c.Rows;y++)for(int x=0;x<c.Columns;x++){int i=y*c.Columns+x;if(x+1<c.Columns)worst=Math.Max(worst,(c.Points[i+1]-c.Points[i]).Length/20);if(y+1<c.Rows)worst=Math.Max(worst,(c.Points[i+c.Columns]-c.Points[i]).Length/20);}return worst;}
 static bool Finite(V3 p){return !(double.IsNaN(p.X)||double.IsNaN(p.Y)||double.IsNaN(p.Z)||double.IsInfinity(p.X)||double.IsInfinity(p.Y)||double.IsInfinity(p.Z));}
 static int Main(){
  var c=new Cloth(0,0,640,440);c.Grab(0,new V3(180,130,110));var start=(V3[])c.Points.Clone();c.Step(0);double moved=0;for(int i=0;i<start.Length;i++)moved=Math.Max(moved,(c.Points[i]-start[i]).Length);Check(moved==0,"zero elapsed time does not change cloth");
  var a=new Cloth(0,0,640,440);var b=new Cloth(0,0,640,440);a.Grab(0,new V3(180,130,110));b.Grab(0,new V3(180,130,110));for(int i=0;i<30;i++)a.Step(1.0/30);for(int i=0;i<120;i++)b.Step(1.0/120);double error=0;for(int i=0;i<a.Points.Length;i++)error=Math.Max(error,(a.Points[i]-b.Points[i]).Length);Console.WriteLine("30/120 Hz maximum difference: "+error.ToString("F3"));Check(error<.001,"frame chunking preserves the same physical trajectory");
  c=new Cloth(0,0,640,440);c.Grab(0,new V3(600,400,180));c.Step(1.0/120);double stretch=MaxStretch(c);Console.WriteLine("Fast grab stretch: "+stretch.ToString("F3"));Check(stretch<1.35,"fast mouse jump does not stretch yarn by more than 35 percent");
  c.Release();for(int i=0;i<360;i++)c.Step(1.0/120);bool finite=true;foreach(var p in c.Points)finite&=Finite(p)&&p.Z>=0;Check(finite,"released fast drag settles above the floor without invalid coordinates");
  var ground=new Cloth(0,0,640,440);ground.Grab(0,new V3(.1,0,0));for(int i=0;i<8;i++)ground.Step(1.0/120);Check((ground.Points[0]-new V3(.1,0,0)).Length<.001,"floor friction does not override the held point");
  return failures==0?0:1;
 }
}
