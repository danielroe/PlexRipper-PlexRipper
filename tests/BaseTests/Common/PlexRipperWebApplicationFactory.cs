using System.Collections.Specialized;
using Autofac;
using Autofac.Extras.Quartz;
using FileSystem.Contracts;
using Logging.Interface;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using PlexRipper.Data;
using PlexRipper.Domain.Autofac;
using PlexRipper.WebAPI.Common;
using Settings.Contracts;

namespace PlexRipper.BaseTests;

public class PlexRipperWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    private readonly string _memoryDbName;
    private readonly MockPlexApi _mockPlexApi;
    private static readonly ILog _log = LogManager.CreateLogInstance(typeof(PlexRipperWebApplicationFactory<>));

    private readonly UnitTestDataConfig _config;

    public PlexRipperWebApplicationFactory(string memoryDbName, Action<UnitTestDataConfig> options = null, MockPlexApi mockPlexApi = null)
    {
        _memoryDbName = memoryDbName;
        _mockPlexApi = mockPlexApi;
        _config = UnitTestDataConfig.FromOptions(options);
    }

    protected override IHostBuilder CreateHostBuilder()
    {
        return PlexRipperHost.Setup();
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // source: https://github.com/autofac/Autofac/issues/1207#issuecomment-850405602
        builder
            .ConfigureContainer<ContainerBuilder>(autoFacBuilder =>
            {
                autoFacBuilder

                    // Database context can be setup once and then retrieved by its DB name.
                    .Register((_, _) => MockDatabase.GetMemoryDbContext(_memoryDbName))
                    .As<PlexRipperDbContext>()
                    .InstancePerDependency();

                autoFacBuilder.RegisterModule<TestModule>();

                SetMockedDependencies(autoFacBuilder);
                RegisterBackgroundScheduler(autoFacBuilder);
            });

        try
        {
            return base.CreateHost(builder);
        }
        catch (Exception e)
        {
            _log.Fatal(e);
            throw;
        }
    }

    private void SetMockedDependencies(ContainerBuilder builder)
    {
        if (_mockPlexApi is not null)
        {
            builder
                .RegisterInstance(_mockPlexApi.CreateClient())
                .As<HttpClient>()
                .SingleInstance();
        }

        if (_config.MockFileSystem is not null)
            builder.RegisterInstance(_config.MockFileSystem).As<IFileSystem>();

        if (_config.MockConfigManager is not null)
            builder.RegisterInstance(_config.MockConfigManager).As<IConfigManager>();
    }

    private void RegisterBackgroundScheduler(ContainerBuilder builder)
    {
        var testQuartzProps = new NameValueCollection
        {
            { "quartz.scheduler.instanceName", "TestPlexRipper Scheduler" },
            { "quartz.serializer.type", "json" },
            { "quartz.threadPool.type", "Quartz.Simpl.SimpleThreadPool, Quartz" },
            { "quartz.threadPool.threadCount", "10" },
            { "quartz.jobStore.misfireThreshold", "60000" },
        };

        // Register Quartz dependencies
        builder.RegisterModule(new QuartzAutofacFactoryModule
        {
            // JobScopeConfigurator = (cb, tag) =>
            // {
            //     // override dependency for job scope
            //     cb.Register(_ => new ScopedDependency("job-local " + DateTime.UtcNow.ToLongTimeString()))
            //         .AsImplementedInterfaces()
            //         .InstancePerMatchingLifetimeScope(tag);
            // },

            // During integration testing, we cannot use a real JobStore so we revert to default
            ConfigurationProvider = _ => testQuartzProps,
        });
    }
}