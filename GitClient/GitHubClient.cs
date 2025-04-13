using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using LibGit2Sharp;
using Newtonsoft.Json.Linq;

namespace Terminal_App.GitClient;

public class GitHubClient
{
    private string _username = "";
    private string _email = "";

    private string? _oauthToken;

    private string _localRepoDirectory = "";
    private Signature Signature => new (new Identity(_username, _email), DateTime.Now);
    

    public string Username
    {
        get => _username;
        set => _username = value;
    }

    public string Directory
    {
        get => _localRepoDirectory;
        set => _localRepoDirectory = value;
    }

    public string Email
    {
        get => _email;
        set => _email = value;
    }

    public void Commit(string message, string[] changedFilePaths)
    {
        using Repository repo = new Repository(_localRepoDirectory);

        foreach (string path in changedFilePaths)
            Commands.Stage(repo, path);
        
        Signature author = Signature;
        repo.Commit(message, author, author);
    }

    // Im assuming these should be hidden but idrk
    private const string ClientId = "Ov23liGTnZI82SWMVEBY";
    private const string ClientSecret = "8a99a32b64d02c7cf6b2b73def249cd31d84c737";
    private const string CallbackUrl = "http://localhost:5000/auth";

    private async Task<string> GetOauthToken()
    {
        if (!string.IsNullOrEmpty(_oauthToken))
            return _oauthToken;
        
        string authUrl = $"https://github.com/login/oauth/authorize?client_id={ClientId}&redirect_uri={CallbackUrl}&scope=repo";
        Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });
        
        string accessToken = await ListenForOAuthCallbackAsync();
        
        if (string.IsNullOrEmpty(accessToken))
            throw new Exception("null or empty token");

        _oauthToken = accessToken;
        return accessToken;

    }
    
    public async Task Push()
    {
        string accessToken = await GetOauthToken();

        using var repo = new Repository(_localRepoDirectory);
        var options = new PushOptions
        {
            CredentialsProvider = (_, _, _) => new UsernamePasswordCredentials
            {
                Username = "oauth",
                Password = accessToken
            }
        };

        try {
            // Push the current branch
            repo.Network.Push(repo.Head, options);
        }
        catch (NonFastForwardException e)
        {
            // prompt the user to pull/merge or cancel 
            await Pull();
        }
    }

    private async Task<string> ListenForOAuthCallbackAsync()
    {
        var listener = new HttpListener();
        listener.Prefixes.Add(CallbackUrl + "/");
        listener.Start();

        // Wait asynchronously for the OAuth callback
        var context = await listener.GetContextAsync();
        string code = context.Request.QueryString["code"];

        // Send a response to the browser
        var response = context.Response;
        byte[] buffer = "<html><body>Authorization successful! You may close this window.</body></html>"u8.ToArray();
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        response.Close();

        listener.Stop();

        // Exchange code for an access token
        using var client = new WebClient();
        
        client.Headers.Add("Accept", "application/json");
        string responseBody = await client.UploadStringTaskAsync(
            "https://github.com/login/oauth/access_token",
            $"client_id={ClientId}&client_secret={ClientSecret}&code={code}"
        );

        // Parse the access token
        JObject json = JObject.Parse(responseBody);
        return json["access_token"]?.ToString();
    }
    

    public string[] GetChangedFiles()
    {
        using var repo = new Repository(_localRepoDirectory);  
        var status = repo.RetrieveStatus();
        // status.Missing

        return status.Modified.Concat(status.Added).Select(item => item.FilePath).ToArray();
    }
    
    
    public async Task Pull()
    {
        string accessToken = await GetOauthToken();
        
        using Repository repo = new Repository(_localRepoDirectory);
        // Pull options with credentials if needed
        PullOptions pullOptions = new PullOptions
        {
            FetchOptions = new FetchOptions
            {
                CredentialsProvider = (_, _, _) =>
                    new UsernamePasswordCredentials
                    {
                        Username = "oauth",
                        Password = accessToken
                    }
            }
        };

        // Merge options
        MergeOptions mergeOptions = new MergeOptions
        {
            FileConflictStrategy = CheckoutFileConflictStrategy.Merge
        };

        pullOptions.MergeOptions = mergeOptions;
        
        // Perform the pull
        MergeResult result = Commands.Pull(repo, Signature, pullOptions);
        if (result.Status == MergeStatus.Conflicts)
        {
            ConflictCollection conflicts = repo.Index.Conflicts;
            
            foreach (Conflict c in conflicts)
                HandleMerge(repo, c);

            repo.Commit("merge", Signature, Signature);

        }
    }

    private void HandleMerge(Repository repo, Conflict conflict)
    {
        Blob yours = repo.Lookup<Blob>(conflict.Ours.Id);
        Blob original = repo.Lookup<Blob>(conflict.Ancestor.Id);
        Blob theirs = repo.Lookup<Blob>(conflict.Theirs.Id);
        
        string mergedContent = theirs.GetContentText(); //uhh just to this for now

        string path = Path.Combine(_localRepoDirectory, conflict.Ours.Path);
        File.WriteAllText(path, mergedContent);
        
        Commands.Stage(repo, path);
    }

}