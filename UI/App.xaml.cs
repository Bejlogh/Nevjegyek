using System;
using System.Windows;

namespace Névjegyzék
{
    public partial class App : Application
    {
        [STAThread]
        public static void Main()
        {
            App app = new App();
            app.InitializeComponent();
            LanguageManager.InitializeLanguage();
            app.Run();
        }
    }
}