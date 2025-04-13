using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI;
using Microsoft.UI;
using Windows.UI.Core;
using ABI.Microsoft.UI.Input;
using testcmd;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Terminal_App
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {

       private string _dirText = "C:\\>";
       public int ActiveTab;
        public MainWindow()
        {
            _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            _pseudoConsole = new PseudoConsole((1000,300),(200,200),_dispatcherQueue);
            this.InitializeComponent();
            
            // _pseudoConsole.Buffer.TextBox = OutputText;
            // Directory2.Text = _dirText;

            Task.Run(async () => await _pseudoConsole.BufferLoop(_cts.Token));
            
        }
            

        
        private void TerminalTabs_AddTabButtonClick(TabView sender, object args)
        {
            var newTab = new TabViewItem();

            newTab.Header = $"Terminal {TerminalTabs.TabItems.Count + 1}";
            newTab.Content = new UserControl(TerminalTabs.TabItems.Count,this);

            TerminalTabs.TabItems.Add(newTab);
            TerminalTabs.SelectedItem = newTab;
        }
        private const uint MAPVK_VK_TO_CHAR = 2;

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        private void TerminalTabs_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ActiveTab = TerminalTabs.TabItems.IndexOf(TerminalTabs.SelectedItem);
        }
    }


}
