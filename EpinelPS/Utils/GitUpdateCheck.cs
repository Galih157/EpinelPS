using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

namespace EpinelPS.Utils;

public class GitUpdateCheck
{
    public static string GitCommit;

    static GitUpdateCheck()
    {
        GitCommit = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "<Unknown>";
        int index = GitCommit.IndexOf("+");
        if (index != -1) GitCommit = GitCommit.Substring(index + 1);
        else GitCommit = "<Unknown>";
    }

    public static async Task CheckForUpdates()
    {
        string extractPath = AppDomain.CurrentDomain.BaseDirectory + "Server_Update/";
        if (Directory.Exists(extractPath)) Directory.Delete(extractPath, true);
        if (Debugger.IsAttached) return;

        HttpClient client = new HttpClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "EpinelPS");
        var response = await client.GetAsync("https://api.github.com/repos/EpinelPS/EpinelPS/actions/workflows/dotnet-desktop.yml/runs?branch=main&status=success&per_page=1");

        if (response.IsSuccessStatusCode)
        {
            var jsonText = await response.Content.ReadAsStringAsync();
            GithubActionsResponse? json = JsonSerializer.Deserialize<GithubActionsResponse>(jsonText);
            if (json != null && json.workflow_runs.Count > 0)
            {
                var run = json.workflow_runs[0];

                if (GitCommit != run.head_commit.id)
                {
                    Logging.Warn($"An update is available (current Git commit: {GitCommit}, latest Git commit: {run.head_commit.id}).");
                }
            }
        }
        else
        {
            Logging.Warn("Failed to check for updates");
        }
    }

    public class GithubActionsResponseWorkflow
    {
        public string artifacts_url { get; set; } = "";

        public GithubActionsResponseHeadCommit head_commit { get; set; } = new();
    }
    public class GithubActionsResponseHeadCommit
    {
        public string id { get; set; } = "";
    }

    private class GithubActionsResponse
    {
        public List<GithubActionsResponseWorkflow> workflow_runs { get; set; } = [];
    }
}
/// <summary>
/// Copied from https://stackoverflow.com/a/46497896
/// </summary>
public static class StreamExtensions
{
    public static async Task CopyToAsync(this Stream source, Stream destination, int bufferSize, IProgress<long>? progress = null, CancellationToken cancellationToken = default)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (!source.CanRead)
            throw new ArgumentException("Has to be readable", nameof(source));
        if (destination == null)
            throw new ArgumentNullException(nameof(destination));
        if (!destination.CanWrite)
            throw new ArgumentException("Has to be writable", nameof(destination));
        if (bufferSize < 0)
            throw new ArgumentOutOfRangeException(nameof(bufferSize));

        var buffer = new byte[bufferSize];
        long totalBytesRead = 0;
        int bytesRead;
        while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
        {
            await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
            totalBytesRead += bytesRead;
            progress?.Report(totalBytesRead);
        }
    }
}
