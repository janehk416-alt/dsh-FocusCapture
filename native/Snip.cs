using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

internal sealed class Chord {
    private bool latched;
    public bool Step(bool left,bool right){if(!left&&!right)latched=false;if(left&&right&&!latched){latched=true;return true;}return false;}
}
internal static class Native {
    [DllImport("user32.dll")] internal static extern bool SetProcessDpiAwarenessContext(IntPtr value);
    [DllImport("user32.dll")] internal static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] internal static extern bool SetForegroundWindow(IntPtr hwnd);
    [DllImport("kernel32.dll",SetLastError=true)] internal static extern IntPtr OpenProcess(uint access,bool inherit,uint id);
    [DllImport("kernel32.dll")] internal static extern uint WaitForSingleObject(IntPtr handle,uint milliseconds);
    [DllImport("kernel32.dll")] internal static extern bool CloseHandle(IntPtr handle);
}
internal static class Program {
    private static readonly JavaScriptSerializer Json=new JavaScriptSerializer{MaxJsonLength=32*1024*1024};
    private static bool armed,english,busy,closing;
    private static Control dispatcher;
    private static RegionForm overlay;
    internal static void Emit(object value){lock(Json){Console.WriteLine(Json.Serialize(value));Console.Out.Flush();}}
    private static void CaptureRegion(){
        if(busy||!armed)return;busy=true;
        try{
            overlay=new RegionForm(delegate(byte[] png){Post(delegate{overlay=null;busy=false;});if(png==null)Emit(new{kind="cancel"});else Emit(new{kind="image",data=Convert.ToBase64String(png)});},english);
            overlay.Show();
        }catch(Exception ex){busy=false;Emit(new{kind="error",message=ex.Message});}
    }
    private static void Post(Action action){try{if(!closing&&!dispatcher.IsDisposed)dispatcher.BeginInvoke(action);}catch{}}
    private static void Command(string line){
        if(line=="stop"){PinnedShot.CloseAll();if(overlay!=null)overlay.Close();Application.ExitThread();}
        else if(line=="arm")armed=true;else if(line=="disarm")armed=false;
        else if(line=="english")english=true;else if(line=="chinese")english=false;
        else if(line=="capture")CaptureRegion();
        else if(line=="status")Emit(new{kind="native-status",armed=armed,busy=busy,version="1.3.0"});
    }
    [STAThread] private static int Main(string[] args){
        Console.InputEncoding=new UTF8Encoding(false);Console.OutputEncoding=new UTF8Encoding(false);
        if(args.Length>0&&args[0]=="--self-test"){
            try{
                Chord c=new Chord();if(c.Step(true,false)||!c.Step(true,true)||c.Step(true,true)||c.Step(false,true)||c.Step(true,true))throw new Exception("latch");
                c.Step(false,false);if(!c.Step(true,true))throw new Exception("rearm");c=new Chord();if(c.Step(false,true)||!c.Step(true,true))throw new Exception("right-first");
                Rectangle r=RegionForm.Normalize(new Point(200,150),new Point(-10,20),new Rectangle(0,0,300,300));if(r!=new Rectangle(0,20,200,130))throw new Exception("geometry");
                Emit(new{kind="self-test",ok=true,tests=DrawingTests.Run(),uiTests=EditorTests.Run(),hotkeyTests=new[]{"left-first","right-first","hold-no-repeat","release-rearm","crop-clamping"}});return 0;
            }catch(Exception ex){Emit(new{kind="self-test",ok=false,message=ex.Message});return 5;}
        }
        try{Native.SetProcessDpiAwarenessContext(new IntPtr(-4));}catch{Native.SetProcessDPIAware();}
        Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        dispatcher=new Control();IntPtr hwnd=dispatcher.Handle;IntPtr parent=IntPtr.Zero;
        try{if(args.Length>0){parent=Native.OpenProcess(0x00100000,false,uint.Parse(args[0]));if(parent==IntPtr.Zero)throw new Exception("Cannot monitor parent: Win32 "+Marshal.GetLastWin32Error());}}
        catch(Exception ex){Emit(new{kind="error",message=ex.Message});return 6;}
        System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer();timer.Interval=1000;
        timer.Tick+=delegate{if(parent!=IntPtr.Zero&&Native.WaitForSingleObject(parent,0)!=258)Application.ExitThread();};
        Thread reader=new Thread(delegate(){try{string line;while((line=Console.ReadLine())!=null){string next=line;Post(delegate{Command(next);});}}catch{}Post(delegate{Application.ExitThread();});});
        reader.IsBackground=true;reader.Start();timer.Start();Emit(new{kind="ready",pid=Process.GetCurrentProcess().Id,version="1.3.0"});
        try{Application.Run();}
        finally{closing=true;timer.Dispose();PinnedShot.CloseAll();if(overlay!=null)overlay.Dispose();dispatcher.Dispose();if(parent!=IntPtr.Zero)Native.CloseHandle(parent);}
        return 0;
    }
}
