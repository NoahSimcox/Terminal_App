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
using Windows.UI.Core;
using Microsoft.UI;
using Terminal_App.GitClient;
using testcmd;

namespace Terminal_App
{

    public sealed partial class UserControl
    {
        private StreamReader _streamReader = new StreamReader(Path.Combine(AppContext.BaseDirectory, "cmdCommands.txt"));
        private List<string> _commands;
        private int _selectedItemIndex = 0;
        public int Id;
        public TextBox OutputText => _OutputText;
        public MainWindow MainWindow;


        public UserControl(int id, MainWindow mainWindow)
        {
            Id = id;
            MainWindow = mainWindow;
            InitializeComponent();
            string contents = _streamReader.ReadToEnd();
            _commands = contents.Split(",").ToList();
            Directory2.Text = "Command: ";
        }
        

        private new void KeyDownEvent(object sender, KeyRoutedEventArgs e)
        {

            if (e.Key == VirtualKey.Enter)
            {
                MainWindow._command.Enqueue( Encoding.ASCII.GetBytes(InputBox.Text+"\r\n"));
                    try
                    {                
                        MainWindow.SemaphoreSlims[Id].Release();
                    }catch{}
                InputBox.Text = "";
            }

            
            if (e.Key == VirtualKey.Tab && AutocompletePopup.IsOpen)
            {
                AutocompletePopup.IsOpen = false;
                InputBox.Text = SuggestionsList.Items[_selectedItemIndex].ToString();
                InputBox.Focus(FocusState.Programmatic);
                e.Handled = true;
                InputBox.SelectionStart = InputBox.Text.Length;
            }
            
            
            if (e.Key == VirtualKey.Up && AutocompletePopup.IsOpen)
            {
                if (_selectedItemIndex > 0)
                    _selectedItemIndex--;
            
            }
            else if (e.Key == VirtualKey.Down && AutocompletePopup.IsOpen)
            {
                if (_selectedItemIndex < SuggestionsList.Items.Count - 1)
                    _selectedItemIndex++;
            
            }
            else
                _selectedItemIndex = 0;
            
            
            SuggestionsList.SelectedIndex = _selectedItemIndex;
            SuggestionsList.SelectedItem = SuggestionsList.Items[_selectedItemIndex];
            
            SuggestionsList.UpdateLayout();
        
        }
        
        private new void KeyUpEvent(object sender, KeyRoutedEventArgs e)
        {
            string text = InputBox.Text;
            
            if (string.IsNullOrEmpty(text) || e.Key == VirtualKey.Tab)
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
            if (e.Key == VirtualKey.Back)
            {
                MainWindow._command.Enqueue([0x7f]);
                try
                {
                    MainWindow.SemaphoreSlims[Id].Release();
                }
                catch
                {
                }

                return;
            }

            uint result = MapVirtualKey((uint)e.Key, MAPVK_VK_TO_CHAR);
            if (result != 0)
            {
                bool b = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift)
                    .HasFlag(CoreVirtualKeyStates.Down);
                var character = (char)result;
                if (!b)
                {
                    result = Char.ToLower(character);
                }
                else
                {
                    result = Char.ToUpper(character);
                }

                MainWindow._command.Enqueue([(byte)result]);
                try
                {
                    MainWindow.SemaphoreSlims[Id].Release();
                }
                catch
                {
                }
            }

        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            Window clientWindow = new GitHubClientWindow();
            clientWindow.Activate();
        }
        

        private const uint MAPVK_VK_TO_CHAR = 2;
 

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);
    }
}