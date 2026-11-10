using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows.Forms;

internal sealed class RegionForm : Form {
    private readonly Bitmap frozen,dimmed;
    private Bitmap view;
    private readonly Rectangle desktop;
    private readonly Action<byte[]> result;
    private readonly MarkScene scene=new MarkScene();
    private readonly EditorPanel panel;
    private readonly System.Windows.Forms.Timer expiry=new System.Windows.Forms.Timer(),paintTimer=new System.Windows.Forms.Timer();
    private Rectangle selected,pendingPaint,cropStart;
    private Point anchor;
    private bool selecting,drawing,moveCrop,moveMark,scaleMark,settled,checkpoint;
    private int resize=-1;
    private DrawTool tool=DrawTool.Select;
    private Mark preview,chosen,originalMark;
    private ScreenshotTextBox textEditor;
    private Point textAt;
    internal static Rectangle Normalize(Point a,Point b,Rectangle bounds){return Rectangle.Intersect(Rectangle.FromLTRB(Math.Min(a.X,b.X),Math.Min(a.Y,b.Y),Math.Max(a.X,b.X),Math.Max(a.Y,b.Y)),bounds);}
    internal RegionForm(Action<byte[]> callback,bool english){
        result=callback;desktop=SystemInformation.VirtualScreen;
        if(desktop.Width<1||desktop.Height<1||(long)desktop.Width*desktop.Height>60000000)throw new Exception("Desktop is too large to capture safely.");
        frozen=new Bitmap(desktop.Width,desktop.Height,PixelFormat.Format24bppRgb);
        try{using(Graphics g=Graphics.FromImage(frozen))g.CopyFromScreen(desktop.Location,Point.Empty,desktop.Size,CopyPixelOperation.SourceCopy);dimmed=(Bitmap)frozen.Clone();using(Graphics g=Graphics.FromImage(dimmed))using(Brush shade=new SolidBrush(Color.FromArgb(130,0,0,0)))g.FillRectangle(shade,0,0,dimmed.Width,dimmed.Height);}catch{frozen.Dispose();throw;}
        scene.SetSource(frozen);AutoScaleMode=AutoScaleMode.None;FormBorderStyle=FormBorderStyle.None;StartPosition=FormStartPosition.Manual;Bounds=desktop;Text="dsh-FocusCapture";TopMost=true;ShowInTaskbar=false;DoubleBuffered=true;KeyPreview=true;Cursor=Cursors.Cross;
        panel=new EditorPanel(english);panel.Visible=false;panel.Command+=Command;panel.StyleStarting+=delegate{if(chosen!=null&&tool==DrawTool.Select)scene.Checkpoint();};panel.SettingsChanged+=ApplyStyle;Controls.Add(panel);
        expiry.Interval=120000;expiry.Tick+=delegate{Finish(false);};expiry.Start();
        paintTimer.Interval=16;paintTimer.Tick+=delegate{if(!pendingPaint.IsEmpty){Rectangle r=pendingPaint;pendingPaint=Rectangle.Empty;Invalidate(r);}};paintTimer.Start();
        Shown+=delegate{Activate();Native.SetForegroundWindow(Handle);Focus();};FormClosed+=delegate{if(!settled){settled=true;result(null);}};
    }
    private void Touch(){expiry.Stop();expiry.Start();}
    private void Dirty(Rectangle old,Rectangle next){
        Rectangle r=EditorLayout.Dirty(old,next,ClientRectangle);if(r.IsEmpty)return;
        if(EditorLayout.ImmediateBorder(selecting,moveCrop,scaleMark,resize)){
            if(!pendingPaint.IsEmpty){r=Rectangle.Union(r,pendingPaint);pendingPaint=Rectangle.Empty;}
            if(!IsHandleCreated||IsDisposed)return;
            // Border gestures paint inside the mouse event, not on the 16ms coalescing timer.
            Invalidate(r);Update();
        }else pendingPaint=pendingPaint.IsEmpty?r:Rectangle.Union(pendingPaint,r);
    }
    private void FullSelection(){Dirty(selected,selected);}
    private void DropView(){if(view!=null){view.Dispose();view=null;}}
    private void RebuildView(Mark omit){DropView();if(selected.Width<2||selected.Height<2||(long)selected.Width*selected.Height>25000000)return;view=frozen.Clone(selected,PixelFormat.Format24bppRgb);using(Graphics g=Graphics.FromImage(view)){g.TranslateTransform(-selected.X,-selected.Y);scene.Draw(g,selected,null,omit);}}
    private Bitmap Image(){CommitText();if(selected.Width<2||selected.Height<2)throw new Exception("Select a screen region first.");if((long)selected.Width*selected.Height>25000000)throw new Exception("Selection exceeds 25 megapixels.");return scene.Export(frozen,selected);}
    private void RefreshPanel(){if(tool==DrawTool.Select&&chosen==null)panel.Tool=DrawTool.Select;panel.EditingSelection=chosen!=null&&tool==DrawTool.Select;panel.RefreshState(scene.CanUndo,scene.CanRedo,scene.Marks.Count>0);PlacePanel();}
    private void PlacePanel(){if(selected.Width<2||selected.Height<2){panel.Visible=false;return;}Point center=new Point(desktop.Left+selected.Left+selected.Width/2,desktop.Top+selected.Top+selected.Height/2);Rectangle screen=Screen.FromPoint(center).Bounds;screen.Offset(-desktop.Left,-desktop.Top);panel.Location=EditorLayout.Toolbar(selected,panel.Size,Rectangle.Intersect(screen,ClientRectangle),0).Location;panel.Visible=true;panel.BringToFront();if(textEditor!=null)textEditor.BringToFront();}
    private void ChooseTool(DrawTool next){CommitText();chosen=null;preview=null;tool=next;panel.EditingSelection=false;panel.Tool=next;Cursor=next==DrawTool.Select?Cursors.Default:next==DrawTool.Text?Cursors.IBeam:Cursors.Cross;RefreshPanel();Focus();FullSelection();}
    private void Command(string id){
        Touch();
        try{
            if(id=="select")ChooseTool(DrawTool.Select);else if(id=="shape")ChooseTool(panel.Ellipse?DrawTool.Circle:DrawTool.Rectangle);else if(id=="arrow")ChooseTool(DrawTool.Arrow);else if(id=="pen")ChooseTool(DrawTool.Pen);else if(id=="text")ChooseTool(DrawTool.Text);else if(id=="mosaic")ChooseTool(DrawTool.Mosaic);
            else if(id=="undo"||id=="redo"){if(textEditor!=null)DiscardText();else if(id=="undo")scene.Undo();else scene.Redo();chosen=null;preview=null;RebuildView(null);RefreshPanel();FullSelection();Focus();}
            else if(id=="delete"){CommitText();if(chosen!=null)scene.Remove(chosen);else scene.Clear();chosen=null;RebuildView(null);RefreshPanel();FullSelection();Focus();}
            else if(id=="cancel")Finish(false);else if(id=="attach")Finish(true);
            else if(id=="copy"){using(Bitmap image=Image())ImageActions.Copy(image);panel.Status="图片已复制";panel.Invalidate();Focus();}
            else if(id=="save"){using(Bitmap image=Image())if(ImageActions.Save(image,this))panel.Status="图片已保存";panel.Invalidate();Touch();Focus();}
            else if(id=="pin"){using(Bitmap image=Image())PinnedShot.ShowShot(image,new Point(desktop.Left+selected.Left,desktop.Top+selected.Top));Finish(false);}
        }catch(Exception ex){panel.Status=ex.Message;panel.Invalidate();}
    }
    private void ApplyStyle(){
        Touch();CommitText();
        if(chosen!=null&&tool==DrawTool.Select){chosen.Color=panel.Ink;chosen.Fill=panel.Fill;chosen.Width=chosen.Tool==DrawTool.Mosaic?Math.Max(18,panel.Stroke*7):panel.Stroke;chosen.BlockSize=panel.MosaicSize;chosen.FontSize=panel.FontSize;chosen.LineSpacing=panel.LineSpacing;if(chosen.Tool==DrawTool.Rectangle||chosen.Tool==DrawTool.Circle){chosen.Tool=panel.Ellipse?DrawTool.Circle:DrawTool.Rectangle;chosen.LockAspect=false;}RebuildView(null);}
        else if(tool==DrawTool.Rectangle||tool==DrawTool.Circle)tool=panel.Tool;
        RefreshPanel();FullSelection();
    }
    protected override void OnPaint(PaintEventArgs e){
        Graphics g=e.Graphics;Rectangle clip=Rectangle.Intersect(e.ClipRectangle,ClientRectangle);if(!clip.IsEmpty)g.DrawImage(dimmed,clip,clip,GraphicsUnit.Pixel);
        if(selected.Width>1&&selected.Height>1){
            GraphicsState state=g.Save();g.SetClip(selected,CombineMode.Intersect);
            if(view!=null&&!selecting&&resize<0){g.DrawImageUnscaled(view,selected.Left,selected.Top);if(preview!=null)scene.DrawPreview(g,selected,preview);}
            else{Rectangle r=Rectangle.Intersect(selected,clip);if(!r.IsEmpty)g.DrawImage(frozen,r,r,GraphicsUnit.Pixel);scene.Draw(g,selected,preview,moveMark||scaleMark?chosen:null);}g.Restore(state);Ui.Prepare(g);g.PixelOffsetMode=PixelOffsetMode.Half;
            using(Pen p=new Pen(Color.FromArgb(65,65,65),1))g.DrawRectangle(p,selected.X,selected.Y,selected.Width-1,selected.Height-1);
            g.PixelOffsetMode=PixelOffsetMode.HighQuality;
            if(chosen!=null&&!drawing){Rectangle box=Rectangle.Round(chosen.Extent);using(Pen p=new Pen(Color.FromArgb(40,120,235),1)){p.DashStyle=DashStyle.Dash;g.DrawRectangle(p,box);}DrawHandles(g,box,true);}
            else if(!drawing&&!selecting)DrawHandles(g,selected,false);
            string size=selected.Width+" × "+selected.Height+" px";Font labelFont=UiTypography.Panel(size);Size text=TextRenderer.MeasureText(size,labelFont);Rectangle label=new Rectangle(selected.Left,Math.Max(2,selected.Top-27),text.Width+10,22);using(GraphicsPath round=Ui.Round(label,5))using(Brush b=new SolidBrush(Color.FromArgb(42,42,42)))g.FillPath(b,round);TextRenderer.DrawText(g,size,labelFont,label,Color.White,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
        }else{string hint="拖动框选屏幕区域 · Esc 取消";TextRenderer.DrawText(g,hint,UiTypography.Panel(hint),new Point(18,18),Color.White);}
    }
    private static Point[] Handles(Rectangle r){return new Point[]{new Point(r.Left,r.Top),new Point(r.Left+r.Width/2,r.Top),new Point(r.Right,r.Top),new Point(r.Right,r.Top+r.Height/2),new Point(r.Right,r.Bottom),new Point(r.Left+r.Width/2,r.Bottom),new Point(r.Left,r.Bottom),new Point(r.Left,r.Top+r.Height/2)};}
    private static void DrawHandles(Graphics g,Rectangle r,bool corners){Point[] points=Handles(r);for(int i=0;i<points.Length;i++){if(corners&&i%2==1)continue;Rectangle h=new Rectangle(points[i].X-2,points[i].Y-2,5,5);g.FillRectangle(Brushes.White,h);g.DrawRectangle(Pens.Gray,h);}}
    private static int HitHandle(Rectangle r,Point p,bool corners){Point[] points=Handles(r);for(int i=0;i<points.Length;i++)if((!corners||i%2==0)&&Math.Abs(points[i].X-p.X)<=6&&Math.Abs(points[i].Y-p.Y)<=6)return i;return -1;}
    private Rectangle ResizeBox(Rectangle r,int handle,Point mouse,Rectangle limit){int l=r.Left,t=r.Top,right=r.Right,b=r.Bottom;Point p=MarkScene.Clamp(mouse,limit);if(handle==0||handle==6||handle==7)l=p.X;if(handle==0||handle==1||handle==2)t=p.Y;if(handle==2||handle==3||handle==4)right=p.X;if(handle==4||handle==5||handle==6)b=p.Y;Rectangle result=Rectangle.FromLTRB(Math.Min(l,right),Math.Min(t,b),Math.Max(l,right),Math.Max(t,b));return result.Width<4||result.Height<4?r:result;}
    protected override void OnMouseDown(MouseEventArgs e){
        base.OnMouseDown(e);Touch();if(e.Button==MouseButtons.Right){Finish(false);return;}if(e.Button!=MouseButtons.Left)return;CommitText();anchor=e.Location;checkpoint=false;cropStart=selected;
        if(tool==DrawTool.Select&&chosen!=null){resize=HitHandle(Rectangle.Round(chosen.Extent),e.Location,true);if(resize>=0){scaleMark=true;originalMark=chosen.Clone();preview=chosen;RebuildView(chosen);Capture=true;panel.Visible=false;return;}}
        if(tool==DrawTool.Select&&selected.Width>1){resize=HitHandle(selected,e.Location,false);if(resize>=0){DropView();Capture=true;panel.Visible=false;return;}
            Mark hit=scene.Hit(e.Location);if(hit!=null&&selected.Contains(e.Location)){chosen=hit;originalMark=hit.Clone();preview=hit;moveMark=true;panel.Tool=hit.Tool;panel.Ink=hit.Color;panel.Stroke=hit.Tool==DrawTool.Mosaic?hit.Width/7:hit.Width;panel.Fill=hit.Fill;panel.FontSize=hit.FontSize;panel.MosaicSize=hit.BlockSize;panel.Ellipse=hit.Tool==DrawTool.Circle;RebuildView(hit);RefreshPanel();Capture=true;FullSelection();return;}
            chosen=null;panel.Tool=DrawTool.Select;RefreshPanel();if(selected.Contains(e.Location)){if(e.Clicks>=2){Finish(true);return;}moveCrop=true;Capture=true;panel.Visible=false;return;}
        }
        if(selected.Width<2||tool==DrawTool.Select){selected=Rectangle.Empty;selecting=true;chosen=null;preview=null;scene.Clear();DropView();panel.Visible=false;Capture=true;Dirty(cropStart,selected);return;}
        if(!selected.Contains(e.Location))return;Point start=MarkScene.Clamp(e.Location,selected);if(tool==DrawTool.Text){StartText(start);return;}
        preview=new Mark(tool,panel.Ink,start);preview.Width=tool==DrawTool.Mosaic?Math.Max(18,panel.Stroke*7):panel.Stroke;preview.Fill=panel.Fill;preview.BlockSize=panel.MosaicSize;preview.LineSpacing=panel.LineSpacing;preview.LockAspect=tool==DrawTool.Circle&&(ModifierKeys&Keys.Shift)!=0;preview.Points.Add(start);drawing=true;Capture=true;
    }
    private static void CopyPose(Mark target,Mark source){target.Points.Clear();target.Points.AddRange(source.Points);target.TextWidth=source.TextWidth;target.TextHeight=source.TextHeight;target.FontSize=source.FontSize;target.LineSpacing=source.LineSpacing;}
    protected override void OnMouseMove(MouseEventArgs e){
        base.OnMouseMove(e);Rectangle old=selected;
        if(selecting){selected=Normalize(anchor,e.Location,ClientRectangle);Dirty(old,selected);}
        else if(scaleMark&&chosen!=null){if(!checkpoint){scene.Checkpoint();checkpoint=true;}Rectangle before=chosen.DirtyBounds;CopyPose(chosen,originalMark);Rectangle next=ResizeBox(Rectangle.Round(originalMark.Extent),resize,e.Location,selected);chosen.Transform(originalMark.Extent,next);Dirty(before,chosen.DirtyBounds);}
        else if(moveMark&&chosen!=null){int dx=e.X-anchor.X,dy=e.Y-anchor.Y;RectangleF bounds=originalMark.Extent;dx=(int)Math.Max(selected.Left-bounds.Left,Math.Min(selected.Right-bounds.Right,dx));dy=(int)Math.Max(selected.Top-bounds.Top,Math.Min(selected.Bottom-bounds.Bottom,dy));if(!checkpoint&&(dx!=0||dy!=0)){scene.Checkpoint();checkpoint=true;}Rectangle before=chosen.DirtyBounds;CopyPose(chosen,originalMark);chosen.Move(dx,dy);Dirty(before,chosen.DirtyBounds);}
        else if(moveCrop){int dx=Math.Max(-cropStart.Left,Math.Min(ClientSize.Width-cropStart.Right,e.X-anchor.X)),dy=Math.Max(-cropStart.Top,Math.Min(ClientSize.Height-cropStart.Bottom,e.Y-anchor.Y));selected=new Rectangle(cropStart.X+dx,cropStart.Y+dy,cropStart.Width,cropStart.Height);int sx=selected.Left-old.Left,sy=selected.Top-old.Top;if(!checkpoint&&(sx!=0||sy!=0)){scene.Checkpoint();checkpoint=true;}foreach(Mark m in scene.Marks)m.Move(sx,sy);Dirty(old,selected);}
        else if(resize>=0){selected=ResizeBox(cropStart,resize,e.Location,ClientRectangle);Dirty(old,selected);}
        else if(drawing&&preview!=null){Rectangle before=preview.DirtyBounds;Point end=MarkScene.Clamp(e.Location,selected);if(tool==DrawTool.Pen||tool==DrawTool.Mosaic){PointF last=preview.Points[preview.Points.Count-1];if(preview.Points.Count<4096&&(Math.Abs(last.X-end.X)+Math.Abs(last.Y-end.Y)>1.5))preview.Points.Add(end);}else preview.Points[1]=end;Dirty(before,preview.DirtyBounds);}
        else if(tool==DrawTool.Select){int handle=chosen!=null?HitHandle(Rectangle.Round(chosen.Extent),e.Location,true):HitHandle(selected,e.Location,false);Cursor=handle<0?(selected.Contains(e.Location)?Cursors.SizeAll:Cursors.Cross):handle==1||handle==5?Cursors.SizeNS:handle==3||handle==7?Cursors.SizeWE:handle==0||handle==4?Cursors.SizeNWSE:Cursors.SizeNESW;}
        if(selecting||drawing||moveCrop||moveMark||scaleMark||resize>=0)Touch();
    }
    protected override void OnMouseUp(MouseEventArgs e){
        base.OnMouseUp(e);if(e.Button!=MouseButtons.Left)return;Touch();
        if(selecting)selected=Normalize(anchor,e.Location,ClientRectangle);if(drawing)scene.Add(preview);
        bool changed=selecting||drawing||moveCrop||moveMark||scaleMark||resize>=0;selecting=drawing=moveCrop=moveMark=scaleMark=false;resize=-1;preview=null;Capture=false;
        if(changed){RebuildView(null);RefreshPanel();FullSelection();}
    }
    private void StartText(Point point){
        textAt=point;Rectangle bounds=new Rectangle(point.X,point.Y,Math.Max(8,Math.Min(300,selected.Right-point.X)),Math.Max(8,Math.Min(110,selected.Bottom-point.Y)));
        using(Bitmap background=new Bitmap(bounds.Width,bounds.Height,PixelFormat.Format24bppRgb)){
            using(Graphics g=Graphics.FromImage(background)){g.TranslateTransform(-bounds.Left,-bounds.Top);g.DrawImageUnscaled(frozen,0,0);scene.Draw(g,selected,null);}
            textEditor=new ScreenshotTextBox(background);
        }
        textEditor.Font=UiTypography.ForText("",panel.FontSize);textEditor.LineSpacing=panel.LineSpacing;textEditor.ForeColor=panel.Ink;textEditor.Bounds=bounds;Controls.Add(textEditor);textEditor.BringToFront();textEditor.Focus();
    }
    protected override void WndProc(ref Message m){
        if((m.Msg==0x0133||m.Msg==0x0138)&&textEditor!=null&&textEditor.IsHandleCreated&&m.LParam==textEditor.Handle){m.Result=textEditor.PrepareBackground(m.WParam);return;}
        base.WndProc(ref m);
    }
    private void CommitText(){if(textEditor==null)return;Mark mark=new Mark(DrawTool.Text,textEditor.ForeColor,textAt);mark.Text=textEditor.Text;mark.FontSize=textEditor.Font.Size;mark.LineSpacing=textEditor.LineSpacing;mark.TextWidth=Math.Max(8,textEditor.Width);using(Graphics g=CreateGraphics())mark.TextHeight=UiTypography.MeasureAnnotation(g,mark.Text,textEditor.Font,mark.TextWidth,mark.LineSpacing)+6;scene.Add(mark);DiscardText();RebuildView(null);RefreshPanel();FullSelection();}
    private void DiscardText(){if(textEditor==null)return;TextBox old=textEditor;textEditor=null;Controls.Remove(old);Font font=old.Font;old.Dispose();font.Dispose();FullSelection();}
    protected override bool ProcessCmdKey(ref Message msg,Keys keys){Touch();if(textEditor!=null){if(keys==(Keys.Control|Keys.Enter)){CommitText();Focus();return true;}if(keys==Keys.Escape){DiscardText();Focus();return true;}if(keys!=(Keys.Control|Keys.S))return base.ProcessCmdKey(ref msg,keys);}
        if(keys==Keys.Escape){Finish(false);return true;}if(keys==Keys.Enter){Finish(true);return true;}if(keys==(Keys.Control|Keys.Z)){Command("undo");return true;}if(keys==(Keys.Control|Keys.Y)||keys==(Keys.Control|Keys.Shift|Keys.Z)){Command("redo");return true;}if(keys==Keys.Delete){Command("delete");return true;}if(keys==(Keys.Control|Keys.C)){Command("copy");return true;}if(keys==(Keys.Control|Keys.S)){Command("save");return true;}return base.ProcessCmdKey(ref msg,keys);
    }
    private void Finish(bool accept){
        if(settled)return;if(accept&&(selected.Width<2||selected.Height<2))return;Bitmap image=null;
        try{if(accept)image=Image();else DiscardText();}catch(Exception ex){panel.Status=ex.Message;panel.Invalidate();return;}
        settled=true;Hide();Close();if(!accept){result(null);return;}
        ThreadPool.QueueUserWorkItem(delegate{byte[] bytes=null;try{using(image)using(MemoryStream output=new MemoryStream()){image.Save(output,ImageFormat.Png);if(output.Length>20*1024*1024)throw new Exception("PNG exceeds 20 MiB.");bytes=output.ToArray();}}catch(Exception ex){Program.Emit(new{kind="error",message=ex.Message});}result(bytes);});
    }
    protected override void Dispose(bool disposing){if(disposing){DiscardText();expiry.Dispose();paintTimer.Dispose();DropView();scene.Dispose();frozen.Dispose();dimmed.Dispose();}base.Dispose(disposing);}
}
