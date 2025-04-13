using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
       private StreamReader _streamReader = new StreamReader(Path.Combine(AppContext.BaseDirectory, "cmdCommands.txt"));
       private List<string> _commands;
       private int _selectedItemIndex = 0;
       public PseudoConsole _pseudoConsole;
       private CancellationTokenSource _cts = new();
        private Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue;
        private ConcurrentQueue<byte[]> _command =new();
        private SemaphoreSlim _autoResetEvent = new(0,1);
        private ScrollViewer GetScrollViewer(DependencyObject parent)
        {
            if (parent is ScrollViewer)
                return (ScrollViewer)parent;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                var result = GetScrollViewer(child);
                if (result != null)
                    return result;
            }
            return null;
        }
        public void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            double fontSize = OutputText.FontSize;
            double height = OutputText.ActualHeight / fontSize;
            TextBlock textBlock = new TextBlock
            {
                Text = "A", // Single character to measure
                FontFamily = new FontFamily("Consolas, Couriers New"), // Replace with your desired monospaced font
                FontSize =  fontSize// Set the desired font size
            };
            
            textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double textWidth = textBlock.DesiredSize.Width;
            double width= OutputText.ActualWidth/textWidth;
             
            short trueSize = (short) Math.Max(width, height);
            _pseudoConsole = new PseudoConsole((3000, 3000), ((short)300,(short)300),Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
            // _pseudoConsole.Buffer.TextBox = OutputText;
            Task.Run(async()=>await _pseudoConsole.BufferLoop(_cts.Token));
            Task.Run(async()=>
            {
                while(!_cts.IsCancellationRequested)
                {
                    await Task.Delay(1000);
                    // if(!_pseudoConsole.Buffer.Dirty)
                    // {
                    //     continue;
                    // }
                    _dispatcherQueue.TryEnqueue(() =>
                    {
                        double vertiOffset = ScrollViewer.VerticalOffset;
                        
                        OutputText.Text = _pseudoConsole.Buffer.PrintString();
                        ScrollViewer.ChangeView(null,vertiOffset,null);
                    });
                }
            });
            Task.Run(async()=>
            {
                while(!_cts.IsCancellationRequested)
                {
                    await _autoResetEvent.WaitAsync();
                    if(_command.TryDequeue(out var result))
                    {
                        
                        await _pseudoConsole.SendInput(result, _cts.Token);
                    }else{
                        
                        await _pseudoConsole.SendCommand("yo the command is null", _cts.Token);
                    }
                } 
                
            });
        }
        public MainWindow()
        {
            _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            this.InitializeComponent();
            
            Directory2.Text = _dirText;

            string contents = _streamReader.ReadToEnd();
            _commands = contents.Split(",").ToList();
            OutputText.Loaded += MainWindow_Loaded;
        }

        private void KeyDownEvent(object sender, KeyRoutedEventArgs e)
        {

            if (e.Key == VirtualKey.Enter)
            {
                _command.Enqueue( Encoding.ASCII.GetBytes(InputBox.Text+"\r\n"));
                try
                {                

                _autoResetEvent.Release();
                }catch{}

                // TextBox outputBox = OutputText;
                // outputBox.Text += _dirText +" "+ InputBox.Text;
                //
                // if (outputBox.Text == OutputText.Text)
                //     outputBox.Text += " " + "\n";
                // else
                //     outputBox.Text += "\n";
                //
                 InputBox.Text = "";
                //
                // ScrollViewer.ChangeView(null, ScrollViewer.ExtentHeight, null);
            }
            

            if (e.Key == VirtualKey.Tab && AutocompletePopup.IsOpen)
            {
                InputBox.Text = SuggestionsList.Items[_selectedItemIndex].ToString();
                InputBox.Focus(FocusState.Programmatic);
                e.Handled = true;
                InputBox.SelectionStart = InputBox.Text.Length;
            }
            
            
            if (e.Key == VirtualKey.Up && AutocompletePopup.IsOpen)
            {
                if (_selectedItemIndex > 0)
                    _selectedItemIndex--;
                
            } else if (e.Key == VirtualKey.Down && AutocompletePopup.IsOpen)
            {
                if (_selectedItemIndex < SuggestionsList.Items.Count - 1)
                    _selectedItemIndex++;
            }else 
                _selectedItemIndex = 0;

            
            SuggestionsList.SelectedIndex = _selectedItemIndex;
        }

        private void KeyUpEvent(object sender, KeyRoutedEventArgs e)
        {
            string text = InputBox.Text;
            
            
            if (string.IsNullOrEmpty(text))
            {
                AutocompletePopup.IsOpen = false;
                return;
            }
            
            var matches = _commands.Where(cmd => cmd.StartsWith(text, StringComparison.OrdinalIgnoreCase)).ToList();
            
            if (matches.Count > 0)
            {
                SuggestionsList.ItemsSource = matches;
                AutocompletePopup.IsOpen = true;
            }
            else 
                AutocompletePopup.IsOpen = false;
            
        }
        
        private void AutocompletePopup_Opened(object sender, object e)
        {
            InputBox.Focus(FocusState.Programmatic);
        }

        private void OutputText_OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if(e.Key == VirtualKey.Back)
            {
                _command.Enqueue([0x7f]);
                try
                {                
                    _autoResetEvent.Release();
                }catch{}
                return;
            }
            uint result = MapVirtualKey((uint)e.Key, MAPVK_VK_TO_CHAR);
            if(result!=0)
            {
                bool b = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
                
                var character = (char)result;
                if(!b)
                {
                    result = Char.ToLower(character);
                }else{
                    result = Char.ToUpper(character);
                }
                _command.Enqueue([(byte)result]);
                try
                {                

                    _autoResetEvent.Release();
                }catch{}
            }
        }
        private const uint MAPVK_VK_TO_CHAR = 2;

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);
    }
}
