using System;
namespace MlmDesktop {
 internal static class ClothBending {
  internal static void Project(V3[] p,int o0,int o1,int e0,int e1,int held,double compliance,double dt,ref double lambda){
   double ex=p[e1].X-p[e0].X,ey=p[e1].Y-p[e0].Y,ez=p[e1].Z-p[e0].Z;
   double length=Math.Sqrt(ex*ex+ey*ey+ez*ez);if(length<1e-6)return;
   double bx=p[o0].X-p[e0].X,by=p[o0].Y-p[e0].Y,bz=p[o0].Z-p[e0].Z;
   double cx=p[o1].X-p[e0].X,cy=p[o1].Y-p[e0].Y,cz=p[o1].Z-p[e0].Z;
   double nx=ey*bz-ez*by,ny=ez*bx-ex*bz,nz=ex*by-ey*bx;
   double mx=cy*ez-cz*ey,my=cz*ex-cx*ez,mz=cx*ey-cy*ex;
   double a=nx*nx+ny*ny+nz*nz,b=mx*mx+my*my+mz*mz;if(a<1e-10||b<1e-10)return;
   double theta=Math.Atan2(ex*(ny*mz-nz*my)+ey*(nz*mx-nx*mz)+ez*(nx*my-ny*mx),length*(nx*mx+ny*my+nz*mz));
   double ia=1/a,ib=1/b;nx*=ia;ny*=ia;nz*=ia;mx*=ib;my*=ib;mz*=ib;
   double g0x=nx*(-length),g0y=ny*(-length),g0z=nz*(-length),g1x=mx*(-length),g1y=my*(-length),g1z=mz*(-length);
   double be=bx*ex+by*ey+bz*ez,ce=cx*ex+cy*ey+cz*ez,il=1/length;
   double g3x=(nx*be+mx*ce)*il,g3y=(ny*be+my*ce)*il,g3z=(nz*be+mz*ce)*il;
   double g2x=(g0x+g1x+g3x)*(-1),g2y=(g0y+g1y+g3y)*(-1),g2z=(g0z+g1z+g3z)*(-1);
   double w0=o0==held?0:1,w1=o1==held?0:1,w2=e0==held?0:1,w3=e1==held?0:1;
   double alpha=compliance/(dt*dt),den=w0*(g0x*g0x+g0y*g0y+g0z*g0z)+w1*(g1x*g1x+g1y*g1y+g1z*g1z)+w2*(g2x*g2x+g2y*g2y+g2z*g2z)+w3*(g3x*g3x+g3y*g3y+g3z*g3z)+alpha;
   if(den<1e-12)return;double change=(-theta-alpha*lambda)/den;lambda+=change;
   double q=w0*change;p[o0].X+=g0x*q;p[o0].Y+=g0y*q;p[o0].Z+=g0z*q;
   q=w1*change;p[o1].X+=g1x*q;p[o1].Y+=g1y*q;p[o1].Z+=g1z*q;
   q=w2*change;p[e0].X+=g2x*q;p[e0].Y+=g2y*q;p[e0].Z+=g2z*q;
   q=w3*change;p[e1].X+=g3x*q;p[e1].Y+=g3y*q;p[e1].Z+=g3z*q;
  }
 }
}