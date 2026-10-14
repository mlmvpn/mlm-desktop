using System;using MlmDesktop;
class GripTests {
 static int errors;static void Check(bool ok,string name){Console.WriteLine((ok?"PASS ":"FAIL ")+name);if(!ok)errors++;}
 static int Main(){var c=new Cloth(0,0,640,440);c.Grab(0,new V3(160,100,100));for(int i=0;i<180;i++)c.Step(1.0/120);V3 old=c.Points[1]-c.Points[0];c.Twist(Math.PI/2);for(int i=0;i<180;i++)c.Step(1.0/120);V3 now=c.Points[1]-c.Points[0];double angle=Math.Atan2(old.X*now.Y-old.Y*now.X,old.X*now.X+old.Y*now.Y);Console.WriteLine("Grip rotation: "+angle);Check(angle>.45,"turning the grip twists the local fabric");Check((c.Points[0]-new V3(160,100,100)).Length<1,"twisting keeps the grabbed point under the hand");c.Release();for(int i=0;i<480;i++)c.Step(1.0/120);bool valid=true;foreach(var p in c.Points)valid&=!double.IsNaN(p.X)&&p.Z>=0;Check(valid,"released twisted rug remains finite above floor");return errors==0?0:1;}
}
