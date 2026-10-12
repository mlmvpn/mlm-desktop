using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
namespace MlmDesktop {
 internal static class Program {
  internal static readonly string Data=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MLM Desktop");
  internal static readonly string Assets=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets");
  internal static readonly string Wallpaper=Path.Combine(Assets,"MLM Desktop.png");
  internal static void Log(Exception e){try{Directory.CreateDirectory(Data);File.AppendAllText(Path.Combine(Data,"error.log"),DateTime.UtcNow+" "+e+Environment.NewLine);}catch{}}
  [STAThread]static int Main(string[] args){
   bool preview=args.Contains("--preview"),created;
   using(var mutex=new Mutex(true,preview?"Local\\MLMDesktopPreview":"Local\\MLMDesktop",out created)){
    if(!created)return 0;
    if(!preview)Autostart.EnsureInBackground(System.Reflection.Assembly.GetExecutingAssembly().Location,Log);
    var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};
    app.DispatcherUnhandledException+=delegate(object s,DispatcherUnhandledExceptionEventArgs e){Log(e.Exception);};
    using(var quit=new EventWaitHandle(false,EventResetMode.AutoReset,preview?"Local\\MLMDesktopPreview.Exit":"Local\\MLMDesktop.Exit")){
     var waiter=ThreadPool.RegisterWaitForSingleObject(quit,delegate(object state,bool timedOut){app.Dispatcher.BeginInvoke(new Action(delegate{app.Shutdown();}));},null,-1,false);
     try{if(preview){app.ShutdownMode=ShutdownMode.OnMainWindowClose;return app.Run(new RugWindow(true,args));}
      using(var controller=new ThemeController(app,args))return app.Run();
     }catch(Exception e){Log(e);return 1;}finally{waiter.Unregister(null);}
    }
   }
  }
 }
 // No tray, taskbar button, startup window, or wallpaper writes. Windows Settings
 // owns selection; this controller only follows the installed theme and its image.
 internal sealed class ThemeController:IDisposable {
  readonly DispatcherTimer timer;readonly Application app;readonly string[] args;RugWindow window;bool loggedFailure;
  internal ThemeController(Application a,string[] arguments){app=a;args=arguments;timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(900)};timer.Tick+=delegate{Refresh();};timer.Start();app.Startup+=delegate{Refresh();};}
  void Refresh(){bool active=false;try{active=ThemeSelection.IsSelected(Program.Wallpaper);loggedFailure=false;}catch(Exception e){if(!loggedFailure){Program.Log(e);loggedFailure=true;}}
   if(active&&window==null){window=new RugWindow(false,args);window.Show();}
   else if(!active&&window!=null){window.Close();window=null;}
  }
  public void Dispose(){timer.Stop();if(window!=null){window.Close();window=null;}}
 }
 internal sealed class RugWindow:Window {
  readonly bool preview;readonly string[] args;readonly Grid root=new Grid();readonly Viewport3D viewport=new Viewport3D();
  MeshGeometry3D mesh,shadow;GeometryModel3D rug;Cloth cloth;OrthographicCamera camera;
  readonly Stopwatch clock=Stopwatch.StartNew();double lastTime,accumulator;int quiet;bool awake=true,dragging,moving;
  Point lastMouse,pressPoint;double lift;IntPtr hwnd;DispatcherTimer desktopTimer;
  ImageBrush wood;ImageSource rugImage;
  public RugWindow(bool p,string[] a){
   preview=p;args=a;Title=preview?"MLM Desktop — Preview":"MLM Desktop";
   Width=preview?1100:SystemParameters.PrimaryScreenWidth;Height=preview?700:SystemParameters.PrimaryScreenHeight;
   Left=preview?40:0;Top=preview?30:0;WindowStyle=preview?WindowStyle.SingleBorderWindow:WindowStyle.None;
   ResizeMode=preview?ResizeMode.CanResize:ResizeMode.NoResize;ShowInTaskbar=preview;ShowActivated=preview;
   if(!preview){AllowsTransparency=true;Background=Brushes.Transparent;}else Background=Brushes.Black;
   Content=root;root.Children.Add(viewport);viewport.ClipToBounds=false;
   rugImage=LoadImage(Path.Combine(Program.Assets,"rug.png"));
   if(File.Exists(Program.Wallpaper))wood=new ImageBrush(LoadImage(Program.Wallpaper)){Stretch=Stretch.UniformToFill};
   if(preview){root.Background=(Brush)wood??new SolidColorBrush(Color.FromRgb(194,153,103));BuildPreviewCaption();}
   Loaded+=OnLoaded;SizeChanged+=delegate{if(IsLoaded){UpdateCamera();ResetRug();}};
   viewport.MouseDown+=OnDown;viewport.MouseMove+=OnMove;viewport.MouseUp+=OnUp;
   viewport.LostMouseCapture+=delegate{if(dragging){cloth.Release();dragging=false;awake=true;}};
   KeyDown+=delegate(object s,KeyEventArgs e){if(e.Key==Key.F)ResetRug();if(e.Key==Key.Escape&&dragging){cloth.Release();dragging=false;Mouse.Capture(null);}};
   Closed+=delegate{CompositionTarget.Rendering-=Animate;if(desktopTimer!=null)desktopTimer.Stop();if(dragging)Mouse.Capture(null);};
  }
  static BitmapImage LoadImage(string path){var b=new BitmapImage();b.BeginInit();b.UriSource=new Uri(path);b.CacheOption=BitmapCacheOption.OnLoad;b.EndInit();b.Freeze();return b;}
  void OnLoaded(object sender,RoutedEventArgs e){
   hwnd=new WindowInteropHelper(this).Handle;
   if(!preview){long style=Native.GetWindowLongPtr(hwnd,-20).ToInt64();Native.SetWindowLongPtr(hwnd,-20,new IntPtr(style|0x08000080));HwndSource.FromHwnd(hwnd).AddHook(Hook);}
   BuildScene();ResetRug();CompositionTarget.Rendering+=Animate;
   if(!preview){desktopTimer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(600)};desktopTimer.Tick+=delegate{if(WindowState==WindowState.Minimized)WindowState=WindowState.Normal;Native.PlaceOnDesktop(hwnd);};desktopTimer.Start();Native.PlaceOnDesktop(hwnd);}
   int snapshot=Array.IndexOf(args,"--snapshot");if(preview&&snapshot>=0&&snapshot+1<args.Length){
    if(args.Contains("--fold")){int i=cloth.Columns-1;var corner=cloth.Points[i];cloth.Grab(i,new V3(corner.X-230,corner.Y+180,85));for(int k=0;k<120;k++)cloth.Step(1.0/120);cloth.Release();for(int k=0;k<150;k++)cloth.Step(1.0/120);UpdateMesh();}
    var t=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(500)};t.Tick+=delegate{t.Stop();SaveSnapshot(args[snapshot+1]);Close();};t.Start();
   }
  }
  IntPtr Hook(IntPtr h,int msg,IntPtr w,IntPtr l,ref bool handled){if(msg==0x21){handled=true;return new IntPtr(3);}if(msg==0x7E){Width=SystemParameters.PrimaryScreenWidth;Height=SystemParameters.PrimaryScreenHeight;ResetRug();}return IntPtr.Zero;}
  void BuildScene(){
   camera=new OrthographicCamera{LookDirection=new Vector3D(0,0,-1),UpDirection=new Vector3D(0,1,0),NearPlaneDistance=.1,FarPlaneDistance=4000};viewport.Camera=camera;UpdateCamera();
   var group=new Model3DGroup();group.Children.Add(new AmbientLight(Color.FromRgb(170,170,170)));group.Children.Add(new DirectionalLight(Color.FromRgb(110,106,100),new Vector3D(.3,-.5,-1)));
   mesh=new MeshGeometry3D();shadow=new MeshGeometry3D();
   var shadowMat=new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(60,30,18,10)));
   group.Children.Add(new GeometryModel3D(shadow,shadowMat){BackMaterial=shadowMat});
   var material=new DiffuseMaterial(new ImageBrush(rugImage){ViewportUnits=BrushMappingMode.RelativeToBoundingBox,Stretch=Stretch.Fill});
   var back=new MaterialGroup();back.Children.Add(new DiffuseMaterial(new ImageBrush(rugImage)));back.Children.Add(new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(65,137,102,68))));
   rug=new GeometryModel3D(mesh,material){BackMaterial=back};group.Children.Add(rug);viewport.Children.Add(new ModelVisual3D{Content=group});
  }
  void UpdateCamera(){if(camera==null)return;double w=Math.Max(1,viewport.ActualWidth),h=Math.Max(1,viewport.ActualHeight);camera.Position=new Point3D(w/2,-h/2,1600);camera.Width=w;}
  void ResetRug(){
   double w=Math.Max(300,viewport.ActualWidth),h=Math.Max(250,viewport.ActualHeight);double rw=Math.Min(760,w*.55),rh=rw*2/3;
   cloth=new Cloth((w-rw)/2,(h-rh)/2,rw,rh);awake=true;quiet=0;
   if(mesh==null)return;var uv=new PointCollection();var indices=new Int32Collection();
   for(int r=0;r<cloth.Rows;r++)for(int c=0;c<cloth.Columns;c++){uv.Add(new Point((double)c/(cloth.Columns-1),(double)r/(cloth.Rows-1)));if(c<cloth.Columns-1 && r<cloth.Rows-1){int i=r*cloth.Columns+c;indices.Add(i);indices.Add(i+cloth.Columns);indices.Add(i+1);indices.Add(i+1);indices.Add(i+cloth.Columns);indices.Add(i+cloth.Columns+1);}}
   uv.Freeze();indices.Freeze();mesh.TextureCoordinates=uv;mesh.TriangleIndices=indices;shadow.TriangleIndices=indices;UpdateMesh();
  }
  void UpdateMesh(){var points=new Point3DCollection(cloth.Points.Length);var shade=new Point3DCollection(cloth.Points.Length);foreach(var v in cloth.Points){points.Add(new Point3D(v.X,-v.Y,v.Z));shade.Add(new Point3D(v.X+4+v.Z*.10,-v.Y-5-v.Z*.12,-2));}mesh.Positions=points;shadow.Positions=shade;}
  void Animate(object sender,EventArgs e){
   double now=clock.Elapsed.TotalSeconds;double elapsed=Math.Min(.05,now-lastTime);lastTime=now;if(!awake||cloth==null)return;
   accumulator+=elapsed;double movement=0;int n=0;while(accumulator>=1.0/120 && n++<6){V3[] before=(V3[])cloth.Points.Clone();cloth.Step(1.0/120);for(int i=0;i<before.Length;i++)movement=Math.Max(movement,(cloth.Points[i]-before[i]).Length);accumulator-=1.0/120;}
   UpdateMesh();if(!dragging&&movement<.015){quiet++;if(quiet>45)awake=false;}else quiet=0;
  }
  void OnDown(object sender,MouseButtonEventArgs e){
   if(e.ChangedButton==MouseButton.Right)return;Point pt=e.GetPosition(viewport);RayMeshGeometry3DHitTestResult hit=null;
   VisualTreeHelper.HitTest(viewport,null,delegate(HitTestResult result){var h=result as RayMeshGeometry3DHitTestResult;if(h!=null&&h.ModelHit==rug){hit=h;return HitTestResultBehavior.Stop;}return HitTestResultBehavior.Continue;},new PointHitTestParameters(pt));
   if(hit==null)return;
   if(e.ClickCount==2){ResetRug();return;}
   Point a=mesh.TextureCoordinates[hit.VertexIndex1],b=mesh.TextureCoordinates[hit.VertexIndex2],c=mesh.TextureCoordinates[hit.VertexIndex3]; Point uv=new Point(a.X*hit.VertexWeight1+b.X*hit.VertexWeight2+c.X*hit.VertexWeight3,a.Y*hit.VertexWeight1+b.Y*hit.VertexWeight2+c.Y*hit.VertexWeight3); moving=e.ChangedButton==MouseButton.Middle || DesktopMode.MoveWhole(uv.X,uv.Y);
   dragging=true;pressPoint=lastMouse=pt;lift=hit.PointHit.Z;awake=true;quiet=0;
   if(!moving){int index=hit.VertexIndex1;double best=hit.VertexWeight1;if(hit.VertexWeight2>best){index=hit.VertexIndex2;best=hit.VertexWeight2;}if(hit.VertexWeight3>best)index=hit.VertexIndex3;cloth.Grab(index,new V3(pt.X,pt.Y,Math.Max(4,lift)));}
   Mouse.Capture(viewport);viewport.Cursor=Cursors.Hand;e.Handled=true;
  }
  void OnMove(object sender,MouseEventArgs e){if(!dragging)return;Point pt=e.GetPosition(viewport);
   pt.X=Math.Max(12,Math.Min(viewport.ActualWidth-12,pt.X));pt.Y=Math.Max(12,Math.Min(viewport.ActualHeight-12,pt.Y));
   if(moving){double dx=pt.X-lastMouse.X,dy=pt.Y-lastMouse.Y;cloth.Move(dx,dy);}else{double length=(pt-pressPoint).Length;cloth.Pull(new V3(pt.X,pt.Y,Math.Max(lift,Math.Min(130,14+length*.28))));}
   lastMouse=pt;awake=true;e.Handled=true;
  }
  void OnUp(object sender,MouseButtonEventArgs e){if(!dragging)return;cloth.Release();dragging=false;Mouse.Capture(null);viewport.Cursor=Cursors.Arrow;awake=true;e.Handled=true;}
  void BuildPreviewCaption(){var panel=new StackPanel{Orientation=Orientation.Vertical,HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Bottom,Margin=new Thickness(32),IsHitTestVisible=false};panel.Children.Add(new TextBlock{Text="MLM DESKTOP",FontFamily=new FontFamily("Segoe UI Semibold"),FontSize=22,Foreground=new SolidColorBrush(Color.FromRgb(55,34,24))});panel.Children.Add(new TextBlock{Text="Grab a corner. Make a little room.",FontSize=14,Margin=new Thickness(0,6,0,0),Foreground=new SolidColorBrush(Color.FromRgb(74,49,31))});root.Children.Add(panel);}
  void SaveSnapshot(string path){var bitmap=new RenderTargetBitmap((int)root.ActualWidth,(int)root.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(root);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using(var stream=File.Create(path))encoder.Save(stream);}
 }
}
