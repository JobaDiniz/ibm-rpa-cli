﻿using Moq;
using System.CommandLine;
using System.Globalization;

namespace Joba.IBM.RPA.Cli.Tests
{
    public class ServerSelectorShould
    {
        [Fact]
        public async Task ThrowException_WhenServerVersionIsNotSupported()
        {
            //arrange
            var supportedVersion = RpaCommand.SupportedServerVersion;
            var url = "https://dev.com/v1.0/";
            var address = new ServerAddress(url);
            var server = new ServerConfig { Version = new Version("20.12.0"), Deployment = DeploymentOption.SaaS, AuthenticationMethod = AuthenticationMethod.WDG };
            var console = new Mock<IConsole>();
            var client = new Mock<IRpaClient>();
            client.Setup(c => c.GetConfigurationAsync(It.IsAny<CultureInfo>(), It.IsAny<CancellationToken>())).ReturnsAsync(server);
            var clientFactory = new Mock<IRpaClientFactory>();
            clientFactory.Setup(c => c.CreateFromAddress(It.Is<Uri>(u => u.AbsoluteUri == url))).Returns(client.Object);
            var project = new Mock<IProject>();
            var sut = new ServerSelector(supportedVersion, console.Object, clientFactory.Object, project.Object);

            //assert
            _ = await Assert.ThrowsAsync<NotSupportedException>(
                async () => await sut.SelectAsync(address, CancellationToken.None));
        }

        [Fact]
        public async Task ThrowException_WhenDeploymentOptionIsNotSupported()
        {
            //arrange
            var url = "https://dev.com/v1.0/";
            var address = new ServerAddress(url);
            var server = new ServerConfig { Version = RpaCommand.SupportedServerVersion, Deployment = new DeploymentOption("not-supported"), AuthenticationMethod = AuthenticationMethod.WDG };
            var console = new Mock<IConsole>();
            var client = new Mock<IRpaClient>();
            client.Setup(c => c.GetConfigurationAsync(It.IsAny<CultureInfo>(), It.IsAny<CancellationToken>())).ReturnsAsync(server);
            var clientFactory = new Mock<IRpaClientFactory>();
            clientFactory.Setup(c => c.CreateFromAddress(It.Is<Uri>(u => u.AbsoluteUri == url))).Returns(client.Object);
            var project = new Mock<IProject>();
            var sut = new ServerSelector(RpaCommand.SupportedServerVersion, console.Object, clientFactory.Object, project.Object);

            //assert
            _ = await Assert.ThrowsAsync<NotSupportedException>(
                async () => await sut.SelectAsync(address, CancellationToken.None));
        }

        [Fact]
        public async Task ThrowException_WhenAuthenticationMethodIsNotSupported()
        {
            //arrange
            var url = "https://dev.com/v1.0/";
            var address = new ServerAddress(url);
            var server = new ServerConfig { Version = RpaCommand.SupportedServerVersion, Deployment = DeploymentOption.SaaS, AuthenticationMethod = new AuthenticationMethod("not-supported") };
            var console = new Mock<IConsole>();
            var client = new Mock<IRpaClient>();
            client.Setup(c => c.GetConfigurationAsync(It.IsAny<CultureInfo>(), It.IsAny<CancellationToken>())).ReturnsAsync(server);
            var clientFactory = new Mock<IRpaClientFactory>();
            clientFactory.Setup(c => c.CreateFromAddress(It.Is<Uri>(u => u.AbsoluteUri == url))).Returns(client.Object);
            var project = new Mock<IProject>();
            var sut = new ServerSelector(RpaCommand.SupportedServerVersion, console.Object, clientFactory.Object, project.Object);

            //assert
            _ = await Assert.ThrowsAsync<NotSupportedException>(
                async () => await sut.SelectAsync(address, CancellationToken.None));
        }
    }
}
