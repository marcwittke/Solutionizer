using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;

namespace Solutionizer {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        public App() {
            InitializeComponent();
        }

        protected override void OnStartup(StartupEventArgs e) {
            if (e.Args.Any(arg => string.Equals(arg, "debug", StringComparison.OrdinalIgnoreCase))) {
                if (!Debugger.IsAttached) {
                    Debugger.Launch();
                }
            }
            base.OnStartup(e);
        }
    }
}