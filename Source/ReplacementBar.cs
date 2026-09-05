using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing; using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
[assembly: System.Reflection.AssemblyTitle("Taskbar Compass")]
[assembly: System.Reflection.AssemblyDescription("A configurable Windows replacement taskbar")]
[assembly: System.Reflection.AssemblyProduct("Taskbar Compass")]
[assembly: System.Reflection.AssemblyVersion("0.14.4.0")]
[assembly: System.Reflection.AssemblyFileVersion("0.14.4.0")]
namespace CompassBar {
static class AppIdentity {
 public static readonly Icon Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);
}
enum Edge { Left=0, Top=1, Right=2, Bottom=3 }
sealed class CompactTip:ToolTip {
 readonly Font font=new Font("Segoe UI",9);
 public static string Clean(string text){string clean=System.Text.RegularExpressions.Regex.Replace(text??"","\\s+"," ").Trim();return clean.Length>180?clean.Substring(0,177)+"...":clean;}
 public new void SetToolTip(Control control,string text){base.SetToolTip(control,Clean(text));}
 public CompactTip(){OwnerDraw=true;InitialDelay=500;ReshowDelay=150;AutoPopDelay=5000;UseAnimation=false;UseFading=false;
  Popup+=delegate(object sender,PopupEventArgs e){float scale;using(Graphics g=e.AssociatedControl.CreateGraphics())scale=g.DpiX/96f;Size text=TextRenderer.MeasureText(GetToolTip(e.AssociatedControl),font,new Size((int)(300*scale),Int32.MaxValue),TextFormatFlags.WordBreak|TextFormatFlags.NoPadding|TextFormatFlags.NoPrefix);e.ToolTipSize=new Size(text.Width+12,text.Height+10);};
  Draw+=delegate(object sender,DrawToolTipEventArgs e){using(SolidBrush fill=new SolidBrush(Color.FromArgb(22,26,33)))e.Graphics.FillRectangle(fill,e.Bounds);using(Pen border=new Pen(Color.FromArgb(57,68,83)))e.Graphics.DrawRectangle(border,0,0,e.Bounds.Width-1,e.Bounds.Height-1);TextRenderer.DrawText(e.Graphics,e.ToolTipText,font,new Rectangle(6,5,e.Bounds.Width-12,e.Bounds.Height-10),Color.FromArgb(238,244,251),TextFormatFlags.WordBreak|TextFormatFlags.NoPadding|TextFormatFlags.NoPrefix);};
 }
 protected override void Dispose(bool disposing){base.Dispose(disposing);if(disposing)font.Dispose();}
}
sealed class MenuColors:ProfessionalColorTable {
 public override Color ToolStripDropDownBackground{get{return Color.FromArgb(22,26,33);}}
 public override Color ImageMarginGradientBegin{get{return ToolStripDropDownBackground;}}
 public override Color ImageMarginGradientMiddle{get{return ToolStripDropDownBackground;}}
 public override Color ImageMarginGradientEnd{get{return ToolStripDropDownBackground;}}
 public override Color MenuBorder{get{return Color.FromArgb(57,68,83);}}
}
sealed class DarkMenuRenderer:ToolStripProfessionalRenderer {
 public DarkMenuRenderer():base(new MenuColors()){RoundedEdges=false;}
 protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e){
  Rectangle r=new Rectangle(2,1,e.Item.Width-4,e.Item.Height-2);bool selected=e.Item.Enabled&&(e.Item.Selected||e.Item.Pressed);
  using(SolidBrush fill=new SolidBrush(selected?Color.FromArgb(39,62,82):Color.FromArgb(22,26,33)))e.Graphics.FillRectangle(fill,r);
  if(selected)using(SolidBrush accent=new SolidBrush(Color.FromArgb(112,196,248)))e.Graphics.FillRectangle(accent,r.Left,r.Top,3,r.Height);
 }
 protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e){
  Color ink=e.Item.Enabled?Color.FromArgb(238,244,251):Color.FromArgb(152,165,183);
  TextRenderer.DrawText(e.Graphics,CompactTip.Clean(e.Text),e.TextFont,e.TextRectangle,ink,TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
 }
 protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e){e.ArrowColor=Color.FromArgb(152,190,218);base.OnRenderArrow(e);}
 protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e){using(Pen line=new Pen(Color.FromArgb(47,57,70)))e.Graphics.DrawLine(line,14,e.Item.Height/2,e.Item.Width-14,e.Item.Height/2);}
 protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e){Rectangle r=e.ImageRectangle;using(Pen pen=new Pen(Color.FromArgb(112,196,248),2)){e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;e.Graphics.DrawLines(pen,new[]{new Point(r.Left+2,r.Top+r.Height/2),new Point(r.Left+r.Width/2-1,r.Bottom-4),new Point(r.Right-1,r.Top+3)});}}
}
sealed class DarkMenu:ContextMenuStrip {
 static readonly ToolStripRenderer Theme=new DarkMenuRenderer();
 public DarkMenu(){Renderer=Theme;BackColor=Color.FromArgb(22,26,33);ForeColor=Color.FromArgb(238,244,251);Font=new Font("Segoe UI",10);Padding=new Padding(3);ShowImageMargin=true;}
 protected override void OnOpening(System.ComponentModel.CancelEventArgs e){Style(this);base.OnOpening(e);}
 static void AttachSubmenu(object sender,EventArgs e){
  ToolStripMenuItem item=(ToolStripMenuItem)sender;ToolStripDropDown child=item.DropDown;ToolStrip parent=item.Owner;
  if(parent==null||!child.Visible)return;
  Native.RECT outer,inner;if(!Native.GetWindowRect(parent.Handle,out outer)||!Native.GetWindowRect(child.Handle,out inner))return;
  Rectangle work=Screen.FromHandle(parent.Handle).WorkingArea;int width=inner.R-inner.L;
  const int gap=4;int x=outer.R+gap;if(x+width>work.Right&&outer.L-width-gap>=work.Left)x=outer.L-width-gap;
  x=Math.Max(work.Left,Math.Min(x,work.Right-width));
  Native.SetWindowPos(child.Handle,IntPtr.Zero,x,inner.T,0,0,0x15);
 }
 public static void Style(ToolStrip strip){
  strip.Renderer=Theme;strip.BackColor=Color.FromArgb(22,26,33);strip.ForeColor=Color.FromArgb(238,244,251);strip.Padding=new Padding(3);
  float scale;using(Graphics g=strip.CreateGraphics())scale=g.DpiX/96f;
  int widest=strip.Items.Cast<ToolStripItem>().Where(i=>!(i is ToolStripSeparator)).Select(i=>TextRenderer.MeasureText(CompactTip.Clean(i.Text),strip.Font).Width).DefaultIfEmpty(0).Max();
  int width=Math.Max((int)(180*scale),Math.Min((int)(300*scale),widest+(int)(54*scale)));
  foreach(ToolStripItem item in strip.Items){item.ForeColor=strip.ForeColor;item.AutoSize=false;item.Size=new Size(width,(int)((item is ToolStripSeparator?8:item.Enabled?28:24)*scale));
   ToolStripMenuItem menu=item as ToolStripMenuItem;if(menu!=null&&menu.HasDropDownItems){menu.DropDown.Font=strip.Font;Style(menu.DropDown);menu.DropDownOpened-=AttachSubmenu;menu.DropDownOpened+=AttachSubmenu;}
  }
 }
}
sealed class SearchEntry {
 public string Name,File;public IntPtr Window;
 public string Label{get{return Name+(Window==IntPtr.Zero?"":" (open)");}}
}
sealed class QuickSite {
 public string Name,Url;
 public override string ToString(){return Name;}
}
static class QuickSites {
 public static bool NormalizeUrl(string text,out string url){
  url=null;string input=text.Trim();if(input.IndexOf(":",StringComparison.Ordinal)<0)input="https://"+input;
  Uri uri;if(!Uri.TryCreate(input,UriKind.Absolute,out uri)||(uri.Scheme!=Uri.UriSchemeHttps&&uri.Scheme!=Uri.UriSchemeHttp)||String.IsNullOrEmpty(uri.Host))return false;
  url=uri.AbsoluteUri;return true;
 }
 public static List<QuickSite> Load(){
  string file=Path.Combine(Preferences.Folder,"quick-sites.txt");
  if(!File.Exists(file))return new List<QuickSite>{new QuickSite{Name="YouTube",Url="https://www.youtube.com/"},new QuickSite{Name="Amazon",Url="https://www.amazon.com/"}};
  List<QuickSite> sites=new List<QuickSite>();
  foreach(string line in File.ReadAllLines(file))try{string[] fields=line.Split('|');if(fields.Length!=2)continue;string name=Encoding.UTF8.GetString(Convert.FromBase64String(fields[0])).Trim(),url;if(name.Length>0&&NormalizeUrl(Encoding.UTF8.GetString(Convert.FromBase64String(fields[1])),out url))sites.Add(new QuickSite{Name=name,Url=url});}catch(FormatException){}
  return sites;
 }
 public static void Save(IEnumerable<QuickSite> sites){
  string[] lines=sites.Select(s=>Convert.ToBase64String(Encoding.UTF8.GetBytes(s.Name))+"|"+Convert.ToBase64String(Encoding.UTF8.GetBytes(s.Url))).ToArray();
  Directory.CreateDirectory(Preferences.Folder);string file=Path.Combine(Preferences.Folder,"quick-sites.txt"),temp=file+"."+Guid.NewGuid().ToString("N")+".tmp";
  try{File.WriteAllLines(temp,lines);if(File.Exists(file))File.Replace(temp,file,null);else File.Move(temp,file);}finally{if(File.Exists(temp))File.Delete(temp);}
 }
 public static void AddTo(ToolStripItemCollection items,Action<string> launch){
  foreach(QuickSite site in Load()){string url=site.Url;items.Add("Open "+site.Name.Replace("&","&&"),null,delegate{launch(url);});}
 }
}
sealed class QuickSitesEditor:Form {
 readonly ListBox sites=new ListBox();readonly TextBox name=new TextBox(),url=new TextBox();
 public QuickSitesEditor(){
  Text="Edit quick websites";Icon=AppIdentity.Icon;ClientSize=new Size(520,490);StartPosition=FormStartPosition.CenterParent;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;Font=new Font("Segoe UI",10);BackColor=Color.FromArgb(22,26,33);ForeColor=Color.FromArgb(238,244,251);
  Controls.Add(new Label{Text="Your quick websites",Location=new Point(24,18),Size=new Size(470,32),Font=new Font("Segoe UI Semibold",17)});
  sites.SetBounds(24,62,472,170);sites.BackColor=Color.FromArgb(34,41,52);sites.ForeColor=ForeColor;sites.BorderStyle=BorderStyle.FixedSingle;Controls.Add(sites);
  foreach(QuickSite site in QuickSites.Load())sites.Items.Add(site);
  Controls.Add(new Label{Text="Button name",Location=new Point(24,248),Size=new Size(130,24)});name.SetBounds(158,245,338,28);name.MaxLength=60;Controls.Add(name);
  Controls.Add(new Label{Text="Website address",Location=new Point(24,287),Size=new Size(130,24)});url.SetBounds(158,284,338,28);Controls.Add(url);
  Controls.Add(new Label{Text="Example: youtube.com — then click Add / update.",Location=new Point(24,324),Size=new Size(472,24),ForeColor=Color.FromArgb(152,165,183)});
  sites.SelectedIndexChanged+=delegate{QuickSite site=sites.SelectedItem as QuickSite;if(site!=null){name.Text=site.Name;url.Text=site.Url;}};
  AddButton("New",24,362,88,delegate{sites.ClearSelected();name.Clear();url.Clear();name.Focus();});
  AddButton("Add / update",124,362,160,delegate{string address,title=name.Text.Trim();if(title.Length==0){MessageBox.Show(this,"Enter a button name.");name.Focus();return;}if(!QuickSites.NormalizeUrl(url.Text,out address)){MessageBox.Show(this,"Enter a website address, such as youtube.com or https://example.com.");url.Focus();return;}int index=sites.SelectedIndex;QuickSite site=new QuickSite{Name=title,Url=address};if(index>=0)sites.Items[index]=site;else index=sites.Items.Add(site);sites.SelectedIndex=index;});
  AddButton("Remove",296,362,100,delegate{int index=sites.SelectedIndex;if(index>=0){sites.Items.RemoveAt(index);sites.ClearSelected();name.Clear();url.Clear();}});
  AddButton("Save",276,434,104,delegate{try{QuickSites.Save(sites.Items.Cast<QuickSite>());DialogResult=DialogResult.OK;}catch(Exception ex){MessageBox.Show(this,"Couldn't save your websites.\n"+ex.Message,"Taskbar Compass");}});
  Button cancel=AddButton("Cancel",392,434,104,delegate{DialogResult=DialogResult.Cancel;});CancelButton=cancel;
  AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;
 }
 Button AddButton(string text,int x,int y,int width,Action action){Button button=new Button{Text=text,Location=new Point(x,y),Size=new Size(width,36),FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(39,62,82),ForeColor=ForeColor};button.FlatAppearance.BorderSize=0;button.Click+=delegate{action();};Controls.Add(button);return button;}
}
sealed class SearchWindow:Form {
 readonly TextBox query=new TextBox();readonly ListBox results=new ListBox();readonly Label status=new Label(),empty=new Label();readonly Button open=new Button();readonly List<SearchEntry> catalog;
 readonly Dictionary<SearchEntry,Bitmap> icons=new Dictionary<SearchEntry,Bitmap>();
 readonly Color muted=Color.FromArgb(152,165,183),ink=Color.FromArgb(238,244,251);
 [DllImport("dwmapi.dll")]static extern int DwmSetWindowAttribute(IntPtr window,int attribute,ref int value,int size);
 public SearchEntry Selected;
 public SearchWindow(){
  SuspendLayout();Text="Taskbar Compass - Search";Icon=AppIdentity.Icon;ClientSize=new Size(640,580);StartPosition=FormStartPosition.CenterScreen;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;Font=new Font("Segoe UI",10);BackColor=Color.FromArgb(22,26,33);ForeColor=ink;
  Controls.Add(new Label{Text="Find your next app",Location=new Point(26,21),Size=new Size(580,42),Font=new Font("Segoe UI Semibold",23)});
  Controls.Add(new Label{Text="Your apps and open windows, one search away.",Location=new Point(28,68),Size=new Size(580,24),ForeColor=muted});
  Panel field=new Panel{Location=new Point(28,108),Size=new Size(584,54),BackColor=Color.FromArgb(34,41,52)};Controls.Add(field);
  field.Paint+=delegate(object sender,PaintEventArgs e){float scale=field.Width/584f;using(Pen pen=new Pen(Color.FromArgb(112,196,248),2*scale)){e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;e.Graphics.DrawEllipse(pen,17*scale,17*scale,14*scale,14*scale);e.Graphics.DrawLine(pen,29*scale,29*scale,36*scale,36*scale);}using(Pen border=new Pen(Color.FromArgb(66,90,113)))e.Graphics.DrawRectangle(border,0,0,field.Width-1,field.Height-1);};
  query.BorderStyle=BorderStyle.None;query.BackColor=field.BackColor;query.ForeColor=ink;query.Font=new Font("Segoe UI",13);query.SetBounds(50,14,518,28);query.AccessibleName="Search apps and open windows";field.Controls.Add(query);
  status.SetBounds(28,179,584,24);status.ForeColor=muted;status.Font=new Font("Segoe UI Semibold",9);Controls.Add(status);
  results.SetBounds(28,211,584,280);results.DisplayMember="Label";results.BorderStyle=BorderStyle.None;results.BackColor=BackColor;results.ForeColor=ink;results.DrawMode=DrawMode.OwnerDrawFixed;results.ItemHeight=68;results.IntegralHeight=false;results.AccessibleName="Search results";results.DrawItem+=DrawResult;Controls.Add(results);
  empty.SetBounds(62,278,516,116);empty.TextAlign=ContentAlignment.MiddleCenter;empty.ForeColor=muted;empty.Font=new Font("Segoe UI",12);Controls.Add(empty);empty.BringToFront();
  Controls.Add(new Label{Text="Enter to open   /   Esc to close",Location=new Point(28,529),Size=new Size(310,24),ForeColor=muted,Font=new Font("Segoe UI",9)});
  open.Text="Open app";open.SetBounds(479,518,133,40);open.FlatStyle=FlatStyle.Flat;open.FlatAppearance.BorderSize=0;open.BackColor=Color.FromArgb(112,196,248);open.ForeColor=Color.FromArgb(14,31,44);open.Enabled=false;open.Click+=delegate{OpenSelected();};Controls.Add(open);AcceptButton=open;
  Button cancel=new Button{Text="Close",Location=new Point(365,518),Size=new Size(102,40),FlatStyle=FlatStyle.Flat,DialogResult=DialogResult.Cancel,BackColor=Color.FromArgb(34,41,52),ForeColor=ink};cancel.FlatAppearance.BorderSize=0;Controls.Add(cancel);CancelButton=cancel;
  catalog=AppSearch.Catalog();query.TextChanged+=delegate{RefreshResults();};
  query.KeyDown+=delegate(object sender,KeyEventArgs e){if(e.KeyCode==Keys.Down&&results.Items.Count>0){e.SuppressKeyPress=true;results.Focus();}};results.DoubleClick+=delegate{OpenSelected();};
  results.SelectedIndexChanged+=delegate{SearchEntry entry=results.SelectedItem as SearchEntry;open.Enabled=entry!=null;open.Text=entry!=null&&entry.Window!=IntPtr.Zero?"Switch to window":"Open app";};
  Shown+=delegate{results.ItemHeight=(int)Math.Round(68*CreateGraphicsDpi());query.Focus();};AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;ResumeLayout(true);RefreshResults();
 }
 float CreateGraphicsDpi(){using(Graphics g=CreateGraphics())return g.DpiX/96f;}
 protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);int dark=1;DwmSetWindowAttribute(Handle,20,ref dark,4);}
 void RefreshResults(){
  results.BeginUpdate();results.Items.Clear();results.Items.AddRange(AppSearch.Find(catalog,query.Text).ToArray());results.EndUpdate();if(results.Items.Count>0)results.SelectedIndex=0;else open.Enabled=false;
  bool blank=query.Text.Trim().Length==0;status.Text=blank?"APP SEARCH":results.Items.Count==0?"NO MATCHES":results.Items.Count+" RESULTS";empty.Visible=results.Items.Count==0;
  empty.Text=blank?"What would you like to open?\n\nTry Minecraft, a browser, or an open window.":"No apps found\n\nTry a shorter name or check Installed apps.";
 }
 void DrawResult(object sender,DrawItemEventArgs e){
  if(e.Index<0)return;SearchEntry entry=(SearchEntry)results.Items[e.Index];float s=e.Graphics.DpiX/96f;bool selected=(e.State&DrawItemState.Selected)!=0;
  using(SolidBrush background=new SolidBrush(BackColor))e.Graphics.FillRectangle(background,e.Bounds);Rectangle card=new Rectangle(e.Bounds.X,e.Bounds.Y+2,e.Bounds.Width-2,e.Bounds.Height-4);
  using(SolidBrush fill=new SolidBrush(selected?Color.FromArgb(39,62,82):Color.FromArgb(29,35,44)))e.Graphics.FillRectangle(fill,card);
  if(selected)using(SolidBrush accent=new SolidBrush(Color.FromArgb(112,196,248)))e.Graphics.FillRectangle(accent,card.Left,card.Top,3*s,card.Height);
  Bitmap icon;if(!icons.TryGetValue(entry,out icon)){icon=entry.Window!=IntPtr.Zero?Native.WindowIcon(entry.Window):new PinnedApp(entry.File).GetIcon();icons.Add(entry,icon);}
  e.Graphics.DrawImage(icon,new Rectangle(card.Left+(int)(16*s),card.Top+(int)(14*s),(int)(32*s),(int)(32*s)));
  Rectangle title=new Rectangle(card.Left+(int)(62*s),card.Top+(int)(9*s),card.Width-(int)(80*s),(int)(25*s));
  TextRenderer.DrawText(e.Graphics,entry.Name,Font,title,ink,TextFormatFlags.EndEllipsis|TextFormatFlags.SingleLine|TextFormatFlags.NoPrefix);
  string detail=entry.Window!=IntPtr.Zero?"Open window - switch back in":entry.File.StartsWith("shell:AppsFolder",StringComparison.OrdinalIgnoreCase)?"Installed app":"App shortcut";
  using(Font small=new Font("Segoe UI",9))TextRenderer.DrawText(e.Graphics,detail,small,new Rectangle(title.Left,card.Top+(int)(35*s),title.Width,(int)(21*s)),muted,TextFormatFlags.EndEllipsis|TextFormatFlags.SingleLine);
  if((e.State&DrawItemState.Focus)!=0)e.DrawFocusRectangle();
 }
 void OpenSelected(){Selected=results.SelectedItem as SearchEntry;if(Selected!=null)DialogResult=DialogResult.OK;}
 protected override void Dispose(bool disposing){if(disposing)foreach(Bitmap icon in icons.Values)icon.Dispose();base.Dispose(disposing);}
}
static class AppSearch {
 static List<SearchEntry> shortcuts;static DateTime indexed;
 static object Com(object obj,string name,System.Reflection.BindingFlags flags,params object[] args){return obj.GetType().InvokeMember(name,flags,null,obj,args);}
 static void Installed(List<SearchEntry> found){
  object shell=null,folder=null,items=null;
  try{shell=Activator.CreateInstance(Type.GetTypeFromProgID("Shell.Application"));folder=Com(shell,"NameSpace",System.Reflection.BindingFlags.InvokeMethod,"shell:AppsFolder");if(folder==null)return;
   items=Com(folder,"Items",System.Reflection.BindingFlags.InvokeMethod);int count=Convert.ToInt32(Com(items,"Count",System.Reflection.BindingFlags.GetProperty));
   for(int n=0;n<count;n++){object item=null;try{item=Com(items,"Item",System.Reflection.BindingFlags.InvokeMethod,n);string name=Convert.ToString(Com(item,"Name",System.Reflection.BindingFlags.GetProperty));string id=Convert.ToString(Com(item,"ExtendedProperty",System.Reflection.BindingFlags.InvokeMethod,"System.AppUserModel.ID"));if(!String.IsNullOrEmpty(name)&&!String.IsNullOrEmpty(id))found.Add(new SearchEntry{Name=name,File="shell:AppsFolder\\"+id});}catch{}finally{if(item!=null)Marshal.ReleaseComObject(item);}}
  }catch{}finally{foreach(object obj in new[]{items,folder,shell})if(obj!=null)Marshal.ReleaseComObject(obj);}
 }
 static void Scan(string folder,List<SearchEntry> found){
  try{foreach(string path in Directory.GetFiles(folder)){string ext=Path.GetExtension(path);if(ext.Equals(".lnk",StringComparison.OrdinalIgnoreCase)||ext.Equals(".appref-ms",StringComparison.OrdinalIgnoreCase)||ext.Equals(".url",StringComparison.OrdinalIgnoreCase))found.Add(new SearchEntry{Name=Path.GetFileNameWithoutExtension(path),File=path});}
   foreach(string sub in Directory.GetDirectories(folder))if((File.GetAttributes(sub)&FileAttributes.ReparsePoint)==0)Scan(sub,found);
  }catch(UnauthorizedAccessException){}catch(IOException){}
 }
 public static List<SearchEntry> Catalog(){
  if(shortcuts==null||(DateTime.UtcNow-indexed).TotalMinutes>2){shortcuts=new List<SearchEntry>();Installed(shortcuts);Scan(Environment.GetFolderPath(Environment.SpecialFolder.Programs),shortcuts);Scan(Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms),shortcuts);indexed=DateTime.UtcNow;}
  List<SearchEntry> result=new List<SearchEntry>();
  foreach(IntPtr h in Native.Windows(Process.GetCurrentProcess().Id))result.Add(new SearchEntry{Name=Native.Title(h),Window=h});
  result.AddRange(Preferences.Pins.Concat(Preferences.Groups.SelectMany(g=>g.Apps)).Select(a=>new SearchEntry{Name=a.Name,File=a.Path}));result.AddRange(shortcuts);
  result.Add(new SearchEntry{Name="Windows Settings",File="ms-settings:"});result.Add(new SearchEntry{Name="File Explorer",File="explorer.exe"});result.Add(new SearchEntry{Name="Notepad",File="notepad.exe"});
  return result.GroupBy(e=>e.Window==IntPtr.Zero?e.File:"window:"+e.Window,StringComparer.OrdinalIgnoreCase).Select(g=>g.First()).ToList();
 }
 public static List<SearchEntry> Find(IEnumerable<SearchEntry> catalog,string query){
  string[] words=query.Trim().Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries);if(words.Length==0)return new List<SearchEntry>();
  return catalog.Where(e=>words.All(w=>e.Name.IndexOf(w,StringComparison.OrdinalIgnoreCase)>=0)).OrderByDescending(e=>e.Name.Equals(query.Trim(),StringComparison.OrdinalIgnoreCase)).ThenByDescending(e=>e.Name.StartsWith(query.Trim(),StringComparison.OrdinalIgnoreCase)).ThenBy(e=>e.Name,StringComparer.OrdinalIgnoreCase).Take(12).ToList();
 }
}
sealed class PinnedApp {
 public string Path,Target;
 public string Name{get{return System.IO.Path.GetFileNameWithoutExtension(Path);}}
 public PinnedApp(string path){Path=path;Target=path;
  if(System.IO.Path.GetExtension(path).Equals(".lnk",StringComparison.OrdinalIgnoreCase)){
   object shell=null,shortcut=null;
   try{Type t=Type.GetTypeFromProgID("WScript.Shell");shell=Activator.CreateInstance(t);
    shortcut=t.InvokeMember("CreateShortcut",System.Reflection.BindingFlags.InvokeMethod,null,shell,new object[]{path});
    Target=(string)shortcut.GetType().InvokeMember("TargetPath",System.Reflection.BindingFlags.GetProperty,null,shortcut,null);
   }catch{}finally{if(shortcut!=null)Marshal.ReleaseComObject(shortcut);if(shell!=null)Marshal.ReleaseComObject(shell);}
  }
 }
 [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]struct INFO{public IntPtr Icon;public int Index;public uint Attributes;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=260)]public string Display;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=80)]public string Type;}
 [DllImport("shell32.dll",CharSet=CharSet.Unicode)]static extern IntPtr SHGetFileInfo(string path,uint attrs,ref INFO info,uint size,uint flags);
 [DllImport("shell32.dll",CharSet=CharSet.Unicode)]static extern int SHParseDisplayName(string name,IntPtr context,out IntPtr pidl,uint flags,IntPtr attributes);
 [DllImport("shell32.dll",EntryPoint="SHGetFileInfoW")]static extern IntPtr SHGetFileInfoPidl(IntPtr pidl,uint attrs,ref INFO info,uint size,uint flags);
 [DllImport("user32.dll")]static extern bool DestroyIcon(IntPtr icon);
 public Bitmap GetIcon(){INFO i=new INFO();IntPtr pidl=IntPtr.Zero;try{if(Path.StartsWith("shell:",StringComparison.OrdinalIgnoreCase)&&SHParseDisplayName(Path,IntPtr.Zero,out pidl,0,IntPtr.Zero)==0)SHGetFileInfoPidl(pidl,0,ref i,(uint)Marshal.SizeOf(i),0x108);else SHGetFileInfo(Path,0,ref i,(uint)Marshal.SizeOf(i),0x100);if(i.Icon!=IntPtr.Zero)return Icon.FromHandle(i.Icon).ToBitmap();}catch{}finally{if(i.Icon!=IntPtr.Zero)DestroyIcon(i.Icon);if(pidl!=IntPtr.Zero)Marshal.FreeCoTaskMem(pidl);}return SystemIcons.Application.ToBitmap();}
 public static string WindowPath(IntPtr h){try{uint pid;Native.GetWindowThreadProcessId(h,out pid);using(Process p=Process.GetProcessById((int)pid))return p.MainModule.FileName;}catch{return "";}}
}
sealed class AppGroup {
 public string Name;
 public string LastSelected="";
 public PinnedApp IconApp{get{return Apps.FirstOrDefault(a=>String.Equals(a.Path,LastSelected,StringComparison.OrdinalIgnoreCase))??Apps.FirstOrDefault();}}
 public readonly List<PinnedApp> Apps=new List<PinnedApp>();
}
sealed class GroupEditor:Form {
 readonly TextBox name=new TextBox();readonly CheckedListBox list=new CheckedListBox();
 readonly List<PinnedApp> candidates=new List<PinnedApp>();public AppGroup Result;
 public GroupEditor(AppGroup group){
  SuspendLayout();Text=group==null?"Create app group":"Edit app group";Icon=AppIdentity.Icon;ClientSize=new Size(480,460);StartPosition=FormStartPosition.CenterScreen;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;Font=new Font("Segoe UI",10);
  Controls.Add(new Label{Text="Group name",Location=new Point(20,16),Size=new Size(430,24)});name.SetBounds(20,43,440,28);name.MaxLength=40;name.Text=group==null?"":group.Name;Controls.Add(name);
  Controls.Add(new Label{Text="Choose apps (unchecked apps stay outside this group)",Location=new Point(20,83),Size=new Size(440,30)});
  list.SetBounds(20,118,440,262);list.CheckOnClick=true;Controls.Add(list);
  IEnumerable<PinnedApp> all=Preferences.Pins.Concat(Preferences.Groups.SelectMany(g=>g.Apps));
  foreach(IntPtr h in Native.Windows(Process.GetCurrentProcess().Id)){string p=PinnedApp.WindowPath(h);if(!String.IsNullOrEmpty(p))all=all.Concat(new[]{new PinnedApp(p)});}
  foreach(PinnedApp app in all)AddCandidate(app,group!=null&&group.Apps.Any(a=>Same(a.Path,app.Path)));
  Button browse=new Button{Text="Browse...",Location=new Point(20,400),Size=new Size(120,38)};
  browse.Click+=delegate{using(OpenFileDialog d=new OpenFileDialog{Title="Add apps to group",Filter="Programs and shortcuts|*.exe;*.lnk",DereferenceLinks=false,Multiselect=true})if(d.ShowDialog(this)==DialogResult.OK)foreach(string path in d.FileNames)AddCandidate(new PinnedApp(path),true);};Controls.Add(browse);
  Button save=new Button{Text="Save group",Location=new Point(210,400),Size=new Size(125,38)};
  save.Click+=delegate{
   string title=name.Text.Trim();if(title.Length==0){MessageBox.Show(this,"Enter a group name.");name.Focus();return;}
   if(Preferences.Groups.Any(g=>g!=group&&Same(g.Name,title))){MessageBox.Show(this,"A group with that name already exists.");return;}
   Result=new AppGroup{Name=title};foreach(int i in list.CheckedIndices)Result.Apps.Add(candidates[i]);DialogResult=DialogResult.OK;
  };Controls.Add(save);AcceptButton=save;
  Button cancel=new Button{Text="Cancel",Location=new Point(345,400),Size=new Size(115,38),DialogResult=DialogResult.Cancel};Controls.Add(cancel);CancelButton=cancel;
  cancel.Click+=delegate{DialogResult=DialogResult.Cancel;Close();};
  AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;ResumeLayout(true);
 }
 static bool Same(string a,string b){return String.Equals(a,b,StringComparison.OrdinalIgnoreCase);}
 void AddCandidate(PinnedApp app,bool selected){int index=candidates.FindIndex(a=>Same(a.Path,app.Path));if(index>=0){if(selected)list.SetItemChecked(index,true);return;}candidates.Add(app);list.Items.Add(app.Name,selected);}
}
static class Preferences {
 public static string Folder=RecoveryRecord.Folder;
 public static int OpacityPercent=100;
 public static readonly Color DefaultTint=Color.FromArgb(28,30,36);
 public static Color BackgroundTint=DefaultTint;
 public static bool AppsAtEnd;
 public static readonly List<PinnedApp> Pins=new List<PinnedApp>();
 public static readonly List<AppGroup> Groups=new List<AppGroup>();
 static string Encode(string s){return Convert.ToBase64String(Encoding.UTF8.GetBytes(s));}
 static string Decode(string s){return Encoding.UTF8.GetString(Convert.FromBase64String(s));}
 public static void SaveGroups(){Directory.CreateDirectory(Folder);File.WriteAllLines(Path.Combine(Folder,"groups.txt"),Groups.Select(g=>String.Join("|",new[]{Encode(g.Name),"@"+Encode(g.LastSelected??"")}.Concat(g.Apps.Select(a=>Encode(a.Path))))).ToArray());}
 public static bool Grouped(PinnedApp app){return Groups.Any(g=>g.Apps.Any(a=>String.Equals(a.Path,app.Path,StringComparison.OrdinalIgnoreCase)||(!String.IsNullOrEmpty(a.Target)&&String.Equals(a.Target,app.Target,StringComparison.OrdinalIgnoreCase))));}
 public static void Load(){
  Directory.CreateDirectory(Folder);Pins.Clear();OpacityPercent=100;int n;
  Groups.Clear();string groups=Path.Combine(Folder,"groups.txt");if(File.Exists(groups))foreach(string line in File.ReadAllLines(groups))try{string[] fields=line.Split('|');AppGroup g=new AppGroup{Name=Decode(fields[0])};foreach(string p in fields.Skip(1)){if(p.StartsWith("@"))g.LastSelected=Decode(p.Substring(1));else g.Apps.Add(new PinnedApp(Decode(p)));}Groups.Add(g);}catch{}
  string alignment=Path.Combine(Folder,"alignment.txt");AppsAtEnd=File.Exists(alignment)&&File.ReadAllText(alignment).Trim()=="end";
  string opacity=Path.Combine(Folder,"opacity.txt");if(File.Exists(opacity)&&Int32.TryParse(File.ReadAllText(opacity),out n))OpacityPercent=Math.Max(35,Math.Min(100,n));
  BackgroundTint=DefaultTint;string tint=Path.Combine(Folder,"tint.txt");
  if(File.Exists(tint)){string hex=File.ReadAllText(tint).Trim();if(hex.Length==6&&Int32.TryParse(hex,System.Globalization.NumberStyles.HexNumber,System.Globalization.CultureInfo.InvariantCulture,out n))BackgroundTint=Color.FromArgb(255,(n>>16)&255,(n>>8)&255,n&255);}
  string file=Path.Combine(Folder,"pins.txt");
  if(File.Exists(file)){foreach(string line in File.ReadAllLines(file))try{Add(Encoding.UTF8.GetString(Convert.FromBase64String(line)));}catch{}}
  else{string native=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),@"Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar");if(Directory.Exists(native))foreach(string p in Directory.GetFiles(native,"*.lnk"))Add(p);SavePins();}
 }
 public static void Add(string path){if(!Pins.Any(p=>String.Equals(p.Path,path,StringComparison.OrdinalIgnoreCase)))Pins.Add(new PinnedApp(path));}
 public static void SavePins(){Directory.CreateDirectory(Folder);File.WriteAllLines(Path.Combine(Folder,"pins.txt"),Pins.Select(p=>Convert.ToBase64String(Encoding.UTF8.GetBytes(p.Path))).ToArray());}
 public static void SetOpacity(int value){OpacityPercent=Math.Max(35,Math.Min(100,value));Directory.CreateDirectory(Folder);File.WriteAllText(Path.Combine(Folder,"opacity.txt"),OpacityPercent.ToString());}
 public static void SetAlignment(bool atEnd){Directory.CreateDirectory(Folder);File.WriteAllText(Path.Combine(Folder,"alignment.txt"),atEnd?"end":"start");AppsAtEnd=atEnd;}
 public static void SetTint(Color color){Directory.CreateDirectory(Folder);File.WriteAllText(Path.Combine(Folder,"tint.txt"),(color.ToArgb()&0xFFFFFF).ToString("X6",System.Globalization.CultureInfo.InvariantCulture));BackgroundTint=Color.FromArgb(255,color.R,color.G,color.B);}
}
static class Native {
 public delegate void WindowEvent(IntPtr hook,uint kind,IntPtr window,int obj,int child,uint thread,uint time);
 [DllImport("user32.dll")]public static extern IntPtr SetWinEventHook(uint first,uint last,IntPtr module,WindowEvent callback,uint process,uint thread,uint flags);
 [DllImport("user32.dll")]public static extern bool UnhookWinEvent(IntPtr hook);
 [DllImport("user32.dll")]public static extern bool IsZoomed(IntPtr window);
 [DllImport("user32.dll")]public static extern IntPtr MonitorFromWindow(IntPtr window,uint flags);
 [StructLayout(LayoutKind.Sequential)] public struct RECT {
  public int L,T,R,B;
  public RECT(Rectangle r){L=r.Left;T=r.Top;R=r.Right;B=r.Bottom;}
  public Rectangle Rectangle{get{return Rectangle.FromLTRB(L,T,R,B);}}
 }
 [StructLayout(LayoutKind.Sequential)] public struct ABD {
  public uint Size;public IntPtr Window;public uint Callback,Edge;public RECT Rect;public IntPtr Param;
 }
 [StructLayout(LayoutKind.Sequential)] public struct MONITORINFO {public int Size;public RECT Monitor,Work;public uint Flags;}
 public delegate bool EnumProc(IntPtr h,IntPtr p);
 [DllImport("shell32.dll")] public static extern UIntPtr SHAppBarMessage(uint m,ref ABD d);
 [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb,IntPtr p);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern IntPtr FindWindow(string c,string t);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern int GetClassName(IntPtr h,StringBuilder s,int n);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern int GetWindowText(IntPtr h,StringBuilder s,int n);
 [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
 [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr h);
 [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h,out RECT rect);
 [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h,int c);
 [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr h);
 [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
 [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr h,uint cmd);
 [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")] public static extern IntPtr GetWindowLongPtr(IntPtr h,int i);
 [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);
 [DllImport("user32.dll")] public static extern IntPtr MonitorFromPoint(Point p,uint f);
 [DllImport("user32.dll",CharSet=CharSet.Auto)] public static extern bool GetMonitorInfo(IntPtr h,ref MONITORINFO m);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern uint RegisterWindowMessage(string s);
 [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr h,int id,uint modifiers,uint key);
 [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr h,int id);
 [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h,uint msg,IntPtr w,IntPtr l);
 [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int cx,int cy,uint f);
 [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr h,int a,out int value,int size);
 public static ABD Data(IntPtr h){return new ABD{Size=(uint)Marshal.SizeOf(typeof(ABD)),Window=h};}
 public static uint State(){ABD d=Data(FindWindow("Shell_TrayWnd",null));return (uint)SHAppBarMessage(4,ref d).ToUInt64();}
 public static void SetState(uint s){ABD d=Data(FindWindow("Shell_TrayWnd",null));d.Param=new IntPtr(s);SHAppBarMessage(10,ref d);}
 public static string Class(IntPtr h){StringBuilder s=new StringBuilder(256);GetClassName(h,s,s.Capacity);return s.ToString();}
 public static string Title(IntPtr h){StringBuilder s=new StringBuilder(1024);GetWindowText(h,s,s.Capacity);return s.ToString();}
 public static List<IntPtr> ShellBars(){
  List<IntPtr>a=new List<IntPtr>();EnumWindows(delegate(IntPtr h,IntPtr p){
   string c=Class(h);if(c=="Shell_TrayWnd"||c=="Shell_SecondaryTrayWnd")a.Add(h);return true;
  },IntPtr.Zero);return a;
 }
 public static Rectangle Monitor(bool work){
  MONITORINFO m=new MONITORINFO();m.Size=Marshal.SizeOf(typeof(MONITORINFO));
  if(!GetMonitorInfo(MonitorFromPoint(new Point(0,0),1),ref m))throw new Exception("Primary monitor unavailable.");
  return work?m.Work.Rectangle:m.Monitor.Rectangle;
 }
 public static void RemoveBar(IntPtr h){ABD d=Data(h);SHAppBarMessage(1,ref d);}
 public static List<IntPtr> Windows(int ownPid){
  List<IntPtr>result=new List<IntPtr>();
  EnumWindows(delegate(IntPtr h,IntPtr p){
   uint pid;GetWindowThreadProcessId(h,out pid);
   if(pid==ownPid||!IsWindowVisible(h)||String.IsNullOrWhiteSpace(Title(h)))return true;
   long ex=GetWindowLongPtr(h,-20).ToInt64();
   if((ex&0x80)!=0||((ex&0x40000)==0&&GetWindow(h,4)!=IntPtr.Zero))return true;
   string c=Class(h);if(c=="Shell_TrayWnd"||c=="Shell_SecondaryTrayWnd"||c=="Progman"||c=="WorkerW")return true;
   int cloaked;if(DwmGetWindowAttribute(h,14,out cloaked,4)==0&&cloaked!=0)return true;
   result.Add(h);return true;
  },IntPtr.Zero);return result;
 }
 [DllImport("user32.dll",EntryPoint="GetClassLongPtrW")] static extern IntPtr GetClassLongPtr(IntPtr h,int index);
[DllImport("user32.dll",CharSet=CharSet.Auto)] static extern IntPtr SendMessageTimeout(IntPtr h,uint m,IntPtr w,IntPtr l,uint flags,uint timeout,out IntPtr result);
public static Bitmap WindowIcon(IntPtr h) {
 try{
  IntPtr icon;SendMessageTimeout(h,0x7F,new IntPtr(2),IntPtr.Zero,2,80,out icon);
  if(icon==IntPtr.Zero)SendMessageTimeout(h,0x7F,IntPtr.Zero,IntPtr.Zero,2,80,out icon);
  if(icon==IntPtr.Zero)icon=GetClassLongPtr(h,-14);
  if(icon!=IntPtr.Zero)return Icon.FromHandle(icon).ToBitmap();
  uint pid;GetWindowThreadProcessId(h,out pid);
  using(Process p=Process.GetProcessById((int)pid))using(Icon i=Icon.ExtractAssociatedIcon(p.MainModule.FileName))if(i!=null)return i.ToBitmap();
 }catch{}
 return SystemIcons.Application.ToBitmap();
}
public static void Activate(IntPtr h){if(!IsWindow(h))return;if(IsIconic(h))ShowWindow(h,9);SetForegroundWindow(h);}
}
sealed class RecoveryRecord {
 public string Token;public uint State;public long Bar;
 public Dictionary<long,bool> Visible=new Dictionary<long,bool>();
 public static readonly string Folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"TaskbarCompass");
 public static readonly string FileName=Path.Combine(Folder,"recovery.txt");
 public void Save(){
  Directory.CreateDirectory(Folder);List<string>lines=new List<string>{Token,State.ToString(),Bar.ToString()};
  lines.AddRange(Visible.Select(x=>x.Key+":"+(x.Value?"1":"0")));File.WriteAllLines(FileName,lines.ToArray());
 }
 public static RecoveryRecord Load(){
  string[]s=File.ReadAllLines(FileName);
  RecoveryRecord r=new RecoveryRecord{Token=s[0],State=UInt32.Parse(s[1]),Bar=Int64.Parse(s[2])};
  foreach(string line in s.Skip(3)){string[]bits=line.Split(':');r.Visible.Add(Int64.Parse(bits[0]),bits[1]=="1");}return r;
 }
 public void Restore(){
  Native.RemoveBar(new IntPtr(Bar));Native.SetState(State);
  foreach(IntPtr h in Native.ShellBars()){bool visible;if(!Visible.TryGetValue(h.ToInt64(),out visible))visible=true;Native.ShowWindow(h,visible?4:0);}
 }
 public static string Event(string token,string suffix){return @"Local\TaskbarCompass."+token+"."+suffix;}
 public void Clear(){try{if(File.Exists(FileName)&&Load().Token==Token)File.Delete(FileName);}catch{}}
}
sealed class Session:IDisposable {
 public RecoveryRecord Record;EventWaitHandle ready,beat,stop,done;Process watcher;bool disposed;
 public void Begin(IntPtr bar){
  if(Native.FindWindow("Shell_TrayWnd",null)==IntPtr.Zero)throw new Exception("Windows taskbar not found. Nothing was hidden.");
  if(File.Exists(RecoveryRecord.FileName)){RecoveryRecord old=RecoveryRecord.Load();old.Restore();old.Clear();}
  Record=new RecoveryRecord{Token=Guid.NewGuid().ToString("N"),State=Native.State(),Bar=bar.ToInt64()};
  foreach(IntPtr h in Native.ShellBars())Record.Visible[h.ToInt64()]=Native.IsWindowVisible(h);
  Record.Save();
  ready=new EventWaitHandle(false,EventResetMode.ManualReset,RecoveryRecord.Event(Record.Token,"ready"));
  beat=new EventWaitHandle(false,EventResetMode.AutoReset,RecoveryRecord.Event(Record.Token,"beat"));
  stop=new EventWaitHandle(false,EventResetMode.ManualReset,RecoveryRecord.Event(Record.Token,"stop"));
  done=new EventWaitHandle(false,EventResetMode.ManualReset,RecoveryRecord.Event(Record.Token,"done"));
  watcher=Process.Start(new ProcessStartInfo(Application.ExecutablePath,"--watchdog "+Process.GetCurrentProcess().Id+" "+Record.Token){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden});
  if(!ready.WaitOne(6000))throw new Exception("Recovery watchdog did not become ready. Nothing was hidden.");
  beat.Set();Native.SetState(Record.State|1);HideNative();
 }
 public bool Tick(){if(disposed||watcher==null||watcher.HasExited||stop.WaitOne(0))return false;beat.Set();HideNative();return true;}
 void HideNative(){foreach(IntPtr h in Native.ShellBars())if(Native.IsWindowVisible(h))Native.ShowWindow(h,0);}
 public void Dispose(){
  if(disposed)return;disposed=true;
  if(Record!=null){
   if(done!=null)done.Set();
   bool restored=watcher!=null&&watcher.WaitForExit(4000)&&watcher.ExitCode==0;
   // A successful process exit alone is not proof that recovery completed.
   bool pending=false;
   try{pending=File.Exists(RecoveryRecord.FileName)&&RecoveryRecord.Load().Token==Record.Token;}catch{pending=true;}
   if(!restored||pending){Record.Restore();Record.Clear();}
  }
  foreach(EventWaitHandle e in new[]{ready,beat,stop,done})if(e!=null)e.Dispose();if(watcher!=null)watcher.Dispose();
 }
 public static int Watch(int parentId,string token){
  RecoveryRecord r=RecoveryRecord.Load();if(r.Token!=token)return 2;
  using(EventWaitHandle ready=EventWaitHandle.OpenExisting(RecoveryRecord.Event(token,"ready")))
  using(EventWaitHandle beat=EventWaitHandle.OpenExisting(RecoveryRecord.Event(token,"beat")))
  using(EventWaitHandle stop=EventWaitHandle.OpenExisting(RecoveryRecord.Event(token,"stop")))
  using(EventWaitHandle done=EventWaitHandle.OpenExisting(RecoveryRecord.Event(token,"done"))){
   Process parent=null;try{parent=Process.GetProcessById(parentId);}catch{}
   ready.Set();Stopwatch silence=Stopwatch.StartNew();
   try{while(parent!=null&&!parent.HasExited&&!done.WaitOne(200)){
    if(beat.WaitOne(0))silence.Restart();
   }}finally{stop.Set();r.Restore();Thread.Sleep(350);r.Restore();r.Clear();if(parent!=null)parent.Dispose();}
  }return 0;
 }
 public static void EmergencyRestore(){
  if(!File.Exists(RecoveryRecord.FileName)){MessageBox.Show("No active replacement session needs restoration.","Taskbar Compass");return;}
  RecoveryRecord r=RecoveryRecord.Load();
  try{using(EventWaitHandle e=EventWaitHandle.OpenExisting(RecoveryRecord.Event(r.Token,"stop")))e.Set();}catch(WaitHandleCannotBeOpenedException){}
  r.Restore();r.Clear();
 }
}
class WindowButton:Button {
 public IntPtr Target;public Bitmap AppIcon;public bool Active,Vertical;
 public WindowButton(IntPtr target){
  Target=target;AppIcon=Native.WindowIcon(target);FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Margin=new Padding(3);TabStop=true;
 }
 protected override void OnPaint(PaintEventArgs e){
  Graphics g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
  g.Clear(Color.FromArgb(28,30,36));
  int side=(int)(Math.Min(Width,Height)*0.56);
  if(AppIcon!=null)g.DrawImage(AppIcon,new Rectangle((Width-side)/2,(Height-side)/2,side,side));
  if(Target!=IntPtr.Zero)using(Pen p=new Pen(Active?Color.FromArgb(104,194,250):Color.FromArgb(145,149,157),Active?3:2)){
   int len=Active?Math.Max(12,Width/3):Math.Max(5,Width/9);
   if(Vertical)g.DrawLine(p,2,(Height-len)/2,2,(Height+len)/2);
   else g.DrawLine(p,(Width-len)/2,Height-3,(Width+len)/2,Height-3);
  }
  if(Focused)ControlPaint.DrawFocusRectangle(g,new Rectangle(2,2,Width-4,Height-4),Color.LightGray,BackColor);
 }
 protected override void Dispose(bool d){if(d&&AppIcon!=null)AppIcon.Dispose();base.Dispose(d);}
}
sealed class GroupButton:WindowButton {
 string iconPath;
 public GroupButton():base(IntPtr.Zero){}
 public void SetApp(PinnedApp app){string next=app==null?"":app.Path;if(iconPath==next)return;iconPath=next;if(AppIcon!=null)AppIcon.Dispose();AppIcon=app==null?SystemIcons.Application.ToBitmap():app.GetIcon();Invalidate();}
 protected override void OnPaint(PaintEventArgs e){
  base.OnPaint(e);float s=e.Graphics.DpiX/96f;float x=Width/2f+5*s,y=Height/2f+3*s;
  using(SolidBrush fill=new SolidBrush(Color.FromArgb(22,26,33)))e.Graphics.FillEllipse(fill,x-2*s,y-2*s,15*s,15*s);
  using(Pen pen=new Pen(Color.FromArgb(112,196,248),Math.Max(1,1.3f*s))){e.Graphics.DrawRectangle(pen,x+3*s,y,8*s,8*s);e.Graphics.DrawRectangle(pen,x,y+3*s,8*s,8*s);}
 }
}
static class WindowsQuickSettings {
 [StructLayout(LayoutKind.Sequential)]public struct Keyboard {public ushort Key,Scan;public uint Flags,Time;public UIntPtr Extra;}
 [StructLayout(LayoutKind.Explicit,Size=32)]public struct InputData {[FieldOffset(0)]public Keyboard Keyboard;}
 [StructLayout(LayoutKind.Sequential)]public struct Input {public uint Type;public InputData Data;}
 [DllImport("user32.dll",SetLastError=true)]static extern uint SendInput(uint count,Input[] input,int size);
 static Input Key(ushort key,bool up){return new Input{Type=1,Data=new InputData{Keyboard=new Keyboard{Key=key,Flags=up?2u:0u}}};}
 public static Input[] Shortcut(){return new[]{Key(0x5B,false),Key(0x41,false),Key(0x41,true),Key(0x5B,true)};}
 public static void Open(){Input[] keys=Shortcut();if(SendInput((uint)keys.Length,keys,Marshal.SizeOf(typeof(Input)))!=keys.Length){Input[] release={Key(0x41,true),Key(0x5B,true)};SendInput(2,release,Marshal.SizeOf(typeof(Input)));throw new Exception("Windows couldn't open quick settings. You can also press Windows + A.");}}
}
sealed class QuickSettingsButton:Button {
 protected override void OnPaint(PaintEventArgs e){
  base.OnPaint(e);Graphics g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;float scale=Math.Min(Width/64f,Height/36f);g.TranslateTransform((Width-64*scale)/2,(Height-36*scale)/2);g.ScaleTransform(scale,scale);
  using(Pen pen=new Pen(ForeColor,1.8f))using(SolidBrush ink=new SolidBrush(ForeColor)){
   g.DrawArc(pen,7,8,24,24,220,100);g.DrawArc(pen,11,12,16,16,220,100);g.DrawArc(pen,15,16,8,8,220,100);g.FillEllipse(ink,17.5f,22,3,3);
   g.DrawPolygon(pen,new[]{new PointF(37,15),new PointF(42,15),new PointF(47,10),new PointF(47,26),new PointF(42,21),new PointF(37,21)});
   g.DrawArc(pen,46,12,10,12,-65,130);g.DrawArc(pen,46,8,17,20,-65,130);
  }
 }
}
sealed class LauncherButton:Button {
 protected override void OnPaint(PaintEventArgs e){
  base.OnPaint(e);int size=(int)(Math.Min(Width,Height)*0.44),gap=Math.Max(2,size/10),part=(size-gap)/2;
  int x=(Width-size)/2,y=(Height-size)/2;
  using(SolidBrush b=new SolidBrush(Color.FromArgb(104,194,250))){
   e.Graphics.FillRectangle(b,x,y,part,part);e.Graphics.FillRectangle(b,x+part+gap,y,part,part);
   e.Graphics.FillRectangle(b,x,y+part+gap,part,part);e.Graphics.FillRectangle(b,x+part+gap,y+part+gap,part,part);
  }
 }
}sealed class BatteryMeter:Control {
 [StructLayout(LayoutKind.Sequential)]struct POWER {public byte AC,Flags,Percent,SystemFlag;public uint Life,FullLife;}
 [DllImport("kernel32.dll")]static extern bool GetSystemPowerStatus(out POWER p);
 POWER power;bool available;public bool Vertical=true;
 public BatteryMeter(){DoubleBuffered=true;Font=new Font("Segoe UI",8.5f);AccessibleName="Laptop battery";BackColor=Color.FromArgb(28,30,36);ForeColor=Color.FromArgb(235,239,245);}
 string ChargeText{get{return !available?"Unknown":power.Flags==128?"No battery":power.Percent>100?"Unknown":power.Percent+"%";}}
 string StatusText{get{return !available?"Unavailable":power.Flags==128?(power.AC==1?"Plugged in":"No battery"):power.Flags!=255&&(power.Flags&8)!=0?"Charging":power.AC==1?"Plugged in":power.AC==0?"On battery":"Power unknown";}}
 public void UpdateReading(){available=GetSystemPowerStatus(out power);AccessibleDescription="Battery "+ChargeText+", "+StatusText;Text=AccessibleDescription;Invalidate();}
 public static void ValidateReadings(){
 using(BatteryMeter b=new BatteryMeter()){
  b.available=true;b.power=new POWER{AC=1,Flags=8,Percent=42};
  if(b.ChargeText!="42%"||b.StatusText!="Charging")throw new Exception("Charging battery status is wrong.");
  b.power=new POWER{AC=0,Flags=0,Percent=17};
  if(b.ChargeText!="17%"||b.StatusText!="On battery")throw new Exception("Discharging battery status is wrong.");
  b.power=new POWER{AC=1,Flags=128,Percent=255};
  if(b.ChargeText!="No battery")throw new Exception("Absent battery status is wrong.");
  b.power=new POWER{AC=255,Flags=255,Percent=255};
  if(b.ChargeText!="Unknown"||b.StatusText!="Power unknown")throw new Exception("Unknown battery status is wrong.");
 }
}
public static string ReadText(){using(BatteryMeter b=new BatteryMeter()){b.UpdateReading();return b.Text;}}
 protected override void OnPaint(PaintEventArgs e){
  base.OnPaint(e);Graphics g=e.Graphics;float s=g.DpiX/96f;int w=(int)(Vertical?38*s:29*s),h=(int)(11*s);
  int x=Vertical?(Width-w)/2:(int)(5*s),y=(int)(Vertical?3*s:11*s);
  using(Pen border=new Pen(Color.FromArgb(184,192,199),Math.Max(1,s))){
   g.DrawRectangle(border,x,y,w,h);g.DrawLine(border,x+w+2*s,y+3*s,x+w+2*s,y+h-3*s);
  }
  bool known=available&&(power.Flags&128)==0&&power.Percent<=100;
  if(known)using(SolidBrush fill=new SolidBrush(power.Percent<=20?Color.FromArgb(242,186,83):Color.FromArgb(127,209,171)))
   g.FillRectangle(fill,x+2*s,y+2*s,Math.Max(0,(w-4*s)*power.Percent/100f),h-4*s);
  using(SolidBrush text=new SolidBrush(ForeColor))using(StringFormat center=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center}){
   RectangleF charge=Vertical?new RectangleF(0,17*s,Width,17*s):new RectangleF(37*s,3*s,Width-37*s,25*s);
   RectangleF status=Vertical?new RectangleF(0,35*s,Width,20*s):new RectangleF(0,28*s,Width,20*s);
   g.DrawString(ChargeText,Font,text,charge,center);g.DrawString(StatusText,Font,text,status,center);
  }
 }
}
sealed class BertReaction:Form {
 readonly System.Windows.Forms.Timer expiry=new System.Windows.Forms.Timer{Interval=6000};
 public BertReaction(string message,string emojis){FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;StartPosition=FormStartPosition.Manual;BackColor=Color.FromArgb(35,43,55);ForeColor=Color.FromArgb(255,236,171);ClientSize=new Size(290,90);Font=new Font("Segoe UI",11);Controls.Add(new Label{Text=emojis+"  "+message,Dock=DockStyle.Fill,Padding=new Padding(12),TextAlign=ContentAlignment.MiddleLeft});AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;expiry.Tick+=delegate{Close();};Shown+=delegate{expiry.Start();};}
 protected override bool ShowWithoutActivation{get{return true;}}
 protected override CreateParams CreateParams{get{CreateParams p=base.CreateParams;p.ExStyle|=0x08000000|0x80;return p;}}
 protected override void Dispose(bool disposing){if(disposing)expiry.Dispose();base.Dispose(disposing);}
 public void ShowBeside(Control anchor,Edge edge){
  Rectangle a=anchor.RectangleToScreen(anchor.ClientRectangle),work=Screen.FromControl(anchor).WorkingArea;int gap=8;
  int x=edge==Edge.Left?a.Right+gap:edge==Edge.Right?a.Left-Width-gap:a.Right-Width;
  int y=edge==Edge.Top?a.Bottom+gap:edge==Edge.Bottom?a.Top-Height-gap:a.Top;
  Location=new Point(Math.Max(work.Left,Math.Min(x,work.Right-Width)),Math.Max(work.Top,Math.Min(y,work.Bottom-Height)));Show(anchor.FindForm());
 }
}
sealed class BarBackground:Form {
 public BarBackground(){FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;StartPosition=FormStartPosition.Manual;BackColor=Color.FromArgb(28,30,36);}
 protected override bool ShowWithoutActivation{get{return true;}}
 protected override CreateParams CreateParams{get{CreateParams p=base.CreateParams;p.ExStyle|=0x08000000|0x80;return p;}}
 protected override void WndProc(ref Message m){if(m.Msg==0x21){m.Result=new IntPtr(3);return;}base.WndProc(ref m);}
}
sealed class Bar:Form {
 IntPtr moveHook;Native.WindowEvent moveCallback;
 public static Rectangle AvoidTaskbar(Rectangle window,Rectangle bar,Rectangle work,Edge edge){
  if(!window.IntersectsWith(bar))return window;
  Rectangle safe=work;
  if(edge==Edge.Left)safe=Rectangle.FromLTRB(Math.Max(work.Left,bar.Right),work.Top,work.Right,work.Bottom);
  if(edge==Edge.Right)safe=Rectangle.FromLTRB(work.Left,work.Top,Math.Min(work.Right,bar.Left),work.Bottom);
  if(edge==Edge.Top)safe=Rectangle.FromLTRB(work.Left,Math.Max(work.Top,bar.Bottom),work.Right,work.Bottom);
  if(edge==Edge.Bottom)safe=Rectangle.FromLTRB(work.Left,work.Top,work.Right,Math.Min(work.Bottom,bar.Top));
  if(safe.Width<=0||safe.Height<=0)return window;
  int width=Math.Min(window.Width,safe.Width),height=Math.Min(window.Height,safe.Height);
  return new Rectangle(Math.Max(safe.Left,Math.Min(window.Left,safe.Right-width)),Math.Max(safe.Top,Math.Min(window.Top,safe.Bottom-height)),width,height);
 }
 void KeepDraggedWindowClear(IntPtr window){
  if(closing||!ready||window==IntPtr.Zero||Native.IsIconic(window)||Native.IsZoomed(window))return;
  if(Native.MonitorFromWindow(window,2)!=Native.MonitorFromPoint(new Point(0,0),1))return;
  if(!Native.Windows(Process.GetCurrentProcess().Id).Contains(window))return;
  Native.RECT rect;if(!Native.GetWindowRect(window,out rect))return;
  Rectangle next=AvoidTaskbar(rect.Rectangle,Bounds,Native.Monitor(true),edge);
  if(next!=rect.Rectangle)Native.SetWindowPos(window,IntPtr.Zero,next.X,next.Y,next.Width,next.Height,0x14);
 }
 readonly BarBackground background=new BarBackground();bool syncingBackground;
 void SyncBackground(){
  if(closing||!registered||syncingBackground)return;syncingBackground=true;
  try{background.BackColor=Preferences.BackgroundTint;background.Opacity=Preferences.OpacityPercent/100.0;background.Bounds=Bounds;background.TopMost=TopMost;
   if(!background.Visible)background.Show();Native.SetWindowPos(background.Handle,Handle,Left,Top,Width,Height,0x10);
  }finally{syncingBackground=false;}
 }
 readonly Session session=new Session();
 readonly FlowLayoutPanel windowList=new FlowLayoutPanel();
 readonly Panel commandPanel=new Panel(),endPanel=new Panel();
 readonly Button apps=new LauncherButton(),restore=new Button();
 readonly Button quickSettings=new QuickSettingsButton();
 readonly Button coinCounter=new Button();
 readonly Label clock=new Label();readonly BatteryMeter battery=new BatteryMeter();ContextMenuStrip activeMenu;
 readonly System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer();
 readonly CompactTip tips=new CompactTip();
 readonly Dictionary<IntPtr,WindowButton> windowButtons=new Dictionary<IntPtr,WindowButton>();
 readonly Dictionary<PinnedApp,WindowButton> pinButtons=new Dictionary<PinnedApp,WindowButton>();
 readonly Dictionary<AppGroup,GroupButton> groupButtons=new Dictionary<AppGroup,GroupButton>();
 readonly uint callback=Native.RegisterWindowMessage("TaskbarCompass.Appbar.Callback"),shellRestart=Native.RegisterWindowMessage("TaskbarCreated");
 int refreshCount; Edge edge=Edge.Left;bool registered,placing,closing,ready;float dpiScale=1;IntPtr lastActive;
 readonly PetWallet pets=new PetWallet();readonly Stopwatch petTime=new Stopwatch();double petSaveSeconds;string petSaveError;DateTime bertFedUntil=DateTime.MinValue;
 BertReaction bertReaction;
 public string Failure;
 public bool Ready{get{return ready;}}public Edge CurrentEdge{get{return edge;}}
 Button ButtonAt(Point screen){
  foreach(Button button in new[]{apps,restore,quickSettings,coinCounter})if(button.Visible&&button.Enabled&&button.RectangleToScreen(button.ClientRectangle).Contains(screen))return button;
  if(battery.Visible&&battery.RectangleToScreen(battery.ClientRectangle).Contains(screen))return quickSettings;
  if(!windowList.RectangleToScreen(windowList.ClientRectangle).Contains(screen))return null;
  return windowList.Controls.OfType<WindowButton>().FirstOrDefault(b=>b.Visible&&b.Enabled&&b.RectangleToScreen(b.ClientRectangle).Contains(screen));
 }
 public Bar(){Icon=AppIdentity.Icon;Preferences.Load();Opacity=1;
  pets.Load();
  Button pressed=null;
  background.MouseDown+=delegate(object sender,MouseEventArgs e){pressed=ButtonAt(background.PointToScreen(e.Location));};
  background.MouseUp+=delegate(object sender,MouseEventArgs e){
   Point screen=background.PointToScreen(e.Location);Button b=ButtonAt(screen);bool clicked=b!=null&&b==pressed;pressed=null;
   if(clicked){if(e.Button==MouseButtons.Left)b.PerformClick();else if(e.Button==MouseButtons.Right&&b.ContextMenuStrip!=null)b.ContextMenuStrip.Show(screen);}
  };
  background.MouseMove+=delegate(object sender,MouseEventArgs e){Button b=ButtonAt(background.PointToScreen(e.Location));string tip=b==null?"":tips.GetToolTip(b);if(tips.GetToolTip(background)!=tip)tips.SetToolTip(background,tip);};
  Text="Taskbar Compass - Replacement";FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=true;TopMost=true;
  AutoScaleMode=AutoScaleMode.None;BackColor=Color.FromArgb(28,30,36);ForeColor=Color.FromArgb(234,242,248);
  TransparencyKey=BackColor;
  Font=new Font("Segoe UI",10);StartPosition=FormStartPosition.Manual;
  foreach(Button b in new[]{apps,restore,quickSettings,coinCounter}){
   b.FlatStyle=FlatStyle.Flat;b.FlatAppearance.BorderSize=0;b.BackColor=Color.FromArgb(32,35,42);b.ForeColor=ForeColor;b.TabStop=true;
  }
  apps.Text="";apps.AccessibleName="Apps and taskbar settings";
  restore.Text="Restore";restore.AccessibleName="Restore Windows taskbar and exit";restore.BackColor=Color.FromArgb(43,48,56);
  apps.Click+=delegate{LaunchMenu();};restore.Click+=delegate{Close();};
  quickSettings.AccessibleName="Open Windows quick settings - Wi-Fi, sound and battery";
  quickSettings.Click+=delegate{try{WindowsQuickSettings.Open();}catch(Exception ex){MessageBox.Show(this,ex.Message,"Quick settings");}};
  battery.Cursor=Cursors.Hand;battery.Click+=delegate{quickSettings.PerformClick();};
  tips.SetToolTip(quickSettings,"Open Windows quick settings (Windows + A)");tips.SetToolTip(battery,"Battery status - click for Windows quick settings");
  endPanel.Controls.Add(quickSettings);
  coinCounter.ForeColor=Color.FromArgb(255,217,110);coinCounter.Font=new Font("Segoe UI Semibold",8.5f);coinCounter.AutoEllipsis=true;coinCounter.Cursor=Cursors.Hand;coinCounter.Click+=delegate{OpenPetShop();};endPanel.Controls.Add(coinCounter);
  commandPanel.Controls.Add(apps);endPanel.Controls.Add(restore);endPanel.Controls.Add(clock);endPanel.Controls.Add(battery);
  clock.TextAlign=ContentAlignment.MiddleCenter;UpdateClock();
  tips.SetToolTip(clock,pets.Current.Name+" - your taskbar pet");
  windowList.AutoScroll=true;windowList.WrapContents=false;windowList.BackColor=BackColor;
  Controls.Add(windowList);Controls.Add(commandPanel);Controls.Add(endPanel);
  tips.SetToolTip(restore,"Restore Windows taskbar and its previous auto-hide setting, then exit.");
  tips.SetToolTip(apps,"Search, launch apps, and adjust taskbar settings.");
  timer.Interval=150;timer.Tick+=delegate{if(!session.Tick()){Close();return;}EarnPetCoins();if(++refreshCount%5==0){UpdateWindows();battery.UpdateReading();}UpdateClock();};
  Shown+=delegate{Initialize();};FormClosing+=delegate{Cleanup();};
 }
 void UpdateClock(){
  coinCounter.Text=CoinText(pets.Coins)+" coins";coinCounter.AccessibleName=pets.Coins.ToString("0.00")+" coins - open Pet Shop";
  string coinTip=petSaveError??("Your coins: "+pets.Coins.ToString("0.00")+". Click to open Pet Shop.");if(tips.GetToolTip(coinCounter)!=coinTip)tips.SetToolTip(coinCounter,coinTip);
  DateTime now=DateTime.Now;
  clock.ForeColor=(now.Hour>=22||now.Hour<6)?Color.FromArgb(255,45,45):Color.FromArgb(80,195,255);
  clock.Text=(now<bertFedUntil?pets.Current.Emoji+" "+pets.Current.Food:"\u23F0 "+pets.Current.Emoji)+(edge==Edge.Left||edge==Edge.Right?"\n":" ")+now.ToString("h:mmtt",System.Globalization.CultureInfo.InvariantCulture).ToLowerInvariant();
 }
 public static string CoinText(decimal value){return value>=1000000m?(Decimal.Floor(value/100000m)/10m).ToString("0.#")+"m":value>=1000m?(Decimal.Floor(value/100m)/10m).ToString("0.#")+"k":(Decimal.Floor(value*10m)/10m).ToString("0.0");}
 void OpenPetShop(){using(PetShop shop=new PetShop(pets,delegate{bertFedUntil=DateTime.MinValue;if(bertReaction!=null)bertReaction.Dispose();tips.SetToolTip(clock,pets.Current.Name+" - your taskbar pet");UpdateClock();}))shop.ShowDialog(this);UpdateClock();}
 void EarnPetCoins(){double seconds=petTime.Elapsed.TotalSeconds;petTime.Restart();pets.Earn(seconds,IdleTime.IsIdle());petSaveSeconds+=Math.Min(seconds,5);if(petSaveSeconds>=5){SavePetProgress();}}
 void FeedBert(){
  try{pets.Feed();}catch(Exception ex){MessageBox.Show(this,"Couldn't save the snack count.\n"+ex.Message,"Your pet");return;}
  bertFedUntil=DateTime.Now.AddSeconds(6);UpdateClock();Pet pet=pets.Current;
  string message=pet.Name+": "+pet.Reaction+"\nSnacks enjoyed: "+pets.Snacks[pets.Equipped];
  BeginInvoke((Action)delegate{if(closing)return;if(bertReaction!=null)bertReaction.Dispose();bertReaction=new BertReaction(message,pet.Emoji+" "+pet.Food);bertReaction.ShowBeside(clock,edge);});
 }
 void SavePetProgress(){try{pets.Save();petSaveSeconds=0;petSaveError=null;}catch(Exception ex){petSaveSeconds=0;petSaveError="Coin saving failed: "+ex.Message;tips.SetToolTip(coinCounter,petSaveError);}}
 void SaveForSessionMessage(int message,IntPtr ending){if(ready&&(message==0x11||(message==0x16&&ending!=IntPtr.Zero)))SavePetProgress();}
 protected override bool ShowWithoutActivation{get{return true;}}
 void Initialize(){
  try{
   using(Graphics g=CreateGraphics())dpiScale=g.DpiX/96f;
   session.Begin(Handle);Native.ABD d=Native.Data(Handle);d.Callback=callback;
   if(Native.SHAppBarMessage(0,ref d)==UIntPtr.Zero)throw new Exception("Windows could not register the replacement taskbar.");
   registered=true;
   string config=Path.Combine(RecoveryRecord.Folder,"edge.txt");Edge saved;
   if(File.Exists(config)&&Enum.TryParse<Edge>(File.ReadAllText(config),out saved)&&Enum.IsDefined(typeof(Edge),saved))edge=saved;
   DockEdge(edge);UpdateWindows();battery.UpdateReading();UpdateClock();
   if(!Native.RegisterHotKey(Handle,1,0x4000|1|2|4,0x52))tips.SetToolTip(restore,"Use this button to restore. Ctrl+Alt+Shift+R is already in use.");
   moveCallback=delegate(IntPtr hook,uint kind,IntPtr window,int obj,int child,uint thread,uint time){if(!closing&&ready)BeginInvoke((Action)delegate{KeepDraggedWindowClear(window);});};
   moveHook=Native.SetWinEventHook(0xB,0xB,IntPtr.Zero,moveCallback,0,0,2);
   if(moveHook==IntPtr.Zero)throw new Exception("Windows could not enable taskbar edge protection.");
   petTime.Restart();timer.Start();ready=true;
  }catch(Exception ex){Failure=ex.Message;Cleanup();MessageBox.Show("The replacement could not start. Windows taskbar restoration was attempted.\n\n"+ex.Message,"Taskbar Compass");Close();}
 }
 public void DockEdge(Edge next){
  if(!registered||placing||closing)return;placing=true;
  try{
   if(edge!=next){
 Native.RemoveBar(Handle);
 Native.ABD registration=Native.Data(Handle);registration.Callback=callback;
 if(Native.SHAppBarMessage(0,ref registration)==UIntPtr.Zero)throw new Exception("Could not register the new taskbar edge.");
}
edge=next;Rectangle bounds=Native.Monitor(false);bool vertical=edge==Edge.Left||edge==Edge.Right;
   int thickness=(int)Math.Round((vertical?74:54)*dpiScale);
   Native.ABD d=Native.Data(Handle);d.Edge=(uint)edge;d.Rect=new Native.RECT(bounds);
   if(edge==Edge.Left)d.Rect.R=d.Rect.L+thickness;if(edge==Edge.Right)d.Rect.L=d.Rect.R-thickness;
   if(edge==Edge.Top)d.Rect.B=d.Rect.T+thickness;if(edge==Edge.Bottom)d.Rect.T=d.Rect.B-thickness;
   Native.SHAppBarMessage(2,ref d);
   if(edge==Edge.Left)d.Rect.R=d.Rect.L+thickness;if(edge==Edge.Right)d.Rect.L=d.Rect.R-thickness;
   if(edge==Edge.Top)d.Rect.B=d.Rect.T+thickness;if(edge==Edge.Bottom)d.Rect.T=d.Rect.B-thickness;
   Native.SHAppBarMessage(3,ref d);Bounds=d.Rect.Rectangle;LayoutBar(vertical);
   try{File.WriteAllText(Path.Combine(RecoveryRecord.Folder,"edge.txt"),edge.ToString());}catch{}
  }finally{placing=false;}
 }
 int D(int v){return (int)Math.Round(v*dpiScale);}
 void LayoutBar(bool vertical){
 SuspendLayout();
 if(vertical){
  commandPanel.SetBounds(0,0,Width,D(56));endPanel.SetBounds(0,Height-D(226),Width,D(226));
  apps.SetBounds(D(12),D(6),D(50),D(44));
  clock.SetBounds(0,0,Width,D(38));coinCounter.SetBounds(D(3),D(40),Width-D(6),D(26));quickSettings.SetBounds(D(5),D(70),Width-D(10),D(36));battery.SetBounds(D(2),D(109),Width-D(4),D(60));battery.Vertical=true;restore.SetBounds(D(7),D(183),Width-D(14),D(32));
  windowList.SetBounds(D(8),D(56),Width-D(8),Math.Max(D(50),Height-D(287)));windowList.FlowDirection=FlowDirection.TopDown;
 }else{
  commandPanel.SetBounds(0,0,D(60),Height);endPanel.SetBounds(Width-D(475),0,D(475),Height);
  apps.SetBounds(D(6),D(4),D(48),D(44));
  restore.SetBounds(D(8),D(10),D(70),D(34));quickSettings.SetBounds(D(84),D(9),D(64),D(36));battery.SetBounds(D(154),D(2),D(94),D(50));battery.Vertical=false;coinCounter.SetBounds(D(252),D(10),D(88),D(34));clock.SetBounds(D(344),D(6),D(129),D(42));
  windowList.SetBounds(D(64),D(2),Math.Max(D(70),Width-D(544)),Height-D(3));windowList.FlowDirection=FlowDirection.LeftToRight;
 }
 foreach(WindowButton b in windowButtons.Values.Concat(pinButtons.Values)){b.Size=new Size(D(48),D(44));b.Vertical=vertical;b.Invalidate();}
 foreach(GroupButton b in groupButtons.Values){b.Size=new Size(D(48),D(44));b.Vertical=vertical;}
 AlignApps();ResumeLayout();SyncBackground();
}public void UpdateWindows(){
  if(closing)return;List<IntPtr>handles=Native.Windows(Process.GetCurrentProcess().Id);
  IntPtr fg=Native.GetForegroundWindow();if(handles.Contains(fg))lastActive=fg;
  List<PinnedApp> visiblePins=Preferences.Pins.Where(p=>!Preferences.Grouped(p)).ToList();
  foreach(PinnedApp removed in pinButtons.Keys.Except(visiblePins).ToArray()){WindowButton b=pinButtons[removed];windowList.Controls.Remove(b);pinButtons.Remove(removed);b.Dispose();}
  Dictionary<IntPtr,string> paths=handles.ToDictionary(h=>h,h=>PinnedApp.WindowPath(h));
  int pinIndex=0;
  foreach(AppGroup removed in groupButtons.Keys.Except(Preferences.Groups).ToArray()){Button b=groupButtons[removed];windowList.Controls.Remove(b);groupButtons.Remove(removed);b.Dispose();}
  foreach(AppGroup group in Preferences.Groups){
   GroupButton b;if(!groupButtons.TryGetValue(group,out b)){
    b=new GroupButton();
    AppGroup item=group;Button owner=b;b.Click+=delegate{GroupMenu(item,owner);};
    ContextMenuStrip edit=new DarkMenu();edit.Items.Add("Edit group...",null,delegate{QueueGroupEditor(item);});b.ContextMenuStrip=edit;b.Disposed+=delegate{edit.Dispose();};
    groupButtons.Add(group,b);windowList.Controls.Add(b);
   }
   List<IntPtr> grouped=handles.Where(h=>group.Apps.Any(a=>!String.IsNullOrEmpty(a.Target)&&String.Equals(paths[h],a.Target,StringComparison.OrdinalIgnoreCase))).ToList();
   b.SetApp(group.IconApp);b.Text="";b.AccessibleName="Open group "+group.Name;b.Size=new Size(D(48),D(44));b.Vertical=edge==Edge.Left||edge==Edge.Right;
   b.Target=grouped.FirstOrDefault();b.Active=grouped.Contains(lastActive);b.Invalidate();
   tips.SetToolTip(b,group.Name+" - "+group.Apps.Count+" apps"+(group.IconApp==null?"":"; logo: "+group.IconApp.Name));windowList.Controls.SetChildIndex(b,pinIndex++);
   handles.RemoveAll(h=>grouped.Contains(h));
  }
  foreach(PinnedApp pin in visiblePins){
   WindowButton b;
   if(!pinButtons.TryGetValue(pin,out b)){
    b=new WindowButton(IntPtr.Zero);b.AppIcon.Dispose();b.AppIcon=pin.GetIcon();WindowButton button=b;PinnedApp item=pin;
    b.Click+=delegate{if(button.Target==IntPtr.Zero)Launch(item.Path);else if(Native.GetForegroundWindow()==button.Target){Native.ShowWindow(button.Target,6);lastActive=IntPtr.Zero;}else{Native.Activate(button.Target);lastActive=button.Target;}};
    ContextMenuStrip menu=new DarkMenu();menu.Items.Add("Open new instance",null,delegate{Launch(item.Path);});
    menu.Items.Add("Unpin from Taskbar Compass",null,delegate{Preferences.Pins.Remove(item);Preferences.SavePins();BeginInvoke((Action)UpdateWindows);});
    b.ContextMenuStrip=menu;b.Disposed+=delegate{menu.Dispose();};pinButtons.Add(pin,b);windowList.Controls.Add(b);
   }
   b.Target=handles.FirstOrDefault(h=>!String.IsNullOrEmpty(pin.Target)&&String.Equals(paths[h],pin.Target,StringComparison.OrdinalIgnoreCase));
   if(b.Target!=IntPtr.Zero)handles.Remove(b.Target);
   b.Active=b.Target!=IntPtr.Zero&&b.Target==lastActive&&!Native.IsIconic(b.Target);
   b.AccessibleName=(b.Target==IntPtr.Zero?"Launch ":"Switch to ")+pin.Name;
   tips.SetToolTip(b,pin.Name+(b.Target==IntPtr.Zero?" (pinned)":" - "+Native.Title(b.Target)));
   b.Size=new Size(D(48),D(44));b.Vertical=edge==Edge.Left||edge==Edge.Right;b.Invalidate();windowList.Controls.SetChildIndex(b,pinIndex++);
  }
  foreach(IntPtr h in windowButtons.Keys.Except(handles).ToArray()){
   WindowButton old=windowButtons[h];windowList.Controls.Remove(old);windowButtons.Remove(h);old.Dispose();
  }
  foreach(IntPtr h in handles){
   WindowButton b;if(!windowButtons.TryGetValue(h,out b)){
    b=new WindowButton(h);IntPtr target=h;
    b.Click+=delegate{
     if(Native.GetForegroundWindow()==target){Native.ShowWindow(target,6);lastActive=IntPtr.Zero;}
     else{Native.Activate(target);lastActive=target;}
    };
    ContextMenuStrip menu=new DarkMenu();
    menu.Items.Add("Activate window",null,delegate{Native.Activate(target);lastActive=target;});
    menu.Items.Add("Close window",null,delegate{Native.PostMessage(target,0x10,IntPtr.Zero,IntPtr.Zero);});
    string executable=PinnedApp.WindowPath(target);
    if(!String.IsNullOrEmpty(executable))menu.Items.Add("Pin to Taskbar Compass",null,delegate{Preferences.Add(executable);Preferences.SavePins();BeginInvoke((Action)UpdateWindows);});
    b.ContextMenuStrip=menu;b.Disposed+=delegate{menu.Dispose();};windowButtons.Add(h,b);windowList.Controls.Add(b);
   }
   string title=Native.Title(h);b.Text="";b.AccessibleName="Switch to "+title;tips.SetToolTip(b,title);
   b.Active=h==lastActive&&!Native.IsIconic(h);b.Invalidate();
   bool vertical=edge==Edge.Left||edge==Edge.Right;b.Size=new Size(D(48),D(44));b.Vertical=vertical;
  }
  AlignApps();
 }
 void AlignApps(){
  bool vertical=edge==Edge.Left||edge==Edge.Right;
  int used=windowList.Controls.Cast<Control>().Sum(c=>vertical?c.Height+c.Margin.Vertical:c.Width+c.Margin.Horizontal);
  int available=vertical?windowList.ClientSize.Height:windowList.ClientSize.Width;
  int gap=Preferences.AppsAtEnd?Math.Max(0,available-used):0;
  Padding padding=vertical?new Padding(0,gap,0,0):new Padding(gap,0,0,0);
  if(windowList.Padding!=padding){windowList.AutoScrollPosition=Point.Empty;windowList.Padding=padding;}
 }
 void Launch(string file){
  try{ProcessStartInfo start=file.StartsWith("shell:AppsFolder\\",StringComparison.OrdinalIgnoreCase)?new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"explorer.exe"),"\""+file.Replace("\"","")+"\""):new ProcessStartInfo(file);start.UseShellExecute=true;Process.Start(start);}catch(Exception ex){MessageBox.Show(this,"Could not open "+file+"\n"+ex.Message,"Taskbar Compass");}
 }
 void EditGroup(AppGroup group){
  if(activeMenu!=null)activeMenu.Close();
  using(GroupEditor editor=new GroupEditor(group))if(editor.ShowDialog(this)==DialogResult.OK){
   if(group!=null){editor.Result.LastSelected=group.LastSelected;foreach(PinnedApp app in group.Apps)Preferences.Add(app.Path);int index=Preferences.Groups.IndexOf(group);Preferences.Groups[index]=editor.Result;}
   else Preferences.Groups.Add(editor.Result);
   Preferences.SaveGroups();Preferences.SavePins();BeginInvoke((Action)UpdateWindows);
  }
 }
 void RememberGroupApp(AppGroup group,PinnedApp app){group.LastSelected=app.Path;Preferences.SaveGroups();GroupButton b;if(groupButtons.TryGetValue(group,out b))b.SetApp(group.IconApp);}
 void GroupMenu(AppGroup group,Control owner){
  ContextMenuStrip menu=new DarkMenu();menu.Items.Add(new ToolStripMenuItem(group.Name){Enabled=false});
  List<IntPtr> windows=Native.Windows(Process.GetCurrentProcess().Id);Dictionary<IntPtr,string> paths=windows.ToDictionary(h=>h,h=>PinnedApp.WindowPath(h));
  foreach(PinnedApp app in group.Apps){
   PinnedApp target=app;Bitmap icon=app.GetIcon();menu.Disposed+=delegate{icon.Dispose();};
   List<IntPtr> matches=windows.Where(h=>!String.IsNullOrEmpty(app.Target)&&String.Equals(paths[h],app.Target,StringComparison.OrdinalIgnoreCase)).ToList();
   ToolStripMenuItem item=new ToolStripMenuItem(app.Name,icon);menu.Items.Add(item);
   if(matches.Count==0)item.Click+=delegate{RememberGroupApp(group,target);Launch(target.Path);};
   else{foreach(IntPtr h in matches){IntPtr window=h;item.DropDownItems.Add(Native.Title(h),null,delegate{RememberGroupApp(group,target);Native.Activate(window);lastActive=window;});}item.DropDownItems.Add("Open new instance",null,delegate{RememberGroupApp(group,target);Launch(target.Path);});}
  }
  if(group.Apps.Count==0)menu.Items.Add(new ToolStripMenuItem("No apps yet - choose Edit group"){Enabled=false});
  menu.Items.Add(new ToolStripSeparator());menu.Items.Add("Edit group...",null,delegate{QueueGroupEditor(group);});
  menu.Items.Add("Ungroup apps",null,delegate{foreach(PinnedApp app in group.Apps)Preferences.Add(app.Path);Preferences.Groups.Remove(group);Preferences.SaveGroups();Preferences.SavePins();BeginInvoke((Action)UpdateWindows);});
  ShowMenu(menu,owner);
 }
 void ShowMenu(ContextMenuStrip m,Control owner){
  DarkMenu.Style(m);if(activeMenu!=null)activeMenu.Dispose();activeMenu=m;
  Size size=m.GetPreferredSize(Size.Empty);Rectangle anchor=owner.RectangleToScreen(owner.ClientRectangle);
  Point location=MenuLocation(Bounds,anchor,size,Screen.FromControl(this).WorkingArea,edge);
  m.Show(location);
 }
 public static Point MenuLocation(Rectangle bar,Rectangle anchor,Size size,Rectangle work,Edge edge){
  const int gap=4;
  int x=edge==Edge.Left?bar.Right+gap:edge==Edge.Right?bar.Left-size.Width-gap:anchor.Left;
  int y=edge==Edge.Top?bar.Bottom+gap:edge==Edge.Bottom?bar.Top-size.Height-gap:anchor.Top;
  return new Point(Math.Max(work.Left,Math.Min(x,work.Right-size.Width)),Math.Max(work.Top,Math.Min(y,work.Bottom-size.Height)));
 }
 void QueueGroupEditor(AppGroup group){if(activeMenu!=null)activeMenu.Close();BeginInvoke((Action)delegate{if(!closing)EditGroup(group);});}
 void LaunchMenu(){
  ContextMenuStrip m=new DarkMenu();
  m.Items.Add(new ToolStripMenuItem("TASKBAR COMPASS"){Enabled=false});
  m.Items.Add(SettingsMenu());
  m.Items.Add("Feed "+pets.Current.Name+" "+pets.Current.Food,null,delegate{FeedBert();});
  m.Items.Add("Pet Shop \U0001FA99",null,delegate{OpenPetShop();});
  m.Items.Add("Search apps...",null,delegate{using(SearchWindow search=new SearchWindow())if(search.ShowDialog(this)==DialogResult.OK){SearchEntry entry=search.Selected;if(entry.Window!=IntPtr.Zero){if(Native.IsWindow(entry.Window)){Native.Activate(entry.Window);lastActive=entry.Window;}else MessageBox.Show(this,"That window has closed. Search again.","Taskbar Compass");}else Launch(entry.File);}});
  m.Items.Add(new ToolStripSeparator());QuickSites.AddTo(m.Items,Launch);
  m.Items.Add("Edit quick websites...",null,delegate{using(QuickSitesEditor editor=new QuickSitesEditor())editor.ShowDialog(this);});
  m.Items.Add(new ToolStripSeparator());
  m.Items.Add("Create app group...",null,delegate{QueueGroupEditor(null);});
  m.Items.Add("Pin an app...",null,delegate{using(OpenFileDialog d=new OpenFileDialog{Title="Pin a program or shortcut",Filter="Programs and shortcuts|*.exe;*.lnk",DereferenceLinks=false})if(d.ShowDialog(this)==DialogResult.OK){Preferences.Add(d.FileName);Preferences.SavePins();UpdateWindows();}});
  m.Items.Add("Installed apps",null,delegate{Launch("shell:AppsFolder");});
  m.Items.Add("File Explorer",null,delegate{Launch("explorer.exe");});
  m.Items.Add("Notepad",null,delegate{Launch("notepad.exe");});
  m.Items.Add("Windows Settings",null,delegate{Launch("ms-settings:");});
  m.Items.Add("Choose a program...",null,delegate{
   using(OpenFileDialog d=new OpenFileDialog{Title="Launch a program or shortcut",Filter="Programs and shortcuts|*.exe;*.lnk|All files|*.*"})
    if(d.ShowDialog(this)==DialogResult.OK)Launch(d.FileName);
  });
  m.Items.Add(new ToolStripSeparator());
  m.Items.Add("About this replacement",null,delegate{
   MessageBox.Show(this,"Taskbar Compass "+Application.ProductVersion+" - a real replacement taskbar\n\n"+
    "Click a running window to activate or minimize it. Right-click for window actions. Apps launches programs. Taskbar settings in this menu moves the bar to any screen edge.\n\n"+
    "Windows reserves room for this bar on the primary monitor. Native taskbars are temporarily hidden and auto-hide is temporarily enabled. Restore & exit returns their original visibility and auto-hide setting.\n\n"+
    "A separate watchdog restores Windows if this process exits unexpectedly. Ctrl+Alt+Shift+R also restores when available. Run RestoreWindowsTaskbar.exe for emergency recovery.\n\n"+
    "Pinned apps stay visible when closed. Taskbar settings includes alignment and transparency. This version has no native notification tray, notification badges, Start-menu clone, or separate bars on other monitors. It does not replace Explorer permanently.",
    "About Taskbar Compass",MessageBoxButtons.OK,MessageBoxIcon.Information);
  });
  m.Items.Add(new ToolStripSeparator());
  m.Items.Add("Restart...",null,delegate{ConfirmPower(true);});
  m.Items.Add("Shut down...",null,delegate{ConfirmPower(false);});
  ShowMenu(m,apps);
 }
 void ConfirmPower(bool restart){
  string action=restart?"Restart":"Shut down";
  if(MessageBox.Show(this,action+" this PC now? Save your work before continuing.",action+" computer",MessageBoxButtons.YesNo,MessageBoxIcon.Question,MessageBoxDefaultButton.Button2)!=DialogResult.Yes)return;
  try{Process.Start(new ProcessStartInfo(Path.Combine(Environment.SystemDirectory,"shutdown.exe"),restart?"/r /t 0":"/s /t 0"){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden});Close();}
  catch(Exception ex){MessageBox.Show(this,"Could not "+action.ToLower()+" the PC.\n"+ex.Message,"Taskbar Compass");}
 }
 ToolStripMenuItem SettingsMenu(){
  ToolStripMenuItem m=new ToolStripMenuItem("Taskbar settings");
  m.DropDownItems.Add(new ToolStripMenuItem("TASKBAR SETTINGS"){Enabled=false});
  foreach(Edge e in Enum.GetValues(typeof(Edge))){Edge target=e;ToolStripMenuItem i=new ToolStripMenuItem(e.ToString()){Checked=e==edge};i.Click+=delegate{DockEdge(target);};m.DropDownItems.Add(i);}
  m.DropDownItems.Add(new ToolStripSeparator());ToolStripMenuItem alignment=new ToolStripMenuItem("App alignment");
  bool vertical=edge==Edge.Left||edge==Edge.Right;
  foreach(bool atEnd in new[]{false,true}){bool target=atEnd;ToolStripMenuItem i=new ToolStripMenuItem(vertical?(atEnd?"Bottom":"Top"):(atEnd?"Right":"Left")){Checked=Preferences.AppsAtEnd==atEnd};i.Click+=delegate{Preferences.SetAlignment(target);AlignApps();};alignment.DropDownItems.Add(i);}m.DropDownItems.Add(alignment);
  m.DropDownItems.Add(new ToolStripSeparator());ToolStripMenuItem transparency=new ToolStripMenuItem("Transparency");
  foreach(int value in new[]{100,85,65,50,35}){int opacity=value;ToolStripMenuItem i=new ToolStripMenuItem(value==100?"Off (solid)":(100-value)+"% transparent"){Checked=Preferences.OpacityPercent==value};i.Click+=delegate{Preferences.SetOpacity(opacity);SyncBackground();};transparency.DropDownItems.Add(i);}m.DropDownItems.Add(transparency);
  transparency.DropDownItems.Add(new ToolStripSeparator());
  ToolStripMenuItem tint=new ToolStripMenuItem("Colour tint");transparency.DropDownItems.Add(tint);
  string[] names={"Default (dark)","Blue","Purple","Pink","Red","Orange","Green","Teal"};
  Color[] colors={Preferences.DefaultTint,Color.FromArgb(35,95,210),Color.FromArgb(125,60,200),Color.FromArgb(210,65,145),Color.FromArgb(205,45,55),Color.FromArgb(225,120,35),Color.FromArgb(40,160,85),Color.FromArgb(25,155,165)};
  for(int n=0;n<names.Length;n++){Color color=colors[n];ToolStripMenuItem choice=new ToolStripMenuItem(names[n]){Checked=Preferences.BackgroundTint.ToArgb()==color.ToArgb()};choice.Click+=delegate{ApplyTint(color);};tint.DropDownItems.Add(choice);}
  tint.DropDownItems.Add(new ToolStripSeparator());
  tint.DropDownItems.Add("Custom colour...",null,delegate{using(ColorDialog picker=new ColorDialog{Color=Preferences.BackgroundTint,FullOpen=true,AnyColor=true})if(picker.ShowDialog(this)==DialogResult.OK)ApplyTint(picker.Color);});
  return m;
 }
 void ApplyTint(Color color){try{Preferences.SetTint(color);SyncBackground();}catch(Exception ex){MessageBox.Show(this,"Couldn't save the background colour.\n"+ex.Message,"Taskbar Compass");}}
 void Cleanup(){
  if(bertReaction!=null){bertReaction.Dispose();bertReaction=null;}
  if(closing)return;closing=true;timer.Stop();background.Dispose();Native.UnregisterHotKey(Handle,1);
  if(moveHook!=IntPtr.Zero){Native.UnhookWinEvent(moveHook);moveHook=IntPtr.Zero;}
  if(ready)SavePetProgress();
  if(registered){Native.RemoveBar(Handle);registered=false;}session.Dispose();
 }
 protected override void Dispose(bool d){if(d){Cleanup();timer.Dispose();tips.Dispose();if(activeMenu!=null)activeMenu.Dispose();}base.Dispose(d);}
 protected override void WndProc(ref Message m){
  SaveForSessionMessage(m.Msg,m.WParam);
  if(m.Msg==0x21){m.Result=new IntPtr(3);return;}
  if(m.Msg==0x312&&m.WParam.ToInt32()==1){Close();return;}
  if(m.Msg==shellRestart&&ready)BeginInvoke((Action)delegate{Close();});
  if(m.Msg==callback&&registered&&!closing){
   int n=m.WParam.ToInt32();if(n==1&&!placing)BeginInvoke((Action)delegate{DockEdge(edge);});
   if(n==2){TopMost=m.LParam==IntPtr.Zero;if(!TopMost)Native.SetWindowPos(Handle,new IntPtr(1),0,0,0,0,0x13);}
  }
  base.WndProc(ref m);
  if(m.Msg==0x47&&registered&&!placing&&!closing)SyncBackground();
  if(registered&&!placing&&!closing&&(m.Msg==6||m.Msg==0x47)){Native.ABD d=Native.Data(Handle);d.Param=m.WParam;Native.SHAppBarMessage(m.Msg==6?6u:9u,ref d);}
  if(m.Msg==0x7E&&ready&&!closing)BeginInvoke((Action)delegate{DockEdge(edge);});
 }
}
sealed class SetupPreview:Control {
 public Edge Edge;
 public SetupPreview(){DoubleBuffered=true;BackColor=Color.FromArgb(23,25,31);AccessibleName="Selected taskbar edge preview";}
 protected override void OnPaint(PaintEventArgs e){
  Graphics g=e.Graphics;float s=Math.Min(Width/640f,Height/210f);g.ScaleTransform(s,s);
  Rectangle screen=new Rectangle(95,10,450,180);
  using(SolidBrush b=new SolidBrush(Color.FromArgb(44,66,95)))g.FillRectangle(b,screen);
  using(Pen border=new Pen(Color.FromArgb(93,113,137),2))g.DrawRectangle(border,screen);
  Rectangle bar=screen;
  if(Edge==Edge.Left)bar.Width=24;if(Edge==Edge.Right){bar.X=screen.Right-24;bar.Width=24;}
  if(Edge==Edge.Top)bar.Height=24;if(Edge==Edge.Bottom){bar.Y=screen.Bottom-24;bar.Height=24;}
  using(SolidBrush b=new SolidBrush(Color.FromArgb(110,196,252)))g.FillRectangle(b,bar);
  for(int i=0;i<5;i++)using(SolidBrush b=new SolidBrush(Color.FromArgb(28,67,93))){
   bool vertical=Edge==Edge.Left||Edge==Edge.Right;
   g.FillRectangle(b,vertical?bar.X+7:bar.X+10+i*23,vertical?bar.Y+10+i*23:bar.Y+7,10,10);
  }
 }
}
sealed class SetupWindow:Form {
 public const string Caption="Taskbar Compass - Setup";
 public static readonly uint ShowMessage=Native.RegisterWindowMessage("TaskbarCompass.ShowSetup.v1");
 readonly SetupPreview preview=new SetupPreview();
 readonly Label status=new Label();
 readonly RadioButton[] choices=new RadioButton[4];
 Bar bar;bool shuttingDown;Edge chosen=Edge.Left;
 public SetupWindow(){Icon=AppIdentity.Icon;
  SuspendLayout();Text=Caption;ClientSize=new Size(700,550);FormBorderStyle=FormBorderStyle.FixedSingle;MaximizeBox=false;
  StartPosition=FormStartPosition.CenterScreen;BackColor=Color.FromArgb(28,30,36);ForeColor=Color.FromArgb(238,242,248);Font=new Font("Segoe UI",10);
  Controls.Add(new Label{Text="Make this your taskbar.",Location=new Point(28,25),Size=new Size(640,48),Font=new Font("Segoe UI",24)});
  Controls.Add(new Label{Text="Choose an edge, then Continue to apply your working replacement.",Location=new Point(30,82),Size=new Size(640,30)});
  string config=Path.Combine(RecoveryRecord.Folder,"edge.txt");Edge saved;
  if(File.Exists(config)&&Enum.TryParse<Edge>(File.ReadAllText(config).Trim(),out saved)&&Enum.IsDefined(typeof(Edge),saved))chosen=saved;
  for(int n=0;n<4;n++){
   Edge edge=(Edge)n;
   RadioButton c=new RadioButton{Text=edge.ToString(),Appearance=Appearance.Button,TextAlign=ContentAlignment.MiddleCenter,
    Location=new Point(30+n*162,125),Size=new Size(154,44),FlatStyle=FlatStyle.Flat,AccessibleName=edge+" taskbar edge"};
   c.CheckedChanged+=delegate{if(c.Checked){chosen=edge;preview.Edge=edge;preview.Invalidate();StyleChoices();}};
   choices[n]=c;Controls.Add(c);
  }
  preview.SetBounds(30,187,640,210);Controls.Add(preview);
  status.SetBounds(30,414,640,45);Controls.Add(status);
  Button go=new Button{Text="Continue — apply taskbar",Location=new Point(30,475),Size=new Size(365,46),FlatStyle=FlatStyle.Flat,
   BackColor=Color.FromArgb(108,195,251),ForeColor=Color.FromArgb(15,31,44),AccessibleName="Continue - apply taskbar"};
  go.Click+=delegate{Apply();};Controls.Add(go);AcceptButton=go;
  Button restore=new Button{Text="Restore / exit",Location=new Point(415,475),Size=new Size(255,46),FlatStyle=FlatStyle.Flat};
  restore.Click+=delegate{shuttingDown=true;if(bar!=null&&!bar.IsDisposed)bar.Close();Close();};Controls.Add(restore);
  choices[(int)chosen].Checked=true;RefreshStatus();
  AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;ResumeLayout(true);
 }
 void StyleChoices(){
  foreach(RadioButton c in choices)if(c!=null){c.BackColor=c.Checked?Color.FromArgb(64,97,126):Color.FromArgb(38,41,49);c.ForeColor=Color.White;}
 }
 void RefreshStatus(){status.Text=bar!=null&&!bar.IsDisposed?
  "Your replacement is active. Continue applies this edge to the running bar.":
  "Windows' taskbar stays visible until Continue. Restore brings it back.\nIncludes running app icons, a launcher, clock, and live battery meter.";}
 void Apply(){
  if(bar!=null&&!bar.IsDisposed){bar.DockEdge(chosen);Hide();return;}
  Directory.CreateDirectory(RecoveryRecord.Folder);File.WriteAllText(Path.Combine(RecoveryRecord.Folder,"edge.txt"),chosen.ToString());
  bar=new Bar();bar.FormClosed+=delegate{shuttingDown=true;Close();};
  Hide();bar.Show();
 }
 protected override void OnFormClosing(FormClosingEventArgs e){
  if(!shuttingDown&&bar!=null&&!bar.IsDisposed&&e.CloseReason==CloseReason.UserClosing){e.Cancel=true;Hide();}
  base.OnFormClosing(e);
 }
 protected override void WndProc(ref Message m){
  if(m.Msg==ShowMessage){
   if(bar!=null&&!bar.IsDisposed){chosen=bar.CurrentEdge;choices[(int)chosen].Checked=true;}
   RefreshStatus();Show();WindowState=FormWindowState.Normal;Activate();Native.SetForegroundWindow(Handle);return;
  }
  base.WndProc(ref m);
 }
}
static class Program {
 static string LogPath{get{return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"replacement-validation.txt");}}
 [STAThread]static int Main(string[]args){
  Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
  try{
   if(args.Length>0&&args[0]=="--inspect"){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"replacement-status.txt"),"Foreground="+Native.GetForegroundWindow()+"; Battery="+BatteryMeter.ReadText()+"; State="+Native.State()+"; workspace="+Native.Monitor(true)+"; bars="+String.Join(",",Native.ShellBars().Select(h=>Native.Class(h)+" visible="+Native.IsWindowVisible(h))));return 0;} if(args.Length>0&&args[0]=="--watchdog")return Session.Watch(Int32.Parse(args[1]),args[2]);
   if((args.Length>0&&args[0]=="--restore")||Path.GetFileNameWithoutExtension(Application.ExecutablePath)=="RestoreWindowsTaskbar"){Session.EmergencyRestore();return 0;}
   if(args.Length>0&&args[0]=="--validate-live")return Validate();
   bool owns;using(Mutex single=new Mutex(true,@"Local\TaskbarCompass.Replacement.v2",out owns)){
    if(!owns){
 IntPtr existing=Native.FindWindow(null,SetupWindow.Caption);
 if(existing!=IntPtr.Zero){Native.PostMessage(existing,SetupWindow.ShowMessage,IntPtr.Zero,IntPtr.Zero);return 0;}
 MessageBox.Show("The replacement is already running. Use its position menu or Restore button.","Taskbar Compass");return 2;
}
if(args.Length==0){Application.Run(new SetupWindow());return 0;}
    using(Bar b=new Bar()){
     if(args.Length>0&&args[0].StartsWith("--validation-"))ConfigureValidation(b,args[0].EndsWith("crash"));
     Application.Run(b);return b.Failure==null?0:1;
    }
   }
  }catch(Exception ex){
   try{if(File.Exists(RecoveryRecord.FileName)){RecoveryRecord r=RecoveryRecord.Load();r.Restore();r.Clear();}}catch{}
   if(args.Length>0&&args[0]=="--validate-live")File.AppendAllText(LogPath,"FAIL: "+ex+"\r\n");
   else MessageBox.Show("Taskbar Compass stopped.\n"+ex.Message,"Taskbar Compass");return 1;
  }
 }
 static void ConfigureValidation(Bar b,bool crash){
  int step=0;Form first=null,second=null;
  System.Windows.Forms.Timer t=new System.Windows.Forms.Timer{Interval=1200};
  t.Tick+=delegate{
   try{
    if(!b.Ready)return;
    if(step==0){
     if(Native.ShellBars().Any(Native.IsWindowVisible))throw new Exception("A native taskbar is still visible: "+String.Join(",",Native.ShellBars().Select(h=>Native.Class(h)+"="+Native.IsWindowVisible(h)))+"; auto-hide state="+Native.State());
     File.AppendAllText(LogPath,"PASS: native taskbars hidden during replacement.\r\n");
    }
    if(step<8 && step%2==0)b.DockEdge((Edge)(step/2)); if(step<8 && step%2==1){
     Edge e=(Edge)(step/2);Rectangle monitor=Native.Monitor(false),work=Native.Monitor(true),bar=b.Bounds;
     bool valid=e==Edge.Left?work.Left>=bar.Right:e==Edge.Right?work.Right<=bar.Left:e==Edge.Top?work.Top>=bar.Bottom:work.Bottom<=bar.Top;
     bool anchored=e==Edge.Left?Math.Abs(bar.Left-monitor.Left)<=3:e==Edge.Right?Math.Abs(bar.Right-monitor.Right)<=3:
      e==Edge.Top?Math.Abs(bar.Top-monitor.Top)<=3:Math.Abs(bar.Bottom-monitor.Bottom)<=3;
     if(!valid||!anchored)throw new Exception("Incorrect appbar bounds/work area: "+e+" "+bar+" "+work);
     File.AppendAllText(LogPath,"PASS: real "+e+" placement and reserved work area: "+bar+" / "+work+"\r\n");
    }
    if(step==8){
     first=new Form{Text="Compass validation window A",Size=new Size(420,250),StartPosition=FormStartPosition.CenterScreen};first.Show();
     second=new Form{Text="Compass validation window B",Size=new Size(420,250),StartPosition=FormStartPosition.CenterScreen};second.Show();
     Native.ShowWindow(first.Handle,6);Native.Activate(first.Handle);
    }
    if(step==9){
     if(Native.IsIconic(first.Handle))throw new Exception("Minimized window did not restore.");
     File.AppendAllText(LogPath,"PASS: minimized test window restored; foreground activation is checked separately by user-interface testing.\r\n");
     first.Close();second.Close();t.Stop();t.Dispose();
     if(crash){File.AppendAllText(LogPath,"Simulating abrupt replacement exit (watchdog must restore).\r\n");Environment.Exit(47);}
     else b.Close();
    }step++;
   }catch(Exception ex){File.AppendAllText(LogPath,"FAIL: "+ex+"\r\n");t.Stop();t.Dispose();if(first!=null)first.Close();if(second!=null)second.Close();b.Failure=ex.Message;b.Close();}
  };t.Start();
 }
 static int Validate(){
  if(File.Exists(RecoveryRecord.FileName))throw new Exception("Restore the running replacement before validation.");
  BatteryMeter.ValidateReadings();uint original=Native.State();Rectangle work=Native.Monitor(true);
  Dictionary<IntPtr,bool> visible=Native.ShellBars().ToDictionary(h=>h,h=>Native.IsWindowVisible(h));
  File.WriteAllText(LogPath,"Taskbar Compass replacement live validation\r\n"+DateTime.Now+"\r\nOriginal state: "+original+"; work area: "+work+"\r\n");
  foreach(string mode in new[]{"--validation-normal","--validation-crash"}){
   using(Process child=Process.Start(new ProcessStartInfo(Application.ExecutablePath,mode){UseShellExecute=false})){
    if(!child.WaitForExit(22000)){child.Kill();child.WaitForExit();throw new Exception("Validation child timed out.");}
    if(child.ExitCode!=(mode.EndsWith("crash")?47:0))throw new Exception("Validation child failed: "+child.ExitCode);
   }
   Stopwatch wait=Stopwatch.StartNew();while(wait.ElapsedMilliseconds<6000&&File.Exists(RecoveryRecord.FileName))Thread.Sleep(200);
   bool restored=Native.State()==original&&Native.Monitor(true)==work&&visible.All(x=>!Native.IsWindow(x.Key)||Native.IsWindowVisible(x.Key)==x.Value);
   if(!restored)throw new Exception("Native state, visibility or work area did not restore after "+mode);
   File.AppendAllText(LogPath,"PASS: native visibility, auto-hide state, and work area restored after "+mode+".\r\n");
  }
  File.AppendAllText(LogPath,"PASS: battery charging, discharging, absent, and unknown status mappings. Live reading: "+BatteryMeter.ReadText()+"\r\nPASS: live replacement validation completed.\r\n");return 0;
 }
}
}























