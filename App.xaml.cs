using System.Windows;

namespace MiddleCV
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        System.Threading.Mutex procMutex;
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            procMutex = new System.Threading.Mutex(true, "_MIDDLE_CV_MUTEX", out var result);
            if (!result)
            {
                Current.Shutdown();
                System.Diagnostics.Process.GetCurrentProcess().Kill();
                return;
            }
            MainWindow = new MainWindow();
            MainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            procMutex?.ReleaseMutex();
        }
    }
}
