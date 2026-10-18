using System;
using System.Diagnostics;
using System.Globalization;
using MlmDesktop;

// Public-API diagnostics: no reflection, solver constants, or private state.
// Compile beside Cloth.cs, ClothContacts.cs and ClothBending.cs, or reference a frozen baseline DLL.
internal static class PhysicsDiagnostics {
 const double Width=640, Height=440;
 static bool desktop;static Cloth NewCloth(){return desktop?new Cloth(0,0,Width,Height,13,9):new Cloth(0,0,Width,Height);}
 static readonly CultureInfo Invariant=CultureInfo.InvariantCulture;
 sealed class Metrics {
  internal double PeakStretch=1, FinalStretch=1, PeakZ, FinalZ, FinalRmsSpeed, HandError, FarCornerTravel;
  internal bool Finite=true;
 }
 static bool Finite(double x){return !double.IsNaN(x)&&!double.IsInfinity(x);}
 static double Length(V3 a){return Math.Sqrt(a.X*a.X+a.Y*a.Y+a.Z*a.Z);}
 static double Stretch(Cloth cloth){
  double result=0;
  for(int r=0;r<cloth.Rows;r++)for(int c=0;c<cloth.Columns;c++){
   int i=r*cloth.Columns+c;
   if(c+1<cloth.Columns)result=Math.Max(result,Length(cloth.Points[i+1]-cloth.Points[i])/(Width/(cloth.Columns-1)));
   if(r+1<cloth.Rows)result=Math.Max(result,Length(cloth.Points[i+cloth.Columns]-cloth.Points[i])/(Height/(cloth.Rows-1)));
  }
  return result;
 }
 static V3 Target(string scenario,double t){
  if(scenario=="fast")return new V3(600,400,180)*Math.Min(t/.15,1);
  if(t<=.75)return new V3(350,180,130)*Math.Min(t/.75,1);
  double a=(t-.75)*2*Math.PI;
  return new V3(200+150*Math.Cos(a),180+150*Math.Sin(a),130+25*Math.Sin(a*.5));
 }
 static Metrics Trajectory(string scenario,double hz){
  Cloth cloth=NewCloth();
  V3[] before=new V3[cloth.Points.Length];
  V3 farStart=cloth.Points[cloth.Points.Length-1];
  double dt=1/hz,held=scenario=="fast"?.75:2.75, duration=held+4;
  int count=(int)Math.Round(duration*hz),release=(int)Math.Round(held*hz);
  Metrics m=new Metrics();cloth.Grab(0,cloth.Points[0]);
  double finalSpeedSquare=0;int finalSamples=0;
  for(int step=0;step<count;step++){
   if(step==release)cloth.Release();
   V3 hand=Target(scenario,(step+1)*dt);
   if(step<release)cloth.Pull(hand);
   Array.Copy(cloth.Points,before,before.Length);cloth.Step(dt);
   double sq=0;
   for(int i=0;i<before.Length;i++){
    V3 p=cloth.Points[i];m.Finite &= Finite(p.X)&&Finite(p.Y)&&Finite(p.Z)&&p.Z>=-1e-7;
    m.PeakZ=Math.Max(m.PeakZ,p.Z);V3 change=p-before[i];double speed=Length(change)/dt;sq+=speed*speed;
   }
   if(step>=count-(int)(hz*.5)){finalSpeedSquare+=sq/before.Length;finalSamples++;}
   m.PeakStretch=Math.Max(m.PeakStretch,Stretch(cloth));
   if(step<release)m.HandError=Math.Max(m.HandError,Length(cloth.Points[0]-hand));
  }
  for(int i=0;i<cloth.Points.Length;i++)m.FinalZ=Math.Max(m.FinalZ,cloth.Points[i].Z);
  m.FinalRmsSpeed=Math.Sqrt(finalSpeedSquare/finalSamples);m.FinalStretch=Stretch(cloth);
  m.FarCornerTravel=Length(cloth.Points[cloth.Points.Length-1]-farStart);
  return m;
 }
 static Cloth Hold(double hz){
  Cloth c=NewCloth();c.Grab(0,new V3(260,160,120));
  for(int i=0;i<(int)hz;i++)c.Step(1/hz);
  return c;
 }
 static void Timesteps(){
  Cloth reference=Hold(120);
  Console.WriteLine("timestep_hz,rms_distance_from_120hz_px,max_distance_from_120hz_px,max_stretch");
  foreach(double hz in new double[]{30,60,120,240}){
   Cloth c=Hold(hz);double sq=0,max=0;
   for(int i=0;i<c.Points.Length;i++){double d=Length(c.Points[i]-reference.Points[i]);sq+=d*d;max=Math.Max(max,d);}
   Console.WriteLine(Format(hz)+","+Format(Math.Sqrt(sq/c.Points.Length))+","+Format(max)+","+Format(Stretch(c)));
  }
 }
 static string Format(double x){return x.ToString("F6",Invariant);}
 static void Bench(string scenario){
  const int Count=900;const double Dt=1.0/120;
  Cloth c=NewCloth();V3[] targets=new V3[Count];long[] ticks=new long[Count];
  c.Grab(0,new V3(0,0,0));
  for(int i=0;i<Count;i++)targets[i]=Target("twist",(300+i+1)*Dt);
  for(int i=0;i<300;i++){if(scenario=="twist")c.Pull(Target("twist",(i+1)*Dt));c.Step(Dt);}
  AppDomain.MonitoringIsEnabled=true;GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();
  long bytes=AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize;int gc0=GC.CollectionCount(0);
  Stopwatch wall=Stopwatch.StartNew();
  for(int i=0;i<Count;i++){
   if(scenario=="twist")c.Pull(targets[i]);
   long start=Stopwatch.GetTimestamp();c.Step(Dt);ticks[i]=Stopwatch.GetTimestamp()-start;
  }
  wall.Stop();bytes=AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize-bytes;gc0=GC.CollectionCount(0)-gc0;
  Array.Sort(ticks);double factor=1000.0/Stopwatch.Frequency;
  Console.WriteLine(scenario+","+Format(wall.Elapsed.TotalMilliseconds/Count)+","+Format(ticks[Count/2]*factor)+","+Format(ticks[(int)(Count*.95)]*factor)+","+Format((double)bytes/Count)+","+gc0);
 }
 static void Main(string[] args){
  desktop=Array.IndexOf(args,"--desktop")>=0;
  Console.WriteLine("Runtime="+Environment.Version+"; 64bit="+Environment.Is64BitProcess+"; CPU="+Environment.ProcessorCount);
  if(Array.IndexOf(args,"--bench-only")>=0){Console.WriteLine("benchmark,mean_ms_step,median_ms_step,p95_ms_step,allocated_bytes_step,gen0_collections");Bench("flat");Bench("twist");return;}
  Timesteps();
  Console.WriteLine("trajectory,hz,peak_stretch,final_stretch,peak_z_px,final_z_px,final_rms_speed_px_sec,peak_hand_error_px,far_corner_travel_px,finite_above_floor");
  foreach(string scenario in new string[]{"fast","twist"}){
   Metrics m=Trajectory(scenario,120);
   Console.WriteLine(scenario+",120,"+Format(m.PeakStretch)+","+Format(m.FinalStretch)+","+Format(m.PeakZ)+","+Format(m.FinalZ)+","+Format(m.FinalRmsSpeed)+","+Format(m.HandError)+","+Format(m.FarCornerTravel)+","+m.Finite);
  }
  Console.WriteLine("benchmark,mean_ms_step,median_ms_step,p95_ms_step,allocated_bytes_step,gen0_collections");
  Bench("flat");Bench("twist");
 }
}

