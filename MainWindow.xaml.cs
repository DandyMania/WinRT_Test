using System;
using System.Windows;
using System.Reflection;

// MessageDialog
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.UI.Popups;
using System.Windows.Interop;
using WinRT;


// toast
using Windows.UI.Notifications;
using System.IO;

namespace WinRT_Test
{
    /// <summary>
    /// 'IAsyncAction' は、参照されていないアセンブリに定義されていますと言われるので自前で定義
    /// https://www.moonmile.net/blog/archives/8584
    /// </summary>
    public static class TaskEx
    {
        public static Task<T> AsTask<T>(this IAsyncOperation<T> operation)
        {
            var tcs = new TaskCompletionSource<T>();
            operation.Completed = delegate  //--- コールバックを設定
            {
                switch (operation.Status)   //--- 状態に合わせて完了通知
                {
                    case AsyncStatus.Completed: tcs.SetResult(operation.GetResults()); break;
                    case AsyncStatus.Error: tcs.SetException(operation.ErrorCode); break;
                    case AsyncStatus.Canceled: tcs.SetCanceled(); break;
                }
            };
            return tcs.Task;  //--- 完了が通知されるTaskを返す
        }
        public static TaskAwaiter<T> GetAwaiter<T>(this IAsyncOperation<T> operation)
        {
            return operation.AsTask().GetAwaiter();
        }
    }


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// WPFからMessageDialogを呼ぶ場合のおまじない
        /// https://qiita.com/okazuki/items/227f8d19e38a67099006
        /// </summary>
        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("3E68D4BD-7135-4D10-8018-9FB6D9F33FA1")]
        public interface IInitializeWithWindow
        {
            void Initialize(IntPtr hwnd);
        }

        /// <summary>
        /// メッセージダイアログ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void MessageDialog_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new MessageDialog("メッセージ", "タイトル");
            // It doesn't work on .NET 5
            // ((IInitializeWithWindow)(object)dlg).Initialize(new WindowInteropHelper(this).Handle);
            var withWindow = dlg.As<IInitializeWithWindow>(); ;
            withWindow.Initialize(new WindowInteropHelper(Application.Current.MainWindow).Handle);

            await dlg.ShowAsync();
        }


        /// <summary>
        /// トースト
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Toast_Click(object sender, RoutedEventArgs e)
        {
            var template = ToastTemplateType.ToastImageAndText04;
            var content = ToastNotificationManager.GetTemplateContent(template);
            var images = content.GetElementsByTagName("image");
            var src = images[0].Attributes.GetNamedItem("src");
            src.InnerText = "file:///" + Path.GetFullPath("sample.jpg"); ;

            var texts = content.GetElementsByTagName("text");
            texts[0].AppendChild(content.CreateTextNode("Title"));
            texts[1].AppendChild(content.CreateTextNode("ToastMessage"));

            // AppIDの代わりにアセンブリ名を突っ込んでおく
            Assembly assembly = Assembly.GetExecutingAssembly();
            AssemblyName asmName = assembly.GetName();

            var notifier = ToastNotificationManager.CreateToastNotifier(asmName.Name);
            notifier.Show(new ToastNotification(content));
        }
    }
}
