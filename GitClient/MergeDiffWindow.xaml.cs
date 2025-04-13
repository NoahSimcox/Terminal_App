using System;
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

        int lines = Math.Max(Math.Max(theirLines.Length, yourLines.Length), baseLines.Length);
        for (int i = 0; i < lines; i++)
        {
            YourGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto});
            TheirGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto});
            BaseGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto});

            TextBlock bt = new TextBlock { Text = baseLines[i] };
            TextBlock yt = new TextBlock { Text = yourLines[i] };
            TextBlock tt = new TextBlock { Text = theirLines[i] };
            
            Grid.SetRow(bt, i);
            Grid.SetColumn(bt, 0);
            BaseGrid.Children.Add(bt);
            Grid.SetRow(yt, i);
            Grid.SetColumn(yt, 0);
            YourGrid.Children.Add(yt);
            Grid.SetRow(tt, i);
            Grid.SetColumn(tt, 0);
            TheirGrid.Children.Add(tt);
        }
        
        ContentChanges youThemDiff = repo.Diff.Compare(yourBlob, theirBlob);
        ContentChanges youBaseDiff = repo.Diff.Compare(yourBlob, baseBlob);
        
        foreach (Line line in youBaseDiff.AddedLines)
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

    }
    
}

