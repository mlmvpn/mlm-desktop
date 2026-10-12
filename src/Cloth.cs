using System;
using System.Collections.Generic;
namespace MlmDesktop {
 public struct V3 {
  public double X,Y,Z;
  public V3(double x,double y,double z){X=x;Y=y;Z=z;}
  public static V3 operator +(V3 a,V3 b){return new V3(a.X+b.X,a.Y+b.Y,a.Z+b.Z);}
  public static V3 operator -(V3 a,V3 b){return new V3(a.X-b.X,a.Y-b.Y,a.Z-b.Z);}
  public static V3 operator *(V3 a,double b){return new V3(a.X*b,a.Y*b,a.Z*b);}
  public double Length {get{return Math.Sqrt(X*X+Y*Y+Z*Z);}}
 }
 public sealed class Cloth {
  struct Link {public int A,B;public double Length,Strength;public Link(int a,int b,double l,double s){A=a;B=b;Length=l;Strength=s;}}
  public V3[] Points; V3[] previous; List<Link> links=new List<Link>(); ClothContacts contacts;
  public int Columns=33,Rows=23;public int Grabbed=-1;
  double originX,originY,width,height;V3 target;
  public Cloth(double x,double y,double w,double h){originX=x;originY=y;width=w;height=h;Points=new V3[Columns*Rows];previous=new V3[Points.Length];Flatten();
   for(int r=0;r<Rows;r++)for(int c=0;c<Columns;c++){
    int i=r*Columns+c;
    if(c+1<Columns)Add(i,i+1,1);if(r+1<Rows)Add(i,i+Columns,1);
    if(c+1<Columns && r+1<Rows){Add(i,i+Columns+1,.75);Add(i+1,i+Columns,.75);}
    if(c+2<Columns)Add(i,i+2,.002);if(r+2<Rows)Add(i,i+2*Columns,.002);
   }
  }
  void Add(int a,int b,double s){links.Add(new Link(a,b,(Points[b]-Points[a]).Length,s));}
  public void Grab(int index,V3 point){if(index<0||index>=Points.Length)return;Grabbed=index;target=point;}
  public void Pull(V3 point){target=point;}
  public void Release(){if(Grabbed>=0)previous[Grabbed]=Points[Grabbed];Grabbed=-1;}
  public void Move(double dx,double dy){var d=new V3(dx,dy,0);for(int i=0;i<Points.Length;i++){Points[i]+=d;previous[i]+=d;}originX+=dx;originY+=dy;target+=d;}
  public void Flatten(){Grabbed=-1;for(int r=0;r<Rows;r++)for(int c=0;c<Columns;c++){int i=r*Columns+c;Points[i]=previous[i]=new V3(originX+c*width/(Columns-1),originY+r*height/(Rows-1),0);}}
  public void Step(double dt){
   dt=Math.Max(0,Math.Min(dt,1.0/60)); if(contacts==null)contacts=new ClothContacts(Columns,Rows);var start=(V3[])Points.Clone();
   for(int i=0;i<Points.Length;i++){if(i==Grabbed)continue;var p=Points[i];var velocity=(p-previous[i])*((p.Z<.5||contacts.Supported[i])?.65:.985);previous[i]=p;Points[i]=p+velocity+new V3(0,0,-4200*dt*dt);if(Points[i].Z<0)Points[i].Z=0;}
   for(int k=0;k<9;k++){
    if(Grabbed>=0)Points[Grabbed]=target;
    foreach(var l in links){var delta=Points[l.B]-Points[l.A];double len=delta.Length;if(len<.0001)continue;
     double wa=l.A==Grabbed?0:(Points[l.A].Z<.5?.14:1),wb=l.B==Grabbed?0:(Points[l.B].Z<.5?.14:1);if(wa+wb==0)continue;
     var correction=delta*((len-l.Length)/len*l.Strength/(wa+wb));Points[l.A]+=correction*wa;Points[l.B]-=correction*wb;
    }
    for(int i=0;i<Points.Length;i++)if(Points[i].Z<0)Points[i].Z=0;
   }
   if(Grabbed>=0){Points[Grabbed]=target;previous[Grabbed]=target;}
   var contactStart=(V3[])Points.Clone();contacts.Resolve(Points,start,Grabbed);for(int i=0;i<Points.Length;i++)if(Math.Abs(Points[i].Z-contactStart[i].Z)>.001)previous[i].Z=Points[i].Z;
  }
 }
}
