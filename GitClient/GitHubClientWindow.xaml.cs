
using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace Terminal_App.GitClient;

public partial class GitHubClientWindow : Window
{
    private GitHubClient _client;
    
    public GitHubClientWindow()
    {
        _client = new GitHubClient();
        InitializeComponent();
    }

    public string Directory
    {
        get => _client.Directory;
        set => _client.Directory = value;
    }

    public string User
    {
        get => _client.Username;
        set => _client.Username = value;
    }

    public string Email
    {
        get => _client.Email;
        set => _client.Email = value;
    }

    public string CommitMessage;
    public string[] ChangedFiles => _client.GetChangedFiles();

    public void Commit() => _client.Commit(CommitMessage, _client.GetChangedFiles()); 
    public async Task Push() => await _client.Push();
    
    public async Task Pull() => await _client.Pull();



}