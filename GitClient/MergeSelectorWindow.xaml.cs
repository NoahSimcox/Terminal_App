using LibGit2Sharp;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Terminal_App.GitClient;

public partial class MergeSelectorWindow : Window
{
    private ConflictCollection _conflicts;
    
    public MergeSelectorWindow(ConflictCollection conflicts)
    {
        InitializeComponent();
        _conflicts = conflicts;

        
        foreach (Conflict c in conflicts)
        {
            Button b = new Button
            {
                Content = c.Ours.Path,
            };
            // b.Click += what the hell

        }
    }

    public void SelectConflict() {}

    public void AcceptTheirs()
    {
        
    }
    
}

