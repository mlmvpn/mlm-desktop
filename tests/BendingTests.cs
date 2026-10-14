using System;using MlmDesktop;
class BendingTests {
 static int failures;static void Check(bool ok,string name){Console.WriteLine((ok?"PASS ":"FAIL ")+name);if(!ok)failures++;}
 static double Angle(V3[] p){V3 a=V3.Cross(p[2]-p[0],p[3]-p[0]),b=V3.Cross(p[3]-p[1],p[2]-p[1]);return Math.Acos(Math.Max(-1,Math.Min(1,V3.Dot(a,b)/(a.Length*b.Length))));}
 static V3 Sum(V3[] p){V3 s=new V3();foreach(var v in p)s+=v;return s;}
 static int Main(){var p=new[]{new V3(0,20,0),new V3(0,-20,20),new V3(0,0,0),new V3(20,0,0)};var before=Sum(p);double initial=Angle(p),lambda=0;for(int i=0;i<8;i++)ClothBending.Project(p,0,1,2,3,-1,1e-6,1.0/120,ref lambda);Check(Angle(p)<initial*.8,"angular bending resists a crease");Check((Sum(p)-before).Length<1e-7,"internal bending conserves center of mass");
 var mirror=new[]{new V3(0,20,0),new V3(0,-20,-20),new V3(0,0,0),new V3(20,0,0)};lambda=0;for(int i=0;i<8;i++)ClothBending.Project(mirror,0,1,2,3,-1,1e-6,1.0/120,ref lambda);double error=0;for(int i=0;i<4;i++)error=Math.Max(error,(p[i]-new V3(mirror[i].X,mirror[i].Y,-mirror[i].Z)).Length);Check(error<1e-7,"bending is symmetric on both sides of the rug");
 var flat=new[]{new V3(0,20,0),new V3(0,-20,0),new V3(0,0,0),new V3(20,0,0)};lambda=0;ClothBending.Project(flat,0,1,2,3,-1,1e-6,1.0/120,ref lambda);Check(Angle(flat)==0&&flat[0].Y==20,"flat rug has no artificial bending force");
 return failures==0?0:1;}
}
