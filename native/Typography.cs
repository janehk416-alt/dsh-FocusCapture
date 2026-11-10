using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;

internal static class UiTypography {
    private static readonly PrivateFontCollection Collection=new PrivateFontCollection();
    private static readonly FontFamily Latin=LoadLatin();
    private static readonly Font PanelLatin=Create(Latin,"Segoe UI",13);
    private static readonly Font PanelCjk=Create(null,"Microsoft YaHei UI",13);
    internal static string LatinFamilyName{get{return Latin==null?"Segoe UI":Latin.Name;}}
    private static FontFamily LoadLatin(){
        try{
            string directory=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"fonts");
            foreach(string name in new[]{"JosefinSans-Regular.ttf","JosefinSans-SemiBold.ttf"}){string path=Path.Combine(directory,name);if(File.Exists(path))Collection.AddFontFile(path);}
            return Collection.Families.FirstOrDefault(f=>f.Name.Equals("Josefin Sans",StringComparison.OrdinalIgnoreCase));
        }catch{return null;}
    }
    private static Font Create(FontFamily family,string fallback,float size){
        try{if(family!=null)return new Font(family,size,FontStyle.Regular,GraphicsUnit.Pixel);}catch{}
        return new Font(fallback,size,FontStyle.Regular,GraphicsUnit.Pixel);
    }
    internal static bool HasCjk(string text){
        if(String.IsNullOrEmpty(text))return false;
        foreach(char c in text)if(c>='　'&&c<='鿿'||c>='豈'&&c<='﫿')return true;
        return false;
    }
    internal static Font Panel(string text){return HasCjk(text)?PanelCjk:PanelLatin;}
    internal static Font ForText(string text,float size){return Create(HasCjk(text)?null:Latin,HasCjk(text)?"Microsoft YaHei UI":"Segoe UI",size);}
    internal static float HorizontalScale(string text){return HasCjk(text)?1f:.92f;}
    private static StringFormat AnnotationFormat(){
        StringFormat format=(StringFormat)StringFormat.GenericTypographic.Clone();
        format.FormatFlags=(format.FormatFlags&~StringFormatFlags.NoWrap)|StringFormatFlags.NoClip;
        format.Trimming=StringTrimming.None;return format;
    }
    private static List<string> Wrap(Graphics graphics,string text,Font font,float width){
        List<string> lines=new List<string>();string normalized=text.Replace("\r\n","\n").Replace('\r','\n');
        using(StringFormat format=AnnotationFormat()){
            foreach(string paragraph in normalized.Split('\n')){
                if(paragraph.Length==0){lines.Add("");continue;}
                int start=0;
                while(start<paragraph.Length){
                    int remaining=paragraph.Length-start;
                    if(graphics.MeasureString(paragraph.Substring(start),font,PointF.Empty,format).Width<=width){lines.Add(paragraph.Substring(start));break;}
                    int low=1,high=remaining,best=0;
                    while(low<=high){int middle=(low+high)/2;float measured=graphics.MeasureString(paragraph.Substring(start,middle),font,PointF.Empty,format).Width;if(measured<=width){best=middle;low=middle+1;}else high=middle-1;}
                    if(best<1)best=1;int take=best;
                    if(best<remaining){int space=paragraph.LastIndexOf(' ',start+best-1,best);if(space>start)take=space-start;}
                    if(take<1)take=best;
                    string line=paragraph.Substring(start,take).TrimEnd();lines.Add(line);
                    start+=take;while(start<paragraph.Length&&(paragraph[start]==' '||paragraph[start]=='\t'))start++;
                }
            }
        }
        return lines;
    }
    internal static float MeasureAnnotation(Graphics graphics,string text,Font font,float width,float lineSpacing){
        if(String.IsNullOrEmpty(text))return 0;
        float scale=HorizontalScale(text),lineHeight=font.GetHeight(graphics)*Math.Max(.5f,lineSpacing);
        int count=Wrap(graphics,text,font,Math.Max(1,width/scale)).Count;return Math.Max(lineHeight,count*lineHeight);
    }
    internal static void DrawAnnotation(Graphics graphics,string text,Font font,Brush brush,RectangleF bounds,float lineSpacing){
        if(String.IsNullOrEmpty(text))return;
        float scale=HorizontalScale(text),available=Math.Max(1,bounds.Width/scale),lineHeight=font.GetHeight(graphics)*Math.Max(.5f,lineSpacing),y=bounds.Y;
        List<string> lines=Wrap(graphics,text,font,available);
        using(StringFormat format=AnnotationFormat())foreach(string line in lines){
            if(y>bounds.Bottom)break;
            GraphicsState state=graphics.Save();
            try{graphics.TranslateTransform(bounds.X,y);graphics.ScaleTransform(scale,1);graphics.DrawString(line,font,brush,new PointF(0,0),format);}
            finally{graphics.Restore(state);}y+=lineHeight;
        }
    }
}
