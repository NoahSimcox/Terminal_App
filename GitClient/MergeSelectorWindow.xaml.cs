using System.IO;
using LibGit2Sharp;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Terminal_App.GitClient;

public partial class MergeSelectorWindow : Window
{
    private ConflictCollection _conflicts;
    private string _selectedConflictPath = "";
    private Repository _repo;
    private string _repoPath;

    private Signature _signature;
    private ToggleButton? _previousButton = null;
    
    
    public MergeSelectorWindow(ConflictCollection conflicts, Repository repo, string repoPath, Signature signature)
    {
        InitializeComponent(); // noah kys // an addition
        _conflicts = conflicts;
        _repo = repo;
        _repoPath = repoPath;
        _signature = signature;
        
        foreach (Conflict c in conflicts) // a line that is different sdhfgksjdfhkjashdkjashdkjh
        {
            ToggleButton b = new ToggleButton
            {
                Content = c.Ours.Path,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            b.Click += SelectConflict;
            MergeButtons.Children.Add(b);

        }
    }

    public void SelectConflict(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton { Content: string content } button)
        {
            _selectedConflictPath = content;
            if (_previousButton != null)
                _previousButton.IsChecked = false;

            _previousButton = button;
        }
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
        Window mergeWindow = new MergeDiffWindow(_conflicts[_selectedConflictPath], _repo);
        mergeWindow.Activate();
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        _repo.Commit("merge", _signature, _signature);
    }
}

