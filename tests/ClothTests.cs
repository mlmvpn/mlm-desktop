using System;
using MlmDesktop;
class Tests {
 static int failures;
 static void Check(bool ok,string name){Console.WriteLine((ok?"PASS ":"FAIL ")+name);if(!ok)failures++;}
 static double Dist(V3 a,V3 b){return Math.Sqrt(Math.Pow(a.X-b.X,2)+Math.Pow(a.Y-b.Y,2)+Math.Pow(a.Z-b.Z,2));}
 static double HeightUnder(Cloth c,int index,int maxColumn){var p=c.Points[index];for(int r=0;r<c.Rows-1;r++)for(int x=0;x<maxColumn;x++){int a=r*c.Columns+x;foreach(var tri in new[]{new[]{a,a+c.Columns,a+1},new[]{a+1,a+c.Columns,a+c.Columns+1}}){var u=c.Points[tri[0]];var v=c.Points[tri[1]];var w=c.Points[tri[2]];double det=(v.Y-w.Y)*(u.X-w.X)+(w.X-v.X)*(u.Y-w.Y);if(Math.Abs(det)<.001)continue;double b=((v.Y-w.Y)*(p.X-w.X)+(w.X-v.X)*(p.Y-w.Y))/det;double d=((w.Y-u.Y)*(p.X-w.X)+(u.X-w.X)*(p.Y-w.Y))/det;if(b>=0&&d>=0&&b+d<=1)return b*u.Z+d*v.Z+(1-b-d)*w.Z;}}return double.PositiveInfinity;}
static int Main(){
 var c=new Cloth(100,100,640,440);
 Check(Math.Abs(c.Points[c.Points.Length-1].X-740)<0.01 && Math.Abs(c.Points[c.Points.Length-1].Y-540)<0.01,"rug spans specified desktop rectangle");
 c.Grab(0,new V3(330,230,100)); for(int n=0;n<180;n++)c.Step(1.0/120);
 Check(Dist(c.Points[0],new V3(330,230,100))<1,"grabbed corner follows hand");
 double maxEdge=0;for(int r=0;r<c.Rows;r++)for(int x=0;x<c.Columns-1;x++)maxEdge=Math.Max(maxEdge,Dist(c.Points[r*c.Columns+x],c.Points[r*c.Columns+x+1]));
 Check(maxEdge>15 && maxEdge<32,"cloth bends without excessive stretching");
 Check(c.Points[c.Points.Length-1].X>650,"opposite corner resists a local fold");
 c.Release();for(int n=0;n<500;n++)c.Step(1.0/120);
 bool valid=true;double maxZ=0; foreach(var p in c.Points){valid &= !double.IsNaN(p.X)&&!double.IsInfinity(p.Y)&&p.Z>=0;maxZ=Math.Max(maxZ,p.Z);}
 Console.WriteLine("Settled height: "+maxZ+"; corner: "+c.Points[0].X+","+c.Points[0].Y); Check(Dist(c.Points[0],new V3(100,100,0))>150,"fold remains open after release for icon placement");Check(valid && maxZ<50 && c.Points[0].Z<20,"released corner lands while allowing a folded ridge");
 var before=c.Points[500]; c.Move(75,-30);var after=c.Points[500];
 Check(Math.Abs(after.X-before.X-75)<.01 && Math.Abs(after.Y-before.Y+30)<.01,"whole-rug move preserves deformation");
 c.Flatten();Check(Math.Abs(c.Points[0].Z)<.01 && Math.Abs(Dist(c.Points[0],c.Points[1])-20)<.01,"reset removes folds and restores rest lengths");
 c.Step(30); foreach(var p in c.Points)valid &= !double.IsNaN(p.X);Check(valid,"long frame pause remains stable");
 var folded=new Cloth(0,0,640,440); for(int r=0;r<folded.Rows;r++)for(int x=0;x<folded.Columns;x++){int i=r*folded.Columns+x;if(x>20)folded.Points[i]=new V3(800-x*20,r*20,4);} Array.Copy(folded.Points,(V3[])typeof(Cloth).GetField("previous",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(folded),folded.Points.Length); for(int n=0;n<240;n++)folded.Step(1.0/120); int upper=11*folded.Columns+30; double lowerHeight=HeightUnder(folded,upper,18);Console.WriteLine("Top clearance: "+(folded.Points[upper].Z-lowerHeight));Check(folded.Points[upper].Z-lowerHeight>=2,"folded top layer stays above lower surface");
return failures==0?0:1;
 }
}
