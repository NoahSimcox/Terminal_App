using Octokit;

namespace Terminal_App.GitClient;

public class GitClient
{
    private string _token; // shouldnt be stored in memory in the future, use OAuth

    private string _currentRepoName;
    private string _currentBranch;

    private void GetClient() =>
        new GitHubClient(new ProductHeaderValue("BallerTerminalGitHubClient")) { Credentials = new Credentials(_token) };

    public async void Commit(string message)
    {
        // var branchRef = await 
    }

}