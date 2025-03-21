﻿using static Joba.IBM.RPA.GitConfigurator;

namespace Joba.IBM.RPA.Tests
{
    public class GitAttributesShould
    {
        [Fact]
        public async Task UpdatePattern()
        {
            //arrange
            var cliName = "rpa";
            var directoryName = Path.GetFullPath(@"assets/gitattributes");
            var file = new FileInfo(@$"{directoryName}/{nameof(UpdatePattern)}.txt");
            var gitAttributes = new GitAttributes(file, cliName);

            //act
            await gitAttributes.ConfigureAsync(CancellationToken.None);

            //assert
            await VerifyFile(file)
                .UseDirectory(directoryName)
                .UseFileName(Path.GetFileNameWithoutExtension(file.Name));
        }

        [Fact]
        public async Task AddPattern()
        {
            //arrange
            var cliName = "rpa";
            var directoryName = Path.GetFullPath(@"assets/gitattributes");
            var file = new FileInfo(@$"{directoryName}/{nameof(AddPattern)}.txt");
            var gitAttributes = new GitAttributes(file, cliName);

            //act
            await gitAttributes.ConfigureAsync(CancellationToken.None);

            //assert
            await VerifyFile(file)
                .UseDirectory(directoryName)
                .UseFileName(Path.GetFileNameWithoutExtension(file.Name));
        }
    }
}
