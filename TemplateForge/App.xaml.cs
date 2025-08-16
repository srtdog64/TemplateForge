using System;
using System.Windows;

namespace TemplateForge
{
    /// <summary>
    /// App.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class App : Application
    {
        // FastMCP는 나중에 추가 예정
        public static object FastMcpClient { get; private set; } = null;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 메인 윈도우 표시
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
        }
    }
}
