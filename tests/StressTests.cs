using System;using MlmDesktop;
class StressTests {
 static int errors;static void Check(bool ok,string name){Console.WriteLine((ok?"PASS ":"FAIL ")+name);if(!ok)errors++;}
 static double Stretch(Cloth c){double m=1;for(int r=0;r<c.Rows;r++)for(int x=0;x<c.Columns;x++){int i=r*c.Columns+x;if(x<c.Columns-1)m=Math.Max(m,(c.Points[i+1]-c.Points[i]).Length/20);if(r<c.Rows-1)m=Math.Max(m,(c.Points[i+c.Columns]-c.Points[i]).Length/20);}return m;}
 static void Run(bool circle){var c=new Cloth(0,0,640,440);var before=new V3[c.Points.Length];c.Grab(0,c.Points[0]);int held=circle?330:90,total=held+720;double max=1,speed=0;bool finite=true;
 for(int i=0;i<total;i++){double t=(i+1)/120.0;if(i==held)c.Release();if(i<held){V3 target;if(!circle)target=new V3(600,400,180)*Math.Min(t/.15,1);else if(t<=.75)target=new V3(350,180,130)*(t/.75);else{double a=(t-.75)*2*Math.PI;target=new V3(200+150*Math.Cos(a),180+150*Math.Sin(a),130+25*Math.Sin(a*.5));}c.Pull(target);}Array.Copy(c.Points,before,before.Length);c.Step(1.0/120);max=Math.Max(max,Stretch(c));for(int j=0;j<c.Points.Length;j++){V3 p=c.Points[j];finite&=!double.IsNaN(p.X)&&!double.IsNaN(p.Y)&&!double.IsNaN(p.Z)&&!double.IsInfinity(p.X)&&!double.IsInfinity(p.Y)&&!double.IsInfinity(p.Z)&&p.Z>=0;if(i>=total-60)speed+=(p-before[j]).LengthSquared*14400/(60*c.Points.Length);}}
 string name=circle?"two circular turns":"fast drag";Console.WriteLine(name+": stretch="+max.ToString("F4")+"; settled RMS speed="+Math.Sqrt(speed).ToString("F4"));Check(max<1.5,name+" keeps structural stretch below 50 percent");Check(Math.Sqrt(speed)<2,name+" comes to rest after release");Check(finite,name+" remains finite above floor");}
 static int Main(){Run(false);Run(true);return errors==0?0:1;}
}
