using System;using System.IO;using System.Reflection;using System.Drawing;using System.Windows.Forms;using System.Linq;
namespace CompassBar {static class LauncherClickTests {
 static object Field(object target,string name){return target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);}
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
 static void InvertBackground(Bar bar,Form background){
  FieldInfo syncing=typeof(Bar).GetField("syncingBackground",BindingFlags.Instance|BindingFlags.NonPublic);
  syncing.SetValue(bar,true);
  try{Native.SetWindowPos(background.Handle,IntPtr.Zero,0,0,0,0,0x13);}finally{syncing.SetValue(bar,false);}
 }
 static bool Above(Form upper,Form lower){for(IntPtr h=Native.GetWindow(upper.Handle,2);h!=IntPtr.Zero;h=Native.GetWindow(h,2))if(h==lower.Handle)return true;return false;}
 [STAThread]static int Main(){try{
  Preferences.Folder=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"click-settings");Directory.CreateDirectory(Preferences.Folder);File.WriteAllText(Path.Combine(Preferences.Folder,"pins.txt"),"");
  GroupIconTests.Run();
  using(Bar bar=new Bar()){
   bar.Location=new Point(-30000,-30000);bar.Show();
   Button launcher=(Button)Field(bar,"apps"),restore=(Button)Field(bar,"restore"),quick=(Button)Field(bar,"quickSettings");Control battery=(Control)Field(bar,"battery"),clock=(Control)Field(bar,"clock");Form background=(Form)Field(bar,"background");
   MethodInfo hit=typeof(Bar).GetMethod("ButtonAt",BindingFlags.Instance|BindingFlags.NonPublic);
   MethodInfo down=typeof(Control).GetMethod("OnMouseDown",BindingFlags.Instance|BindingFlags.NonPublic),up=typeof(Control).GetMethod("OnMouseUp",BindingFlags.Instance|BindingFlags.NonPublic);
   foreach(Edge edge in Enum.GetValues(typeof(Edge))){
    typeof(Bar).GetField("edge",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(bar,edge);
    bool vertical=edge==Edge.Left||edge==Edge.Right;bar.Size=vertical?new Size(74,800):new Size(1200,54);
    typeof(Bar).GetMethod("LayoutBar",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(bar,new object[]{vertical});
    Point screen=launcher.PointToScreen(new Point(launcher.Width/2,launcher.Height/2));
    Check(hit.Invoke(bar,new object[]{screen})==launcher,"Launcher hit area on "+edge);
    Check(hit.Invoke(bar,new object[]{restore.PointToScreen(new Point(4,4))})==restore,"Restore hit area on "+edge);
    Check(hit.Invoke(bar,new object[]{quick.PointToScreen(new Point(4,4))})==quick,"Quick settings hit area on "+edge);
    Button counter=(Button)Field(bar,"coinCounter");Check(hit.Invoke(bar,new object[]{counter.PointToScreen(new Point(4,4))})==counter,"Coin counter hit area on "+edge);
    Check(!counter.Bounds.IntersectsWith(clock.Bounds)&&!counter.Bounds.IntersectsWith(quick.Bounds)&&!counter.Bounds.IntersectsWith(battery.Bounds)&&counter.Parent.ClientRectangle.Contains(counter.Bounds),"Coin counter fits without overlap on "+edge);
    PetWallet coins=(PetWallet)Field(bar,"pets");coins.Coins=1.5m;typeof(Bar).GetMethod("UpdateClock",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(bar,null);Check(counter.Text==Bar.CoinText(1.5m)+" coins","Live balance displays fractional coins");
    Check(hit.Invoke(bar,new object[]{battery.PointToScreen(new Point(4,4))})==quick,"Battery opens quick settings on "+edge);
    Check(!quick.Bounds.IntersectsWith(battery.Bounds)&&!quick.Bounds.IntersectsWith(clock.Bounds)&&!quick.Bounds.IntersectsWith(restore.Bounds),"Quick settings does not overlap controls on "+edge);
    Point local=background.PointToClient(screen);MouseEventArgs click=new MouseEventArgs(MouseButtons.Left,1,local.X,local.Y,0);
    down.Invoke(background,new object[]{click});up.Invoke(background,new object[]{click});
    ContextMenuStrip menu=(ContextMenuStrip)Field(bar,"activeMenu");Check(menu!=null&&menu.Visible,"Background click opens Windows menu on "+edge);menu.Close();
   }
   Point start=background.PointToClient(launcher.PointToScreen(new Point(4,4)));
   down.Invoke(background,new object[]{new MouseEventArgs(MouseButtons.Left,1,start.X,start.Y,0)});
   up.Invoke(background,new object[]{new MouseEventArgs(MouseButtons.Left,1,-100000,-100000,0)});
   Check(!((ContextMenuStrip)Field(bar,"activeMenu")).Visible,"Dragging away does not open menu");
   bar.Enabled=false;
   Check(hit.Invoke(bar,new object[]{launcher.PointToScreen(new Point(4,4))})==null,"Disabled taskbar does not relay background clicks");
   bar.Enabled=true;
   // Exercise actual window ordering without registering an appbar or hiding Explorer.
   FieldInfo registration=typeof(Bar).GetField("registered",BindingFlags.Instance|BindingFlags.NonPublic);
   bar.Location=new Point(100,100);
   registration.SetValue(bar,true);
   try{
    typeof(Bar).GetMethod("SyncBackground",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(bar,null);
    Check(background.Opacity==1,"Fresh settings use a solid background");
    InvertBackground(bar,background);
    Check(Above(background,bar),"Fixture reproduces solid background above icons");
    Application.DoEvents();
    Check(Above(bar,background),"Deferred first-show repair restores icon order");
    InvertBackground(bar,background);
    Check(Above(background,bar),"Fixture reproduces a later background inversion");
    typeof(Bar).GetMethod("EnsureBackgroundOrder",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(bar,null);
    Check(Above(bar,background),"Periodic repair fixes a later inversion");
   }finally{registration.SetValue(bar,false);background.Hide();}
   MethodInfo openShop=typeof(Bar).GetMethod("OpenPetShop",BindingFlags.Instance|BindingFlags.NonPublic);
   foreach(int closeMode in new[]{0,1,2}){
    bool timedOut=false;
    using(System.Windows.Forms.Timer timeout=new System.Windows.Forms.Timer{Interval=3000}){
     timeout.Tick+=delegate{timedOut=true;foreach(Form form in Application.OpenForms.Cast<Form>().ToArray())if(form is PetShop)form.Close();};
     timeout.Start();openShop.Invoke(bar,null);timeout.Stop();
    }
    Form shop=(Form)Field(bar,"petShop");
    Check(!timedOut&&shop!=null&&shop.Visible&&!shop.Modal&&bar.Enabled,"Pet Shop opens without blocking taskbar");
    openShop.Invoke(bar,null);Check(Field(bar,"petShop")==shop,"Pet Shop remains single instance");
    launcher.PerformClick();Check(((ContextMenuStrip)Field(bar,"activeMenu")).Visible,"Launcher works while Pet Shop is open");((ContextMenuStrip)Field(bar,"activeMenu")).Close();
    if(closeMode==0)shop.Controls.OfType<Button>().Single(b=>b.Text=="Close").PerformClick();
    else if(closeMode==1)shop.Close();
    else typeof(Form).GetMethod("ProcessDialogKey",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(shop,new object[]{Keys.Escape});
    Application.DoEvents();Check(shop.IsDisposed&&Field(bar,"petShop")==null&&bar.Enabled,"Modeless shop closes and can reopen using method "+closeMode);
   }
   MethodInfo editGroup=typeof(Bar).GetMethod("EditGroup",BindingFlags.Instance|BindingFlags.NonPublic);
   foreach(int closeMode in new[]{0,1,2,3}){
    bool timedOut=false;
    using(System.Windows.Forms.Timer timeout=new System.Windows.Forms.Timer{Interval=3000}){
     timeout.Tick+=delegate{timedOut=true;foreach(Form form in Application.OpenForms.Cast<Form>().ToArray())if(form is GroupEditor)form.Close();};
     timeout.Start();editGroup.Invoke(bar,new object[]{null});timeout.Stop();
    }
    GroupEditor editor=(GroupEditor)Field(bar,"groupEditor");
    Check(!timedOut&&editor!=null&&editor.Visible&&!editor.Modal&&editor.Owner==null&&bar.Enabled,"Group editor is independent and does not block the taskbar");
    editGroup.Invoke(bar,new object[]{null});Check(Field(bar,"groupEditor")==editor,"Group editor remains single instance");
    launcher.PerformClick();Check(((ContextMenuStrip)Field(bar,"activeMenu")).Visible,"Launcher works while group editor is open");((ContextMenuStrip)Field(bar,"activeMenu")).Close();
    int before=Preferences.Groups.Count;
    if(closeMode==0){editor.Controls.OfType<TextBox>().Single().Text="Saved independent group";editor.Controls.OfType<Button>().Single(b=>b.Text=="Save group").PerformClick();}
    else if(closeMode==1)editor.Controls.OfType<Button>().Single(b=>b.Text=="Cancel").PerformClick();
    else if(closeMode==2)editor.Close();
    else typeof(Form).GetMethod("ProcessDialogKey",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(editor,new object[]{Keys.Escape});
    Application.DoEvents();Check(editor.IsDisposed&&Field(bar,"groupEditor")==null&&bar.Enabled,"Independent group editor closes using method "+closeMode);
    Check(Preferences.Groups.Count==before+(closeMode==0?1:0),"Only Save writes the group");
   }
   Rectangle monitor=new Rectangle(0,0,1920,1080);
   foreach(Edge edge in Enum.GetValues(typeof(Edge))){Rectangle strip=edge==Edge.Left?new Rectangle(0,0,74,1080):edge==Edge.Right?new Rectangle(1846,0,74,1080):edge==Edge.Top?new Rectangle(0,0,1920,54):new Rectangle(0,1026,1920,54);Size menuSize=new Size(300,700);Point point=Bar.MenuLocation(strip,new Rectangle(strip.Left,strip.Top,48,44),menuSize,monitor,edge);Rectangle menuRect=new Rectangle(point,menuSize);Check(monitor.Contains(menuRect)&&!menuRect.IntersectsWith(strip),"Menu stays on screen beside the taskbar on "+edge);}
   foreach(int closeMode in new[]{0,1,2}){
    using(GroupEditor editor=new GroupEditor(null)){
     bool closeAttempted=false;editor.Shown+=delegate{editor.BeginInvoke((Action)delegate{Check(Screen.FromControl(editor).WorkingArea.Contains(editor.Bounds),"Group editor fits on screen");closeAttempted=true;if(closeMode==0)editor.Controls.OfType<Button>().Single(b=>b.Text=="Cancel").PerformClick();else if(closeMode==1)editor.Close();else typeof(Form).GetMethod("ProcessDialogKey",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(editor,new object[]{Keys.Escape});});};
     using(System.Windows.Forms.Timer timeout=new System.Windows.Forms.Timer{Interval=3000}){bool timedOut=false;timeout.Tick+=delegate{timedOut=true;editor.Close();};timeout.Start();DialogResult result=editor.ShowDialog(bar);timeout.Stop();Check(!timedOut&&closeAttempted&&result==DialogResult.Cancel&&bar.Enabled,"Group editor closes and re-enables taskbar using method "+closeMode);}
    }
   }
   PetWallet wallet=(PetWallet)Field(bar,"pets");int snacks=wallet.Snacks[wallet.Equipped];typeof(Bar).GetMethod("FeedBert",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(bar,null);Application.DoEvents();
   Form reaction=(Form)Field(bar,"bertReaction");Check(reaction!=null&&reaction.Visible,"Bert reaction is visible after feeding");Check(clock.Text.Contains("\U0001F9C0"),"Clock shows Bert's cheese");Check(wallet.Snacks[wallet.Equipped]==snacks+1,"Bert snack count saved");
   ToolTip petTips=(ToolTip)Field(bar,"tips");Check(petTips.GetToolTip(clock)==wallet.Current.Name+" - your taskbar pet","Feeding does not replace hover label with a thank-you");
   DateTime deadline=DateTime.UtcNow.AddSeconds(8);while(!reaction.IsDisposed&&DateTime.UtcNow<deadline){Application.DoEvents();System.Threading.Thread.Sleep(20);}Check(reaction.IsDisposed,"Bert reaction dismisses automatically");
   Check(petTips.GetToolTip(clock)==wallet.Current.Name+" - your taskbar pet","No stale thank-you after reaction expires");
   openShop.Invoke(bar,null);Form ownedShop=(Form)Field(bar,"petShop");editGroup.Invoke(bar,new object[]{null});Form openEditor=(Form)Field(bar,"groupEditor");bar.Close();Check(ownedShop.IsDisposed&&openEditor.IsDisposed,"Closing taskbar disposes Pet Shop and group editor");
  }
  WindowsQuickSettings.Input[] keys=WindowsQuickSettings.Shortcut();Check(System.Runtime.InteropServices.Marshal.SizeOf(typeof(WindowsQuickSettings.Input))==40,"x64 input structure size");Check(keys.Length==4&&keys[0].Data.Keyboard.Key==0x5B&&keys[0].Data.Keyboard.Flags==0&&keys[1].Data.Keyboard.Key==0x41&&keys[1].Data.Keyboard.Flags==0&&keys[2].Data.Keyboard.Key==0x41&&keys[2].Data.Keyboard.Flags==2&&keys[3].Data.Keyboard.Key==0x5B&&keys[3].Data.Keyboard.Flags==2,"Quick settings sends Windows+A with both keys released");
  File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"launcher-click-validation.txt"),"PASS: launcher, Restore, quick settings and battery hit areas on all four edges; no overlapping controls; menu routing and drag cancellation; disabled-owner routing; solid background first-show and later z-order repair; modeless single-instance Pet Shop, launcher interaction, Close/Escape/window-close and owner cleanup; independent group editor Save/Cancel/Escape/window-close and cleanup; custom group icon persistence and legacy compatibility; pet feeding and reaction expiry; Windows+A key sequence and x64 input layout. Test fixture disables real taskbar initialization and does not send keys.");return 0;
 }catch(Exception ex){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"launcher-click-validation.txt"),"FAIL: "+ex);return 1;}}
}}
