using System;
using System.Windows;
using FDNJ_CSharp.Views;

namespace FDNJ_CSharp;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Crucial: prevent WPF from terminating when LoginWindow closes
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        DispatcherUnhandledException += (s, args) =>
        {
            MessageBox.Show($"Произошла ошибка:\n\n{args.Exception.Message}\n\n{args.Exception.StackTrace}",
                "Ошибка приложения DRIVE", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                MessageBox.Show($"Критический сбой:\n\n{ex.Message}\n\n{ex.StackTrace}",
                    "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };

        try
        {
            var loginWnd = new LoginWindow();
            bool? dialogResult = loginWnd.ShowDialog();

            if (dialogResult == true && loginWnd.AuthenticatedUser != null)
            {
                var mainWnd = new MainWindow(loginWnd.AuthenticatedUser);
                MainWindow = mainWnd;
                ShutdownMode = ShutdownMode.OnMainWindowClose;
                mainWnd.Show();
            }
            else
            {
                Shutdown();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка инициализации главного окна:\n\n{ex.Message}\n\n{ex.StackTrace}",
                "Ошибка запуска", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }
}
