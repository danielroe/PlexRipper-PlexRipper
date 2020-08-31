﻿using System.Collections.Generic;
using System.Threading.Tasks;
using FluentResults;
using PlexRipper.Domain.Entities;

namespace PlexRipper.Application.Common
{
    public interface IFolderPathService
    {
        Task<Result<List<FolderPath>>> GetAllFolderPathsAsync();
        Task<Result<FolderPath>> UpdateFolderPathAsync(FolderPath folderPath);
        Task<Result<FolderPath>> GetDownloadFolderAsync();
    }
}
