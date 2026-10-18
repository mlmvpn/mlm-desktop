using System;using MlmDesktop;
class DesktopPhysicsTests {
 static int failures;static void Check(bool ok,string name){Console.WriteLine((ok?"PASS ":"FAIL ")+name);if(!ok)failures++;}
 static Cloth Rug(){return new Cloth(0,0,640,440,13,9);}
 static bool Finite(V3 p){return !(double.IsNaN(p.X)||double.IsNaN(p.Y)||double.IsNaN(p.Z)||double.IsInfinity(p.X)||double.IsInfinity(p.Y)||double.IsInfinity(p.Z))&&p.Z>=0;}
 static double Stretch(Cloth c){double m=1;for(int r=0;r<c.Rows;r++)for(int x=0;x<c.Columns;x++){int i=r*c.Columns+x;if(x+1<c.Columns)m=Math.Max(m,(c.Points[i+1]-c.Points[i]).Length/(640.0/(c.Columns-1)));if(r+1<c.Rows)m=Math.Max(m,(c.Points[i+c.Columns]-c.Points[i]).Length/(440.0/(c.Rows-1)));}return m;}
 static int Main(){
  var local=Rug();V3 far=local.Points[local.Points.Length-1];local.Grab(0,new V3(180,130,100));for(int i=0;i<120;i++)local.Step(1.0/120);double farTravel=(local.Points[local.Points.Length-1]-far).Length;Console.WriteLine("Opposite corner travel: "+farTravel);Check(farTravel<20,"corner grab bends locally instead of dragging the opposite corner");
  var c=Rug();V3 target=new V3(300,200,120);c.Grab(0,target);for(int i=0;i<60;i++)c.Step(1.0/120);Check((c.Points[0]-target).Length<.01,"desktop corner catches up to a stationary hand within half a simulated second");
  var before=(V3[])c.Points.Clone();c.Move(75,-30);bool translated=true;for(int i=0;i<before.Length;i++)translated&=(c.Points[i]-before[i]-new V3(75,-30,0)).Length<1e-8;Check(translated,"desktop center drag translates every particle immediately");
  var a=Rug();var b=Rug();a.Grab(0,target);b.Grab(0,target);for(int i=0;i<30;i++)a.Step(1.0/30);for(int i=0;i<120;i++)b.Step(1.0/120);double difference=0;for(int i=0;i<a.Points.Length;i++)difference=Math.Max(difference,(a.Points[i]-b.Points[i]).Length);Check(difference<.001,"desktop physics preserves time across render rates");
  c=Rug();c.Grab(0,c.Points[0]);double maximum=1;bool valid=true;for(int i=0;i<330+720;i++){double t=(i+1)/120.0;if(i==330)c.Release();if(i<330){double angle=Math.Max(0,t-.75)*2*Math.PI;c.Pull(t<=.75?new V3(350,180,130)*(t/.75):new V3(200+150*Math.Cos(angle),180+150*Math.Sin(angle),130+25*Math.Sin(angle*.5)));}c.Step(1.0/120);maximum=Math.Max(maximum,Stretch(c));foreach(var p in c.Points)valid&=Finite(p);}Console.WriteLine("Desktop circular-pull peak stretch: "+maximum);Check(maximum<1.5,"desktop mesh withstands repeated circular pulling");Check(valid,"desktop drag and release remain finite above the floor");
  // Settling/creep is measured by PhysicsDiagnostics, not hidden behind a sleep
  // threshold. The desktop mesh does not meet the dense model's <2 px/s bound.
  return failures==0?0:1;
 }
}