using Microsoft.Win32;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Forms=System.Windows.Forms;
using Point=System.Windows.Point;
using Rectangle=System.Windows.Shapes.Rectangle;
using Brushes=System.Windows.Media.Brushes;

namespace ChichiCapture;
public partial class MainWindow:Window
{
 sealed class CaptureEntry{public required BitmapSource Image{get;init;}public required string Title{get;init;}}
 enum Tool{Select,Crop,Pen,Arrow,Rectangle,Ellipse,Text,Mosaic}
 Tool tool;Point start;Shape? preview;Polyline? stroke;BitmapSource? source;UIElement? moving;Point movingStart;double movingLeft,movingTop;CropFrame? cropFrame;
 readonly Stack<Action> undo=new();readonly AppSettings settings=AppSettings.Load();readonly Forms.NotifyIcon tray;HwndSource? hookSource;bool exiting;
 const int HK=1,HK_WINDOW=2,WM_HOTKEY=0x0312,ALT=1,CTRL=2,SHIFT=4;
 [StructLayout(LayoutKind.Sequential)]struct NativeRect{public int Left,Top,Right,Bottom;}
 [DllImport("user32.dll")]static extern bool RegisterHotKey(IntPtr h,int id,uint mods,uint key);
 [DllImport("user32.dll")]static extern bool UnregisterHotKey(IntPtr h,int id);
 [DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")]static extern bool GetWindowRect(IntPtr h,out NativeRect r);
 [DllImport("user32.dll")]static extern bool IsIconic(IntPtr h);

 public MainWindow(){
  InitializeComponent();PreviewKeyDown+=Main_KeyDown;
  var iconStream=System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("ChichiCapture.chichi.ico");tray=new Forms.NotifyIcon{Icon=iconStream==null?SystemIcons.Application:new Icon(iconStream),Text="ChichiCapture",Visible=true};
  var menu=new Forms.ContextMenuStrip();
  menu.Items.Add("영역 캡처",null,(_,_)=>Dispatcher.Invoke(BeginRegion));
  menu.Items.Add("활성 창 캡처",null,(_,_)=>Dispatcher.Invoke(BeginWindow));
  menu.Items.Add("편집기 열기",null,(_,_)=>Dispatcher.Invoke(Restore));
  menu.Items.Add("종료",null,(_,_)=>Dispatcher.Invoke(Exit));
  tray.ContextMenuStrip=menu;tray.DoubleClick+=(_,_)=>Dispatcher.Invoke(Restore);
  Loaded+=(_,_)=>RegisterKeys();Closing+=OnClosing;
 }
 void Restore(){Show();WindowState=WindowState.Normal;Activate();}
 void OnClosing(object? s,System.ComponentModel.CancelEventArgs e){if(!exiting){e.Cancel=true;Hide();tray.ShowBalloonTip(1000,"ChichiCapture","시스템 트레이에서 계속 실행됩니다.",Forms.ToolTipIcon.Info);}}
 void Exit(){exiting=true;Unregister();tray.Dispose();Application.Current.Shutdown();}
 void RegisterKeys(){Unregister();var h=new WindowInteropHelper(this).Handle;uint a=Mods(settings.Ctrl,settings.Shift,settings.Alt),b=Mods(settings.WindowCtrl,settings.WindowShift,settings.WindowAlt);RegisterHotKey(h,HK,a,(uint)settings.HotKey);RegisterHotKey(h,HK_WINDOW,b,(uint)settings.WindowHotKey);hookSource=HwndSource.FromHwnd(h);hookSource?.AddHook(Hook);StatusText.Text=$"영역: {Hotkey(false)} / 창: {Hotkey(true)}";}
 uint Mods(bool c,bool s,bool a)=>(uint)((c?CTRL:0)|(s?SHIFT:0)|(a?ALT:0));
 string Hotkey(bool w)=>$"{((w?settings.WindowCtrl:settings.Ctrl)?"Ctrl+":"")}{((w?settings.WindowShift:settings.Shift)?"Shift+":"")}{((w?settings.WindowAlt:settings.Alt)?"Alt+":"")}{(char)(w?settings.WindowHotKey:settings.HotKey)}";
 void Unregister(){var h=new WindowInteropHelper(this).Handle;if(h!=IntPtr.Zero){UnregisterHotKey(h,HK);UnregisterHotKey(h,HK_WINDOW);}if(hookSource!=null){hookSource.RemoveHook(Hook);hookSource=null;}}
 IntPtr Hook(IntPtr h,int m,IntPtr w,IntPtr l,ref bool done){if(m==WM_HOTKEY){if(w.ToInt32()==HK){done=true;BeginRegion();}else if(w.ToInt32()==HK_WINDOW){done=true;BeginWindow();}}return IntPtr.Zero;}
 void Main_KeyDown(object s,System.Windows.Input.KeyEventArgs e){if(e.Key==Key.Z&&Keyboard.Modifiers.HasFlag(ModifierKeys.Control)){Undo();e.Handled=true;}else if(e.Key==Key.Escape&&cropFrame!=null){CancelCrop_Click(this,new RoutedEventArgs());e.Handled=true;}}

 void RegionCapture_Click(object s,RoutedEventArgs e)=>BeginRegion();void WindowCapture_Click(object s,RoutedEventArgs e)=>BeginWindow();
 void BeginRegion(){Hide();Thread.Sleep(100);var p=new RegionWindow();if(p.ShowDialog()==true)Capture(p.Selection);else Restore();}
 void BeginWindow(){Hide();Thread.Sleep(100);var p=new WindowPicker();if(p.ShowDialog()==true)Capture(p.Selection);else Restore();}
 void Capture(Rect r){using var bmp=new Bitmap((int)r.Width,(int)r.Height,PixelFormat.Format32bppArgb);using(var g=Graphics.FromImage(bmp))g.CopyFromScreen((int)r.X,(int)r.Y,0,0,bmp.Size);Restore();using var ms=new MemoryStream();bmp.Save(ms,ImageFormat.Png);ms.Position=0;var bi=new BitmapImage();bi.BeginInit();bi.CacheOption=BitmapCacheOption.OnLoad;bi.StreamSource=ms;bi.EndInit();bi.Freeze();SetSource(bi);AutoSave();}
 void SetSource(BitmapSource bi,bool addHistory=true,bool clearUndo=true){source=bi;CaptureImage.Source=bi;Overlay.Children.Clear();cropFrame=null;if(clearUndo)undo.Clear();Overlay.Width=EditorHost.Width=bi.PixelWidth;Overlay.Height=EditorHost.Height=bi.PixelHeight;if(addHistory){var x=new CaptureEntry{Image=bi,Title=$"Capture_{DateTime.Now:HH-mm-ss}.png"};HistoryList.Items.Insert(0,x);HistoryList.SelectedItem=x;while(HistoryList.Items.Count>30)HistoryList.Items.RemoveAt(HistoryList.Items.Count-1);}StatusText.Text=$"캡처 완료: {bi.PixelWidth}×{bi.PixelHeight}";}
 void HistoryList_SelectionChanged(object s,SelectionChangedEventArgs e){if(HistoryList.SelectedItem is CaptureEntry x&&x.Image!=source)SetSource(x.Image,false);}
 void DeleteHistory_Click(object s,RoutedEventArgs e){if(HistoryList.SelectedItem!=null)HistoryList.Items.Remove(HistoryList.SelectedItem);}
 void ClearHistory_Click(object s,RoutedEventArgs e)=>HistoryList.Items.Clear();

 System.Windows.Media.Brush CurrentBrush(){var i=(ComboBoxItem)ColorBox.SelectedItem;return (System.Windows.Media.Brush)new BrushConverter().ConvertFromString((string)i.Tag)!;}
 void SelectTool_Click(object s,RoutedEventArgs e)=>tool=Tool.Select;void CropTool_Click(object s,RoutedEventArgs e)=>tool=Tool.Crop;void PenTool_Click(object s,RoutedEventArgs e)=>tool=Tool.Pen;void ArrowTool_Click(object s,RoutedEventArgs e)=>tool=Tool.Arrow;void RectTool_Click(object s,RoutedEventArgs e)=>tool=Tool.Rectangle;void EllipseTool_Click(object s,RoutedEventArgs e)=>tool=Tool.Ellipse;void TextTool_Click(object s,RoutedEventArgs e)=>tool=Tool.Text;void MosaicTool_Click(object s,RoutedEventArgs e)=>tool=Tool.Mosaic;
 void MakeMovable(UIElement el){el.MouseLeftButtonDown+=(s,e)=>{if(tool!=Tool.Select)return;moving=(UIElement)s;movingStart=e.GetPosition(Overlay);movingLeft=double.IsNaN(Canvas.GetLeft(moving))?0:Canvas.GetLeft(moving);movingTop=double.IsNaN(Canvas.GetTop(moving))?0:Canvas.GetTop(moving);moving.CaptureMouse();e.Handled=true;};el.MouseMove+=(s,e)=>{if(moving!=(UIElement)s||e.LeftButton!=MouseButtonState.Pressed)return;var p=e.GetPosition(Overlay);Canvas.SetLeft(moving,movingLeft+p.X-movingStart.X);Canvas.SetTop(moving,movingTop+p.Y-movingStart.Y);e.Handled=true;};el.MouseLeftButtonUp+=(s,e)=>{if(moving!=(UIElement)s)return;var el0=moving;double fromL=movingLeft,fromT=movingTop,toL=Canvas.GetLeft(el0),toT=Canvas.GetTop(el0);el0.ReleaseMouseCapture();moving=null;if(fromL!=toL||fromT!=toT)undo.Push(()=>{Canvas.SetLeft(el0,fromL);Canvas.SetTop(el0,fromT);});e.Handled=true;};}
 void AddElement(UIElement el){Overlay.Children.Add(el);MakeMovable(el);undo.Push(()=>Overlay.Children.Remove(el));}

 void Host_Down(object s,MouseButtonEventArgs e){if(source==null||tool==Tool.Select)return;start=e.GetPosition(Overlay);Overlay.CaptureMouse();var b=CurrentBrush();double t=StrokeSlider.Value;
  if(tool==Tool.Pen){stroke=new Polyline{Stroke=b,StrokeThickness=t,StrokeLineJoin=PenLineJoin.Round};stroke.Points.Add(start);Overlay.Children.Add(stroke);}
  else if(tool is Tool.Rectangle or Tool.Mosaic or Tool.Crop)preview=new Rectangle();
  else if(tool==Tool.Ellipse)preview=new Ellipse();else if(tool==Tool.Arrow)preview=new Line();
  else if(tool==Tool.Text){var x=Microsoft.VisualBasic.Interaction.InputBox("넣을 글자를 입력하세요.","텍스트");if(x.Length>0){var tb=new TextBlock{Text=x,Foreground=b,FontSize=Math.Max(16,t*5),FontWeight=FontWeights.Bold,Background=Brushes.Transparent};Canvas.SetLeft(tb,start.X);Canvas.SetTop(tb,start.Y);AddElement(tb);}}
  if(preview!=null){preview.Stroke=b;preview.StrokeThickness=t;if(FillShape.IsChecked==true&&tool is Tool.Rectangle or Tool.Ellipse)preview.Fill=b;Overlay.Children.Add(preview);}
 }
 void Host_Move(object s,System.Windows.Input.MouseEventArgs e){if(e.LeftButton!=MouseButtonState.Pressed)return;var p=e.GetPosition(Overlay);if(stroke!=null){stroke.Points.Add(p);return;}if(preview is Line l){l.X1=start.X;l.Y1=start.Y;l.X2=p.X;l.Y2=p.Y;}else if(preview!=null){Canvas.SetLeft(preview,Math.Min(start.X,p.X));Canvas.SetTop(preview,Math.Min(start.Y,p.Y));preview.Width=Math.Abs(p.X-start.X);preview.Height=Math.Abs(p.Y-start.Y);}}
 void Host_Up(object s,MouseButtonEventArgs e){var p=e.GetPosition(Overlay);if(tool==Tool.Mosaic&&preview!=null){Overlay.Children.Remove(preview);Mosaic(start,p);}else if(tool==Tool.Crop&&preview!=null){Overlay.Children.Remove(preview);ShowCrop(start,p);}else if(preview!=null){var item=preview;if(tool==Tool.Arrow&&item is Line l){var head=ArrowHead(l);MakeMovable(item);MakeMovable(head);undo.Push(()=>{Overlay.Children.Remove(item);Overlay.Children.Remove(head);});}else{MakeMovable(item);undo.Push(()=>Overlay.Children.Remove(item));}}if(stroke!=null){var item=stroke;MakeMovable(item);undo.Push(()=>Overlay.Children.Remove(item));}preview=null;stroke=null;Overlay.ReleaseMouseCapture();}
 Polygon ArrowHead(Line l){double a=Math.Atan2(l.Y2-l.Y1,l.X2-l.X1),n=16;var p=new Polygon{Fill=l.Stroke,Points=new PointCollection{new(l.X2,l.Y2),new(l.X2-n*Math.Cos(a-.45),l.Y2-n*Math.Sin(a-.45)),new(l.X2-n*Math.Cos(a+.45),l.Y2-n*Math.Sin(a+.45))}};Overlay.Children.Add(p);return p;}
 Int32Rect Sel(Point a,Point b){if(source==null)return Int32Rect.Empty;int x=(int)Math.Max(0,Math.Min(a.X,b.X)),y=(int)Math.Max(0,Math.Min(a.Y,b.Y)),w=(int)Math.Min(source.PixelWidth-x,Math.Abs(b.X-a.X)),h=(int)Math.Min(source.PixelHeight-y,Math.Abs(b.Y-a.Y));return w>1&&h>1?new Int32Rect(x,y,w,h):Int32Rect.Empty;}
 void Mosaic(Point a,Point b){var r=Sel(a,b);if(r.IsEmpty)return;var c=new CroppedBitmap(Render(),r);int w=Math.Max(1,r.Width/12),h=Math.Max(1,r.Height/12);var sm=new TransformedBitmap(c,new ScaleTransform((double)w/r.Width,(double)h/r.Height));var im=new System.Windows.Controls.Image{Source=sm,Width=r.Width,Height=r.Height,Stretch=Stretch.Fill};RenderOptions.SetBitmapScalingMode(im,BitmapScalingMode.NearestNeighbor);Canvas.SetLeft(im,r.X);Canvas.SetTop(im,r.Y);AddElement(im);}
 void ShowCrop(Point a,Point b){CancelCrop_Click(this,new RoutedEventArgs());var r=Sel(a,b);if(r.IsEmpty)return;cropFrame=new CropFrame(new Rect(r.X,r.Y,r.Width,r.Height));Overlay.Children.Add(cropFrame);}
 void ApplyCrop_Click(object s,RoutedEventArgs e){if(cropFrame==null||source==null)return;var r=cropFrame.Selection(source.PixelWidth,source.PixelHeight);Overlay.Children.Remove(cropFrame);cropFrame=null;var before=Render();var after=new CroppedBitmap(before,r);SetSource(after,false,false);undo.Push(()=>SetSource(before,false,false));}
 void CancelCrop_Click(object s,RoutedEventArgs e){if(cropFrame!=null){Overlay.Children.Remove(cropFrame);cropFrame=null;}}
 void Undo(){if(undo.Count>0)undo.Pop()();}
 BitmapSource Render(){var z=new System.Windows.Size(EditorHost.Width,EditorHost.Height);EditorHost.Measure(z);EditorHost.Arrange(new Rect(z));var r=new RenderTargetBitmap((int)z.Width,(int)z.Height,96,96,PixelFormats.Pbgra32);r.Render(EditorHost);return r;}
 void Copy_Click(object s,RoutedEventArgs e){if(source!=null){Clipboard.SetImage(Render());StatusText.Text="클립보드에 복사했습니다.";}}
 void SavePng_Click(object s,RoutedEventArgs e){if(source==null)return;var d=new SaveFileDialog{Filter="PNG 이미지|*.png",InitialDirectory=Directory.Exists(settings.SaveFolder)?settings.SaveFolder:Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),FileName=FileName()};if(d.ShowDialog()==true){settings.SaveFolder=Path.GetDirectoryName(d.FileName)!;settings.Save();Save(d.FileName);}}
 void Save(string p){Directory.CreateDirectory(Path.GetDirectoryName(p)!);var e=new PngBitmapEncoder();e.Frames.Add(BitmapFrame.Create(Render()));using var f=File.Create(p);e.Save(f);StatusText.Text=$"저장: {p}";}
 string FileName()=>$"Capture_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";void AutoSave(){if(settings.AutoSave&&source!=null)Save(Path.Combine(settings.SaveFolder,FileName()));}
 void Settings_Click(object s,RoutedEventArgs e){var w=new SettingsWindow(settings){Owner=this};if(w.ShowDialog()==true){settings.Save();RegisterKeys();}}
}
