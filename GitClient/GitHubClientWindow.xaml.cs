
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace Terminal_App.GitClient;

public partial class GitHubClientWindow : Window
{
    private GitHubClient _client;
    
    public GitHubClientWindow()
    {
        _client = new GitHubClient();
        InitializeComponent();
        LoadSettings();
    }

    public string RepoDirectory
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


    private readonly string _settingsFilePath = "githubClientSettings.txt";
    private void OnClosed(object sender, WindowEventArgs args)
    {
        using StreamWriter writer = new StreamWriter(_settingsFilePath);
        writer.WriteLine(RepoDirectory);
        writer.WriteLine(User);
        writer.WriteLine(Email);
        
        writer.Flush();
        writer.Close();
        
    }
    
    private void LoadSettings()
    {
        if (!Directory.Exists(_settingsFilePath)) return;
        
        using StreamReader reader = new StreamReader(new FileStream(_settingsFilePath, FileMode.Open));
        RepoDirectory = reader.ReadLine() ?? "";
        User = reader.ReadLine() ?? "";
        Email = reader.ReadLine() ?? "";
        
        reader.Close();
    }
}