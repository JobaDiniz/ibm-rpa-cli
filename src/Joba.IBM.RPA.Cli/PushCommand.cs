using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joba.IBM.RPA.Cli
{
    [RequiresProject]
    internal class PushCommand : Command
    {
        public const string CommandName = "push";
        public PushCommand() : base(CommandName, "Push wal files as scripts to the environment without modifying them.")
        {
            var searchPattern = new Argument<string>("searchPattern", "The search string to match against the names of wal files in current directory. For all wal files use '*.wal'.");
            var environmentName = new Option<string>("--env", "The alias of the environment to push wal files to.") { Arity = ArgumentArity.ExactlyOne };
            var properties = new Option<IEnumerable<string>?>(new[] { "--property", "-p" }, $"A key-value pair property to specify script parameters such as timeout and 'production version'. Read more in the documentation.") { AllowMultipleArgumentsPerToken = true };

            AddArgument(searchPattern);
            AddOption(environmentName);
            AddOption(properties);

            this.SetHandler(HandleAsync, searchPattern, environmentName,
                new PropertyOptionsBinder(properties),
                Bind.FromLogger<PushCommand>(),
                Bind.FromServiceProvider<IRpaClientFactory>(),
                Bind.FromServiceProvider<IProject>(),
                Bind.FromServiceProvider<InvocationContext>());
        }

        private async Task HandleAsync(string searchPattern, string environmentName, PropertyOptions properties, ILogger<PushCommand> logger,
            IRpaClientFactory clientFactory, IProject project, InvocationContext context)
        {
            var service = new PushService(logger, project, clientFactory);
            await service.PushAsync(environmentName, searchPattern, new DirectoryInfo(System.Environment.CurrentDirectory), properties, context.GetCancellationToken());
        }
    }
}
