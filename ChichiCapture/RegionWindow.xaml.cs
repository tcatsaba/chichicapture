using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Forms=System.Windows.Forms;

namespace ChichiCapture;

public partial class RegionWindow:Window
{
 System.Windows.Point start;
 readonly BitmapSource frozenScreen;
 public BitmapSource? CapturedImage{get;private set;}

 public RegionWindow()
 {
  var v=Forms.SystemInformation.VirtualScreen;
  using(var bmp=new Bitmap(v.Width,v.Height,PixelFormat.Format32bppArgb))
  {
   using(var g=Graphics.FromImage(bmp))g.CopyFromScreen(v.Left,v.Top,0,0,bmp.Size);
   using var ms=new MemoryStream();
   bmp.Save(ms,ImageFormat.Png);ms.Position=0;
   var bi=new BitmapImage();bi.BeginInit();bi.CacheOption=BitmapCacheOption.OnLoad;bi.StreamSource=ms;bi.EndInit();bi.Freeze();
   frozenScreen=bi;
  }
  InitializeComponent();FrozenScreen.Source=frozenScreen;
  Left=v.Left;Top=v.Top;Width=v.Width;Height=v.Height;
  Loaded+=(_,_)=>{Activate();Focus();Keyboard.Focus(this);};
 }

 void Window_Down(object s,MouseButtonEventArgs e)
 {
  start=e.GetPosition(SelectionCanvas);Box.Visibility=Visibility.Visible;CaptureMouse();
 }

 void Window_Move(object s,System.Windows.Input.MouseEventArgs e)
 {
  if(e.LeftButton!=MouseButtonState.Pressed)return;
  var p=e.GetPosition(SelectionCanvas);
  Canvas.SetLeft(Box,Math.Min(start.X,p.X));Canvas.SetTop(Box,Math.Min(start.Y,p.Y));
  Box.Width=Math.Abs(p.X-start.X);Box.Height=Math.Abs(p.Y-start.Y);
 }

 void Window_Up(object s,MouseButtonEventArgs e)
 {
  var p=e.GetPosition(SelectionCanvas);ReleaseMouseCapture();
  double left=Math.Max(0,Math.Min(start.X,p.X)),top=Math.Max(0,Math.Min(start.Y,p.Y));
  double right=Math.Min(SelectionCanvas.ActualWidth,Math.Max(start.X,p.X)),bottom=Math.Min(SelectionCanvas.ActualHeight,Math.Max(start.Y,p.Y));
  if(right-left<=1||bottom-top<=1){DialogResult=false;return;}
  double sx=frozenScreen.PixelWidth/SelectionCanvas.ActualWidth,sy=frozenScreen.PixelHeight/SelectionCanvas.ActualHeight;
  int x=(int)Math.Round(left*sx),y=(int)Math.Round(top*sy);
  int w=Math.Min(frozenScreen.PixelWidth-x,Math.Max(1,(int)Math.Round((right-left)*sx)));
  int h=Math.Min(frozenScreen.PixelHeight-y,Math.Max(1,(int)Math.Round((bottom-top)*sy)));
  var crop=new CroppedBitmap(frozenScreen,new Int32Rect(x,y,w,h));crop.Freeze();CapturedImage=crop;DialogResult=true;
 }

 void Window_KeyDown(object s,System.Windows.Input.KeyEventArgs e)
 {
  if(e.Key==Key.Escape){DialogResult=false;Close();}
 }
}
