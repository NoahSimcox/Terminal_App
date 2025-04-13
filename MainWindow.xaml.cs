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
       public List<PseudoConsole> PseudoConsoles = [];
       private Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue;
       public List<CancellationTokenSource> CancellationTokenSources = [];
       public List<SemaphoreSlim> SemaphoreSlims = [];
       public ConcurrentQueue<byte[]> _command =new();
       public void AddConsole()
       {
           var newTab = new TabViewItem();
           newTab.Header = $"Terminal {TerminalTabs.TabItems.Count + 1}";
           var userControl = new UserControl(TerminalTabs.TabItems.Count,this);
           newTab.Content = userControl;
           var pseudoConsole = new PseudoConsole((3000, 3000), ((short)300,(short)300),Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
           CancellationTokenSource cts = new CancellationTokenSource();
           SemaphoreSlims.Add(new SemaphoreSlim(0,1));
           CancellationTokenSources.Add(cts);
           TerminalTabs.TabItems.Add(newTab);

           int id = TerminalTabs.TabItems.Count - 1;
           Task.Run(async()=>await pseudoConsole.BufferLoop(cts.Token));
           Task.Run(async()=>
           {
               while(!cts.IsCancellationRequested)
               {
                   await Task.Delay(1000);
                   if(ActiveTab != id)
                   {
                       continue;
                   }
                   _dispatcherQueue.TryEnqueue(() =>
                   {
                       // double vertiOffset = ScrollViewer.VerticalOffset;
                       userControl.OutputText.Text = pseudoConsole.Buffer.PrintString();
                       // ScrollViewer.ChangeView(null,vertiOffset,null);
                   });
               }
           });
           Task.Run(async()=>
           {
               while(!cts.IsCancellationRequested)
               {
                   await SemaphoreSlims[id].WaitAsync();
                   if(_command.TryDequeue(out var result))
                   {
                        
                       await pseudoConsole.SendInput(result, cts.Token);
                   }else{
                        
                       await pseudoConsole.SendCommand("yo the command is null", cts.Token);
                   }
               } 
                
           });
           TerminalTabs.SelectedItem = newTab;
       }
        public MainWindow()
        {
            _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            InitializeComponent();

            // _pseudoConsole.Buffer.TextBox = OutputText;
        }

        private void TerminalTabs_AddTabButtonClick(TabView sender, object args)
        {
            AddConsole();
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
