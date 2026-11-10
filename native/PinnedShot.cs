using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

internal static class ImageActions {
    internal static void Copy(Bitmap image){Clipboard.SetDataObject(image,true,3,40);}
    internal static bool Save(Bitmap image,IWin32Window owner){using(SaveFileDialog dialog=new SaveFileDialog()){dialog.Title="dsh-FocusCapture · 保存截图";dialog.Filter="PNG 图片 (*.png)|*.png";dialog.DefaultExt="png";dialog.AddExtension=true;dialog.OverwritePrompt=true;dialog.RestoreDirectory=true;dialog.FileName="截图_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".png";if(dialog.ShowDialog(owner)!=DialogResult.OK)return false;image.Save(dialog.FileName,ImageFormat.Png);return true;}}
}
internal sealed class PinnedShot : Form {
    private static readonly List<PinnedShot> OpenShots=new List<PinnedShot>();
    private readonly Bitmap image;
    private readonly System.Windows.Forms.Timer topTimer=new System.Windows.Forms.Timer();
    private readonly ContextMenuStrip menu=new ContextMenuStrip();
    private Point dragStart,windowStart;
    private bool dragging;
    private const int Header=24;
    [DllImport("user32.dll",SetLastError=true)]private static extern bool SetWindowPos(IntPtr hwnd,IntPtr after,int x,int y,int width,int height,uint flags);
    internal static void ShowShot(Bitmap source,Point position){if(OpenShots.Count>=8)OpenShots[0].Close();PinnedShot shot=new PinnedShot(source,position);OpenShots.Add(shot);shot.Show();shot.EnsureTopmost();}
    internal static void CloseAll(){foreach(PinnedShot shot in OpenShots.ToArray())shot.Close();OpenShots.Clear();}
    [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")]private static extern IntPtr GetWindowLongPtr(IntPtr hwnd,int index);
    [DllImport("user32.dll",EntryPoint="GetWindowLongW")]private static extern int GetWindowLong(IntPtr hwnd,int index);
    internal static bool TestTopmost(){using(Bitmap b=new Bitmap(20,20))using(PinnedShot shot=new PinnedShot(b,new Point(100,100))){IntPtr handle=shot.Handle;long style=IntPtr.Size==8?GetWindowLongPtr(handle,-20).ToInt64():GetWindowLong(handle,-20);return shot.TopMost&&(style&8)!=0&&!shot.ShowInTaskbar;}}
    private PinnedShot(Bitmap source,Point position){
        image=(Bitmap)source.Clone();Text="dsh-FocusCapture · 贴图";FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;DoubleBuffered=true;KeyPreview=true;AutoScaleMode=AutoScaleMode.None;StartPosition=FormStartPosition.Manual;
        Rectangle work=Screen.FromPoint(position).WorkingArea;float scale=Math.Min(1,Math.Min((work.Width-32)/(float)image.Width,(work.Height-Header-32)/(float)image.Height));ClientSize=new Size(Math.Max(60,(int)(image.Width*scale)),Math.Max(40,(int)(image.Height*scale))+Header);Location=new Point(Math.Max(work.Left,Math.Min(work.Right-Width,position.X)),Math.Max(work.Top,Math.Min(work.Bottom-Height,position.Y)));
        menu.Items.Add("复制图片",null,delegate{try{ImageActions.Copy(image);}catch(Exception ex){MessageBox.Show(this,ex.Message,"复制失败");}});
        menu.Items.Add("另存为…",null,delegate{try{ImageActions.Save(image,this);}catch(Exception ex){MessageBox.Show(this,ex.Message,"保存失败");}});
        menu.Items.Add("关闭贴图",null,delegate{Close();});ContextMenuStrip=menu;
        topTimer.Interval=1000;topTimer.Tick+=delegate{EnsureTopmost();};topTimer.Start();
        FormClosed+=delegate{OpenShots.Remove(this);};
    }
    private void EnsureTopmost(){if(IsDisposed||!IsHandleCreated)return;TopMost=true;SetWindowPos(Handle,new IntPtr(-1),0,0,0,0,0x0001|0x0002|0x0010);}
    protected override void OnShown(EventArgs e){base.OnShown(e);EnsureTopmost();}
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);Ui.Prepare(e.Graphics);e.Graphics.Clear(Color.White);using(Brush b=new SolidBrush(Color.FromArgb(245,245,245)))e.Graphics.FillRectangle(b,0,0,Width,Header);string title="dsh-FocusCapture · 置顶贴图";TextRenderer.DrawText(e.Graphics,title,UiTypography.Panel(title),new Rectangle(7,1,Math.Max(1,Width-34),Header-2),Ui.Ink,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);Ui.Icon(e.Graphics,"cancel",new Rectangle(Width-22,4,15,15),Ui.Ink);e.Graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;e.Graphics.DrawImage(image,new Rectangle(1,Header,Width-2,Height-Header-1));using(Pen p=new Pen(Ui.Border))e.Graphics.DrawRectangle(p,0,0,Width-1,Height-1);}
    protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button!=MouseButtons.Left)return;if(e.Y<Header&&e.X>Width-28){Close();return;}dragging=true;dragStart=MousePosition;windowStart=Location;Capture=true;}
    protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(dragging){Point p=MousePosition;Location=new Point(windowStart.X+p.X-dragStart.X,windowStart.Y+p.Y-dragStart.Y);}}
    protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);dragging=false;Capture=false;}
    protected override void OnMouseWheel(MouseEventArgs e){base.OnMouseWheel(e);float ratio=e.Delta>0?1.1f:1/1.1f;Rectangle work=Screen.FromControl(this).WorkingArea;int w=Math.Max(80,Math.Min(work.Width,(int)(Width*ratio)));int h=Math.Max(40,(int)(w/(float)image.Width*image.Height))+Header;if(h>work.Height){h=work.Height;w=(int)((h-Header)/(float)image.Height*image.Width);}ClientSize=new Size(w,h);EnsureTopmost();Invalidate();}
    protected override bool ProcessCmdKey(ref Message msg,Keys keys){if(keys==Keys.Escape){Close();return true;}if(keys==(Keys.Control|Keys.C)){try{ImageActions.Copy(image);}catch{}return true;}return base.ProcessCmdKey(ref msg,keys);}
    protected override void Dispose(bool disposing){if(disposing){topTimer.Dispose();menu.Dispose();image.Dispose();}base.Dispose(disposing);}
}
