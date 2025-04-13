using System.IO;
using LibGit2Sharp;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Terminal_App.GitClient;

public partial class MergeSelectorWindow : Window
{
    private ConflictCollection _conflicts;
    private string _selectedConflictPath = "";
    private Repository _repo;
    private string _repoPath;
    
    public MergeSelectorWindow(ConflictCollection conflicts, Repository repo, string repoPath)
    {
        InitializeComponent();
        _conflicts = conflicts;
        _repo = repo;
        _repoPath = repoPath;
        
        foreach (Conflict c in conflicts)
        {
            Button b = new Button
            {
                Content = c.Ours.Path,
            };
            b.Click += SelectConflict;
            MergeButtons.Children.Add(b);

        }
    }

    public void SelectConflict(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Content: string content })
            _selectedConflictPath = content;
    }

    public void AcceptTheirs()
    {
        Conflict c = _conflicts[_selectedConflictPath];
        Blob theirs = _repo.Lookup<Blob>(c.Theirs.Id);
        
        string mergedContent = theirs.GetContentText();
        string path = Path.Combine(_repoPath, c.Ours.Path);
        File.WriteAllText(path, mergedContent);
        
        Commands.Stage(_repo, path);
    }

    public void AcceptYours()
    {
        Conflict c = _conflicts[_selectedConflictPath];
        Blob theirs = _repo.Lookup<Blob>(c.Ours.Id);
        
        string mergedContent = theirs.GetContentText();
        string path = Path.Combine(_repoPath, c.Ours.Path);
        File.WriteAllText(path, mergedContent);
        
        Commands.Stage(_repo, path);
    }

    public void Merge()
    {
        
    }
    
}

