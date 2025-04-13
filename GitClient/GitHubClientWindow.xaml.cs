
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LibGit2Sharp;
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
        RefreshChangedFilesText();
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

    public void RefreshChangedFilesText()
    {
        try { ChangedFilesText.Text = string.Join('\n', _client.GetChangedFiles()); }
        catch (Exception e) { ChangedFilesText.Text = e.Message; }
    }
        
    public void Commit()
    {
        _client.Commit(CommitMessage, _client.GetChangedFiles());
        RefreshChangedFilesText();
    }

    public async Task Push() => await _client.Push();

    public async Task Pull()
    {
        ConflictCollection? conflicts = await _client.Pull();
        
        if (conflicts == null) return;

        // open merge window
        foreach (Conflict c in conflicts)
        {
            
        }
    }
    


    // private readonly string _settingsFilePath = Path.Combine(AppContext.BaseDirectory, "githubClientSettings.txt");
    private void OnClosed(object sender, WindowEventArgs args)
    {
        // StreamWriter writer = new StreamWriter(_settingsFilePath);
        // writer.WriteLine("RepoDirectory");
        // writer.WriteLine(User);
        // writer.WriteLine(Email);
        //
        // // File.WriteAllLines(_settingsFilePath, [RepoDirectory, User, Email]);
        //
        // writer.Flush();
        // writer.Close();
        
    }
    
    private void LoadSettings()
    {
        // if (!Directory.Exists(_settingsFilePath)) return;
        //
        // string[] settings = File.ReadAllLines(_settingsFilePath);
        // // StreamReader reader = new StreamReader(new FileStream(_settingsFilePath, FileMode.Open));
        // RepoDirectory = settings[0];
        // User = settings[1];
        // Email = settings[2];
        //
        // // reader.Close();
    }
}