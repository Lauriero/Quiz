using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Quiz.Support
{
    class Extensions
    {
        public static void ExecuteInApplicationThread(Action action) {
            Application.Current.Dispatcher.BeginInvoke(action, DispatcherPriority.ApplicationIdle);
        }
    }
}
