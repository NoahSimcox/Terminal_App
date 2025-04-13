using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Terminal_App
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {

       private string dirText = "C:\\>";

       private StreamReader reader = new StreamReader(Path.Combine(AppContext.BaseDirectory, "cmdCommands.txt"));
       private List<string> commands;
       private int selectedItemIndex = 0;
        
        public MainWindow()
        {
            this.InitializeComponent();
            Directory2.Text = dirText;

            string contents = reader.ReadToEnd();
            commands = contents.Split(",").ToList();
        }

        private void KeyDownEvent(object sender, KeyRoutedEventArgs e)
        {

            if (e.Key == VirtualKey.Enter)
            {
                TextBox outputBox = OutputText;
                outputBox.Text += dirText +" "+ InputBox.Text;
                
                if (outputBox.Text == OutputText.Text)
                    outputBox.Text += " " + "\n";
                else
                    outputBox.Text += "\n";
                
                InputBox.Text = "";
                
                ScrollViewer.ChangeView(null, ScrollViewer.ExtentHeight, null);
            }
            

            if (e.Key == VirtualKey.Tab && AutocompletePopup.IsOpen)
            {
                InputBox.Text = SuggestionsList.Items[selectedItemIndex].ToString();
                InputBox.Focus(FocusState.Programmatic);
                e.Handled = true;
                InputBox.SelectionStart = InputBox.Text.Length;
            }
            
            if (e.Key == VirtualKey.Up && AutocompletePopup.IsOpen)
            {
                if (selectedItemIndex > 0)
                    selectedItemIndex--;

                
            } else if (e.Key == VirtualKey.Down && AutocompletePopup.IsOpen)
            {
                if (selectedItemIndex < SuggestionsList.Items.Count - 1)
                    selectedItemIndex++;

            }
            else 
                selectedItemIndex = 0;

            
            SuggestionsList.SelectedIndex = selectedItemIndex;

        }

        private void KeyUpEvent(object sender, KeyRoutedEventArgs e)
        {
            string text = InputBox.Text;
            
            if (string.IsNullOrEmpty(text))
            {
                AutocompletePopup.IsOpen = false;
                return;
            }
            
            var matches = commands.Where(cmd => cmd.StartsWith(text, StringComparison.OrdinalIgnoreCase)).ToList();
            
            if (matches.Count > 0)
            {
                SuggestionsList.ItemsSource = matches;
                AutocompletePopup.IsOpen = true;
            }
            else 
                AutocompletePopup.IsOpen = false;
            
        }

        private void MouseClicked(Object sender, PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint(SuggestionsList).Properties.IsLeftButtonPressed)
            {
                InputBox.Text = SuggestionsList.SelectedItem.ToString();
            }
        }
        
        private void AutocompletePopup_Opened(object sender, object e)
        {
            InputBox.Focus(FocusState.Programmatic);
        }

    }
}
