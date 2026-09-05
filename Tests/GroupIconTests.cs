using System;using System.IO;using System.Linq;using System.Drawing;using System.Windows.Forms;
namespace CompassBar {static class GroupIconTests {
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 public static void Run(){
  string previous=Preferences.Folder;Preferences.Folder=Path.Combine(previous,"group-icons");Directory.CreateDirectory(Preferences.Folder);
  try{
   Preferences.Pins.Clear();Preferences.Groups.Clear();File.WriteAllText(Path.Combine(Preferences.Folder,"pins.txt"),"");
   AppGroup group=new AppGroup{Name="Purple friends",IconStyle=3,IconColor=Color.FromArgb(177,133,245),LastSelected="alpha.exe"};group.Apps.Add(new PinnedApp("alpha.exe"));group.Apps.Add(new PinnedApp("beta.exe"));Preferences.Groups.Add(group);Preferences.SaveGroups();Preferences.Load();
   group=Preferences.Groups.Single();Check(group.IconStyle==3&&group.IconColor.ToArgb()==Color.FromArgb(177,133,245).ToArgb()&&group.Apps.Count==2&&group.LastSelected=="alpha.exe","Custom group icon survives loading without changing apps");
   using(GroupButton button=new GroupButton()){button.SetGroup(group);Bitmap image=button.AppIcon;group.LastSelected="beta.exe";button.SetGroup(group);Check(Object.ReferenceEquals(image,button.AppIcon),"Opening another app keeps the custom group icon");group.IconColor=Color.Blue;button.SetGroup(group);Check(!Object.ReferenceEquals(image,button.AppIcon),"Changing colour refreshes the group icon");}
   using(GroupEditor editor=new GroupEditor(group)){
    editor.Show();Application.DoEvents();editor.Controls.OfType<Button>().Single(b=>b.AccessibleName=="Purple group icon").PerformClick();editor.Controls.OfType<Button>().Single(b=>b.AccessibleName=="Star group icon shape").PerformClick();
    Check(group.IconStyle==3&&group.IconColor==Color.Blue,"Preview changes do not edit the saved group");
    editor.Controls.OfType<Button>().Single(b=>b.Text=="Save group").PerformClick();Check(editor.Result.IconStyle==2&&editor.Result.IconColor.ToArgb()==Color.FromArgb(177,133,245).ToArgb()&&editor.Result.Apps.Count==2,"Editor saves selected shape, colour and membership");editor.Close();
   }
   for(int style=1;style<=4;style++)using(Bitmap icon=GroupIcon.Create(style,Color.Purple)){Check(icon.Width==64&&icon.GetPixel(0,0).A==0,"Custom icons retain transparent edges");}
   File.Delete(Path.Combine(Preferences.Folder,"group-icons.txt"));Preferences.Load();Check(Preferences.Groups.Single().IconStyle==0&&Preferences.Groups.Single().Apps.Count==2,"Older groups load with their app icon");
   File.WriteAllText(Path.Combine(Preferences.Folder,"group-icons.txt"),"not base64|3|B185F5\n");Preferences.Load();Check(Preferences.Groups.Count==1&&Preferences.Groups[0].Apps.Count==2,"Invalid icon metadata does not erase group membership");
  }finally{Preferences.Folder=previous;Preferences.Load();}
 }
}}
