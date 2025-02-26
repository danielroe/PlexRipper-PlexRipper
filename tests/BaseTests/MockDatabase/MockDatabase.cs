#region

using Logging.Interface;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NaturalSort.Extension;
using PlexRipper.Data;
using PlexRipper.Data.Common;

#endregion

namespace PlexRipper.BaseTests;

public static class MockDatabase
{
    private static int _seed;
    private static readonly ILog _log = LogManager.CreateLogInstance(typeof(MockDatabase));
    private static readonly NaturalSortComparer NaturalComparer = new(StringComparison.InvariantCultureIgnoreCase);

    #region Methods

    #region Private

    private static async Task<PlexRipperDbContext> AddPlexServers(this PlexRipperDbContext context, Action<FakeDataConfig> options = null)
    {
        var config = FakeDataConfig.FromOptions(options);

        var plexServers = FakeData.GetPlexServer(_seed).Generate(config.PlexServerCount);

        if (config.MockServerUris.Any())
        {
            config.MockServerUris.Count.ShouldBeGreaterThanOrEqualTo(plexServers.Count,
                $"The mocked plex server count ({config.MockServerUris.Count}) was lower than the generated {nameof(config.PlexServerCount)} ({config.PlexServerCount})");

            for (var i = 0; i < config.MockServerUris.Count; i++)
            {
                var serverUri = config.MockServerUris[i];
                var connection = plexServers[i].PlexServerConnections[0];
                connection.Protocol = serverUri.Scheme;
                connection.Address = serverUri.Host;
                connection.Port = serverUri.Port;
            }
        }

        await context.PlexServers.AddRangeAsync(plexServers);

        await context.SaveChangesAsync();

        _log.Here().Debug("Added {PlexServerCount} {NameOfPlexServer}s to {NameOfPlexRipperDbContext}: {DatabaseName}", config.PlexServerCount,
            nameof(PlexServer), nameof(PlexRipperDbContext), context.DatabaseName);
        return context;
    }

    private static async Task<PlexRipperDbContext> AddPlexLibraries(this PlexRipperDbContext context, Action<FakeDataConfig> options = null)
    {
        var plexServers = await context.PlexServers.ToListAsync();
        plexServers.ShouldNotBeEmpty();

        var config = FakeDataConfig.FromOptions(options);

        var plexLibrariesToDb = new List<PlexLibrary>();

        var plexLibraryCount = config.PlexLibraryCount;
        if (config.MovieCount > 0)
            plexLibraryCount--;

        if (config.TvShowCount > 0)
            plexLibraryCount--;

        foreach (var plexServer in plexServers)
        {
            var plexLibraries = new List<PlexLibrary>();
            if (config.MovieCount > 0)
                plexLibraries.Add(FakeData.GetPlexLibrary(_seed, PlexMediaType.Movie).Generate());

            if (config.TvShowCount > 0)
                plexLibraries.Add(FakeData.GetPlexLibrary(_seed, PlexMediaType.TvShow).Generate());

            if (plexLibraryCount > 0)
                plexLibraries.AddRange(FakeData.GetPlexLibrary(_seed).Generate(plexLibraryCount));

            foreach (var plexLibrary in plexLibraries)
                plexLibrary.PlexServerId = plexServer.Id;

            plexLibrariesToDb.AddRange(plexLibraries);
        }

        context.PlexLibraries.AddRange(plexLibrariesToDb);
        await context.SaveChangesAsync();
        return context;
    }

    private static async Task<PlexRipperDbContext> AddPlexAccount(this PlexRipperDbContext context, Action<FakeDataConfig> options = null)
    {
        var config = FakeDataConfig.FromOptions(options);
        var plexServers = context.PlexServers.Include(x => x.PlexLibraries).ToList();

        var plexAccount = FakeData.GetPlexAccount(_seed).Generate();

        await context.PlexAccounts.AddAsync(plexAccount);
        await context.SaveChangesAsync();

        _log.Here().Debug("Added 1 {NameOfPlexAccount}: {PlexAccountTitle} to PlexRipperDbContext: {DatabaseName}", nameof(PlexAccount), plexAccount.Title,
            context.DatabaseName);

        var plexAccountServer = plexServers.Select(x => new PlexAccountServer
        {
            AuthTokenCreationDate = DateTime.UtcNow,
            PlexServerId = x.Id,
            PlexAccountId = plexAccount.Id,
            AuthToken = "FAKE_AUTH_TOKEN",
        });

        // Add account -> server relation
        context.PlexAccountServers.AddRange(plexAccountServer);
        await context.SaveChangesAsync();

        // Add account -> library relation
        var plexAccountLibraries = plexServers.SelectMany(x => x.PlexLibraries)
            .Select(x => new PlexAccountLibrary
            {
                PlexAccountId = plexAccount.Id,
                PlexServerId = x.PlexServerId,
                PlexLibraryId = x.Id,
            });
        context.PlexAccountLibraries.AddRange(plexAccountLibraries);
        await context.SaveChangesAsync();

        return context;
    }

    private static async Task<PlexRipperDbContext> AddPlexAccountLibraries(this PlexRipperDbContext context, Action<FakeDataConfig> options = null)
    {
        var config = FakeDataConfig.FromOptions(options);

        var plexLibraries = await context.PlexLibraries.ToListAsync();
        var plexAccounts = await context.PlexAccounts.ToListAsync();
        plexAccounts.ShouldNotBeEmpty();
        plexLibraries.ShouldNotBeEmpty();

        var plexAccountLibraries = new List<PlexAccountLibrary>();
        foreach (var plexAccount in plexAccounts)
        foreach (var plexLibrary in plexLibraries)
            plexAccountLibraries.Add(new PlexAccountLibrary
            {
                PlexAccountId = plexAccount.Id,
                PlexServerId = plexLibrary.PlexServerId,
                PlexLibraryId = plexLibrary.Id,
            });

        context.PlexAccountLibraries.AddRange(plexAccountLibraries);
        await context.SaveChangesAsync();
        return context;
    }

    private static void ValidateOptions(FakeDataConfig config)
    {
        if (config.PlexLibraryCount > 0)
            config.PlexServerCount.ShouldBeGreaterThan(0);

        if (config.MovieCount > 0 && config.TvShowCount > 0)
        {
            config.PlexServerCount.ShouldBeGreaterThanOrEqualTo(1,
                $"{nameof(config.PlexServerCount)} should be greater than zero when either {nameof(config.MovieCount)} or {nameof(config.TvShowCount)} is also greater than zero!");

            config.PlexLibraryCount.ShouldBeGreaterThanOrEqualTo(2,
                $"{nameof(config.PlexLibraryCount)} should be greater than the sum of when either {nameof(config.MovieCount)} or {nameof(config.TvShowCount)} is also greater than zero!");
        }

        if (config.MovieCount > 0 || config.TvShowCount > 0)
        {
            config.PlexServerCount.ShouldBeGreaterThanOrEqualTo(1,
                $"{nameof(config.PlexServerCount)} should be greater than zero when either {nameof(config.MovieCount)} or {nameof(config.TvShowCount)} is also greater than zero!");
            config.PlexLibraryCount.ShouldBeGreaterThanOrEqualTo(1,
                $"{nameof(config.PlexLibraryCount)} should be greater than zero when either {nameof(config.MovieCount)} or {nameof(config.TvShowCount)} is also greater than zero!");
        }
    }

    #endregion

    #region Public

    public static string GetMemoryDatabaseName()
    {
        return $"memory_database_{Random.Shared.Next(1, int.MaxValue)}_{Random.Shared.Next(int.MaxValue)}";
    }

    /// <summary>
    /// Creates an in-memory database only to be used for unit and integration testing.
    /// Passing in the same dbName will create a new context for the same database
    /// </summary>
    /// <param name="dbName">leave empty to generate a random one</param>
    /// <param name="disableForeignKeyCheck">By default, don't enforce foreign key check for handling database data.</param>
    /// <returns>A <see cref="PlexRipperDbContext" /> in memory instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static PlexRipperDbContext GetMemoryDbContext(string dbName = "", bool disableForeignKeyCheck = false)
    {
        // TODO Remove disableForeignKeyCheck as it is bad practice, even for unit tests
        var optionsBuilder = new DbContextOptionsBuilder<PlexRipperDbContext>();
        dbName = string.IsNullOrEmpty(dbName) ? GetMemoryDatabaseName() : dbName;

        var connectionString = DatabaseConnectionString(dbName, disableForeignKeyCheck);
        SqliteConnection databaseConnection = new(connectionString);

        databaseConnection.CreateCollation(OrderByNaturalExtensions.CollationName, (x, y) => NaturalComparer.Compare(x, y));

        optionsBuilder.UseSqlite(databaseConnection);

        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.EnableDetailedErrors();
        optionsBuilder.LogTo(text => LogManager.DbContextLogger(text), LogLevel.Warning);
        return new PlexRipperDbContext(optionsBuilder.Options, dbName);
    }

    public static string DatabaseConnectionString(string dbName = "", bool disableForeignKeyCheck = false)
    {
        // https://docs.microsoft.com/en-us/dotnet/standard/data/sqlite/in-memory-databases
        return new SqliteConnectionStringBuilder
        {
            // Real file is used for testing due to otherwise flaky tests when doing in memory
            Mode = SqliteOpenMode.ReadWriteCreate,
            ForeignKeys = !disableForeignKeyCheck,

            // Database name
            DataSource = dbName,
            Cache = SqliteCacheMode.Shared,
        }.ToString();
    }

    public static async Task<PlexRipperDbContext> Setup(this PlexRipperDbContext context, int seed = 0, Action<FakeDataConfig> options = null)
    {
        _seed = seed;

        var config = FakeDataConfig.FromOptions(options);
        ValidateOptions(config);

        context.HasBeenSetup = true;

        // PlexServers and Libraries added
        _log.Here().Debug("Setting up {NameOfPlexRipperDbContext} for {DatabaseName}", nameof(PlexRipperDbContext), context.DatabaseName);

        if (config.PlexServerCount > 0)
            context = await context.AddPlexServers(options);

        if (config.PlexLibraryCount > 0)
            context = await context.AddPlexLibraries(options);

        if (config.PlexAccountCount > 0)
            context = await context.AddPlexAccount(options);

        if (config.MovieCount > 0)
            context = await context.AddPlexMovies(options);

        if (config.TvShowCount > 0)
            context = await context.AddPlexTvShows(options);

        if (config.MovieDownloadTasksCount > 0)
            context = await context.AddMovieDownloadTasks(options);

        if (config.TvShowDownloadTasksCount > 0)
            context = await context.AddTvShowDownloadTasks(options);

        if (config.AccountHasAccessToAllLibraries)
            context = await context.AddPlexAccountLibraries(options);

        return context;
    }

    #endregion

    #endregion

    #region Add DownloadTasks

    private static async Task<PlexRipperDbContext> AddMovieDownloadTasks(this PlexRipperDbContext context, Action<FakeDataConfig> options = null)
    {
        var config = FakeDataConfig.FromOptions(options);

        var downloadTasks = FakeData.GetMovieDownloadTask(_seed, options).Generate(config.MovieDownloadTasksCount);
        var plexLibrary = context.PlexLibraries.FirstOrDefault(x => x.Type == PlexMediaType.Movie);
        plexLibrary.ShouldNotBeNull();

        var plexServer = context.PlexServers.IncludeConnections().FirstOrDefault(x => x.Id == plexLibrary.PlexServerId);
        plexServer.ShouldNotBeNull();

        downloadTasks = downloadTasks.SetIds(plexLibrary.PlexServerId, plexLibrary.Id, plexServer.MachineIdentifier);


        context.DownloadTasks.AddRange(downloadTasks);
        await context.SaveChangesAsync();

        // Ensure there is a rootId
        foreach (var downloadTask in downloadTasks)
        {
            downloadTask.RootDownloadTaskId = downloadTask.Id;
            downloadTask.Children.SetRootId(downloadTask.Id);
        }

        await context.SaveChangesAsync();

        _log.Here().Debug("Added {MovieDownloadTasksCount} Movie {NameOfDownloadTask}s to PlexRipperDbContext: {DatabaseName}", config.MovieDownloadTasksCount, nameof(DownloadTask), context.DatabaseName);

        return context;
    }

    private static async Task<PlexRipperDbContext> AddTvShowDownloadTasks(this PlexRipperDbContext context, Action<FakeDataConfig> options = null)
    {
        var config = FakeDataConfig.FromOptions(options);

        var downloadTasks = FakeData.GetTvShowDownloadTask(_seed, options).Generate(config.TvShowDownloadTasksCount);
        var plexLibrary = context.PlexLibraries.FirstOrDefault(x => x.Type == PlexMediaType.TvShow);
        plexLibrary.ShouldNotBeNull();
        var plexServer = context.PlexServers.IncludeConnections().FirstOrDefault(x => x.Id == plexLibrary.PlexServerId);
        plexServer.ShouldNotBeNull();

        downloadTasks.SetIds(plexLibrary.PlexServerId, plexLibrary.Id, plexServer.MachineIdentifier);

        plexServer.PlexServerConnections.ShouldNotBeEmpty();

        context.DownloadTasks.AddRange(downloadTasks);
        await context.SaveChangesAsync();

        // Ensure there is a rootId
        foreach (var downloadTask in downloadTasks)
        {
            downloadTask.RootDownloadTaskId = downloadTask.Id;
            downloadTask.Children.SetRootId(downloadTask.Id);
        }

        await context.SaveChangesAsync();

        _log.Here().Debug("Added {TvShowDownloadTasksCount} TvShow {NameOfDownloadTask}s to PlexRipperDbContext: {DatabaseName}", config.TvShowDownloadTasksCount, nameof(DownloadTask), context.DatabaseName);

        return context;
    }

    #endregion

    #region Add Media

    private static async Task<PlexRipperDbContext> AddPlexMovies(this PlexRipperDbContext context, Action<FakeDataConfig> options = null)
    {
        var config = FakeDataConfig.FromOptions(options);

        var plexLibraries = context.PlexLibraries.Where(x => x.Type == PlexMediaType.Movie).ToList();
        plexLibraries.ShouldNotBeNull().ShouldNotBeEmpty();

        foreach (var plexLibrary in plexLibraries)
        {
            var movies = FakeData.GetPlexMovies(_seed, options).Generate(config.MovieCount);

            foreach (var movie in movies)
            {
                movie.PlexLibraryId = plexLibrary.Id;
                movie.PlexServerId = plexLibrary.PlexServerId;
            }

            context.PlexMovies.AddRange(movies);
        }

        await context.SaveChangesAsync();

        _log.Here().Debug("Added {MovieCount} {NameOfPlexMovie}s to PlexRipperDbContext: {DatabaseName}", config.MovieCount, nameof(PlexMovie), context.DatabaseName);

        return context;
    }

    private static async Task<PlexRipperDbContext> AddPlexTvShows(this PlexRipperDbContext context, Action<FakeDataConfig> options = null)
    {
        var config = FakeDataConfig.FromOptions(options);

        var plexLibraries = context.PlexLibraries.Where(x => x.Type == PlexMediaType.TvShow).ToList();
        plexLibraries.ShouldNotBeNull().ShouldNotBeEmpty();

        foreach (var plexLibrary in plexLibraries)
        {
            var tvShows = FakeData.GetPlexTvShows(_seed, options).Generate(config.TvShowCount);

            foreach (var tvShow in tvShows)
            {
                tvShow.PlexLibraryId = plexLibrary.Id;
                tvShow.PlexServerId = plexLibrary.PlexServerId;

                foreach (var season in tvShow.Seasons)
                {
                    season.TvShow = tvShow;
                    season.PlexLibraryId = plexLibrary.Id;
                    season.PlexServerId = plexLibrary.PlexServerId;

                    foreach (var episode in season.Episodes)
                    {
                        episode.TvShow = tvShow;
                        episode.TvShowSeason = season;
                        episode.PlexLibraryId = plexLibrary.Id;
                        episode.PlexServerId = plexLibrary.PlexServerId;
                    }
                }
            }

            context.PlexTvShows.AddRange(tvShows);
        }

        await context.SaveChangesAsync();

        _log.Here().Debug("Added {TvShowCount} {NameOfPlexTvShow}s to PlexRipperDbContext: {DatabaseName}", config.TvShowCount, nameof(PlexTvShow), context.DatabaseName);

        return context;
    }

    #endregion
}