using System;
using System.Collections.Generic;
namespace MlmDesktop {
 public struct V3 {
  public double X,Y,Z;
  [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
  public V3(double x,double y,double z){X=x;Y=y;Z=z;}
  [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
  public static V3 operator +(V3 a,V3 b){return new V3(a.X+b.X,a.Y+b.Y,a.Z+b.Z);}
  [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
  public static V3 operator -(V3 a,V3 b){return new V3(a.X-b.X,a.Y-b.Y,a.Z-b.Z);}
  [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
  public static V3 operator *(V3 a,double b){return new V3(a.X*b,a.Y*b,a.Z*b);}
  public double LengthSquared {get{return X*X+Y*Y+Z*Z;}}
  public double Length {get{return Math.Sqrt(LengthSquared);}}
  [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
  public static double Dot(V3 a,V3 b){return a.X*b.X+a.Y*b.Y+a.Z*b.Z;}
  [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
  public static V3 Cross(V3 a,V3 b){return new V3(a.Y*b.Z-a.Z*b.Y,a.Z*b.X-a.X*b.Z,a.X*b.Y-a.Y*b.X);}
 }
 public sealed class Cloth {
  struct Link {public int A,B;public double Rest,Compliance;public Link(int a,int b,double rest,double compliance){A=a;B=b;Rest=rest;Compliance=compliance;}}
  const double Tick=1.0/240,Gravity=4200,HandSpeed=1800;
  public readonly int Columns,Rows;
  public readonly V3[] Points;
  readonly V3[] previous,start,velocities;
  readonly Link[] links;
  readonly double[] lambdas,bendLambdas;
  readonly int[] hinges;
  readonly ClothContacts contacts;
  double originX,originY,width,height,accumulator;
  V3 target,hand;
  readonly int[] grip=new int[8];readonly V3[] gripOffset=new V3[8];int gripCount;double twist,twistTarget;
  public int Grabbed=-1;
  public double MaximumMotion {get;private set;}
  public Cloth(double x,double y,double w,double h,int columns=33,int rows=23){
   if(columns<3||rows<3||columns>65||rows>65)throw new ArgumentOutOfRangeException("columns","Cloth grid must be between 3 and 65 nodes per axis.");Columns=columns;Rows=rows;
   originX=x;originY=y;width=w;height=h;
   Points=new V3[Columns*Rows];previous=new V3[Points.Length];start=new V3[Points.Length];velocities=new V3[Points.Length];
   contacts=new ClothContacts(Columns,Rows,Tick,Columns<=17?1:2);Flatten();
   var built=new List<Link>();
   for(int r=0;r<Rows;r++)for(int c=0;c<Columns;c++){
    int i=r*Columns+c;
    if(c+1<Columns)Add(built,i,i+1,1e-8);if(r+1<Rows)Add(built,i,i+Columns,1e-8);
    // Match the rendered triangle edge. A second crossing diagonal locks the hinge into a rigid quad.
    if(c+1<Columns&&r+1<Rows)Add(built,i+1,i+Columns,8e-8);

   }
   links=built.ToArray();lambdas=new double[links.Length];
   var shared=new Dictionary<long,int>();var bends=new List<int>();
   for(int r=0;r<Rows-1;r++)for(int c=0;c<Columns-1;c++){int i=r*Columns+c;AddFace(shared,bends,i,i+Columns,i+1);AddFace(shared,bends,i+1,i+Columns,i+Columns+1);}
   hinges=bends.ToArray();bendLambdas=new double[hinges.Length/4];
  }
  void Add(List<Link> list,int a,int b,double compliance){list.Add(new Link(a,b,(Points[b]-Points[a]).Length,compliance));}
  static void AddFace(Dictionary<long,int> shared,List<int> bends,int a,int b,int c){AddHinge(shared,bends,a,b,c);AddHinge(shared,bends,b,c,a);AddHinge(shared,bends,c,a,b);}
  static void AddHinge(Dictionary<long,int> shared,List<int> bends,int a,int b,int opposite){long key=((long)Math.Min(a,b)<<32)|(uint)Math.Max(a,b);int other;if(shared.TryGetValue(key,out other)){bends.Add(other);bends.Add(opposite);bends.Add(a);bends.Add(b);}else shared[key]=opposite;}
  public void Twist(double radians){if(Grabbed>=0&&!double.IsNaN(radians)&&!double.IsInfinity(radians))twistTarget+=Math.Max(-Math.PI,Math.Min(Math.PI,radians));}
  void SolveGrip(){if(Grabbed<0)return;double cosine=Math.Cos(twist),sine=Math.Sin(twist);for(int j=0;j<gripCount;j++){V3 d=gripOffset[j];V3 desired=hand+new V3(d.X*cosine-d.Y*sine,d.X*sine+d.Y*cosine,d.Z);V3 correction=desired-Points[grip[j]];double length=correction.Length;double weight=.18;Points[grip[j]]+=correction*(length>8?weight*8/length:weight);}}
  static bool Finite(V3 p){return !(double.IsNaN(p.X)||double.IsNaN(p.Y)||double.IsNaN(p.Z)||double.IsInfinity(p.X)||double.IsInfinity(p.Y)||double.IsInfinity(p.Z));}
  public void Grab(int index,V3 point){if(index<0||index>=Points.Length||!Finite(point))return;Grabbed=index;hand=Points[index];target=point;target.Z=Math.Max(0,target.Z);twist=twistTarget=0;gripCount=0;int row=index/Columns,col=index%Columns;for(int r=Math.Max(0,row-1);r<=Math.Min(Rows-1,row+1);r++)for(int c=Math.Max(0,col-1);c<=Math.Min(Columns-1,col+1);c++){int i=r*Columns+c;if(i==index)continue;grip[gripCount]=i;gripOffset[gripCount++]=Points[i]-hand;}}
  public void Pull(V3 point){if(!Finite(point))return;target=point;target.Z=Math.Max(0,target.Z);}
  public void Release(){if(Grabbed>=0)previous[Grabbed]=Points[Grabbed];Grabbed=-1;}
  public void Move(double dx,double dy){if(double.IsNaN(dx)||double.IsNaN(dy)||double.IsInfinity(dx)||double.IsInfinity(dy))return;var d=new V3(dx,dy,0);for(int i=0;i<Points.Length;i++){Points[i]+=d;previous[i]+=d;}originX+=dx;originY+=dy;target+=d;hand+=d;}
  public void Flatten(){Grabbed=-1;accumulator=0;MaximumMotion=0;for(int r=0;r<Rows;r++)for(int c=0;c<Columns;c++){int i=r*Columns+c;Points[i]=previous[i]=new V3(originX+c*width/(Columns-1),originY+r*height/(Rows-1),0);}if(contacts!=null)Array.Clear(contacts.Supported,0,contacts.Supported.Length);}
  public void Step(double dt){
   MaximumMotion=0;if(dt<=0||double.IsNaN(dt)||double.IsInfinity(dt))return;
   // Fixed physical ticks make damping, stiffness and input filtering independent of rendering.
   accumulator+=Math.Min(dt,1.0/15);int budget=16;
   while(accumulator+1e-10>=Tick&&budget-->0){Simulate();accumulator-=Tick;}
  }
  void LimitStrain(int passes){
   for(int pass=0;pass<passes;pass++)for(int n=0;n<links.Length;n++){
    Link l=links[(pass&1)==0?n:links.Length-1-n];V3 delta=Points[l.B]-Points[l.A];double maximum=l.Rest*1.10,minimum=l.Rest*.90,lengthSquared=delta.LengthSquared;if(lengthSquared<=maximum*maximum&&lengthSquared>=minimum*minimum)continue;if(lengthSquared<1e-10)continue;
    double length=Math.Sqrt(lengthSquared);double desired=length>maximum?maximum:minimum;double wa=l.A==Grabbed?0:1,wb=l.B==Grabbed?0:1;if(wa+wb==0)continue;V3 correction=delta*((length-desired)/(length*(wa+wb)));Points[l.A]+=correction*wa;Points[l.B]-=correction*wb;
   }
  }
  void ProjectMaterial(int k){
    // Alternating traversal avoids a persistent left-to-right solver bias.
    for(int n=0;n<links.Length;n++){
     int j=(k&1)==0?n:links.Length-1-n;Link l=links[j];V3 delta=Points[l.B]-Points[l.A];double len=delta.Length;if(len<1e-8)continue;
     double wa=l.A==Grabbed?0:1,wb=l.B==Grabbed?0:1,alpha=l.Compliance/(Tick*Tick);
     double dl=(-(len-l.Rest)-alpha*lambdas[j])/(wa+wb+alpha);lambdas[j]+=dl;
     V3 change=delta*(dl/len);Points[l.A]-=change*wa;Points[l.B]+=change*wb;
    }
    for(int j=0;j<bendLambdas.Length;j++){int h=j*4;ClothBending.Project(Points,hinges[h],hinges[h+1],hinges[h+2],hinges[h+3],Grabbed,2e-5,Tick,ref bendLambdas[j]);}
  }
  bool RestingFlat(){
   // The floor exactly balances gravity for an undeformed, motionless rug.
   // Validate public particle arrays as callers may supply a folded fixture.
   if(Grabbed>=0&&((target-hand).LengthSquared>1e-18||Math.Abs(twistTarget)>1e-12))return false;
   for(int r=0;r<Rows;r++)for(int c=0;c<Columns;c++){
    int i=r*Columns+c;V3 p=Points[i],old=previous[i];
    if(p.Z!=0||old.Z!=0||p.X!=old.X||p.Y!=old.Y||Math.Abs(p.X-(originX+c*width/(Columns-1)))>1e-9||Math.Abs(p.Y-(originY+r*height/(Rows-1)))>1e-9)return false;
   }
   return true;
  }
  void Simulate(){
   if(RestingFlat())return;
   Array.Copy(Points,start,Points.Length);Array.Clear(lambdas,0,lambdas.Length);Array.Clear(bendLambdas,0,bendLambdas.Length);
   if(Grabbed>=0){V3 delta=target-hand;double length=delta.Length;double handStep=Math.Min(HandSpeed*Tick,(Columns<=17?.2:.4)*Tick*120*Math.Min(width/(Columns-1),height/(Rows-1)));hand+=delta*(length>handStep?handStep/length:1);}
   for(int i=0;i<Points.Length;i++){
    V3 p=Points[i],v=(p-previous[i])*.994485;
    if(p.Z<.05||contacts.Supported[i]){v.X*=.848528;v.Y*=.848528;}
    previous[i]=p;Points[i]=p+v+new V3(0,0,-Gravity*Tick*Tick);
   }
   twist+=Math.Max(-3*Tick,Math.Min(3*Tick,twistTarget-twist));
   for(int k=0;k<4;k++){
    if(Grabbed>=0)Points[Grabbed]=hand;
    if(k<4)SolveGrip();
    ProjectMaterial(k);
    for(int i=0;i<Points.Length;i++)if(Points[i].Z<0)Points[i].Z=0;
   }
   if(Grabbed>=0)Points[Grabbed]=hand;
   LimitStrain(8);
   // Static floor friction precedes contact projection so it cannot erase separation.
   for(int i=0;i<Points.Length;i++)if(i!=Grabbed&&Points[i].Z<=.001){
    Points[i].Z=0;V3 slip=Points[i]-start[i];double limit=.65*Gravity*Tick*Tick;
    if(slip.X*slip.X+slip.Y*slip.Y<=limit*limit){Points[i].X=start[i].X;Points[i].Y=start[i].Y;}
   }

   contacts.Resolve(Points,start,Grabbed);
   for(int relax=0;relax<8;relax++){ProjectMaterial(relax);for(int i=0;i<Points.Length;i++)if(Points[i].Z<0)Points[i].Z=0;contacts.ProjectExisting(Points,Grabbed,1);}
   // Reconstruct velocity from the coupled solution. Resolve each material contact
   // separately: averaging normals loses constraints where several folds meet.
   for(int i=0;i<Points.Length;i++)velocities[i]=Points[i]-start[i];
   contacts.ProjectVelocities(Points,velocities,Grabbed);
   for(int i=0;i<Points.Length;i++){
    V3 p=Points[i],velocity=velocities[i];
    if(p.Z<=.001){
     p.Z=0;velocity.Z=Math.Max(0,velocity.Z);
     double slip=Math.Sqrt(velocity.X*velocity.X+velocity.Y*velocity.Y);
     double friction=slip<=.65*Gravity*Tick*Tick?0:Math.Max(0,1-.45*Gravity*Tick*Tick/slip);
     velocity.X*=friction;velocity.Y*=friction;
    }
    Points[i]=p;previous[i]=p-velocity;
    if(i==Grabbed)previous[i]=p;
    MaximumMotion=Math.Max(MaximumMotion,(p-start[i]).Length);
   }
  }
 }
}
