using System.Runtime.InteropServices;using System.Text;using System.Windows;using System.Windows.Input;
namespace ChichiCapture;
public partial class WindowPicker:Window{
 public sealed class WindowItem{public required IntPtr Handle{get;init;}public required string Title{get;init;}public required Rect Bounds{get;init;}public string Display=>Title;}
 [StructLayout(LayoutKind.Sequential)]struct R{public int Left,Top,Right,Bottom;}delegate bool EnumProc(IntPtr h,IntPtr l);
 [DllImport("user32.dll")]static extern bool EnumWindows(EnumProc p,IntPtr l);[DllImport("user32.dll")]static extern bool IsWindowVisible(IntPtr h);[DllImport("user32.dll")]static extern bool IsIconic(IntPtr h);[DllImport("user32.dll")]static extern int GetWindowTextLength(IntPtr h);[DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetWindowText(IntPtr h,StringBuilder s,int n);[DllImport("user32.dll")]static extern bool GetWindowRect(IntPtr h,out R r);
 public Rect Selection{get;private set;}public IntPtr SelectedHandle{get;private set;}
 public WindowPicker(){InitializeComponent();Loaded+=(_,_)=>{LoadWindows();Activate();Focus();};}
 void LoadWindows(){WindowList.Items.Clear();int number=1;EnumWindows((h,l)=>{if(!IsWindowVisible(h)||IsIconic(h))return true;int len=GetWindowTextLength(h);if(len<1)return true;var b=new StringBuilder(len+1);GetWindowText(h,b,b.Capacity);string title=b.ToString().Trim();if(title.Length==0||!GetWindowRect(h,out var r)||r.Right-r.Left<40||r.Bottom-r.Top<40)return true;WindowList.Items.Add(new WindowItem{Handle=h,Title=$"{number++}. {title}",Bounds=new Rect(r.Left,r.Top,r.Right-r.Left,r.Bottom-r.Top)});return true;},IntPtr.Zero);if(WindowList.Items.Count>0)WindowList.SelectedIndex=0;}
 void Accept(){if(WindowList.SelectedItem is not WindowItem x)return;SelectedHandle=x.Handle;Selection=x.Bounds;DialogResult=true;}
 void Capture_Click(object s,RoutedEventArgs e)=>Accept();void WindowList_DoubleClick(object s,System.Windows.Input.MouseButtonEventArgs e)=>Accept();void Refresh_Click(object s,RoutedEventArgs e)=>LoadWindows();void Window_KeyDown(object s,System.Windows.Input.KeyEventArgs e){if(e.Key==Key.Escape){DialogResult=false;Close();}}
}
