using Documentation.Models;
using Documentation.Services;
using EnvDTE;
using System.IO;

namespace Documentation
{
    [Command(PackageIds.MyCommand)]
    internal sealed class MyCommand : BaseCommand<MyCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            var projects = await VS.Solutions.GetAllProjectsAsync();

            DocsInfo docsInfo = new DocsInfo(
                projects,
                Path.Combine(Path.GetDirectoryName((await VS.Solutions.GetCurrentSolutionAsync()).FullPath),
                "docs"));

            DocumentationBuilder docsBuilder = new DocumentationBuilder(docsInfo);


#if DEBUG
            await docsBuilder.BuildAsyncDebug();
#else
            await docsBuilder.BuildAsync();
#endif

        }
    }
}