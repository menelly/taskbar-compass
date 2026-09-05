using System;using System.IO;using System.Linq;using System.Drawing;using System.Globalization;using System.Runtime.InteropServices;using System.Windows.Forms;
namespace CompassBar {
sealed class Pet {
 public string Name,Emoji,Food,Reaction;public int Price;
 public static readonly Pet[] All={
  new Pet{Name="Bert",Emoji="\U0001F42D",Food="\U0001F9C0",Reaction="Squeak! Thanks for the cheese!",Price=0},
  new Pet{Name="Bark",Emoji="\U0001F436",Food="\U0001F9B4",Reaction="Woof! Tail wagging! Thanks for the treat!",Price=300},
  new Pet{Name="Sulverster",Emoji="\U0001F431",Food="\U0001F41F",Reaction="Purr... That's a tasty fish!",Price=100}};
}
sealed class PetWallet {
 bool recoveredBackup;
 public decimal Coins;public int Equipped;public bool[] Owned={true,false,false};public int[] Snacks=new int[3];
 public Pet Current{get{return Pet.All[Equipped];}}
 string FileName{get{return Path.Combine(Preferences.Folder,"pets.txt");}}
 public void Load(){
  Coins=0;Equipped=0;Owned=new[]{true,false,false};Snacks=new int[3];
  recoveredBackup=false;
  if(!File.Exists(FileName)&&!File.Exists(FileName+".bak")){int old;string legacy=Path.Combine(Preferences.Folder,"bert-snacks.txt");if(File.Exists(legacy)&&Int32.TryParse(File.ReadAllText(legacy),out old))Snacks[0]=Math.Max(0,old);return;}
  try{ReadSave(File.ReadAllLines(FileName));}catch(IOException){if(!File.Exists(FileName+".bak"))throw;ReadSave(File.ReadAllLines(FileName+".bak"));recoveredBackup=true;}
 }
 void ReadSave(string[] lines){decimal coins;int equipped;
  if(lines.Length<4||!Decimal.TryParse(lines[0],NumberStyles.Number,CultureInfo.InvariantCulture,out coins)||coins<0||coins>1000000000m||!Int32.TryParse(lines[2],out equipped))throw new IOException("Your pet save could not be read. It has been kept unchanged.");
  string[] owned=lines[1].Split(','),snacks=lines[3].Split(',');if(owned.Length!=3||snacks.Length!=3||equipped<0||equipped>=3)throw new IOException("Invalid pet save. It has been kept unchanged.");
  bool[] nextOwned=new bool[3];int[] nextSnacks=new int[3];
  for(int i=0;i<3;i++){int count;if((owned[i]!="0"&&owned[i]!="1")||!Int32.TryParse(snacks[i],out count)||count<0)throw new IOException("Invalid pet save. It has been kept unchanged.");nextOwned[i]=owned[i]=="1";nextSnacks[i]=count;}
  Owned=nextOwned;Snacks=nextSnacks;Owned[0]=true;Coins=coins;Equipped=Owned[equipped]?equipped:0;
 }
 public void Save(){
  Directory.CreateDirectory(Preferences.Folder);string temp=FileName+"."+Guid.NewGuid().ToString("N")+".tmp";
  try{
   using(FileStream stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
    using(StreamWriter writer=new StreamWriter(stream)){foreach(string line in new[]{Coins.ToString(CultureInfo.InvariantCulture),String.Join(",",Owned.Select(x=>x?"1":"0")),Equipped.ToString(),String.Join(",",Snacks.Select(x=>x.ToString()))})writer.WriteLine(line);writer.Flush();stream.Flush(true);}
   }
   if(File.Exists(FileName))File.Replace(temp,FileName,recoveredBackup?null:FileName+".bak");else File.Move(temp,FileName);recoveredBackup=false;
  }finally{if(File.Exists(temp))File.Delete(temp);}
 }
 public void Earn(double seconds,bool idle){if(seconds<=0||seconds>5)return;Coins=Math.Min(1000000000m,Coins+(decimal)seconds/(idle?120m:60m));}
 public void Select(int index){
  if(index<0||index>=3)throw new ArgumentOutOfRangeException("index");if(!Owned[index]&&Coins<Pet.All[index].Price)throw new InvalidOperationException("You need more coins to unlock this pet.");
  decimal oldCoins=Coins;int oldEquipped=Equipped;bool oldOwned=Owned[index];
  if(!Owned[index]){Coins-=Pet.All[index].Price;Owned[index]=true;}Equipped=index;
  try{Save();}catch{Coins=oldCoins;Equipped=oldEquipped;Owned[index]=oldOwned;throw;}
 }
 public void Feed(){int before=Snacks[Equipped];Snacks[Equipped]=before==Int32.MaxValue?before:before+1;try{Save();}catch{Snacks[Equipped]=before;throw;}}
}
static class IdleTime {
 [StructLayout(LayoutKind.Sequential)]struct LastInput {public uint Size,Tick;}
 [DllImport("user32.dll")]static extern bool GetLastInputInfo(ref LastInput input);
 public static bool IsIdle(){LastInput input=new LastInput{Size=(uint)Marshal.SizeOf(typeof(LastInput))};return !GetLastInputInfo(ref input)||unchecked((uint)Environment.TickCount-input.Tick)>=300000u;}
}
sealed class PetShop:Form {
 readonly Label balance=new Label();readonly Button[] buttons=new Button[3];readonly PetWallet wallet;readonly System.Windows.Forms.Timer refresh=new System.Windows.Forms.Timer{Interval=1000};
 public PetShop(PetWallet state,Action changed){
  wallet=state;Text="Pet Shop";Icon=AppIdentity.Icon;ClientSize=new Size(510,470);StartPosition=FormStartPosition.CenterParent;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;Font=new Font("Segoe UI",10);BackColor=Color.FromArgb(22,26,33);ForeColor=Color.FromArgb(238,244,251);
  Controls.Add(new Label{Text="A little friend for your taskbar",Location=new Point(24,18),Size=new Size(465,35),Font=new Font("Segoe UI Semibold",17)});
  balance.SetBounds(24,62,460,27);balance.ForeColor=Color.FromArgb(255,217,110);Controls.Add(balance);
  Controls.Add(new Label{Text="1 coin/min active · ½ coin/min AFK\nAFK begins after 5 minutes idle. Earn while the taskbar runs.",Location=new Point(24,96),Size=new Size(462,48),ForeColor=Color.FromArgb(152,165,183)});
  for(int i=0;i<3;i++){int index=i;Pet pet=Pet.All[i];int y=158+i*86;
   Panel card=new Panel{Location=new Point(24,y),Size=new Size(462,76),BackColor=Color.FromArgb(34,41,52)};Controls.Add(card);
   card.Controls.Add(new Label{Text=pet.Emoji,Location=new Point(10,7),Size=new Size(38,35),Font=new Font("Segoe UI Emoji",18)});
   card.Controls.Add(new Label{Text=pet.Name,Location=new Point(52,10),Size=new Size(220,30),Font=new Font("Segoe UI",15)});
   card.Controls.Add(new Label{Text=i==0?"Your original friend · free forever":pet.Price+" coins · keep forever",Location=new Point(14,44),Size=new Size(265,23),ForeColor=Color.FromArgb(170,184,203)});
   Button button=new Button{Location=new Point(302,19),Size=new Size(146,38),FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(39,62,82),ForeColor=ForeColor};button.FlatAppearance.BorderSize=0;buttons[i]=button;card.Controls.Add(button);
   button.Click+=delegate{try{wallet.Select(index);changed();UpdateButtons();}catch(Exception ex){MessageBox.Show(this,ex.Message,"Pet Shop");}};
  }
  Button close=new Button{Text="Close",Location=new Point(366,425),Size=new Size(120,32),DialogResult=DialogResult.Cancel};Controls.Add(close);CancelButton=close;
  AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;refresh.Tick+=delegate{UpdateButtons();};Shown+=delegate{refresh.Start();};UpdateButtons();
 }
 void UpdateButtons(){balance.Text="Your coins: "+wallet.Coins.ToString("0.0",CultureInfo.CurrentCulture);for(int i=0;i<3;i++){buttons[i].Text=wallet.Equipped==i?"Equipped":wallet.Owned[i]?"Equip":"Unlock · "+Pet.All[i].Price;buttons[i].Enabled=wallet.Equipped!=i&&(wallet.Owned[i]||wallet.Coins>=Pet.All[i].Price);}}
 protected override void Dispose(bool disposing){if(disposing)refresh.Dispose();base.Dispose(disposing);}
}
}
