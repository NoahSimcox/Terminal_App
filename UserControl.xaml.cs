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
using testcmd;

namespace Terminal_App
{

    public sealed partial class UserControl
    {
        
        private string _dirText = "C:\\>";
        private StreamReader _streamReader = new StreamReader(Path.Combine(AppContext.BaseDirectory, "cmdCommands.txt"));
        private List<string> _commands;
        private int _selectedItemIndex = 0;


        public UserControl()
        {
            string contents = _streamReader.ReadToEnd();
            _commands = contents.Split(",").ToList();
        }
        

        private void KeyDownEvent(object sender, KeyRoutedEventArgs e)
        {

            if (e.Key == VirtualKey.Enter)
            {
                // Task.Run(async () => await _pseudoConsole.SendCommand(InputBox.Text, _cts.Token));

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

        private void KeyUpEvent(object sender, KeyRoutedEventArgs e)
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
        

        private UIElement CreateNewTerminalInstance()
        {
            // Return a fresh Grid that has the full terminal layout (clone your original Grid)
            var terminalGrid = new Grid();
            foreach (var item in EnclosingGrid.Children)
                terminalGrid.Children.Add(item);

            // Add rows, columns, textboxes, etc. like in your XAML
            return terminalGrid;
        }

    }
}