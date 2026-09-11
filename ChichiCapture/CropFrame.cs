using System.Windows;using System.Windows.Controls;using System.Windows.Controls.Primitives;using System.Windows.Input;using System.Windows.Media;
namespace ChichiCapture;
public sealed class CropFrame:Canvas{
 readonly System.Windows.Shapes.Rectangle frame;readonly Thumb handle;bool moving;System.Windows.Point down;double left,top;
 public CropFrame(Rect r){Width=Math.Max(20,r.Width);Height=Math.Max(20,r.Height);Canvas.SetLeft(this,r.X);Canvas.SetTop(this,r.Y);Background=Brushes.Transparent;Cursor=Cursors.SizeAll;frame=new System.Windows.Shapes.Rectangle{Stroke=Brushes.DeepSkyBlue,StrokeThickness=2,StrokeDashArray=new DoubleCollection{5,3},Fill=new SolidColorBrush(Color.FromArgb(20,0,150,255)),IsHitTestVisible=false};Children.Add(frame);handle=new Thumb{Width=14,Height=14,Background=Brushes.DeepSkyBlue,Cursor=Cursors.SizeNWSE};Children.Add(handle);Update();handle.DragDelta+=(_,e)=>{Width=Math.Max(20,Width+e.HorizontalChange);Height=Math.Max(20,Height+e.VerticalChange);Update();e.Handled=true;};MouseLeftButtonDown+=StartMove;MouseMove+=Move;MouseLeftButtonUp+=EndMove;}
 void Update(){frame.Width=Width;frame.Height=Height;Canvas.SetLeft(handle,Width-handle.Width);Canvas.SetTop(handle,Height-handle.Height);}
 void StartMove(object s,MouseButtonEventArgs e){if(e.OriginalSource==handle)return;moving=true;down=e.GetPosition((IInputElement)Parent);left=Canvas.GetLeft(this);top=Canvas.GetTop(this);CaptureMouse();e.Handled=true;}
 void Move(object s,System.Windows.Input.MouseEventArgs e){if(!moving||e.LeftButton!=MouseButtonState.Pressed)return;var p=e.GetPosition((IInputElement)Parent);Canvas.SetLeft(this,Math.Max(0,left+p.X-down.X));Canvas.SetTop(this,Math.Max(0,top+p.Y-down.Y));e.Handled=true;}
 void EndMove(object s,MouseButtonEventArgs e){if(moving){moving=false;ReleaseMouseCapture();e.Handled=true;}}
 public Int32Rect Selection(int maxW,int maxH){int x=(int)Math.Max(0,Canvas.GetLeft(this)),y=(int)Math.Max(0,Canvas.GetTop(this));return new Int32Rect(x,y,Math.Max(1,Math.Min(maxW-x,(int)Width)),Math.Max(1,Math.Min(maxH-y,(int)Height)));}
}
