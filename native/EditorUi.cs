using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

internal static class Ui {
    internal static GraphicsPath Round(RectangleF b,float radius){GraphicsPath p=new GraphicsPath();float d=radius*2;p.AddArc(b.X,b.Y,d,d,180,90);p.AddArc(b.Right-d,b.Y,d,d,270,90);p.AddArc(b.Right-d,b.Bottom-d,d,d,0,90);p.AddArc(b.X,b.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
    internal static readonly Color Ink=Color.FromArgb(65,65,65),Border=Color.FromArgb(218,218,218),Surface=Color.FromArgb(250,250,250);
    internal static void Icon(Graphics g,string id,Rectangle rect,Color color){
        GraphicsState s=g.Save();g.TranslateTransform(rect.X,rect.Y);g.ScaleTransform(rect.Width/24f,rect.Height/24f);g.SmoothingMode=SmoothingMode.AntiAlias;
        using(Pen p=new Pen(color,1.7f))using(Brush b=new SolidBrush(color)){p.StartCap=LineCap.Round;p.EndCap=LineCap.Round;p.LineJoin=LineJoin.Round;
            if(id=="select"){PointF[] a={new PointF(5,3),new PointF(19,11),new PointF(13,13),new PointF(11,20),new PointF(5,3)};g.DrawLines(p,a);g.DrawLine(p,13,13,18,19);}
            else if(id=="shape")g.DrawRectangle(p,5,5,14,14);
            else if(id=="arrow"){g.DrawLine(p,5,19,18,6);g.DrawLine(p,10,6,18,6);g.DrawLine(p,18,6,18,14);}
            else if(id=="pen"){g.DrawLines(p,new PointF[]{new PointF(4,19),new PointF(7,12),new PointF(16,3),new PointF(20,7),new PointF(11,16),new PointF(4,19)});g.DrawLine(p,7,12,11,16);}
            else if(id=="text"){g.DrawLine(p,5,20,12,4);g.DrawLine(p,12,4,19,20);g.DrawLine(p,8,14,16,14);}
            else if(id=="mosaic"){for(int y=0;y<3;y++)for(int x=0;x<3;x++)g.FillRectangle(b,4+x*6,4+y*6,3,3);}
            else if(id=="undo"||id=="redo"){if(id=="redo"){g.TranslateTransform(24,0);g.ScaleTransform(-1,1);}g.DrawArc(p,6,8,14,12,210,240);g.DrawLine(p,3,9,8,5);g.DrawLine(p,3,9,9,12);}
            else if(id=="delete"){g.DrawLine(p,4,6,20,6);g.DrawLine(p,9,3,15,3);g.DrawLines(p,new PointF[]{new PointF(6,6),new PointF(7,21),new PointF(17,21),new PointF(18,6)});g.DrawLine(p,10,10,10,17);g.DrawLine(p,14,10,14,17);}
            else if(id=="cancel"){g.DrawLine(p,5,5,19,19);g.DrawLine(p,19,5,5,19);}
            else if(id=="pin"){g.DrawPolygon(p,new PointF[]{new PointF(12,3),new PointF(20,10),new PointF(15,13),new PointF(13,18),new PointF(6,11)});g.DrawLine(p,9,15,4,21);}
            else if(id=="save"){g.DrawLine(p,12,3,12,15);g.DrawLine(p,7,10,12,15);g.DrawLine(p,12,15,17,10);g.DrawLines(p,new PointF[]{new PointF(4,16),new PointF(4,21),new PointF(20,21),new PointF(20,16)});}
            else if(id=="copy"){g.DrawRectangle(p,8,7,12,14);g.DrawLines(p,new PointF[]{new PointF(16,7),new PointF(16,3),new PointF(4,3),new PointF(4,17),new PointF(8,17)});}
            else if(id=="attach"){g.DrawLines(p,new PointF[]{new PointF(19,5),new PointF(19,14),new PointF(5,14)});g.DrawLine(p,5,14,10,9);g.DrawLine(p,5,14,10,19);}
        }g.Restore(s);
    }
}
internal sealed class ToolButton : Button {
    internal string Glyph;
    internal bool Active;
    private bool hover;
    internal ToolButton(string glyph,string label){Glyph=glyph;Text=label;AccessibleName=label;FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;TabStop=true;Cursor=Cursors.Hand;SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);}
    protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
    protected override void OnPaint(PaintEventArgs e){
        e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;e.Graphics.Clear(Ui.Surface);
        if(Active||hover||Focused){using(GraphicsPath r=Ui.Round(new RectangleF(1,1,Width-2,Height-2),6))using(Brush b=new SolidBrush(Active?Color.FromArgb(226,226,226):Color.FromArgb(238,238,238)))e.Graphics.FillPath(b,r);}
        Ui.Icon(e.Graphics,Glyph,new Rectangle((Width-17)/2,(Height-17)/2,17,17),Enabled?Ui.Ink:Color.FromArgb(177,177,177));
    }
}
internal sealed class EditorPanel : Control {
    internal DrawTool Tool=DrawTool.Select;
    internal bool Ellipse,Fill,EditingSelection;
    internal Color Ink=MarkScene.Colors[0];
    internal float Stroke=3,FontSize=20;
    internal int MosaicSize=12;
    private string status="";
    internal string Status{get{return status;}set{status=value??"";if(IsHandleCreated&&!IsDisposed&&status.Length>0)tips.Show(status,this,Math.Max(4,Width-220),Height+4,2400);}}
    internal event Action<string> Command;
    internal event Action SettingsChanged;
    internal event Action StyleStarting;
    private readonly Dictionary<string,ToolButton> buttons=new Dictionary<string,ToolButton>();
    private readonly ToolTip tips=new ToolTip();
    private readonly bool english;
    private bool draggingSlider;
    private Rectangle slider;
    private string L(string zh,string en){return english?en:zh;}
    internal EditorPanel(bool useEnglish){
        english=useEnglish;Size=new Size(424,44);AccessibleName="dsh-FocusCapture";BackColor=Ui.Surface;SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);
        string[] ids={"select","shape","arrow","pen","text","mosaic","undo","redo","delete","cancel","pin","save","copy","attach"};
        string[] labels=english?new string[]{"Select / move / resize","Rectangle / ellipse","Arrow","Freehand","Text","Mosaic","Undo · Ctrl+Z","Redo · Ctrl+Y","Delete selected, or clear all","Cancel · Esc","Pin above all apps","Save · Ctrl+S","Copy image · Ctrl+C","Add to chat"}:new string[]{"选择 / 移动 / 调整大小","矩形 / 椭圆","箭头","涂鸦","文字","马赛克","撤销 · Ctrl+Z","重做 · Ctrl+Y","删除所选，未选中时清空全部","取消 · Esc","贴图：置于所有应用上方","保存 · Ctrl+S","复制图片 · Ctrl+C","添加到对话"};
        int x=8;for(int i=0;i<ids.Length;i++){if(i==6||i==9)x+=8;string id=ids[i];ToolButton b=new ToolButton(id,labels[i]);b.Bounds=new Rectangle(x,7,28,29);b.Click+=delegate{if(Command!=null)Command(id);};buttons[id]=b;Controls.Add(b);tips.SetToolTip(b,labels[i]);x+=28;}
        RefreshState(false,false,false);
    }
    internal void RefreshState(bool undo,bool redo,bool delete){
        foreach(KeyValuePair<string,ToolButton> pair in buttons)pair.Value.Active=(pair.Key=="select"&&(Tool==DrawTool.Select||EditingSelection))||(!EditingSelection&&((pair.Key=="shape"&&(Tool==DrawTool.Rectangle||Tool==DrawTool.Circle))||(pair.Key=="arrow"&&Tool==DrawTool.Arrow)||(pair.Key=="pen"&&Tool==DrawTool.Pen)||(pair.Key=="text"&&Tool==DrawTool.Text)||(pair.Key=="mosaic"&&Tool==DrawTool.Mosaic)));
        buttons["undo"].Enabled=undo;buttons["redo"].Enabled=redo;buttons["delete"].Enabled=delete;
        Height=Tool==DrawTool.Select?44:124;foreach(ToolButton b in buttons.Values)b.Invalidate();
        using(GraphicsPath path=Ui.Round(new RectangleF(0,0,Width-1,Height-1),10)){Region old=Region;Region=new Region(path);if(old!=null)old.Dispose();}Invalidate();
    }
    protected override void OnPaint(PaintEventArgs e){
        base.OnPaint(e);Graphics g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Ui.Surface);using(GraphicsPath r=Ui.Round(new RectangleF(.5f,.5f,Width-1,Height-1),10))using(Pen p=new Pen(Ui.Border))g.DrawPath(p,r);
        using(Pen p=new Pen(Ui.Border)){g.DrawLine(p,180,11,180,31);g.DrawLine(p,272,11,272,31);if(Tool!=DrawTool.Select)g.DrawLine(p,0,42,Width,42);}
        if(Tool!=DrawTool.Select){
            int colorX=12;
            if(Tool==DrawTool.Rectangle||Tool==DrawTool.Circle){DrawTab(g,new Rectangle(12,51,46,27),L("矩形","Rect"),!Ellipse);DrawTab(g,new Rectangle(58,51,46,27),L("椭圆","Oval"),Ellipse);colorX=122;}
            if(Tool!=DrawTool.Mosaic){for(int i=0;i<MarkScene.Colors.Length;i++){Rectangle box=new Rectangle(colorX+i*29,55,19,19);using(GraphicsPath r=Ui.Round(box,6))using(Brush b=new SolidBrush(MarkScene.Colors[i]))g.FillPath(b,r);if(MarkScene.Colors[i].ToArgb()==Ink.ToArgb()){Rectangle ring=box;ring.Inflate(3,3);using(GraphicsPath r=Ui.Round(ring,8))using(Pen p=new Pen(Color.FromArgb(90,90,90),1.2f))g.DrawPath(p,r);}}}
            else TextRenderer.DrawText(g,L("马赛克：拖动涂抹选区内容","Mosaic: drag to obscure the selection"),Font,new Point(12,56),Ui.Ink);
            string label=Tool==DrawTool.Text?L("字号","Size"):Tool==DrawTool.Mosaic?L("强度","Block"):L("线宽","Width");TextRenderer.DrawText(g,label,Font,new Point(12,94),Ui.Ink);
            slider=new Rectangle(52,96,91,10);float value=Tool==DrawTool.Text?(FontSize-12)/36f:Tool==DrawTool.Mosaic?(MosaicSize-6)/24f:(Stroke-1)/11f;
            using(Pen p=new Pen(Color.FromArgb(185,185,185),4)){p.StartCap=LineCap.Round;p.EndCap=LineCap.Round;g.DrawLine(p,slider.Left,slider.Top+5,slider.Right,slider.Top+5);}
            using(Brush b=new SolidBrush(Color.FromArgb(45,45,45)))g.FillEllipse(b,slider.Left+value*slider.Width-6,slider.Top-1,12,12);
            string number=Tool==DrawTool.Text?((int)FontSize).ToString():Tool==DrawTool.Mosaic?MosaicSize.ToString():((int)Stroke).ToString();TextRenderer.DrawText(g,number,Font,new Point(151,94),Ui.Ink);
            if(Tool==DrawTool.Rectangle||Tool==DrawTool.Circle){Rectangle check=new Rectangle(194,95,12,12);using(Pen p=new Pen(Color.FromArgb(140,140,140)))g.DrawRectangle(p,check);if(Fill)using(Brush b=new SolidBrush(Ui.Ink))g.FillRectangle(b,197,98,7,7);TextRenderer.DrawText(g,L("填充","Fill"),Font,new Point(211,93),Ui.Ink);}
            if(Tool==DrawTool.Text)TextRenderer.DrawText(g,L("Ctrl+Enter 完成文字","Ctrl+Enter commits text"),Font,new Point(194,94),Color.FromArgb(130,130,130));
        }
    }
    private void DrawTab(Graphics g,Rectangle b,string text,bool active){using(GraphicsPath r=Ui.Round(b,6)){using(Brush fill=new SolidBrush(active?Color.White:Color.FromArgb(242,242,242)))g.FillPath(fill,r);using(Pen p=new Pen(Ui.Border))g.DrawPath(p,r);}TextRenderer.DrawText(g,text,Font,b,Ui.Ink,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);}
    protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button!=MouseButtons.Left||Tool==DrawTool.Select)return;if(StyleStarting!=null)StyleStarting();
        bool shapes=Tool==DrawTool.Rectangle||Tool==DrawTool.Circle;int colorX=shapes?122:12;
        if(shapes&&e.Y>=51&&e.Y<=78&&e.X>=12&&e.X<=104){Ellipse=e.X>=58;Tool=Ellipse?DrawTool.Circle:DrawTool.Rectangle;Changed();return;}
        if(Tool!=DrawTool.Mosaic&&e.Y>=49&&e.Y<=79){int i=(e.X-colorX)/29;if(e.X>=colorX&&i>=0&&i<MarkScene.Colors.Length){Ink=MarkScene.Colors[i];Changed();return;}}
        if(shapes&&new Rectangle(190,89,70,26).Contains(e.Location)){Fill=!Fill;Changed();return;}
        Rectangle hit=slider;hit.Inflate(8,8);if(hit.Contains(e.Location)){draggingSlider=true;Capture=true;SetSlider(e.X);}
    }
    protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(draggingSlider)SetSlider(e.X);}
    protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);draggingSlider=false;Capture=false;}
    private void SetSlider(int x){float ratio=Math.Max(0,Math.Min(1,(x-slider.Left)/(float)Math.Max(1,slider.Width)));if(Tool==DrawTool.Text)FontSize=(float)Math.Round(12+ratio*36);else if(Tool==DrawTool.Mosaic)MosaicSize=(int)Math.Round(6+ratio*24);else Stroke=(float)Math.Round(1+ratio*11);Changed();}
    private void Changed(){if(SettingsChanged!=null)SettingsChanged();Invalidate();}
    internal string TooltipFor(string id){return tips.GetToolTip(buttons[id]);}
    protected override void Dispose(bool disposing){if(disposing)tips.Dispose();base.Dispose(disposing);}
}
