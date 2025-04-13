using System;
using System.Linq;
using LibGit2Sharp;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Terminal_App.GitClient;

public partial class MergeDiffWindow : Window
{

    private Conflict _conflict;
    
    public MergeDiffWindow(Conflict conflict, Repository repo)
    {
        InitializeComponent();
        
        _conflict = conflict;

        Blob baseBlob = repo.Lookup<Blob>(conflict.Ancestor.Id);
        Blob yourBlob = repo.Lookup<Blob>(conflict.Ours.Id);
        Blob theirBlob = repo.Lookup<Blob>(conflict.Theirs.Id);
        
        string[] baseLines = baseBlob.GetContentText().Split(["\n", "\n\r", "\r"], StringSplitOptions.None);
        string[] yourLines = yourBlob.GetContentText().Split(["\n", "\n\r", "\r"], StringSplitOptions.None);
        string[] theirLines = theirBlob.GetContentText().Split(["\n", "\n\r", "\r"], StringSplitOptions.None);

        // add all of base and then add changed lines later to the you and they grids
        ContentChanges youBaseDiff = repo.Diff.Compare(yourBlob, baseBlob);
        ContentChanges themBaseDiff = repo.Diff.Compare(theirBlob, baseBlob);

        int[] yourAdditionLineNumbers = new int[youBaseDiff.AddedLines.Count];
        for (int i = 0; i < youBaseDiff.AddedLines.Count; i++)
        {
            AddButton(youBaseDiff.AddedLines[i]);
            AddText(YourGrid, youBaseDiff.AddedLines[i].Content, youBaseDiff.AddedLines[i].LineNumber);
            AddText(BaseGrid, "<><><><>", youBaseDiff.AddedLines[i].LineNumber);
            yourAdditionLineNumbers[i] = youBaseDiff.AddedLines[i].LineNumber;
        }

        int[] theirAdditionLineNumbers = new int[themBaseDiff.AddedLines.Count];
        for (int i = 0; i < themBaseDiff.AddedLines.Count; i++)
        {
            AddButton(themBaseDiff.AddedLines[i]);
            AddText(TheirGrid, themBaseDiff.AddedLines[i].Content, themBaseDiff.AddedLines[i].LineNumber);
            AddText(BaseGrid, "><><><><", themBaseDiff.AddedLines[i].LineNumber);
            theirAdditionLineNumbers[i] = themBaseDiff.AddedLines[i].LineNumber;
        }
        
        
        int lines = Math.Max(Math.Max(theirLines.Length, yourLines.Length), baseLines.Length);
        int baseCurrentLine = 0;
        int yourCurrentLine = 0;
        int theirCurrentLine = 0;
        for (int i = 0; i < lines; i++)
        {
            if (yourAdditionLineNumbers.Contains(i))
            {
                yourCurrentLine++;
                continue;
            }

            if (theirAdditionLineNumbers.Contains(i))
            {
                theirCurrentLine++;
                continue;
            }
            
            // at this point the line must be empty (no additions)
            AddText(BaseGrid, baseLines[baseCurrentLine++], baseCurrentLine);
            AddText(YourGrid, yourLines[yourCurrentLine++], yourCurrentLine);
            AddText(TheirGrid, theirLines[theirCurrentLine++], theirCurrentLine);

        }



    }
    
    private void AddButton(Line line)
    {
        Button addButton = new Button
        {
            Content = "+",
            Tag = line
        };
        // addButton.Click +=
            
        Grid.SetColumn(addButton, 1);
        Grid.SetRow(addButton, line.LineNumber);
        YourGrid.Children.Add(addButton);
    }

    private void AddText(Grid grid, string text, int line)
    {
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto});
        TextBlock block = new TextBlock { Text = text };
        Grid.SetRow(block, line);
        Grid.SetColumn(block, 0);
        grid.Children.Add(block);
    }
    
}

