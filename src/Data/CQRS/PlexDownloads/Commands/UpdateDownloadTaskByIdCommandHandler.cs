﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EFCore.BulkExtensions;
using FluentResults;
using FluentValidation;
using MediatR;
using PlexRipper.Application.PlexDownloads;
using PlexRipper.Data.Common;
using PlexRipper.Domain;

namespace PlexRipper.Data.CQRS.PlexDownloads
{
    public class UpdateDownloadTaskByIdCommandValidator : AbstractValidator<UpdateDownloadTaskByIdCommand>
    {
        public UpdateDownloadTaskByIdCommandValidator()
        {
            RuleFor(x => x.DownloadTask).NotNull();
            RuleFor(x => x.DownloadTask.IsValid().IsSuccess).Equal(true);
        }
    }

    public class UpdateDownloadTaskByIdCommandHandler : BaseHandler,
        IRequestHandler<UpdateDownloadTaskByIdCommand, Result>
    {
        public UpdateDownloadTaskByIdCommandHandler(PlexRipperDbContext dbContext) : base(dbContext) { }

        public async Task<Result> Handle(UpdateDownloadTaskByIdCommand command, CancellationToken cancellationToken)
        {
            // var downloadTask = await _dbContext.DownloadTasks
            //     .Include(x => x.DownloadWorkerTasks)
            //     .FirstOrDefaultAsync(x => x.Id == command.DownloadTask.Id, cancellationToken);
            //
            // if (downloadTask == null)
            // {
            //     return ResultExtensions.GetEntityNotFound(nameof(downloadTask), command.DownloadTask.Id);
            // }
            //
            // _dbContext.Entry(downloadTask).CurrentValues.SetValues(command.DownloadTask);
            // _dbContext.Entry(downloadTask).Reference(x => x.DestinationFolder).IsModified = false;
            // _dbContext.Entry(downloadTask).Reference(x => x.DestinationFolder).IsModified = false;
            // _dbContext.Entry(downloadTask).Reference(x => x.DownloadFolder).IsModified = false;
            // _dbContext.Entry(downloadTask).Reference(x => x.PlexServer).IsModified = false;
            // _dbContext.Entry(downloadTask).Reference(x => x.PlexLibrary).IsModified = false;
            //
            // await _dbContext.SaveChangesAsync(cancellationToken);
            await _dbContext.BulkUpdateAsync(new List<DownloadTask> { command.DownloadTask });
            await _dbContext.BulkInsertOrUpdateAsync(command.DownloadTask.DownloadWorkerTasks);
            return Result.Ok();
        }
    }
}