using System;
namespace MlmDesktop {
 // Contacts use material coordinates at impact, so a fast cursor cannot simply
 // re-label a crossed layer. Spatial storage is reused after its initial growth.
 internal sealed class ClothContacts {
  struct Triangle { internal int A,B,C; internal Triangle(int a,int b,int c){A=a;B=b;C=c;} }
  struct Edge {internal int A,B;internal Edge(int a,int b){A=a;B=b;}}
  struct CellKey : IEquatable<CellKey> {
   internal int X,Y,Z;
   internal CellKey(int x,int y,int z){X=x;Y=y;Z=z;}
   public bool Equals(CellKey b){return X==b.X&&Y==b.Y&&Z==b.Z;}
   public override bool Equals(object b){return b is CellKey&&Equals((CellKey)b);}
   public override int GetHashCode(){unchecked{return X*73856093^Y*19349663^Z*83492791;}}
  }
  struct Contact {internal int A,B,C,D;internal double Wa,Wb,Wc,Wd,Lambda,Alpha;internal V3 Normal;}
  readonly double contactAlpha;readonly Triangle[] triangles;readonly Edge[] edges;readonly int[] edgeOrder;
  CellKey[] gridKeys=new CellKey[16384];int[] gridHeads=new int[16384],gridMarks=new int[16384];int gridEpoch=1,gridCount;
  readonly int[] seen,oversized;readonly V3[] minimum,maximum;V3 queryLow,queryHigh;readonly int vertexCount;readonly int[] materialOrder;readonly bool[] neighbors;
  int[] items=new int[16384],next=new int[16384];int entryCount,oversizedCount,stamp;
  Contact[] contacts=new Contact[4096];int contactCount;readonly double[] remainingCorrection,usedCorrection;
  internal readonly bool[] Supported;
  internal int ContactCount {get{return contactCount;}}
  internal int CandidateCount {get;private set;}
  internal int SweptContactCount {get;private set;}
  internal double MaximumCorrection {get;private set;}
  const double Cell=24,Thickness=3.5,Margin=17.5;
  internal ClothContacts(int c,int rows,double step=1.0/120,int neighborRadius=2){contactAlpha=.2/(14400*step*step);
   vertexCount=c*rows;materialOrder=new int[vertexCount];for(int i=0;i<vertexCount;i++)materialOrder[i]=(i%c)*vertexCount+i/c;remainingCorrection=new double[vertexCount];usedCorrection=new double[vertexCount];neighbors=new bool[vertexCount*vertexCount];for(int y=0;y<rows;y++)for(int x=0;x<c;x++)for(int dy=-neighborRadius;dy<=neighborRadius;dy++)for(int dx=-neighborRadius;dx<=neighborRadius;dx++)if(x+dx>=0&&x+dx<c&&y+dy>=0&&y+dy<rows)neighbors[(y*c+x)*vertexCount+(y+dy)*c+x+dx]=true;Supported=new bool[c*rows];triangles=new Triangle[(c-1)*(rows-1)*2];int n=0;
   for(int y=0;y<rows-1;y++)for(int x=0;x<c-1;x++){int a=y*c+x;triangles[n++]=new Triangle(a,a+c,a+1);triangles[n++]=new Triangle(a+1,a+c,a+c+1);}
      edges=new Edge[(c-1)*rows+c*(rows-1)+(c-1)*(rows-1)];n=0;
   for(int y=0;y<rows;y++)for(int x=0;x<c;x++){int a=y*c+x;if(x+1<c)edges[n++]=new Edge(a,a+1);if(y+1<rows)edges[n++]=new Edge(a,a+c);if(x+1<c&&y+1<rows)edges[n++]=new Edge(a+1,a+c);}
   edgeOrder=new int[edges.Length];for(int i=0;i<edgeOrder.Length;i++)edgeOrder[i]=i;
   seen=new int[Math.Max(triangles.Length,edges.Length)];oversized=new int[seen.Length];minimum=new V3[seen.Length];maximum=new V3[seen.Length];
  }
  [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
  static double Dot(V3 a,V3 b){return a.X*b.X+a.Y*b.Y+a.Z*b.Z;}
  [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
  static V3 Cross(V3 a,V3 b){return new V3(a.Y*b.Z-a.Z*b.Y,a.Z*b.X-a.X*b.Z,a.X*b.Y-a.Y*b.X);}
  [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
  static V3 Lerp(V3 a,V3 b,double t){return a+(b-a)*t;}
  [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
  bool Adjacent(int a,int b){return neighbors[a*vertexCount+b];}
  [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
  static void Bounds(V3 a,ref V3 lo,ref V3 hi){lo.X=Math.Min(lo.X,a.X);lo.Y=Math.Min(lo.Y,a.Y);lo.Z=Math.Min(lo.Z,a.Z);hi.X=Math.Max(hi.X,a.X);hi.Y=Math.Max(hi.Y,a.Y);hi.Z=Math.Max(hi.Z,a.Z);}
  [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
  static int CellOf(double v){return (int)Math.Floor((v+Cell*.5)/Cell);}
  [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
  bool Overlap(int item){V3 lo=minimum[item],hi=maximum[item];return queryLow.X<=hi.X+Margin&&queryHigh.X>=lo.X-Margin&&queryLow.Y<=hi.Y+Margin&&queryHigh.Y>=lo.Y-Margin&&queryLow.Z<=hi.Z+Margin&&queryHigh.Z>=lo.Z-Margin;}
  [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
  bool SweptOverlap(int item){V3 lo=minimum[item],hi=maximum[item];const double epsilon=1e-7;return queryLow.X<=hi.X+epsilon&&queryHigh.X>=lo.X-epsilon&&queryLow.Y<=hi.Y+epsilon&&queryHigh.Y>=lo.Y-epsilon&&queryLow.Z<=hi.Z+epsilon&&queryHigh.Z>=lo.Z-epsilon;}
  void ClearGrid(){if(++gridEpoch==int.MaxValue){Array.Clear(gridMarks,0,gridMarks.Length);gridEpoch=1;}gridCount=0;}
  int GridCell(int x,int y,int z,bool create){
   CellKey key=new CellKey(x,y,z);int slot=key.GetHashCode()&(gridHeads.Length-1);
   while(gridMarks[slot]==gridEpoch){if(gridKeys[slot].Equals(key))return slot;slot=(slot+1)&(gridHeads.Length-1);}
   if(!create)return -1;
   if(gridCount*2>=gridHeads.Length){
    CellKey[] oldKeys=gridKeys;int[] oldHeads=gridHeads,oldMarks=gridMarks;int oldEpoch=gridEpoch;
    gridKeys=new CellKey[oldHeads.Length*2];gridHeads=new int[gridKeys.Length];gridMarks=new int[gridKeys.Length];gridEpoch=1;gridCount=0;
    for(int i=0;i<oldHeads.Length;i++)if(oldMarks[i]==oldEpoch){CellKey k=oldKeys[i];int cell=GridCell(k.X,k.Y,k.Z,true);gridHeads[cell]=oldHeads[i];}
    return GridCell(x,y,z,true);
   }
   gridKeys[slot]=key;gridMarks[slot]=gridEpoch;gridHeads[slot]=-1;gridCount++;return slot;
  }
  void Insert(V3 lo,V3 hi,int item){
   minimum[item]=lo;maximum[item]=hi;
   int x0=CellOf(lo.X-Margin),x1=CellOf(hi.X+Margin),y0=CellOf(lo.Y-Margin),y1=CellOf(hi.Y+Margin),z0=CellOf(lo.Z-Margin),z1=CellOf(hi.Z+Margin);
   // Unusually long swept primitives use a bounded fallback instead of filling
   // millions of grid cells after a desktop resize or very large cursor jump.
   if((long)(x1-x0+1)*(y1-y0+1)*(z1-z0+1)>512){oversized[oversizedCount++]=item;return;}
   for(int z=z0;z<=z1;z++)for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++){
    int cell=GridCell(x,y,z,true),head=gridHeads[cell];
    if(entryCount==items.Length){Array.Resize(ref items,items.Length*2);Array.Resize(ref next,next.Length*2);}
    items[entryCount]=item;next[entryCount]=head;gridHeads[cell]=entryCount++;
   }
  }
  void BuildTriangles(V3[] p,V3[] start){
   ClearGrid();entryCount=oversizedCount=0;
   for(int i=0;i<triangles.Length;i++){Triangle t=triangles[i];V3 lo=p[t.A],hi=lo;Bounds(p[t.B],ref lo,ref hi);Bounds(p[t.C],ref lo,ref hi);Bounds(start[t.A],ref lo,ref hi);Bounds(start[t.B],ref lo,ref hi);Bounds(start[t.C],ref lo,ref hi);Insert(lo,hi,i);}
  }
  void Add(int a,int b,int c,int d,double wa,double wb,double wc,double wd,V3 normal,bool swept=false){
   if(contactCount==contacts.Length)Array.Resize(ref contacts,contacts.Length*2);
   contacts[contactCount++]=new Contact{A=a,B=b,C=c,D=d,Wa=wa,Wb=wb,Wc=wc,Wd=wd,Normal=normal,Alpha=swept?0:contactAlpha,Lambda=0};
  }
  // Barycentric coordinates on the plane; rejects degenerate triangles.
  static bool Barycentric(V3 v,V3 a,V3 b,V3 c,out double u,out double w,out double q){
   V3 e=b-a,f=c-a,r=v-a;double ee=Dot(e,e),ff=Dot(f,f),ef=Dot(e,f),er=Dot(e,r),fr=Dot(f,r),den=ee*ff-ef*ef;
   u=w=q=0;if(den<1e-10)return false;w=(ff*er-ef*fr)/den;q=(ee*fr-ef*er)/den;u=1-w-q;return u>=-1e-6&&w>=-1e-6&&q>=-1e-6;
  }
  static double Polynomial(double c0,double c1,double c2,double c3,double t){return ((c3*t+c2)*t+c1)*t+c0;}
  // Earliest coplanarity root in [0,1]. Derivative extrema split the cubic
  // into monotone intervals; this also finds two crossings within one step.
  static int Roots(double c0,double c1,double c2,double c3,ref V3 roots){
   if(Math.Abs(c0)+Math.Abs(c1)+Math.Abs(c2)+Math.Abs(c3)<1e-8)return 0;
   double b=1,c=1;int intervals=1;
   if(Math.Abs(c3)>1e-12){double disc=c2*c2-3*c3*c1;if(disc>0){double s=Math.Sqrt(disc),t1=(-c2-s)/(3*c3),t2=(-c2+s)/(3*c3);if(t1>t2){double swap=t1;t1=t2;t2=swap;}if(t1>0&&t1<1){b=t1;intervals++;}if(t2>0&&t2<1){if(intervals==1)b=t2;else c=t2;intervals++;}}}
   else if(Math.Abs(c2)>1e-12){double t=-c1/(2*c2);if(t>0&&t<1){b=t;intervals=2;}}
   int count=0;double left=0;
   for(int i=0;i<intervals;i++){
    double right=i==0?b:(i==1?c:1);double fl=Polynomial(c0,c1,c2,c3,left),fr=Polynomial(c0,c1,c2,c3,right);
    if(fl*fr<0||Math.Abs(fr)<1e-8){
     double low=left,high=right;
     if(Math.Abs(fr)>=1e-8)for(int k=0;k<26;k++){double mid=(low+high)*.5,fm=Polynomial(c0,c1,c2,c3,mid);if(fl*fm<=0)high=mid;else{low=mid;fl=fm;}}
     else low=high=right;
     double t=(low+high)*.5;if(t>1e-7){if(count==0)roots.X=t;else if(count==1)roots.Y=t;else roots.Z=t;count++;}
    }
    left=right;
   }
   return count;
  }
  static int Coplanarity(V3 a0,V3 b0,V3 c0,V3 d0,V3 a1,V3 b1,V3 c1,V3 d1,ref V3 roots){
   V3 r=d0-a0,e=b0-a0,f=c0-a0,dr=(d1-a1)-r,de=(b1-a1)-e,df=(c1-a1)-f;
   V3 n0=Cross(e,f),n1=Cross(de,f)+Cross(e,df),n2=Cross(de,df);
   return Roots(Dot(r,n0),Dot(dr,n0)+Dot(r,n1),Dot(dr,n1)+Dot(r,n2),Dot(dr,n2),ref roots);
  }
  // When separate pieces start exactly coincident there is no geometric side.
  // Ordering their material coordinates gives reciprocal contacts the same
  // layer order, independent of each face's winding.
  static double TieSide(V3 normal,double materialDifference){double ax=Math.Abs(normal.X),ay=Math.Abs(normal.Y),az=Math.Abs(normal.Z);double dominant=ax>ay&&ax>az?normal.X:(ay>az?normal.Y:normal.Z);return (materialDifference<0?-1:1)*(dominant<0?-1:1);}
  void AllowSweep(int a,int b,int c,int d,double wa,double wb,double wc,double wd,V3[] p,V3[] start){
   V3 relative=(p[a]-start[a])*wa+(p[b]-start[b])*wb+(p[c]-start[c])*wc+(p[d]-start[d])*wd;double allowance=relative.Length+Thickness*2;
   // A pinned moving feature has no correction budget of its own. Its
   // stationary supports must still be able to resolve the actual swept hit.
   if(remainingCorrection[a]>0)remainingCorrection[a]=Math.Max(remainingCorrection[a],Math.Max(0,allowance-usedCorrection[a]));
   if(remainingCorrection[b]>0)remainingCorrection[b]=Math.Max(remainingCorrection[b],Math.Max(0,allowance-usedCorrection[b]));
   if(remainingCorrection[c]>0)remainingCorrection[c]=Math.Max(remainingCorrection[c],Math.Max(0,allowance-usedCorrection[c]));
   if(remainingCorrection[d]>0)remainingCorrection[d]=Math.Max(remainingCorrection[d],Math.Max(0,allowance-usedCorrection[d]));
  }
  void VertexTriangle(int i,int ti,V3[] p,V3[] start){
   if(seen[ti]==stamp)return;seen[ti]=stamp;if(!Overlap(ti))return;Triangle t=triangles[ti];if(Adjacent(i,t.A)||Adjacent(i,t.B)||Adjacent(i,t.C))return;CandidateCount++;
   V3 a=p[t.A],b=p[t.B],c=p[t.C],v=p[i],normal=Cross(b-a,c-a);double len=normal.Length;if(len<1e-8)return;normal=normal*(1/len);
   double u,w,q;bool inside=Barycentric(v,a,b,c,out u,out w,out q);double distance=Dot(v-a,normal);bool nearby=inside&&Math.Abs(distance)<Margin;
   V3 oldNormal=Cross(start[t.B]-start[t.A],start[t.C]-start[t.A]);double oldSide=Dot(start[i]-start[t.A],oldNormal);
   double side=Math.Abs(oldSide)>1e-8?Math.Sign(oldSide):(Math.Abs(distance)>1e-8?Math.Sign(distance):TieSide(normal,materialOrder[i]-u*materialOrder[t.A]-w*materialOrder[t.B]-q*materialOrder[t.C]));

   V3 motion=v-start[i];bool moving=(a-start[t.A]-motion).LengthSquared+(b-start[t.B]-motion).LengthSquared+(c-start[t.C]-motion).LengthSquared>=1e-16;
   if(moving&&SweptOverlap(ti)){
    V3 roots=new V3();int count=Coplanarity(start[t.A],start[t.B],start[t.C],start[i],a,b,c,v,ref roots);
    for(int r=0;r<count;r++){
     double time=r==0?roots.X:(r==1?roots.Y:roots.Z);V3 ah=Lerp(start[t.A],a,time),bh=Lerp(start[t.B],b,time),ch=Lerp(start[t.C],c,time),vh=Lerp(start[i],v,time);double hu,hw,hq;
     if(!Barycentric(vh,ah,bh,ch,out hu,out hw,out hq))continue;
     V3 n=Cross(bh-ah,ch-ah);len=n.Length;if(len<1e-8)continue;n=n*(1/len);
     double approach=Dot(start[i]-(start[t.A]*hu+start[t.B]*hw+start[t.C]*hq),n);if(approach<0)n=n*(-1);
     SweptContactCount++;AllowSweep(i,t.A,t.B,t.C,1,-hu,-hw,-hq,p,start);Add(i,t.A,t.B,t.C,1,-hu,-hw,-hq,n,true);return;
    }
   }
   // A plane-side change without an actual finite-face hit went around an
   // edge. Keep that new side instead of manufacturing a crossing response.
   if(nearby)Add(i,t.A,t.B,t.C,1,-u,-w,-q,normal*(Math.Abs(distance)<1e-8?side:(distance<0?-1:1)));
  }
  static void SegmentParameters(V3 a,V3 b,V3 c,V3 d,out double s,out double t){
   double ex=b.X-a.X,ey=b.Y-a.Y,ez=b.Z-a.Z,fx=d.X-c.X,fy=d.Y-c.Y,fz=d.Z-c.Z,rx=a.X-c.X,ry=a.Y-c.Y,rz=a.Z-c.Z;
   double ee=ex*ex+ey*ey+ez*ez,ff=fx*fx+fy*fy+fz*fz,ef=ex*fx+ey*fy+ez*fz,er=ex*rx+ey*ry+ez*rz,fr=fx*rx+fy*ry+fz*rz;
   s=t=0;if(ee<1e-12&&ff<1e-12)return;
   if(ee<1e-12){t=Math.Max(0,Math.Min(1,fr/ff));return;}
   if(ff<1e-12){s=Math.Max(0,Math.Min(1,-er/ee));return;}
   double den=ee*ff-ef*ef;s=den>1e-10?Math.Max(0,Math.Min(1,(ef*fr-er*ff)/den)):0;t=(ef*s+fr)/ff;
   if(t<0){t=0;s=Math.Max(0,Math.Min(1,-er/ee));}else if(t>1){t=1;s=Math.Max(0,Math.Min(1,(ef-er)/ee));}
  }
  void EdgePair(int ei,int other,V3[] p,V3[] start){
   Edge e=edges[ei],f=edges[other];
   if(Adjacent(e.A,f.A)||Adjacent(e.A,f.B)||Adjacent(e.B,f.A)||Adjacent(e.B,f.B))return;CandidateCount++;
   V3 a=p[e.A],b=p[e.B],c=p[f.A],d=p[f.B];if((b-a).LengthSquared<1e-12||(d-c).LengthSquared<1e-12)return;
   double s,t;SegmentParameters(a,b,c,d,out s,out t);V3 delta=Lerp(a,b,s)-Lerp(c,d,t);double length=delta.Length;V3 proximityNormal=new V3();bool nearby=length<Margin;
   if(nearby){
    V3 n=length>1e-8?delta*(1/length):Cross(b-a,d-c);double nl=n.Length;
    if(nl>1e-8){n=n*(1/nl);double oldSide=Dot(Lerp(start[e.A],start[e.B],s)-Lerp(start[f.A],start[f.B],t),n);
     if(length<1e-8)n=n*(Math.Abs(oldSide)>1e-8?Math.Sign(oldSide):TieSide(n,(1-s)*materialOrder[e.A]+s*materialOrder[e.B]-(1-t)*materialOrder[f.A]-t*materialOrder[f.B]));proximityNormal=n;

    }else nearby=false;
   }
   V3 motion=a-start[e.A];bool moving=(b-start[e.B]-motion).LengthSquared+(c-start[f.A]-motion).LengthSquared+(d-start[f.B]-motion).LengthSquared>=1e-16;
   if(moving&&SweptOverlap(other)){
    V3 roots=new V3();int count=Coplanarity(start[e.A],start[e.B],start[f.A],start[f.B],a,b,c,d,ref roots);
    for(int r=0;r<count;r++){
     double time=r==0?roots.X:(r==1?roots.Y:roots.Z);V3 ah=Lerp(start[e.A],a,time),bh=Lerp(start[e.B],b,time),ch=Lerp(start[f.A],c,time),dh=Lerp(start[f.B],d,time);double hs,ht;
     SegmentParameters(ah,bh,ch,dh,out hs,out ht);if((Lerp(ah,bh,hs)-Lerp(ch,dh,ht)).LengthSquared>1e-8)continue;
     V3 n=Cross(bh-ah,dh-ch);double nl=n.Length;if(nl<1e-8)continue;n=n*(1/nl);
     double oldSide=Dot(Lerp(start[e.A],start[e.B],hs)-Lerp(start[f.A],start[f.B],ht),n);if(oldSide<0)n=n*(-1);
     SweptContactCount++;AllowSweep(e.A,e.B,f.A,f.B,1-hs,hs,ht-1,-ht,p,start);Add(e.A,e.B,f.A,f.B,1-hs,hs,ht-1,-ht,n,true);return;
    }
   }
   if(nearby)Add(e.A,e.B,f.A,f.B,1-s,s,t-1,-t,proximityNormal);
  }
  void EdgeContacts(V3[] p,V3[] start){
   for(int i=0;i<edges.Length;i++){
    Edge e=edges[i];V3 lo=p[e.A],hi=lo;Bounds(p[e.B],ref lo,ref hi);Bounds(start[e.A],ref lo,ref hi);Bounds(start[e.B],ref lo,ref hi);minimum[i]=lo;maximum[i]=hi;
   }
   // Coherent insertion sort: only a small part of the cloth moves each tick.
   for(int i=1;i<edgeOrder.Length;i++){int value=edgeOrder[i],j=i-1;double x=minimum[value].X;while(j>=0&&minimum[edgeOrder[j]].X>x){edgeOrder[j+1]=edgeOrder[j];j--;}edgeOrder[j+1]=value;}
   for(int i=0;i<edgeOrder.Length;i++){
    int a=edgeOrder[i];V3 lo=minimum[a],hi=maximum[a];
    for(int j=i+1;j<edgeOrder.Length;j++){
     int b=edgeOrder[j];V3 otherLo=minimum[b];if(otherLo.X>hi.X+Margin)break;V3 otherHi=maximum[b];
     if(lo.Y>otherHi.Y+Margin||hi.Y<otherLo.Y-Margin||lo.Z>otherHi.Z+Margin||hi.Z<otherLo.Z-Margin)continue;
     int first=Math.Min(a,b),second=Math.Max(a,b);queryLow=minimum[first];queryHigh=maximum[first];
     EdgePair(first,second,p,start);
    }
   }
  }
  [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
  static double ContactMass(int i,int held,double coefficient,V3 normal,V3[] p){return i==held||(p[i].Z<.05&&coefficient*normal.Z<-.001)?0:1;}
  [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
  double Weight(int i,int held,double coefficient,V3 normal,V3[] p){return remainingCorrection[i]<1e-8?0:ContactMass(i,held,coefficient,normal,p);}
  [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
  double BudgetScale(int i,double amount){return Math.Abs(amount)<1e-12?1:Math.Min(1,remainingCorrection[i]/Math.Abs(amount));}
  void Push(V3[] p,int i,double amount,V3 normal){
   double distance=Math.Min(Math.Abs(amount),remainingCorrection[i]);if(distance<=0)return;
   remainingCorrection[i]-=distance;usedCorrection[i]+=distance;MaximumCorrection=Math.Max(MaximumCorrection,usedCorrection[i]);
   double scale=amount<0?-distance:distance;p[i].X+=normal.X*scale;p[i].Y+=normal.Y*scale;p[i].Z+=normal.Z*scale;
  }
  internal void ProjectExisting(V3[] p,int held,int passes){
   for(int pass=0;pass<passes;pass++){
    for(int i=0;i<contactCount;i++){
     Contact t=contacts[i];V3 n=t.Normal,a=p[t.A],b=p[t.B],c=p[t.C],d=p[t.D];double distance=(a.X*t.Wa+b.X*t.Wb+c.X*t.Wc+d.X*t.Wd)*n.X+(a.Y*t.Wa+b.Y*t.Wb+c.Y*t.Wc+d.Y*t.Wd)*n.Y+(a.Z*t.Wa+b.Z*t.Wb+c.Z*t.Wc+d.Z*t.Wd)*n.Z;
     if(distance<Thickness+1){if(t.Wa*n.Z>.2)Supported[t.A]=true;if(t.Wb*n.Z>.2)Supported[t.B]=true;if(t.Wc*n.Z>.2)Supported[t.C]=true;if(t.Wd*n.Z>.2)Supported[t.D]=true;}
     double error=Thickness-distance;if(error<=0&&t.Lambda<=0)continue;
     double direction=error-t.Alpha*t.Lambda<0?-1:1;
     double ma=Weight(t.A,held,t.Wa*direction,n,p),mb=Weight(t.B,held,t.Wb*direction,n,p),mc=Weight(t.C,held,t.Wc*direction,n,p),md=Weight(t.D,held,t.Wd*direction,n,p);
          double den=ma*t.Wa*t.Wa+mb*t.Wb*t.Wb+mc*t.Wc*t.Wc+md*t.Wd*t.Wd;if(den<1e-10)continue;
     // Unilateral XPBD lets packed layers yield slightly instead of fighting
     // the material's strain constraints with a fresh hard impulse each pass.
     double proposed=(error-t.Alpha*t.Lambda)/(den+t.Alpha),correction=Math.Max(0,t.Lambda+proposed)-t.Lambda;if(Math.Abs(correction)<1e-10)continue;
     double scale=Math.Min(Math.Min(BudgetScale(t.A,correction*ma*t.Wa),BudgetScale(t.B,correction*mb*t.Wb)),Math.Min(BudgetScale(t.C,correction*mc*t.Wc),BudgetScale(t.D,correction*md*t.Wd)));
     correction*=scale;t.Lambda+=correction;contacts[i].Lambda=t.Lambda;
     Push(p,t.A,correction*ma*t.Wa,n);Push(p,t.B,correction*mb*t.Wb,n);Push(p,t.C,correction*mc*t.Wc,n);Push(p,t.D,correction*md*t.Wd,n);
    }
    for(int i=0;i<p.Length;i++)if(p[i].Z<0)p[i].Z=0;
   }
  }
  // Velocities are per-tick displacements. Project each contact's relative
  // normal velocity; a sum of position corrections is not a physical normal.
  internal void ProjectVelocities(V3[] p,V3[] velocity,int held){
   for(int pass=0;pass<2;pass++)for(int i=0;i<contactCount;i++){
    Contact t=contacts[i];V3 n=t.Normal;
    V3 separation=p[t.A]*t.Wa+p[t.B]*t.Wb+p[t.C]*t.Wc+p[t.D]*t.Wd;if(Dot(separation,n)>Thickness+1)continue;
    V3 relative=velocity[t.A]*t.Wa+velocity[t.B]*t.Wb+velocity[t.C]*t.Wc+velocity[t.D]*t.Wd;double inward=Dot(relative,n);if(inward>=0)continue;
    double ma=ContactMass(t.A,held,t.Wa,n,p),mb=ContactMass(t.B,held,t.Wb,n,p),mc=ContactMass(t.C,held,t.Wc,n,p),md=ContactMass(t.D,held,t.Wd,n,p);
    double den=ma*t.Wa*t.Wa+mb*t.Wb*t.Wb+mc*t.Wc*t.Wc+md*t.Wd*t.Wd;if(den<1e-10)continue;
    double normalImpulse=-inward/den;V3 impulse=n*normalImpulse,tangent=relative-n*inward;double slip=tangent.Length;
    if(slip>1e-8)impulse-=tangent*(Math.Min(.35*normalImpulse,slip/den)/slip);
    velocity[t.A]+=impulse*(ma*t.Wa);velocity[t.B]+=impulse*(mb*t.Wb);velocity[t.C]+=impulse*(mc*t.Wc);velocity[t.D]+=impulse*(md*t.Wd);
   }
  }
  internal void Resolve(V3[] p,V3[] start,int held){ResolveCore(p,start,held,true);}
  void ResolveCore(V3[] p,V3[] start,int held,bool repair){
   Array.Clear(Supported,0,Supported.Length);contactCount=0;if(repair){CandidateCount=0;SweptContactCount=0;MaximumCorrection=0;}
   // Shared by all cached re-projections in this tick. Actual swept motion
   // enlarges the budget; a stationary contradictory fold cannot manufacture
   // arbitrarily long edges and then an enormous swept broadphase next tick.
   if(repair)for(int i=0;i<p.Length;i++){remainingCorrection[i]=i==held?0:(p[i]-start[i]).Length+Thickness*2;usedCorrection[i]=0;}
   BuildTriangles(p,start);
   for(int i=0;i<p.Length;i++){
    if(++stamp==int.MaxValue){Array.Clear(seen,0,seen.Length);stamp=1;}
    V3 lo=p[i],hi=lo;Bounds(start[i],ref lo,ref hi);queryLow=lo;queryHigh=hi;int x0=CellOf(lo.X),x1=CellOf(hi.X),y0=CellOf(lo.Y),y1=CellOf(hi.Y),z0=CellOf(lo.Z),z1=CellOf(hi.Z);
    if((long)(x1-x0+1)*(y1-y0+1)*(z1-z0+1)>512){for(int ti=0;ti<triangles.Length;ti++)VertexTriangle(i,ti,p,start);continue;}
    for(int z=z0;z<=z1;z++)for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++){int cell=GridCell(x,y,z,false);if(cell<0)continue;int head=gridHeads[cell];for(int e=head;e>=0;e=next[e])VertexTriangle(i,items[e],p,start);}
    for(int k=0;k<oversizedCount;k++)VertexTriangle(i,oversized[k],p,start);
   }
   EdgeContacts(p,start);
   if(repair&&SweptContactCount>0){
    int kept=0;for(int i=0;i<contactCount;i++)if(contacts[i].Alpha==0)contacts[kept++]=contacts[i];contactCount=kept;
    ProjectExisting(p,held,4);
    ResolveCore(p,start,held,false);return;
   }
   ProjectExisting(p,held,4);
  }
 }
}