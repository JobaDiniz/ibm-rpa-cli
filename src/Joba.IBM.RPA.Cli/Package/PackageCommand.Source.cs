﻿namespace Joba.IBM.RPA.Cli
{
    partial class PackageCommand
    {
        [RequiresProject]
        internal class PackageSourceCommand : Command
        {
            public const string CommandName = "source";
            public PackageSourceCommand() : base(CommandName, "Adds a package source")
            {
                var alias = new Argument<string>("alias", "The source name");
                var region = new Option<string>("--region", "The region of the package source");
                var userName = new Option<string>("--userName", "The user name to authenticate, usually the e-mail, to use for authentication");
                var tenant = new Option<int?>("--tenant", "The tenant code to use for authentication");

                AddArgument(alias);
                AddOption(region);
                AddOption(userName);
                AddOption(tenant);
                this.SetHandler(HandleAsync,
                    new RemoteOptionsBinder(alias, region, userName, tenant),
                    Bind.FromServiceProvider<Project>(),
                    Bind.FromServiceProvider<InvocationContext>());
            }

            private async Task HandleAsync(RemoteOptions options, Project project, InvocationContext context)
            {
                var cancellation = context.GetCancellationToken();
                using var regionSelector = new RegionSelector();
                var region = await regionSelector.SelectAsync(options.RegionName, cancellation);

                using var client = RpaClientFactory.CreateFromRegion(region);
                var accountSelector = new AccountSelector(client.Account);
                var credentials = await accountSelector.SelectAsync(options.UserName, options.TenantCode, cancellation);

                var package = await project.PackageSources.AddAsync(client.Account, options.Alias, region, credentials, cancellation);
                await project.SaveAsync(cancellation);

                ExtendedConsole.WriteLine($"Package source added:");
                ExtendedConsole.WriteLine($"  {package.Alias:blue} ({package.Remote.TenantName}), [{package.Remote.Region:blue}]({package.Remote.Address})");
            }
        }
    }
}