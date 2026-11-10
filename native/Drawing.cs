using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

internal enum DrawTool { Select, Arrow, Rectangle, Circle, Text, Pen, Mosaic }
internal sealed class Mark {
    internal DrawTool Tool;
    internal Color Color;
    internal float Width=3,FontSize=20,LineSpacing=1.1f,TextWidth=250,TextHeight=40;
    internal int BlockSize=12;
    internal bool Fill,LockAspect=true;
    internal string Text="";
    internal readonly List<PointF> Points=new List<PointF>();
    internal Mark(DrawTool tool,Color color,PointF start){Tool=tool;Color=color;Points.Add(start);}
    internal Mark Clone(){Mark m=new Mark(Tool,Color,PointF.Empty);m.Points.Clear();m.Points.AddRange(Points);m.Width=Width;m.FontSize=FontSize;m.LineSpacing=LineSpacing;m.TextWidth=TextWidth;m.TextHeight=TextHeight;m.Text=Text;m.Fill=Fill;m.LockAspect=LockAspect;m.BlockSize=BlockSize;return m;}
    internal static RectangleF Bounds(PointF a,PointF b){return RectangleF.FromLTRB(Math.Min(a.X,b.X),Math.Min(a.Y,b.Y),Math.Max(a.X,b.X),Math.Max(a.Y,b.Y));}
    internal static RectangleF CircleBounds(PointF a,PointF b){float s=Math.Min(Math.Abs(b.X-a.X),Math.Abs(b.Y-a.Y));return Bounds(a,new PointF(a.X+(b.X>=a.X?s:-s),a.Y+(b.Y>=a.Y?s:-s)));}
    internal bool IsVisible{get{if(Tool==DrawTool.Text)return !String.IsNullOrWhiteSpace(Text);if(Points.Count<2)return false;return Tool==DrawTool.Pen||Tool==DrawTool.Mosaic||Math.Abs(Points[1].X-Points[0].X)>1||Math.Abs(Points[1].Y-Points[0].Y)>1;}}
    internal RectangleF Extent{
        get{
            if(Tool==DrawTool.Text)return new RectangleF(Points[0].X,Points[0].Y,TextWidth,TextHeight);
            if(Tool==DrawTool.Circle&&Points.Count>1&&LockAspect)return CircleBounds(Points[0],Points[1]);
            float l=Points[0].X,r=l,t=Points[0].Y,b=t;foreach(PointF p in Points){l=Math.Min(l,p.X);r=Math.Max(r,p.X);t=Math.Min(t,p.Y);b=Math.Max(b,p.Y);}return new RectangleF(l,t,Math.Max(1,r-l),Math.Max(1,b-t));
        }
    }
    internal Rectangle DirtyBounds{get{Rectangle r=Rectangle.Ceiling(Extent);r.Inflate((int)Math.Ceiling(Tool==DrawTool.Mosaic?Width/2+4:Math.Max(12,Width*6)),(int)Math.Ceiling(Tool==DrawTool.Mosaic?Width/2+4:Math.Max(12,Width*6)));return r;}}
    internal void Move(float dx,float dy){for(int i=0;i<Points.Count;i++)Points[i]=new PointF(Points[i].X+dx,Points[i].Y+dy);}
    internal void Transform(RectangleF old,RectangleF next){float sx=next.Width/Math.Max(1,old.Width),sy=next.Height/Math.Max(1,old.Height);for(int i=0;i<Points.Count;i++)Points[i]=new PointF(next.X+(Points[i].X-old.X)*sx,next.Y+(Points[i].Y-old.Y)*sy);if(Tool==DrawTool.Text){TextWidth=Math.Max(8,TextWidth*sx);TextHeight=Math.Max(8,TextHeight*sy);FontSize=Math.Max(8,Math.Min(96,FontSize*Math.Min(sx,sy)));}}
    private static float Distance(PointF p,PointF a,PointF b){float dx=b.X-a.X,dy=b.Y-a.Y,n=dx*dx+dy*dy;float k=n==0?0:Math.Max(0,Math.Min(1,((p.X-a.X)*dx+(p.Y-a.Y)*dy)/n));dx=p.X-a.X-k*dx;dy=p.Y-a.Y-k*dy;return(float)Math.Sqrt(dx*dx+dy*dy);}
    internal bool Hit(PointF point){
        float tolerance=Math.Max(6,Width/2+3);RectangleF b=Extent;RectangleF outer=b;outer.Inflate(tolerance,tolerance);if(!outer.Contains(point))return false;
        if(Tool==DrawTool.Text||Tool==DrawTool.Mosaic)return true;
        if(Tool==DrawTool.Rectangle){if(Fill)return b.Contains(point);RectangleF inner=b;inner.Inflate(-tolerance,-tolerance);return !inner.Contains(point);}
        if(Tool==DrawTool.Circle){double rx=Math.Max(1,b.Width/2),ry=Math.Max(1,b.Height/2),x=(point.X-b.X-rx)/rx,y=(point.Y-b.Y-ry)/ry,v=x*x+y*y;return Fill?v<=1:Math.Abs(v-1)<tolerance/Math.Min(rx,ry)*2;}
        for(int i=1;i<Points.Count;i++)if(Distance(point,Points[i-1],Points[i])<=tolerance)return true;return false;
    }
    internal void Draw(Graphics g){Draw(g,null);}
    internal void Draw(Graphics g,Bitmap mosaic){
        if(!IsVisible)return;
        if(Tool==DrawTool.Mosaic){
            if(mosaic==null)return;
            using(GraphicsPath path=new GraphicsPath())using(Pen brush=new Pen(Color.Black,Width)){
                brush.StartCap=LineCap.Round;brush.EndCap=LineCap.Round;brush.LineJoin=LineJoin.Round;if(Points.Count==2&&Points[0]==Points[1])path.AddEllipse(Points[0].X-Width/2,Points[0].Y-Width/2,Width,Width);else{path.AddLines(Points.ToArray());path.Widen(brush);}
                GraphicsState state=g.Save();try{g.SetClip(path,CombineMode.Intersect);g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;g.DrawImage(mosaic,new Rectangle(0,0,mosaic.Tag is Size?((Size)mosaic.Tag).Width:mosaic.Width,mosaic.Tag is Size?((Size)mosaic.Tag).Height:mosaic.Height),0,0,mosaic.Width,mosaic.Height,GraphicsUnit.Pixel);}finally{g.Restore(state);}
            }return;
        }
        using(Pen pen=new Pen(Color,Width))using(Brush brush=new SolidBrush(Color)){
            pen.StartCap=LineCap.Round;pen.EndCap=LineCap.Round;pen.LineJoin=LineJoin.Round;PointF first=Points[0];
            if(Tool==DrawTool.Pen){if(Points.Count==2&&Points[0]==Points[1])g.FillEllipse(brush,first.X-Width/2,first.Y-Width/2,Width,Width);else g.DrawLines(pen,Points.ToArray());}
            else if(Tool==DrawTool.Text){using(Font font=UiTypography.ForText(Text,FontSize))UiTypography.DrawAnnotation(g,Text,font,brush,new RectangleF(first.X,first.Y,Math.Max(8,TextWidth),Math.Max(TextHeight,FontSize*1.6f)),LineSpacing);}
            else if(Tool==DrawTool.Arrow){using(AdjustableArrowCap cap=new AdjustableArrowCap(4,5,true)){pen.CustomEndCap=cap;g.DrawLine(pen,first,Points[1]);}}
            else{RectangleF b=Tool==DrawTool.Circle&&LockAspect?CircleBounds(first,Points[1]):Bounds(first,Points[1]);if(b.Width<=0||b.Height<=0)return;if(Tool==DrawTool.Rectangle){if(Fill)g.FillRectangle(brush,b);else g.DrawRectangle(pen,b.X,b.Y,b.Width,b.Height);}else if(Tool==DrawTool.Circle){if(Fill)g.FillEllipse(brush,b);else g.DrawEllipse(pen,b);}}
        }
    }
}
internal sealed class MarkScene : IDisposable {
    internal readonly List<Mark> Marks=new List<Mark>();
    internal static readonly Color[] Colors={Color.FromArgb(239,68,68),Color.FromArgb(250,204,21),Color.FromArgb(42,122,245),Color.FromArgb(42,190,90),Color.FromArgb(245,110,180),Color.FromArgb(167,90,220),Color.Black};
    private readonly List<List<Mark>> undo=new List<List<Mark>>(),redo=new List<List<Mark>>();
    private readonly Dictionary<int,Bitmap> mosaics=new Dictionary<int,Bitmap>();
    private Bitmap source;
    internal bool CanUndo{get{return undo.Count>0;}}internal bool CanRedo{get{return redo.Count>0;}}
    internal static Point Clamp(Point point,Rectangle box){return new Point(Math.Max(box.Left,Math.Min(box.Right-1,point.X)),Math.Max(box.Top,Math.Min(box.Bottom-1,point.Y)));}
    private List<Mark> Snapshot(){List<Mark> result=new List<Mark>();foreach(Mark m in Marks)result.Add(m.Clone());return result;}
    internal void Checkpoint(){undo.Add(Snapshot());redo.Clear();int points=0;for(int i=undo.Count-1;i>=0;i--){foreach(Mark m in undo[i])points+=m.Points.Count;if(undo.Count-i>30||points>300000){undo.RemoveRange(0,i+1);break;}}}
    private void Restore(List<Mark> snapshot){Marks.Clear();foreach(Mark m in snapshot)Marks.Add(m.Clone());}
    internal void Undo(){if(!CanUndo)return;redo.Add(Snapshot());List<Mark> s=undo[undo.Count-1];undo.RemoveAt(undo.Count-1);Restore(s);}
    internal void Redo(){if(!CanRedo)return;undo.Add(Snapshot());List<Mark> s=redo[redo.Count-1];redo.RemoveAt(redo.Count-1);Restore(s);}
    internal void Add(Mark mark){if(mark==null||!mark.IsVisible||Marks.Count>=200)return;Checkpoint();Marks.Add(mark);}
    internal void Remove(Mark mark){if(!Marks.Contains(mark))return;Checkpoint();Marks.Remove(mark);}
    internal void Clear(){if(Marks.Count==0)return;Checkpoint();Marks.Clear();}
    internal Mark Hit(PointF point){for(int i=Marks.Count-1;i>=0;i--)if(Marks[i].Hit(point))return Marks[i];return null;}
    internal void SetSource(Bitmap value){source=value;foreach(Bitmap b in mosaics.Values)b.Dispose();mosaics.Clear();}
    private Bitmap Mosaic(int block){if(source==null)return null;Bitmap result;if(mosaics.TryGetValue(block,out result))return result;if(mosaics.Count>=4){foreach(Bitmap b in mosaics.Values)b.Dispose();mosaics.Clear();}result=new Bitmap(Math.Max(1,source.Width/block),Math.Max(1,source.Height/block),PixelFormat.Format24bppRgb);using(Graphics g=Graphics.FromImage(result)){g.InterpolationMode=InterpolationMode.HighQualityBilinear;g.DrawImage(source,new Rectangle(0,0,result.Width,result.Height));}result.Tag=source.Size;mosaics[block]=result;return result;}
    internal void Draw(Graphics g,Rectangle clip,Mark preview){Draw(g,clip,preview,null);}
    internal void DrawPreview(Graphics g,Rectangle clip,Mark mark){GraphicsState state=g.Save();try{g.SetClip(clip,CombineMode.Intersect);g.SmoothingMode=SmoothingMode.AntiAlias;mark.Draw(g,mark.Tool==DrawTool.Mosaic?Mosaic(mark.BlockSize):null);}finally{g.Restore(state);}}
    internal void Draw(Graphics g,Rectangle clip,Mark preview,Mark omit){GraphicsState state=g.Save();try{g.SetClip(clip,CombineMode.Intersect);g.SmoothingMode=SmoothingMode.AntiAlias;foreach(Mark m in Marks)if(m!=omit&&m.DirtyBounds.IntersectsWith(Rectangle.Ceiling(g.ClipBounds)))m.Draw(g,m.Tool==DrawTool.Mosaic?Mosaic(m.BlockSize):null);if(preview!=null)preview.Draw(g,preview.Tool==DrawTool.Mosaic?Mosaic(preview.BlockSize):null);}finally{g.Restore(state);}}
    internal Bitmap Export(Bitmap image,Rectangle selected){SetSourceIfNeeded(image);Bitmap crop=image.Clone(selected,PixelFormat.Format24bppRgb);try{using(Graphics g=Graphics.FromImage(crop)){g.TranslateTransform(-selected.X,-selected.Y);Draw(g,selected,null);}return crop;}catch{crop.Dispose();throw;}}
    private void SetSourceIfNeeded(Bitmap image){if(source!=image)SetSource(image);}
    public void Dispose(){foreach(Bitmap b in mosaics.Values)b.Dispose();mosaics.Clear();Marks.Clear();undo.Clear();redo.Clear();}
}
internal static class EditorLayout {
    internal static bool ImmediateBorder(bool selecting,bool movingCrop,bool scalingMark,int resize){return selecting||movingCrop||(!scalingMark&&resize>=0);}
    internal static Rectangle Toolbar(Rectangle selection,Size toolbar,Rectangle screen,int extra){int x=selection.Right-toolbar.Width,y=selection.Bottom+10;if(y+toolbar.Height+extra>screen.Bottom)y=selection.Top-toolbar.Height-extra-10;x=Math.Max(screen.Left+4,Math.Min(screen.Right-toolbar.Width-4,x));y=Math.Max(screen.Top+4,Math.Min(screen.Bottom-toolbar.Height-extra-4,y));return new Rectangle(x,y,toolbar.Width,toolbar.Height);}
    internal static Rectangle Dirty(Rectangle old,Rectangle next,Rectangle viewport){Rectangle r=old.IsEmpty?next:next.IsEmpty?old:Rectangle.Union(old,next);r.Inflate(12,36);return Rectangle.Intersect(r,viewport);}
}
internal static class DrawingTests {
    internal static string[] Run(){
        if(MarkScene.Colors.Length!=7||MarkScene.Colors[6]!=Color.Black)throw new Exception("palette");Rectangle clip=new Rectangle(20,20,160,160);
        if(MarkScene.Clamp(new Point(-3,500),clip)!=new Point(20,179))throw new Exception("clamp");RectangleF circle=Mark.CircleBounds(new PointF(90,90),new PointF(30,40));if(circle.Width!=50||circle.X!=40)throw new Exception("circle");
        Rectangle bar=EditorLayout.Toolbar(new Rectangle(100,100,400,200),new Size(424,86),new Rectangle(0,0,1000,800),0);if(bar.Y!=310||bar.X<0)throw new Exception("toolbar-below");bar=EditorLayout.Toolbar(new Rectangle(600,650,390,140),new Size(424,160),new Rectangle(0,0,1000,800),0);if(bar.Right>1000||bar.Bottom>800)throw new Exception("toolbar-edge");
        using(Bitmap image=new Bitmap(200,200))using(MarkScene scene=new MarkScene()){
            using(Graphics g=Graphics.FromImage(image))g.Clear(Color.White);scene.SetSource(image);
            foreach(Point end in new Point[]{new Point(150,100),new Point(50,100),new Point(100,50),new Point(100,150)}){Mark a=new Mark(DrawTool.Arrow,Color.Red,new Point(100,100));a.Points.Add(end);scene.Add(a);}
            using(Bitmap b=scene.Export(image,clip)){if(b.Width!=160||b.GetPixel(80,80).G>100)throw new Exception("arrow-export");}
            scene.Undo();if(scene.Marks.Count!=3||!scene.CanRedo)throw new Exception("undo");scene.Redo();if(scene.Marks.Count!=4)throw new Exception("redo");scene.Clear();
            Mark box=new Mark(DrawTool.Rectangle,Color.Blue,new Point(30,30));box.Points.Add(new Point(150,150));box.Fill=true;box.Width=7;scene.Add(box);if(scene.Hit(new Point(70,70))!=box)throw new Exception("hit");
            scene.Checkpoint();box.Move(5,5);scene.Undo();if(scene.Marks[0].Points[0].X!=30)throw new Exception("move-undo");scene.Clear();
            Mark p=new Mark(DrawTool.Pen,Color.Green,new Point(30,60));p.Points.Add(new Point(170,60));scene.Add(p);Mark text=new Mark(DrawTool.Text,Color.Black,new Point(30,90));text.Text="DSH 中文";text.TextWidth=120;scene.Add(text);
            using(Bitmap b=scene.Export(image,clip)){if(b.GetPixel(50,40).G<80||b.GetPixel(50,40).R>30)throw new Exception("pen-export");}if(image.GetPixel(70,60)!=Color.FromArgb(255,255,255))throw new Exception("source-mutated");
            Mark mosaic=new Mark(DrawTool.Mosaic,Color.Black,new Point(30,70));mosaic.Width=20;mosaic.Points.Add(new Point(120,70));scene.Add(mosaic);using(Bitmap b=scene.Export(image,clip)){if(b.Width!=160)throw new Exception("mosaic");}
            scene.Remove(mosaic);scene.Undo();if(scene.Marks[scene.Marks.Count-1].Tool!=DrawTool.Mosaic)throw new Exception("delete-undo");
            Rectangle dirty=EditorLayout.Dirty(new Rectangle(50,50,10,10),new Rectangle(55,55,10,10),new Rectangle(0,0,3840,2160));if(dirty.Width>50||dirty.Height>100)throw new Exception("dirty-repaint");
            if(!EditorLayout.ImmediateBorder(true,false,false,-1)||!EditorLayout.ImmediateBorder(false,true,false,-1)||!EditorLayout.ImmediateBorder(false,false,false,0)||EditorLayout.ImmediateBorder(false,false,true,0)||EditorLayout.ImmediateBorder(false,false,false,-1))throw new Exception("immediate-border-scheduling");
        }
        return new string[]{"seven-colors","clipping","circle-ellipse","toolbar-placement","arrow-four-directions","fill-width","hit-select","move-undo","undo-redo","delete-undo","text-pen-export","mosaic-cache","source-preserved","local-dirty-region","immediate-border-scheduling"};
    }
}
