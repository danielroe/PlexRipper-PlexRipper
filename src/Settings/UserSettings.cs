using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using FluentResults;
using Logging;
using PlexRipper.Application;
using PlexRipper.Domain.Config;
using PlexRipper.Settings.Models;

namespace PlexRipper.Settings
{
    /// <inheritdoc cref="IUserSettings"/>
    public class UserSettings : IUserSettings
    {
        #region Fields

        private readonly IConfirmationSettingsModule _confirmationSettingsModule;

        private readonly IDateTimeSettingsModule _dateTimeSettingsModule;

        private readonly IDisplaySettingsModule _displaySettingsModule;

        private readonly IDownloadManagerSettingsModule _downloadManagerSettingsModule;

        private readonly IGeneralSettingsModule _generalSettingsModule;

        private readonly ILanguageSettingsModule _languageSettingsModule;

        private readonly IServerSettingsModule _serverSettingsModule;

        private readonly Subject<ISettingsModel> _settingsUpdated = new();

        #endregion

        public UserSettings(
            IConfirmationSettingsModule confirmationSettingsModule,
            IDateTimeSettingsModule dateTimeSettingsModule,
            IDisplaySettingsModule displaySettingsModule,
            IDownloadManagerSettingsModule downloadManagerSettingsModule,
            IGeneralSettingsModule generalSettingsModule,
            ILanguageSettingsModule languageSettingsModule,
            IServerSettingsModule serverSettingsModule)
        {
            _confirmationSettingsModule = confirmationSettingsModule;
            _dateTimeSettingsModule = dateTimeSettingsModule;
            _displaySettingsModule = displaySettingsModule;
            _downloadManagerSettingsModule = downloadManagerSettingsModule;
            _generalSettingsModule = generalSettingsModule;
            _languageSettingsModule = languageSettingsModule;
            _serverSettingsModule = serverSettingsModule;

            // Alert of any module changes
            Observable.Merge(
                _confirmationSettingsModule.ModuleHasChanged.Select(_ => 1),
                _dateTimeSettingsModule.ModuleHasChanged.Select(_ => 1),
                _displaySettingsModule.ModuleHasChanged.Select(_ => 1),
                _downloadManagerSettingsModule.ModuleHasChanged.Select(_ => 1),
                _generalSettingsModule.ModuleHasChanged.Select(_ => 1),
                _languageSettingsModule.ModuleHasChanged.Select(_ => 1),
                _serverSettingsModule.ModuleHasChanged.Select(_ => 1)
            ).Subscribe(_ => _settingsUpdated.OnNext(GetSettingsModel()));
        }

        #region Methods

        #region Public

        public IObservable<ISettingsModel> SettingsUpdated => _settingsUpdated.AsObservable();

        public void Reset()
        {
            Log.Information("Resetting UserSettings");

            _confirmationSettingsModule.Reset();
            _dateTimeSettingsModule.Reset();
            _displaySettingsModule.Reset();
            _downloadManagerSettingsModule.Reset();
            _generalSettingsModule.Reset();
            _languageSettingsModule.Reset();
            _serverSettingsModule.Reset();
        }

        public Result<ISettingsModel> UpdateSettings(ISettingsModel sourceSettings)
        {
            var results = new List<Result>
            {
                _confirmationSettingsModule.Update(sourceSettings.ConfirmationSettings),
                _dateTimeSettingsModule.Update(sourceSettings.DateTimeSettings),
                _displaySettingsModule.Update(sourceSettings.DisplaySettings),
                _downloadManagerSettingsModule.Update(sourceSettings.DownloadManagerSettings),
                _generalSettingsModule.Update(sourceSettings.GeneralSettings),
                _languageSettingsModule.Update(sourceSettings.LanguageSettings),
                _serverSettingsModule.Update(sourceSettings.ServerSettings),
            };

            if (results.Any(x => x.IsFailed))
            {
                return Result.Fail("Failed to update settings").AddNestedErrors(results.SelectMany(x => x.Errors).ToList());
            }

            return Result.Ok(GetSettingsModel());
        }

        public ISettingsModel GetSettingsModel()
        {
            return new SettingsModel
            {
                ConfirmationSettings = _confirmationSettingsModule.GetValues(),
                GeneralSettings = _generalSettingsModule.GetValues(),
                DisplaySettings = _displaySettingsModule.GetValues(),
                LanguageSettings = _languageSettingsModule.GetValues(),
                ServerSettings = _serverSettingsModule.GetValues(),
                DateTimeSettings = _dateTimeSettingsModule.GetValues(),
                DownloadManagerSettings = _downloadManagerSettingsModule.GetValues(),
            };
        }

        /// <inheritdoc/>
        public Result<string> GetJsonSettingsObject()
        {
            try
            {
                return Result.Ok(JsonSerializer.Serialize(GetSettingsModel(), DefaultJsonSerializerOptions.Config));
            }
            catch (Exception e)
            {
                return Result.Fail(new ExceptionalError(e)).LogError();
            }
        }

        public Result SetFromJsonObject(JsonElement settingsJsonElement)
        {
            var results = new List<Result>
            {
                _confirmationSettingsModule.SetFromJsonObject(settingsJsonElement),
                _dateTimeSettingsModule.SetFromJsonObject(settingsJsonElement),
                _displaySettingsModule.SetFromJsonObject(settingsJsonElement),
                _downloadManagerSettingsModule.SetFromJsonObject(settingsJsonElement),
                _generalSettingsModule.SetFromJsonObject(settingsJsonElement),
                _languageSettingsModule.SetFromJsonObject(settingsJsonElement),
                _serverSettingsModule.SetFromJsonObject(settingsJsonElement),
            };

            if (results.Any(x => x.IsFailed))
            {
                return Result.Fail("Failed to set from json object").AddNestedErrors(results.SelectMany(x => x.Errors).ToList());
            }

            return Result.Ok();
        }

        #endregion

        #endregion
    }
}