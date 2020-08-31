﻿using System.Collections.Generic;
using System.Threading.Tasks;
using FluentResults;
using PlexRipper.Domain.Entities;

namespace PlexRipper.Application.Common
{
    public interface IDownloadManager
    {
        Result<bool> StopDownload(int downloadTaskId);

        /// <summary>
        /// Adds a single DownloadTask to the Download queue
        /// </summary>
        /// <param name="downloadTask">The <see cref="DownloadTask"/> that will be checked and added.</param>
        /// <param name="performCheck">Should the CheckDownloadQueue() be called at the end</param>
        /// <returns>Returns true if successfully added and false if the downloadTask already exists</returns>
        Task<Result<bool>> AddToDownloadQueueAsync(DownloadTask downloadTask, bool performCheck = true);

        /// <summary>
        /// Adds a list of <see cref="DownloadTask"/>s to the download queue.
        /// </summary>
        /// <param name="downloadTasks">The list of <see cref="DownloadTask"/>s that will be checked and added.</param>
        /// <returns>Returns true if all downloadTasks were added successfully.</returns>
        Task<Result<bool>> AddToDownloadQueueAsync(List<DownloadTask> downloadTasks);

        Task<Result<bool>> RestartDownloadAsync(int downloadTaskId);
    }
}