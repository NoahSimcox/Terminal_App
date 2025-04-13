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
        ContentChanges youBaseDiff = repo.Diff.Compare(baseBlob, yourBlob);
        ContentChanges themBaseDiff = repo.Diff.Compare(baseBlob, yourBlob);

        int[] yourAdditionLineNumbers = new int[youBaseDiff.LinesAdded];
        for (int i = 0; i < youBaseDiff.LinesAdded; i++)
        {
            AddButton(YourGrid, youBaseDiff.AddedLines[i]);
            yourAdditionLineNumbers[i] = youBaseDiff.AddedLines[i].LineNumber;
        }

        int[] yourRemovalLineNumbers = new int[youBaseDiff.LinesDeleted];
        for (int i = 0; i < youBaseDiff.LinesDeleted; i++)
        {
            AddButton(YourGrid, youBaseDiff.DeletedLines[i]);
            yourRemovalLineNumbers[i] = youBaseDiff.DeletedLines[i].LineNumber;
        }

        int[] theirAdditionLineNumbers = new int[themBaseDiff.LinesAdded];
        for (int i = 0; i < themBaseDiff.LinesAdded; i++)
        {
            AddButton(YourGrid, themBaseDiff.AddedLines[i]);
            theirAdditionLineNumbers[i] = themBaseDiff.AddedLines[i].LineNumber;
        }
        
        int[] theirRemovalLineNumbers = new int[youBaseDiff.LinesDeleted];
        for (int i = 0; i < themBaseDiff.LinesDeleted; i++)
        {
            AddButton(TheirGrid, themBaseDiff.DeletedLines[i]);
            theirRemovalLineNumbers[i] = themBaseDiff.DeletedLines[i].LineNumber;
        }
        
        
        int lines = Math.Max(Math.Max(theirLines.Length, yourLines.Length), baseLines.Length);
        int baseCurrentLine = 0;
        int yourCurrentLine = 0;
        int theirCurrentLine = 0;

        try
        {
            for (int i = 0; i < lines; i++)
            {
                string? yt = null;
                string? tt = null;
                string? bt = null;
                
                if (yourAdditionLineNumbers.Contains(yourCurrentLine) &&
                    yourRemovalLineNumbers.Contains(yourCurrentLine))
                {
                    AddButton(YourGrid, yourLines[yourCurrentLine], i); // technically should have 2 options
                    yt = yourLines[yourCurrentLine++];
                    bt = baseLines[baseCurrentLine++];
                }
                else if (yourAdditionLineNumbers.Contains(yourCurrentLine))
                {
                    AddButton(YourGrid, yourLines[yourCurrentLine], i);
                    yt = yourLines[yourCurrentLine++];
                    bt = " >>>> ";
                }
                else if (yourRemovalLineNumbers.Contains(baseCurrentLine))
                {
                    AddButton(YourGrid, "", i);
                    yt = " >--- ";
                    bt = baseLines[baseCurrentLine++];
                }

                if (theirAdditionLineNumbers.Contains(theirCurrentLine) &&
                    theirRemovalLineNumbers.Contains(theirCurrentLine))
                { 
                    AddButton(TheirGrid, theirLines[theirCurrentLine], i);
                    tt = theirLines[theirCurrentLine++];
                    if (bt == null)
                        bt = baseLines[baseCurrentLine++];
                }
                else if (theirAdditionLineNumbers.Contains(theirCurrentLine))
                {
                    AddButton(TheirGrid, theirLines[theirCurrentLine], i);
                    tt = theirLines[theirCurrentLine++];
                    if (bt == null)
                        bt = " <<<< ";
                }
                else if (theirAdditionLineNumbers.Contains(baseCurrentLine))
                {
                    AddButton(TheirGrid, "", i);
                    tt = " ---< ";
                    if (bt == null)
                        bt = baseLines[baseCurrentLine++];
                }

                if (bt == null)
                    bt = baseLines[baseCurrentLine++];
                if (tt == null)
                    tt = theirLines[theirCurrentLine++];
                if (yt == null)
                    yt = yourLines[yourCurrentLine++];
                
                // at this point the line must be empty (no additions)
                AddText(BaseGrid, bt, i);
                AddText(YourGrid, yt, i);
                AddText(TheirGrid, tt, i);

            }
        }
        catch (IndexOutOfRangeException e)
        {
            
        }



    }
    
    private void AddButton(Grid grid, Line line)
    {
        AddButton(grid, line.Content, line.LineNumber);
    }

    private void AddButton(Grid grid, string content, int lineNumber)
    {
        Button addButton = new Button
        {
            Content = "+",
            Tag = (content, lineNumber),
            Width = 10,
            Height = 10
        };
        // addButton.Click +=
            
        Grid.SetColumn(addButton, 1);
        Grid.SetRow(addButton, lineNumber);
        grid.Children.Add(addButton);
    }

    private void AddText(Grid grid, string text, int line)
    {
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto});
        TextBlock block = new TextBlock { Text = text};
        Grid.SetRow(block, line);
        Grid.SetColumn(block, 0);
        grid.Children.Add(block);
    }//
    
}

