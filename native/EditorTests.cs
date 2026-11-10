using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

internal static class EditorTests {
    internal static string[] Run(){
        using(EditorPanel panel=new EditorPanel(false)){
            int icons=0,textBoxes=0;bool pin=false,mosaic=false,attach=false;ToolButton attachButton=null,copyButton=null;
            foreach(Control child in panel.Controls){if(child is TextBox)textBoxes++;ToolButton button=child as ToolButton;if(button==null)continue;if(button.Glyph=="pin")pin=true;if(button.Glyph=="mosaic")mosaic=true;if(!String.IsNullOrEmpty(button.Glyph))icons++;if(button.Glyph=="attach"){attach=true;attachButton=button;}if(button.Glyph=="copy")copyButton=button;}
            if(icons!=14||!pin||!mosaic||!attach||textBoxes!=0)throw new Exception("compact-ui-tools");
            if(attachButton.Left<copyButton.Right||attachButton.Top!=copyButton.Top||panel.TooltipFor("attach")!="添加到对话")throw new Exception("return-icon-position-tooltip");
            panel.Tool=DrawTool.Rectangle;panel.RefreshState(true,true,true);if(panel.Height!=124)throw new Exception("expand-settings");panel.Tool=DrawTool.Select;panel.RefreshState(false,false,false);if(panel.Height!=44)throw new Exception("compact-settings");
            if(panel.BackColor!=Color.Transparent||panel.Region!=null)throw new Exception("rounded-panel-hard-clipping");
        }
        if(UiTypography.LatinFamilyName!="Josefin Sans")throw new Exception("josefin-sans-not-loaded");
        using(Font latin=UiTypography.ForText("DSH",20))using(Font cjk=UiTypography.ForText("中文",20)){if(!latin.FontFamily.Name.Contains("Josefin Sans")||!cjk.FontFamily.Name.Contains("YaHei"))throw new Exception("font-fallback");}
        if(UiTypography.HorizontalScale("DSH")>=1||UiTypography.HorizontalScale("中文")!=1)throw new Exception("font-tracking");
        using(Bitmap surface=new Bitmap(200,200))using(Graphics g=Graphics.FromImage(surface))using(Font font=UiTypography.ForText("A\nA",20)){float tight=UiTypography.MeasureAnnotation(g,"A\nA",font,160,1),wide=UiTypography.MeasureAnnotation(g,"A\nA",font,160,1.5f);if(wide<=tight)throw new Exception("line-spacing");}
        Mark textMark=new Mark(DrawTool.Text,Color.Black,PointF.Empty);textMark.LineSpacing=1.35f;if(textMark.Clone().LineSpacing!=1.35f)throw new Exception("line-spacing-clone");
        using(Bitmap icon=new Bitmap(48,48)){using(Graphics g=Graphics.FromImage(icon)){g.Clear(Color.White);Ui.Icon(g,"copy",new Rectangle(0,0,48,48),Ui.Ink);}HashSet<int> shades=new HashSet<int>();for(int y=0;y<48;y++)for(int x=0;x<48;x++){Color c=icon.GetPixel(x,y);if(c.R<250)shades.Add(c.ToArgb());}if(shades.Count<12)throw new Exception("high-quality-icon-antialiasing");}
        using(GraphicsPath border=Ui.Round(new RectangleF(.5f,.5f,422,42),10)){RectangleF bounds=border.GetBounds();if(bounds.Right>423||bounds.Bottom>43)throw new Exception("panel-border-alignment");}
        if(!PinnedShot.TestTopmost())throw new Exception("WS_EX_TOPMOST");
        using(Bitmap background=new Bitmap(80,40)){using(Graphics g=Graphics.FromImage(background))g.Clear(Color.Red);using(ScreenshotTextBox input=new ScreenshotTextBox(background)){if(input.BorderStyle!=BorderStyle.None||input.BackColor.ToArgb()!=Color.Red.ToArgb()||!input.Multiline)throw new Exception("screenshot-text-input");}}
        using(Bitmap source=new Bitmap(2560,1440))using(Bitmap target=new Bitmap(2560,1440))using(Graphics g=Graphics.FromImage(target)){
            Stopwatch watch=Stopwatch.StartNew();Rectangle full=new Rectangle(0,0,source.Width,source.Height);for(int i=0;i<30;i++)g.DrawImage(source,full,full,GraphicsUnit.Pixel);long fullMs=watch.ElapsedMilliseconds;watch.Restart();Rectangle dirty=new Rectangle(200,200,120,90);for(int i=0;i<30;i++)g.DrawImage(source,dirty,dirty,GraphicsUnit.Pixel);long localMs=watch.ElapsedMilliseconds;
            Program.Emit(new{kind="paint-benchmark",frames=30,fullMs=fullMs,localMs=localMs,fullPixels=(long)2560*1440*30,localPixels=(long)120*90*30,note="in-memory dirty-blit benchmark, not measured UI FPS"});
        }
        return new string[]{"14-icon-tools","return-after-copy-tooltip","no-footer-row","screenshot-pattern-native-input","no-sequence-tool","no-description-input","collapsible-settings","josefin-sans-with-cjk-fallback","font-tracking-line-spacing","high-quality-icon-antialiasing","panel-border-alignment","transparent-rounded-panel","native-topmost-style","bounded-dirty-blits"};
    }
}
