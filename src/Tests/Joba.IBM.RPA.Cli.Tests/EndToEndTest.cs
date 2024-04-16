using Joba.Xunit;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Xunit.Abstractions;

namespace Joba.IBM.RPA.Cli.Tests
{
    [Trait("Category", "e2e")]
    public class EndToEndTest
    {
        private readonly ILogger logger;

        public EndToEndTest(ITestOutputHelper output)
        {
            logger = new XunitLogger(output);
        }

        [Fact]
        public async Task PushScriptsWithoutModification()
        {
            var workingDirectory = new DirectoryInfo(Path.Combine(System.Environment.CurrentDirectory, "assets/e2e"));
            ClearWorkingDirectory(workingDirectory);
            try
            {
                var parameters = ReadParameters();
                await RunAsync($"project new {parameters.ProjectName} -d '{parameters.ProjectDescription}'", workingDirectory);
                await RunAsync($"env new target --url {parameters.TargetOptions.ApiUrl} --region {parameters.TargetOptions.Region} --userName {parameters.TargetOptions.Username} --tenant {parameters.TargetOptions.TenantCode}", workingDirectory);
                await RunAsync("push *.wal --env target", workingDirectory);
            }
            finally
            {
                ClearWorkingDirectory(workingDirectory);
            }
        }

        [Fact]
        public async Task CreateAndDeployProject()
        {
            var workingDirectory = new DirectoryInfo(Path.Combine(System.Environment.CurrentDirectory, "e2e"));
            workingDirectory.Create();
            ClearWorkingDirectory(workingDirectory);
            try
            {
                //TODO: need to create computer+computergroups+chat since QA by default is empty.
                var parameters = ReadParameters();
                await RunAsync($"project new {parameters.ProjectName} -d '{parameters.ProjectDescription}'", workingDirectory);
                await RunAsync($"env new source --url {parameters.SourceOptions.ApiUrl} --region {parameters.SourceOptions.Region} --userName {parameters.SourceOptions.Username} --tenant {parameters.SourceOptions.TenantCode}", workingDirectory);
                await RunAsync($"env new target --url {parameters.TargetOptions.ApiUrl} --region {parameters.TargetOptions.Region} --userName {parameters.TargetOptions.Username} --tenant {parameters.TargetOptions.TenantCode}", workingDirectory);
                await RunAsync($"package source package --url {parameters.PackageOptions.ApiUrl} --region {parameters.PackageOptions.Region} --userName {parameters.PackageOptions.Username} --tenant {parameters.PackageOptions.TenantCode}", workingDirectory);
                await RunAsync("package install Joba*", workingDirectory);
                await RunAsync("pull e2e* --env source", workingDirectory);
                await RunAsync("bot new attended --template attended", workingDirectory);
                await RunAsync($"bot new unattended --template unattended -p:computer-group={parameters.Unattended.ComputerGroup}", workingDirectory);
                await RunAsync($"bot new e2e --template chatbot -p:handle={parameters.Chat.Handle} -p:name=e2e -p:computers={parameters.Chat.Computers}", workingDirectory);

                await RunAsync("deploy target", workingDirectory);
            }
            finally
            {
                ClearWorkingDirectory(workingDirectory);
            }
        }

        private E2eParameters ReadParameters()
        {
            return new E2eParameters(
                "e2e",
                "e2e",
                new E2eRemoteOptions(
                    GetAndAssertEnvironmentVariable("E2E_SOURCE_URL"),
                    GetAndAssertEnvironmentVariable("E2E_SOURCE_REGION"),
                    Convert.ToInt32(GetAndAssertEnvironmentVariable("E2E_SOURCE_TENANT")),
                    GetAndAssertEnvironmentVariable("E2E_SOURCE_USERNAME")),
                new E2eRemoteOptions(
                    GetAndAssertEnvironmentVariable("E2E_TARGET_URL"),
                    GetAndAssertEnvironmentVariable("E2E_TARGET_REGION"),
                    Convert.ToInt32(GetAndAssertEnvironmentVariable("E2E_TARGET_TENANT")),
                    GetAndAssertEnvironmentVariable("E2E_TARGET_USERNAME")),
                new E2eRemoteOptions(
                    GetAndAssertEnvironmentVariable("E2E_PACKAGE_URL"),
                    GetAndAssertEnvironmentVariable("E2E_PACKAGE_REGION"),
                    Convert.ToInt32(GetAndAssertEnvironmentVariable("E2E_PACKAGE_TENANT")),
                    GetAndAssertEnvironmentVariable("E2E_PACKAGE_USERNAME")),
                new E2eChat(GetAndAssertEnvironmentVariable("E2E_TARGET_CHAT_HANDLE"), GetAndAssertEnvironmentVariable("E2E_TARGET_CHAT_COMPUTERS")),
                new E2eUnattended(GetAndAssertEnvironmentVariable("E2E_TARGET_COMPUTERGROUP")));

            static string GetAndAssertEnvironmentVariable(string variable) =>
                System.Environment.GetEnvironmentVariable(variable) ?? throw new InvalidOperationException($"The environment variable '{variable}' is required and was not found.");
        }

        private async Task RunAsync(string arguments, DirectoryInfo workingDirectory)
        {
            var exitCode = await StartProcessAsync(arguments, workingDirectory);
            Assert.True(exitCode >= 0);
        }

        private async Task<int> StartProcessAsync(string arguments, DirectoryInfo workingDirectory)
        {
            logger.LogInformation($"rpa {arguments}");
            arguments = $"{arguments} -v Diagnostic";
            var envVarName = "RPA_EXECUTABLE_PATH";
            var path = System.Environment.GetEnvironmentVariable(envVarName) ?? throw new InvalidOperationException($"The environment variable {envVarName} was not set.");
            var info = new ProcessStartInfo(Path.GetFullPath(path), arguments)
            {
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                WorkingDirectory = workingDirectory.FullName
            };

            using var process = Process.Start(info) ?? throw new Exception($"Could not start '{info.FileName} {info.Arguments}'");
            process.ErrorDataReceived += OnError;
            process.BeginErrorReadLine();
            var output = await process.StandardOutput.ReadToEndAsync();
            if (!string.IsNullOrWhiteSpace(output))
                logger.LogInformation(output);
            await process.WaitForExitAsync();

            return process.ExitCode;
        }

        private void OnError(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
                logger.LogError($"Process error: {e.Data}");
        }

        private static void ClearWorkingDirectory(DirectoryInfo workingDirectory)
        {
            var rpaDir = new DirectoryInfo(Path.Combine(workingDirectory.FullName, ProjectFile.HiddenDirectoryName));
            if (rpaDir.Exists)
                rpaDir.Delete(true);
            var packagesDir = new DirectoryInfo(Path.Combine(workingDirectory.FullName, PackageSourcesFile.PackagesDirectoryName));
            if (packagesDir.Exists)
                packagesDir.Delete(true);

            var projectFiles = workingDirectory.EnumerateFiles(ProjectFile.Extension);
            foreach (var file in projectFiles)
                file.Delete();

            var packageSourceFiles = workingDirectory.EnumerateFiles(PackageSourcesFile.Extension);
            foreach (var file in packageSourceFiles)
                file.Delete();

            var walFiles = workingDirectory.EnumerateFiles(WalFile.Extension);
            foreach (var file in walFiles)
                file.Delete();
        }

        record struct E2eParameters(string ProjectName, string ProjectDescription, E2eRemoteOptions SourceOptions, E2eRemoteOptions TargetOptions, E2eRemoteOptions PackageOptions, E2eChat Chat, E2eUnattended Unattended);
        record struct E2eRemoteOptions(string ApiUrl, string Region, int TenantCode, string Username);
        record struct E2eChat(string Handle, string Computers);
        record struct E2eUnattended(string ComputerGroup);
    }
}
