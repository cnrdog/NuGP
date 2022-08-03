using System.Reflection;
using Worsoon;
using Worsoon.Core;
using Worsoon.Core.Threads;
using Worsoon.Networks;

namespace NuGP;

public class NuGPRunner
{
    private IConfigurationService _configurationService;

    public NuGPRunner(IConfigurationService configurationService)
    {
        _configurationService = configurationService;
    }

    public void Run()
    {
        var plNames = _configurationService.Get<string>("project:names");
        var ns = plNames.Split(',');

        foreach (var s in ns)
        {
            var project = _configurationService.Get<string>("{0}:location".Format(s));

            if (_configurationService.Get<string>("app:action").Equals("build"))
            {
                AssertX.Info("操作 = 编译");
                Build(project, s);
                AssertX.Info("编译 {0} 结束！".Format(project));
            }
            else if (_configurationService.Get<string>("app:action").Equals("pack"))
            {
                AssertX.Info("操作 = 打包");
                Build(project, s);
                AssertX.Info("编译 {0} 结束！".Format(project));
                Pack(project, s);
                AssertX.Info("打包 {0} 结束！".Format(project));
            }
            else if (_configurationService.Get<string>("app:action").Equals("push"))
            {
                AssertX.Info("操作 = 发布");
                Build(project, s);
                AssertX.Info("编译 {0} 结束！".Format(project));
                Pack(project, s);
                AssertX.Info("打包 {0} 结束！".Format(project));
                Push(project, s);
                AssertX.Info("发布 {0} 结束！".Format(project));
            }
            else
            {
                AssertX.Info("未知操作，请检查 app --> action 是否配置正确！");
            }
        }
    }

    private void Build(string path, string name)
    {
        IProcessService service = new ProcessServiceBuilder().Build();
        service.ExecuteNoneQuery(e =>
        {
            e.Command = "dotnet build --configuration Release {0}".Format(path);
            e.Realtime = _configurationService.Get<bool>("app:debug");
            e.OnOutput += AssertX.Info;
        });
    }

    private void Pack(string path, string name)
    {
        IProcessService service = new ProcessServiceBuilder().Build();
        service.ExecuteNoneQuery(e =>
        {
            e.Command = "dotnet pack {0} -c Release --version-suffix {1} -p:PackageVersion={1}".Format(path,
                _configurationService.Get<string>("{0}:version".Format(name)));
            e.Realtime = _configurationService.Get<bool>("app:debug");
            e.OnOutput += AssertX.Info;
        });
    }

    private void Push(string path, string name)
    {
        //抽取文件
        var pkg = "{0}\\bin\\Release\\{1}.{2}.nupkg".Format(path,
            _configurationService.Get<string>("{0}:name".Format(name)),
            _configurationService.Get<string>("{0}:version".Format(name)));
        AssertX.Info(pkg);
        AssertX.Info(pkg.IsFile(true));

        //下载nuget
        int tries = 0;
        while (tries++ < 4)
        {
            if (!"nuget.exe".IsFile(exist: true))
            {
                AssertX.Warn("Nuget不存在正在下载！".Format(tries));
                IHttpService service = new HttpServiceBuilder().Build();
                service.DownloadFile("https://x.newlifex.com/nuget.exe", "./nuget.exe", e => { });
            }

            if ("nuget.exe".IsFile(true))
            {
                AssertX.Info("已找到 nuget.exe 文件！");
                break;
            }

            AssertX.Warn("Nuget不存在且下载失败，正在重新尝试第 {0} 次！".Format(tries));
        }

        IProcessService processService = new ProcessServiceBuilder().Build();
        processService.ExecuteNoneQuery(e =>
        {
            e.Command = "dotnet nuget push {0} -k {1} -s {2}".Format(pkg,
                _configurationService.Get<string>("app:key"),
                _configurationService.Get<string>("app:server"));
            e.CommandPrinter = true;
            e.Realtime = _configurationService.Get<bool>("app:debug");
            e.OnOutput += AssertX.Info;
        });
    }
}