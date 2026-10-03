using System.Configuration;
using System.Data;
using System.Windows;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace RestaurantApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly string _logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup-exceptions.log");

        public App()
        {
            // UI thread exceptions
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;

            // Non-UI thread exceptions
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // Task exceptions
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogException("DispatcherUnhandledException", e.Exception);
            // Let the app crash after logging to make the failure visible in debugger
            e.Handled = false;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogException("CurrentDomain_UnhandledException", ex);
            }
            else
            {
                LogMessage("CurrentDomain_UnhandledException: non-exception object thrown");
            }
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            LogException("UnobservedTaskException", e.Exception);
            // Don't mark as observed so failures remain visible during development
            e.SetObserved();
        }

        private void LogException(string kind, Exception ex)
        {
            try
            {
                var text = $"[{DateTime.Now:O}] {kind}: {ex}\n";
                File.AppendAllText(_logPath, text);
            }
            catch
            {
                // Swallow logging errors to avoid recursive failures
            }
        }

        private void LogMessage(string message)
        {
            try
            {
                var text = $"[{DateTime.Now:O}] {message}\n";
                File.AppendAllText(_logPath, text);
            }
            catch
            {
            }
        }
    }

}
