using EnvDTE;
using System.IO;
using System.Linq;

namespace DocsMaster.VSIX
{
    [Command(PackageIds.MyCommand)]
    internal sealed class MyCommand : BaseCommand<MyCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            var projects = await VS.Solutions.GetAllProjectsAsync();

            Configuration.DocsInfo docsInfo = new Configuration.DocsInfo(
                Path.GetDirectoryName((await VS.Solutions.GetCurrentSolutionAsync()).FullPath), projects.ToList());

            var docsBuilder = new Engine.DocumentationBuilder(docsInfo.Map());

#if DEBUG
            await docsBuilder.BuildAsyncDebug();
#else
            await docsBuilder.BuildAsync();
#endif

        }
    }
}