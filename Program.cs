// See https://aka.ms/new-console-template for more information

using NuGP;using Worsoon;
using Worsoon.Core;
using Worsoon.Providers.Configurations;
using Worsoon.Providers.Logs;

Console.WriteLine("Hello, World!");

var log = new LogServiceBuilder()
    .AddFileProvider(e =>
    {
        e.Banner = false;
        e.Cache = 10;
        e.UseConsole = true;
    })
    .Build();
AssertX.AddLog(log);

IConfigurationService configurationService = new ConfigurationServiceBuilder()
    .AddIniFile("E:/NuGP/app.ini")
    .Build();

AssertX.Info("开始工作...");

new NuGPRunner(configurationService).Run();


AssertX.Info("工作结束！");