using System;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    internal static class AppNavigator
    {
        private static AppShellForm shell;

        public static void Initialize(AppShellForm appShell)
        {
            shell = appShell ?? throw new ArgumentNullException(nameof(appShell));
        }

        public static void Navigate(Func<Form> screenFactory, bool animated = true)
        {
            if (shell == null)
                throw new InvalidOperationException("App shell has not been initialized.");

            shell.NavigateTo(screenFactory, animated);
        }

        public static void ExitGame()
        {
            shell?.Close();
        }
    }
}
