using Joba.IBM.RPA.Server;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Joba.IBM.RPA
{
    public class PushService
    {
        private readonly ILogger logger;
        private readonly IProject project;
        private readonly IRpaClientFactory clientFactory;

        public PushService(ILogger logger, IProject project, IRpaClientFactory clientFactory)
        {
            this.logger = logger;
            this.project = project;
            this.clientFactory = clientFactory;
        }

        public async Task PushAsync(string environmentName, string searchPattern, DirectoryInfo directory, PropertyOptions properties, CancellationToken cancellation)
        {
            var client = clientFactory.CreateFromEnvironment(project.Environments[environmentName]);
            
            var files = directory.EnumerateFiles(searchPattern, SearchOption.AllDirectories).ToArray();
            if (files.Any(f => f.Extension != WalFile.Extension))
                throw new Exception("The search pattern yielded files that are not wal files.");

            var timeout = properties["timeout"] is not null ? TimeSpan.ParseExact(properties["timeout"]!, @"hh\:mm\:ss", CultureInfo.CurrentCulture) : TimeSpan.FromMinutes(5);
            var setAsProduction = properties["prod"] is not null ? bool.Parse(properties["prod"]!) : false;
            foreach (var wal in files.Select(WalFile.Read))
            {
                logger.LogDebug("Publishing '{Script}'", wal.Name);

                PublishScript model;
                var publishComment = $"New version from {project.Name} project published as-is at {DateTime.UtcNow} from {System.Environment.MachineName}";
                var latest = await client.Script.GetLatestVersionAsync(wal.Name.WithoutExtension, cancellation);
                if (latest == null)
                    model = wal.PrepareToPublish(publishComment, timeout, resetIds: true, setAsProduction: setAsProduction);
                else
                {
                    wal.Overwrite(latest.ScriptId, latest.Id, latest.Version);
                    model = wal.PrepareToPublish(publishComment, timeout, setAsProduction: setAsProduction);
                }

                _ = await client.Script.PublishAsync(model, cancellation);
            }
        }
    }
}
