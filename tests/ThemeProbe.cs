using System;using MlmDesktop;
class ThemeProbe { [STAThread]static int Main(string[] args){try{Console.WriteLine("MLM selected: "+ThemeSelection.IsSelected(args[0]));return 0;}catch(Exception e){Console.WriteLine(e);return 1;}}}
