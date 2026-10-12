using System;
using System.Collections.Generic;
namespace MlmDesktop {
 // Vertex/triangle contact in the desktop projection. Contact side is taken from
 // the pre-step surface, not its new normal: turning a fold over must not swap layers.
 internal sealed class ClothContacts {
  struct Triangle {public int A,B,C;public Triangle(int a,int b,int c){A=a;B=b;C=c;}}
  readonly List<Triangle> triangles=new List<Triangle>();
  readonly Dictionary<long,List<int>> cells=new Dictionary<long,List<int>>();
  internal readonly bool[] Supported; readonly int columns;const double Cell=36,Thickness=3.5;
  internal ClothContacts(int c,int rows){columns=c;Supported=new bool[c*rows];for(int y=0;y<rows-1;y++)for(int x=0;x<c-1;x++){int a=y*c+x;triangles.Add(new Triangle(a,a+c,a+1));triangles.Add(new Triangle(a+1,a+c,a+c+1));}}
  static long Key(int x,int y){return ((long)x<<32)^(uint)y;}
  bool Adjacent(int a,int b){return Math.Abs(a%columns-b%columns)<=2&&Math.Abs(a/columns-b/columns)<=2;}
  internal void Resolve(V3[] p,V3[] start,int held){
   Array.Clear(Supported,0,Supported.Length);cells.Clear();for(int i=0;i<triangles.Count;i++){var t=triangles[i];var a=p[t.A];var b=p[t.B];var c=p[t.C];int x0=(int)Math.Floor(Math.Min(a.X,Math.Min(b.X,c.X))/Cell),x1=(int)Math.Floor(Math.Max(a.X,Math.Max(b.X,c.X))/Cell);int y0=(int)Math.Floor(Math.Min(a.Y,Math.Min(b.Y,c.Y))/Cell),y1=(int)Math.Floor(Math.Max(a.Y,Math.Max(b.Y,c.Y))/Cell);if(x1-x0>30||y1-y0>30)continue;for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++){long key=Key(x,y);List<int> bucket;if(!cells.TryGetValue(key,out bucket)){bucket=new List<int>();cells[key]=bucket;}bucket.Add(i);}}
   for(int pass=0;pass<4;pass++){
    for(int i=0;i<p.Length;i++){
     List<int> bucket;if(!cells.TryGetValue(Key((int)Math.Floor(p[i].X/Cell),(int)Math.Floor(p[i].Y/Cell)),out bucket))continue;
     foreach(int ti in bucket){var t=triangles[ti];if(Adjacent(i,t.A)||Adjacent(i,t.B)||Adjacent(i,t.C))continue;
      var a=p[t.A];var b=p[t.B];var c=p[t.C];var v=p[i];double det=(b.Y-c.Y)*(a.X-c.X)+(c.X-b.X)*(a.Y-c.Y);if(Math.Abs(det)<.01)continue;
      double u=((b.Y-c.Y)*(v.X-c.X)+(c.X-b.X)*(v.Y-c.Y))/det;
      double w=((c.Y-a.Y)*(v.X-c.X)+(a.X-c.X)*(v.Y-c.Y))/det;double q=1-u-w;
      if(u<-.001||w<-.001||q<-.001)continue;
      double surface=u*a.Z+w*b.Z+q*c.Z;double dz=v.Z-surface;
      double oldDelta=start[i].Z-(u*start[t.A].Z+w*start[t.B].Z+q*start[t.C].Z);
      double side=Math.Abs(oldDelta)>.05?Math.Sign(oldDelta):(Math.Abs(dz)>.05?Math.Sign(dz):(i<t.A?1:-1));
      if(side*dz<Thickness+1&&Math.Abs(dz)<10){if(side>0)Supported[i]=true;else{Supported[t.A]=true;Supported[t.B]=true;Supported[t.C]=true;}}
      double error=Thickness-side*dz;if(error<=0 || error>80)continue;
      // Never pull a floor contact below zero; transfer that correction to the upper face.
      double wi=i==held?0:(side<0&&v.Z<.1?0:1);
      double wa=t.A==held?0:(side>0&&a.Z<.1?0:1),wb=t.B==held?0:(side>0&&b.Z<.1?0:1),wc=t.C==held?0:(side>0&&c.Z<.1?0:1);
      double denominator=wi+u*u*wa+w*w*wb+q*q*wc;if(denominator<.0001)continue;
      double correction=side*error/denominator;
      p[i].Z+=correction*wi;p[t.A].Z-=correction*u*wa;p[t.B].Z-=correction*w*wb;p[t.C].Z-=correction*q*wc;
     }
    }
    for(int i=0;i<p.Length;i++)if(p[i].Z<0)p[i].Z=0;
   }
  }
 }
}
