using System;using MlmDesktop;
class DesktopModeTests {
 static int errors;static void Check(bool b,string message){Console.WriteLine((b?"PASS ":"FAIL ")+message);if(!b)errors++;}
 static int Main(){
 Check(DesktopMode.Active(@"C:\Themes\MLM Desktop.theme",@"c:\themes\MLM Desktop.theme",@"C:\App\MLM Desktop.png",@"c:\app\MLM Desktop.png",true),"selected MLM theme and wallpaper activate rug");
 Check(!DesktopMode.Active(@"C:\Themes\Other.theme",@"C:\Themes\MLM Desktop.theme",@"C:\App\MLM Desktop.png",@"C:\App\MLM Desktop.png",true),"different theme disables rug even with same picture");
 Check(!DesktopMode.Active("a.theme","a.theme","other.png","rug.png",true),"changing picture disables rug");
 Check(!DesktopMode.Active("a.theme","a.theme","rug.png","rug.png",false),"solid color or slideshow disables rug");
 Check(!DesktopMode.Active(null,"a.theme",null,"rug.png",true),"missing Windows state leaves rug disabled");
 Check(DesktopMode.MoveWhole(.5,.5),"plain drag in middle moves whole rug");
 Check(!DesktopMode.MoveWhole(.02,.5)&&!DesktopMode.MoveWhole(.98,.5)&&!DesktopMode.MoveWhole(.5,.02)&&!DesktopMode.MoveWhole(.5,.98),"all four edges fold without modifier keys");
 return errors==0?0:1;
 }
}
