﻿using System.Text.Json;
using Environment;
using FileSystem.Contracts;
using Logging.Interface;
using PlexRipper.Domain.Config;
using Settings.Contracts;

namespace PlexRipper.Settings;

public class ConfigManager : IConfigManager
{
    #region Fields

    private readonly ILog _log;
    private readonly IFileSystem _fileSystem;

    private readonly IDirectorySystem _directorySystem;

    private readonly IPathProvider _pathProvider;

    private readonly IUserSettings _userSettings;

    #endregion

    #region Constructor

    public ConfigManager(ILog log, IFileSystem fileSystem, IDirectorySystem directorySystem, IPathProvider pathProvider, IUserSettings userSettings)
    {
        _log = log;
        _fileSystem = fileSystem;
        _directorySystem = directorySystem;
        _pathProvider = pathProvider;
        _userSettings = userSettings;
    }

    #endregion

    #region Public Methods

    public Result Setup()
    {
        _userSettings.SettingsUpdated.Subscribe(_ => SaveConfig());

        _log.Information("Checking if \"{ConfigFileName}\" exists at \"{ConfigDirectory}\"", _pathProvider.ConfigFileName, _pathProvider.ConfigDirectory, 0);

        var configDirectoryExistsResult = _directorySystem.Exists(_pathProvider.ConfigDirectory);
        if (configDirectoryExistsResult.IsFailed)
            return configDirectoryExistsResult.LogFatal();

        if (configDirectoryExistsResult.Value)
            _log.Information("Config directory exists, will use \"{ConfigDirectory}\"", _pathProvider.ConfigDirectory);
        else
        {
            _log.Information("Config directory does not exist, will create now at \"{ConfigDirectory}\"", _pathProvider.ConfigDirectory);
            var createResult = _directorySystem.CreateDirectory(_pathProvider.ConfigDirectory);
            if (createResult.IsFailed)
            {
                Log.Fatal($"Failed to create config directory at \"{_pathProvider.ConfigDirectory}\"");
                return createResult.LogFatal();
            }

            _log.Debug("Directory: \"{ConfigDirectory}\" created!", _pathProvider.ConfigDirectory);
        }

        if (!ConfigFileExists())
        {
            _log.Information("\"{ConfigFileName}\" doesn't exist, will create new one now in \"{ConfigDirectory}\"", _pathProvider.ConfigFileName,
                _pathProvider.ConfigDirectory, 0);
            return SaveConfig();
        }

        var loadResult = LoadConfig();
        return loadResult.IsFailed ? loadResult : Result.Ok();
    }

    public virtual Result LoadConfig()
    {
        Log.Information("Loading user config settings now.");
        var readResult = ReadFromConfigFile();
        if (readResult.IsFailed)
        {
            _log.Information("Resetting {ConfigFileName} because it could not be loaded correctly", _pathProvider.ConfigFileName);
            return ResetConfig();
        }

        try
        {
            var cleanedJson = readResult.Value.Replace("\r\n", "");
            var loadedSettings = JsonSerializer.Deserialize<JsonElement>(cleanedJson, DefaultJsonSerializerOptions.ConfigManagerOptions);
            var setFromJsonResult = _userSettings.SetFromJsonObject(loadedSettings);
            if (setFromJsonResult.IsFailed)
            {
                Log.Warning("Certain properties were missing or had missing or invalid values. Will correct those and re-save now!");
                setFromJsonResult.LogWarning();
                return ResetConfig();
            }

            return Result.Ok().WithSuccess("UserSettings were loaded successfully!").LogInformation();
        }
        catch (Exception e)
        {
            Result.Fail(new ExceptionalError(e)).LogError();
            Log.Error($"Failed to JSON parse the contents from {_pathProvider.ConfigFileName}");
            Log.Error($"Contents: {readResult.Value}");
            return ResetConfig();
        }
    }

    public virtual Result ResetConfig()
    {
        _userSettings.Reset();
        var saveResult = SaveConfig();
        if (saveResult.IsFailed)
            saveResult.WithError(new Error("Failed to save a new config after resetting")).LogError();

        return Result.Ok();
    }

    public virtual Result SaveConfig()
    {
        Log.Information("Saving user config settings now.");

        var jsonSettings = GetJsonSettingsObject();
        if (jsonSettings.IsFailed)
            return jsonSettings.ToResult();

        var writeResult = WriteToConfigFile(jsonSettings.Value);
        if (writeResult.IsFailed)
            return writeResult;

        return Result.Ok().WithSuccess("UserSettings were saved successfully!").LogInformation();
    }

    public virtual bool ConfigFileExists()
    {
        return _fileSystem.FileExists(_pathProvider.ConfigFileLocation);
    }

    #endregion

    #region Private Methods

    private Result WriteToConfigFile(string jsonSettingsString)
    {
        var writeResult = _fileSystem.FileWriteAllText(_pathProvider.ConfigFileLocation, jsonSettingsString);
        return writeResult.IsFailed ? writeResult.WithError("Failed to write config settings").LogError() : Result.Ok();
    }

    private Result<string> ReadFromConfigFile()
    {
        var readResult = _fileSystem.FileReadAllText(_pathProvider.ConfigFileLocation);
        if (readResult.IsFailed)
        {
            Log.Error($"Failed to read {_pathProvider.ConfigFileName} from {_pathProvider.ConfigDirectory}");
            readResult.LogError();
        }

        return readResult;
    }

    private Result<string> GetJsonSettingsObject()
    {
        try
        {
            return Result.Ok(JsonSerializer.Serialize(_userSettings.GetSettingsModel(), DefaultJsonSerializerOptions.ConfigManagerOptions));
        }
        catch (Exception e)
        {
            return Result.Fail(new ExceptionalError(e)).LogError();
        }
    }

    #endregion
}