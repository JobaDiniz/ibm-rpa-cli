﻿using Joba.Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using System.CommandLine;
using System.Globalization;
using Xunit.Abstractions;
using static Joba.IBM.RPA.Cli.PackageCommand;

namespace Joba.IBM.RPA.Cli.Tests
{
    [Trait("Category", "Integration")]
    public class AddPackageSourceHandlerShould : RequireProjectTest
    {
        private readonly ILogger logger;

        public AddPackageSourceHandlerShould(ITestOutputHelper output) => logger = new XunitLogger(output);

        [Fact]
        public async Task ThrowException_WhenPackageSourceAlreadyExists()
        {
            //arrange
            var console = new Mock<IConsole>();
            var clientFactory = new Mock<IRpaClientFactory>();
            var authenticatorFactory = new Mock<IAccountAuthenticatorFactory>();
            var workingDir = new DirectoryInfo(Path.GetFullPath(Path.Combine("assets", $"{nameof(AddPackageSourceHandlerShould)}", nameof(ThrowException_WhenPackageSourceAlreadyExists))));
            var project = await LoadProjectAsync(workingDir);
            var sut = new AddPackageSourceHandler(logger, project, console.Object, clientFactory.Object, new DefaultSecretProvider(), authenticatorFactory.Object);
            var options = new RemoteOptions("dev", new ServerAddress());
            var properties = new PropertyOptions();

            //assert
            _ = await Assert.ThrowsAsync<ProjectException>(
                async () => await sut.HandleAsync(options, properties, CancellationToken.None));
        }

        [Fact]
        public async Task ThrowException_WhenEnvironmentAlreadyExists()
        {
            //arrange
            var console = new Mock<IConsole>();
            var clientFactory = new Mock<IRpaClientFactory>();
            var authenticatorFactory = new Mock<IAccountAuthenticatorFactory>();
            var workingDir = new DirectoryInfo(Path.GetFullPath(Path.Combine("assets", $"{nameof(AddPackageSourceHandlerShould)}", nameof(ThrowException_WhenEnvironmentAlreadyExists))));
            var project = await LoadProjectAsync(workingDir);
            var sut = new AddPackageSourceHandler(logger, project, console.Object, clientFactory.Object, new DefaultSecretProvider(), authenticatorFactory.Object);
            var options = new RemoteOptions("dev", new ServerAddress());
            var properties = new PropertyOptions();

            //assert
            _ = await Assert.ThrowsAsync<ProjectException>(
                async () => await sut.HandleAsync(options, properties, CancellationToken.None));
        }

        [Fact]
        public async Task CreatePackageSourcesFile()
        {
            var workingDir = new DirectoryInfo(Path.GetFullPath(Path.Combine("assets", $"{nameof(AddPackageSourceHandlerShould)}", nameof(CreatePackageSourcesFile))));
            workingDir.Create();
            try
            {
                //arrange
                var properties = new PropertyOptions();
                var options = new RemoteOptions("dev", new ServerAddress("https://dev.ibm.com"), "us1", "username", 500, "password");
                var region = new Region(options.RegionName!, options.Address.ToUri());
                var config = new ServerConfig { Regions = new Region[] { region }, Version = RpaCommand.SupportedServerVersion, Deployment = DeploymentOption.SaaS, AuthenticationMethod = AuthenticationMethod.WDG };
                var session = new CreatedSession { TenantCode = options.TenantCode!.Value, TenantName = "development" };
                var credentials = new AccountCredentials(options.TenantCode!.Value, options.UserName!, options.Password!);
                var console = new Mock<IConsole>();
                var client = new Mock<IRpaClient>();
                client.Setup(c => c.GetConfigurationAsync(It.IsAny<CultureInfo>(), It.IsAny<CancellationToken>())).ReturnsAsync(config);
                var clientFactory = new Mock<IRpaClientFactory>();
                clientFactory.Setup(c => c.CreateFromAddress(region.ApiAddress)).Returns(client.Object);
                clientFactory.Setup(c => c.CreateFromRegion(region)).Returns(client.Object);
                var authenticator = new Mock<IAccountAuthenticator>();
                authenticator.Setup(a => a.AuthenticateAsync(credentials, It.IsAny<CancellationToken>())).ReturnsAsync(session);
                var authenticatorFactory = new Mock<IAccountAuthenticatorFactory>();
                authenticatorFactory.Setup(a => a.Create(config.Deployment, config.AuthenticationMethod, region, properties)).Returns(authenticator.Object);
                var project = await LoadProjectAsync(workingDir);
                var sut = new AddPackageSourceHandler(logger, project, console.Object, clientFactory.Object, new DefaultSecretProvider(), authenticatorFactory.Object);

                //act
                await sut.HandleAsync(options, properties, CancellationToken.None);

                //assert
                var filePath = Path.Combine(workingDir.FullName, $"{project.Name}.sources.json");
                Assert.True(File.Exists(filePath), $"File {filePath} should have been created");
                await VerifyFile(filePath)
                   .UseDirectory(Path.GetFullPath(Path.Combine("assets", $"{nameof(AddPackageSourceHandlerShould)}", nameof(CreatePackageSourcesFile))))
                   .UseFileName($"{project.Name}.sources");
            }
            finally
            {
                workingDir.Delete(true);
            }
        }

        [Fact]
        public async Task UpdatePackageSourcesFile()
        {
            var workingDir = new DirectoryInfo(Path.GetFullPath(Path.Combine("assets", $"{nameof(AddPackageSourceHandlerShould)}", nameof(UpdatePackageSourcesFile))));
            workingDir.Create();
            try
            {
                //arrange
                var properties = new PropertyOptions();
                var options = new RemoteOptions("qa", new ServerAddress("https://qa.ibm.com"), "us1qa", "username", 501, "password");
                var region = new Region(options.RegionName!, options.Address.ToUri());
                var config = new ServerConfig { Regions = new Region[] { region }, Version = RpaCommand.SupportedServerVersion, Deployment = DeploymentOption.SaaS, AuthenticationMethod = AuthenticationMethod.WDG };
                var session = new CreatedSession { TenantCode = options.TenantCode!.Value, TenantName = "quality assurance" };
                var credentials = new AccountCredentials(options.TenantCode!.Value, options.UserName!, options.Password!);
                var console = new Mock<IConsole>();
                var client = new Mock<IRpaClient>();
                client.Setup(c => c.GetConfigurationAsync(It.IsAny<CultureInfo>(), It.IsAny<CancellationToken>())).ReturnsAsync(config);
                var clientFactory = new Mock<IRpaClientFactory>();
                clientFactory.Setup(c => c.CreateFromAddress(region.ApiAddress)).Returns(client.Object);
                clientFactory.Setup(c => c.CreateFromRegion(region)).Returns(client.Object);
                var authenticator = new Mock<IAccountAuthenticator>();
                authenticator.Setup(a => a.AuthenticateAsync(credentials, It.IsAny<CancellationToken>())).ReturnsAsync(session);
                var authenticatorFactory = new Mock<IAccountAuthenticatorFactory>();
                authenticatorFactory.Setup(a => a.Create(config.Deployment, config.AuthenticationMethod, region, properties)).Returns(authenticator.Object);
                var project = await LoadProjectAsync(workingDir);
                var sut = new AddPackageSourceHandler(logger, project, console.Object, clientFactory.Object, new DefaultSecretProvider(), authenticatorFactory.Object);

                //act
                await sut.HandleAsync(options, properties, CancellationToken.None);

                //assert
                await VerifyFile(Path.Combine(workingDir.FullName, $"{project.Name}.sources.json"))
                   .UseDirectory(Path.GetFullPath(Path.Combine("assets", $"{nameof(AddPackageSourceHandlerShould)}", nameof(UpdatePackageSourcesFile))))
                   .UseFileName($"{project.Name}.sources");
            }
            finally
            {
                workingDir.Delete(true);
            }
        }

        [Fact]
        public async Task UpdatePackageSourcesFile_WithCloudPakOpenShift()
        {
            var workingDir = new DirectoryInfo(Path.GetFullPath(Path.Combine("assets", $"{nameof(AddPackageSourceHandlerShould)}", nameof(UpdatePackageSourcesFile_WithCloudPakOpenShift))));
            workingDir.Create();
            try
            {
                //arrange
                var properties = PropertyOptions.Parse($"{PropertyOptions.CloudPakConsoleAddress}=https://cloudpakconsole.ibm.com/");
                var options = new RemoteOptions("qa", new ServerAddress("https://qa.ibm.com"), "us1qa", "username", 501, "password");
                var region = new Region(options.RegionName!, options.Address.ToUri());
                var config = new ServerConfig { Regions = new Region[] { region }, Version = RpaCommand.SupportedServerVersion, Deployment = DeploymentOption.OCP, AuthenticationMethod = AuthenticationMethod.OIDC };
                var session = new CreatedSession { TenantCode = options.TenantCode!.Value, TenantName = "quality assurance" };
                var credentials = new AccountCredentials(options.TenantCode!.Value, options.UserName!, options.Password!);
                var console = new Mock<IConsole>();
                var client = new Mock<IRpaClient>();
                client.Setup(c => c.GetConfigurationAsync(It.IsAny<CultureInfo>(), It.IsAny<CancellationToken>())).ReturnsAsync(config);
                var clientFactory = new Mock<IRpaClientFactory>();
                clientFactory.Setup(c => c.CreateFromAddress(region.ApiAddress)).Returns(client.Object);
                clientFactory.Setup(c => c.CreateFromRegion(region)).Returns(client.Object);
                var authenticator = new Mock<IAccountAuthenticator>();
                authenticator.Setup(a => a.AuthenticateAsync(credentials, It.IsAny<CancellationToken>())).ReturnsAsync(session);
                var authenticatorFactory = new Mock<IAccountAuthenticatorFactory>();
                authenticatorFactory.Setup(a => a.Create(config.Deployment, config.AuthenticationMethod, region, properties)).Returns(authenticator.Object);
                var project = await LoadProjectAsync(workingDir);
                var sut = new AddPackageSourceHandler(logger, project, console.Object, clientFactory.Object, new DefaultSecretProvider(), authenticatorFactory.Object);

                //act
                await sut.HandleAsync(options, properties, CancellationToken.None);

                //assert
                await VerifyFile(Path.Combine(workingDir.FullName, $"{project.Name}.sources.json"))
                   .UseDirectory(Path.GetFullPath(Path.Combine("assets", $"{nameof(AddPackageSourceHandlerShould)}", nameof(UpdatePackageSourcesFile_WithCloudPakOpenShift))))
                   .UseFileName($"{project.Name}.sources");
            }
            finally
            {
                workingDir.Delete(true);
            }
        }
    }
}