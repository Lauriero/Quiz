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
        public static void ExcecuteWithAppIdleDispatcher(Action action)
        {
            try {
                Application.Current.Dispatcher.BeginInvoke(action, DispatcherPriority.ApplicationIdle);
            } catch { }  
        }

        /// <summary>
        /// Using with timers
        /// </summary>
        /// <param name="action"></param>
        public static void ExecuteWithNormalDispatcher(Action action)
        {
            try {
                Application.Current.Dispatcher.BeginInvoke(action, DispatcherPriority.Normal);
            } catch { }  
        }
    }
}
