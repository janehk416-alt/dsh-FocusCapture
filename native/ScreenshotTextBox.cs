using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

// Native editable text with a screenshot-pattern background: keeps IME, caret and selection.
// No opaque frame, no replacement white rectangle, no owner-painted fake input control.
internal sealed class ScreenshotTextBox : TextBox {
    private IntPtr backgroundBrush;
    [StructLayout(LayoutKind.Sequential)]private struct RECT {internal int Left,Top,Right,Bottom;}
    [DllImport("gdi32.dll")]private static extern IntPtr CreatePatternBrush(IntPtr bitmap);
    [DllImport("gdi32.dll")]private static extern bool DeleteObject(IntPtr obj);
    [DllImport("gdi32.dll")]private static extern int SetBkMode(IntPtr dc,int mode);
    [DllImport("gdi32.dll")]private static extern int SetTextColor(IntPtr dc,int color);
    [DllImport("gdi32.dll")]private static extern bool SetBrushOrgEx(IntPtr dc,int x,int y,IntPtr previous);
    [DllImport("user32.dll")]private static extern int FillRect(IntPtr dc,ref RECT rect,IntPtr brush);
    [DllImport("user32.dll")]private static extern bool GetClientRect(IntPtr window,out RECT rect);
    [DllImport("user32.dll")]private static extern IntPtr SendMessage(IntPtr hwnd,int message,IntPtr w,IntPtr l);
    internal ScreenshotTextBox(Bitmap screenshotBackground){
        BorderStyle=BorderStyle.None;Multiline=true;AcceptsReturn=true;WordWrap=true;ScrollBars=ScrollBars.None;MaxLength=4096;ImeMode=ImeMode.On;
        BackColor=screenshotBackground.GetPixel(0,0);
        IntPtr bitmap=screenshotBackground.GetHbitmap();try{backgroundBrush=CreatePatternBrush(bitmap);if(backgroundBrush==IntPtr.Zero)throw new InvalidOperationException("Could not create screenshot text background");}finally{DeleteObject(bitmap);}
    }
    internal IntPtr PrepareBackground(IntPtr dc){SetBkMode(dc,1);SetTextColor(dc,ColorTranslator.ToWin32(ForeColor));SetBrushOrgEx(dc,0,0,IntPtr.Zero);return backgroundBrush;}
    protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);SendMessage(Handle,0x00D3,new IntPtr(3),IntPtr.Zero);}
    protected override void WndProc(ref Message m){
        if(m.Msg==0x0014&&backgroundBrush!=IntPtr.Zero){RECT rect;GetClientRect(Handle,out rect);PrepareBackground(m.WParam);FillRect(m.WParam,ref rect,backgroundBrush);m.Result=new IntPtr(1);return;}
        base.WndProc(ref m);
    }
    protected override void Dispose(bool disposing){if(backgroundBrush!=IntPtr.Zero){DeleteObject(backgroundBrush);backgroundBrush=IntPtr.Zero;}base.Dispose(disposing);}
}
