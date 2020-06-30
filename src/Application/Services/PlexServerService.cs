﻿using PlexRipper.Application.Common.Interfaces;
using PlexRipper.Application.Common.Interfaces.PlexApi;
using PlexRipper.Application.Common.Interfaces.Repositories;
using PlexRipper.Domain;
using PlexRipper.Domain.Entities;
using PlexRipper.Domain.Entities.JoinTables;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PlexRipper.Application.Services
{
    public class PlexServerService : IPlexServerService
    {
        private readonly IPlexServerRepository _plexServerRepository;
        private readonly IPlexLibraryService _plexLibraryService;
        private readonly IPlexServerStatusRepository _plexServerStatusRepository;
        private readonly IPlexApiService _plexServiceApi;
        private readonly IPlexAuthenticationService _plexAuthenticationService;


        public PlexServerService(
            IPlexApiService plexServiceApi,
            IPlexAuthenticationService plexAuthenticationService,
            IPlexServerRepository plexServerRepository,
            IPlexLibraryService plexLibraryService,
            IPlexServerStatusRepository plexServerStatusRepository)
        {
            _plexServerRepository = plexServerRepository;
            _plexLibraryService = plexLibraryService;
            _plexServerStatusRepository = plexServerStatusRepository;
            _plexServiceApi = plexServiceApi;
            _plexAuthenticationService = plexAuthenticationService;
        }

        /// <summary>
        /// Retrieves the latest <see cref="PlexServer"/> data, and the corresponding <see cref="PlexLibrary"/>, from the PlexAPI and stores it in the Database.
        /// </summary>
        /// <param name="plexAccount">PlexAccount to use to retrieve the servers</param>
        /// <returns>Is successful</returns>
        public async Task<bool> RefreshPlexServersAsync(PlexAccount plexAccount)
        {
            if (plexAccount == null)
            {
                Log.Warning("plexAccount was null");
                return false;
            }

            Log.Debug($"Refreshing PlexLibraries for PlexAccount: {plexAccount.Id}");

            var token = await _plexAuthenticationService.GetPlexToken(plexAccount);

            if (string.IsNullOrEmpty(token))
            {
                Log.Warning("Token was empty");
                return false;
            }

            var serverList = await _plexServiceApi.GetServerAsync(token);

            // First add or update the plex servers
            await AddOrUpdatePlexServersAsync(plexAccount, serverList);

            //Refresh all libraries for each plexServer as well
            foreach (var plexServer in serverList)
            {
                var status = await GetPlexServerStatusAsync(plexServer);
                if (status.IsSuccessful)
                {
                    await _plexLibraryService.RefreshLibrariesAsync(plexServer);
                }
            }

            return true;
        }

        /// <summary>
        /// Check if the <see cref="PlexServer"/> is available and log the status
        /// </summary>
        /// <param name="plexServer"></param>
        /// <returns></returns>
        public async Task<PlexServerStatus> GetPlexServerStatusAsync(PlexServer plexServer)
        {
            // Request status
            var serverStatus = await _plexServiceApi.GetPlexServerStatusAsync(plexServer.AccessToken, plexServer.BaseUrl);

            //serverStatus.PlexServer = plexServer;
            serverStatus.PlexServerId = plexServer.Id;

            // Add plexServer status to DB, the PlexServerStatus table functions as a server log.
            await _plexServerStatusRepository.AddAsync(serverStatus);

            return serverStatus;
        }


        /// <summary>
        /// This will get all <see cref="PlexLibrary"/>s with their media in the parent <see cref="PlexServer"/>
        /// </summary>
        /// <param name="plexServer"></param>
        /// <param name="refresh">Force refresh from PlexApi</param>
        /// <returns></returns>
        public async Task<PlexServer> GetAllLibraryMediaAsync(PlexServer plexServer, bool refresh = false)
        {
            var plexServerDB = await _plexServerRepository.GetAsync(plexServer.Id);

            if (refresh)
            {
                foreach (var library in plexServerDB.PlexLibraries)
                {
                    await _plexLibraryService.GetLibraryMediaAsync(plexServerDB, library.Key, true);
                }
                return await _plexServerRepository.GetAsync(plexServer.Id);
            }
            return plexServerDB;
        }

        #region CRUD


        public Task<PlexServer> GetServerAsync(int plexServerId)
        {
            return _plexServerRepository.GetAsync(plexServerId);
        }

        public async Task<List<PlexServer>> AddServersAsync(PlexAccount plexAccount, List<PlexServer> servers)
        {
            await _plexServerRepository.AddRangeAsync(servers);
            var x = await _plexServerRepository.GetServers(plexAccount.Id);
            return x.ToList();
        }

        public async Task<List<PlexServer>> GetServersAsync(PlexAccount plexAccount, bool refresh = false)
        {
            if (plexAccount == null)
            {
                Log.Warning($"{nameof(GetServersAsync)} => The plexAccount was null");
                return new List<PlexServer>();
            }

            // Retrieve all servers
            var serverList = await _plexServerRepository.GetServers(plexAccount.Id);
            if (refresh || !serverList.Any())
            {
                if (!serverList.Any())
                {
                    Log.Warning($"{nameof(GetServersAsync)} => PlexAccount {plexAccount.Id} did not have any PlexServers assigned. Forcing {nameof(RefreshPlexServersAsync)} was null");
                }

                var refreshSuccess = await RefreshPlexServersAsync(plexAccount);
                if (refreshSuccess)
                {
                    serverList = await _plexServerRepository.GetServers(plexAccount.Id);
                }
            }

            return serverList.ToList();
        }

        public async Task AddOrUpdatePlexServersAsync(PlexAccount plexAccount, List<PlexServer> servers)
        {

            if (plexAccount == null)
            {
                Log.Warning($"{nameof(AddOrUpdatePlexServersAsync)} => plexAccount was null");
                return;
            }

            if (servers.Count == 0)
            {
                Log.Warning($"{nameof(AddOrUpdatePlexServersAsync)} => servers list was empty");
                return;
            }

            Log.Debug($"{ nameof(AddOrUpdatePlexServersAsync)} => Starting adding or updating servers");

            // Add or update the plex servers
            foreach (var plexServer in servers)
            {
                // There might be cases where the scheme is not set properly by the PlexAPI so correct this
                if (plexServer.Port == 443)
                {
                    plexServer.Scheme = "https";
                }

                // TODO Might need a better way to identify servers, multiple Plex instances might run on the same server.
                var plexServerDB = await
                    _plexServerRepository.FindAsync(x => x.MachineIdentifier == plexServer.MachineIdentifier);

                if (plexServerDB != null)
                {
                    plexServer.Id = plexServerDB.Id;
                    await _plexServerRepository.UpdateAsync(plexServer);
                }
                else
                {
                    // Create entry in many-to-many table
                    var plexAccountServer = new PlexAccountServer
                    {
                        PlexAccountId = plexAccount.Id,
                        PlexServerId = plexServer.Id
                    };
                    plexServer.PlexAccountServers = new List<PlexAccountServer> { plexAccountServer };
                    await _plexServerRepository.AddAsync(plexServer);
                }
            }

            // TODO Add check if it was successful


        }



        #endregion
    }
}
