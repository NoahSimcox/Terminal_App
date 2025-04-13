﻿using System;
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

        int[] yourAdditionLineNumbers = new int[youBaseDiff.LinesAdded];
        for (int i = 0; i < youBaseDiff.LinesAdded; i++)
        {
            AddButton(YourGrid, youBaseDiff.AddedLines[i]);
            AddText(YourGrid, youBaseDiff.AddedLines[i].Content, youBaseDiff.AddedLines[i].LineNumber);
            AddText(BaseGrid, "<><><><>", youBaseDiff.AddedLines[i].LineNumber);
            yourAdditionLineNumbers[i] = youBaseDiff.AddedLines[i].LineNumber;
        }

        int[] yourRemovalLineNumbers = new int[youBaseDiff.LinesDeleted];
        for (int i = 0; i < youBaseDiff.LinesDeleted; i++)
        {
            AddButton(YourGrid, youBaseDiff.DeletedLines[i]);
            AddText(YourGrid, " line to remove here", youBaseDiff.DeletedLines[i].LineNumber);
            AddText(BaseGrid, youBaseDiff.DeletedLines[i].Content, youBaseDiff.DeletedLines[i].LineNumber);
            yourRemovalLineNumbers[i] = youBaseDiff.DeletedLines[i].LineNumber;
        }

        int[] theirAdditionLineNumbers = new int[themBaseDiff.LinesAdded];
        for (int i = 0; i < themBaseDiff.LinesAdded; i++)
        {
            AddButton(YourGrid, themBaseDiff.AddedLines[i]);
            AddText(TheirGrid, themBaseDiff.AddedLines[i].Content, themBaseDiff.AddedLines[i].LineNumber);
            AddText(BaseGrid, "><><><><", themBaseDiff.AddedLines[i].LineNumber);
            theirAdditionLineNumbers[i] = themBaseDiff.AddedLines[i].LineNumber;
        }
        
        int[] theirRemovalLineNumbers = new int[youBaseDiff.LinesDeleted];
        for (int i = 0; i < themBaseDiff.LinesDeleted; i++)
        {
            AddButton(TheirGrid, themBaseDiff.DeletedLines[i]);
            AddText(TheirGrid, " line to remove here", themBaseDiff.DeletedLines[i].LineNumber);
            AddText(BaseGrid, themBaseDiff.DeletedLines[i].Content, themBaseDiff.DeletedLines[i].LineNumber);
            theirRemovalLineNumbers[i] = themBaseDiff.DeletedLines[i].LineNumber;
        }
        
        
        int lines = Math.Max(Math.Max(theirLines.Length, yourLines.Length), baseLines.Length);
        int baseCurrentLine = 0;
        int yourCurrentLine = 0;
        int theirCurrentLine = 0;
        for (int i = 0; i < lines; i++)
        {
            bool shouldContinue = false;
            if (yourAdditionLineNumbers.Contains(yourCurrentLine) && yourRemovalLineNumbers.Contains(yourCurrentLine))
            {
                AddText(YourGrid, yourLines[yourCurrentLine++], i);
                AddText(BaseGrid, baseLines[baseCurrentLine++], i);
                shouldContinue = true;
            }
            
            if (theirAdditionLineNumbers.Contains(theirCurrentLine) && theirRemovalLineNumbers.Contains(theirCurrentLine))
            {
                AddText(TheirGrid, theirLines[theirCurrentLine++], i);
                if (!shouldContinue)
                    AddText(BaseGrid, baseLines[baseCurrentLine++], i);
                shouldContinue = true;
            }
            
            if (shouldContinue) continue;

            if (yourAdditionLineNumbers.Contains(yourCurrentLine))
            {
                AddText(YourGrid, yourLines[yourCurrentLine++], i);
                AddText(BaseGrid, " added line goes here", i);
                shouldContinue = true;
            }

            if (theirAdditionLineNumbers.Contains(theirCurrentLine))
            {
                AddText(TheirGrid, yourLines[yourCurrentLine++], i);
                if (!shouldContinue)
                    AddText(BaseGrid, " added line goes here", i);
                shouldContinue = true;
            }
            if (shouldContinue) continue;
            
            if (yourRemovalLineNumbers.Contains(yourCurrentLine))
            {
                AddText(YourGrid, " removed line", i);
                AddText(BaseGrid, baseLines[baseCurrentLine++], i);
                shouldContinue = true;
            }

            if (theirAdditionLineNumbers.Contains(theirCurrentLine))
            {
                AddText(TheirGrid, " removed line", i);
                if (!shouldContinue)
                    AddText(BaseGrid, baseLines[baseCurrentLine++], i);
                shouldContinue = true;
            }
            if (shouldContinue) continue;
            
            // at this point the line must be empty (no additions)
            AddText(BaseGrid, baseLines[baseCurrentLine++], i);
            AddText(YourGrid, yourLines[yourCurrentLine++], i);
            AddText(TheirGrid, theirLines[theirCurrentLine++], i);

        }



    }
    
    private void AddButton(Grid grid, Line line)
    {
        Button addButton = new Button
        {
            Content = "+",
            Tag = line,
            Width = 10,
            Height = 10
        };
        // addButton.Click +=
            
        Grid.SetColumn(addButton, 1);
        Grid.SetRow(addButton, line.LineNumber);
        grid.Children.Add(addButton);
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

