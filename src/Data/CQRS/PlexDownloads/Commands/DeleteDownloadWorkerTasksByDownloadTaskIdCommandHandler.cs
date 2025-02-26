﻿using Data.Contracts;
using FluentValidation;
using Logging.Interface;
using Microsoft.EntityFrameworkCore;
using PlexRipper.Data.Common;

namespace PlexRipper.Data;

public class DeleteDownloadWorkerTasksByDownloadTaskIdCommandValidator : AbstractValidator<DeleteDownloadWorkerTasksByDownloadTaskIdCommand>
{
    public DeleteDownloadWorkerTasksByDownloadTaskIdCommandValidator()
    {
        RuleFor(x => x.DownloadTaskId).GreaterThan(0);
    }
}

public class DeleteDownloadWorkerTasksByDownloadTaskIdCommandHandler : BaseHandler,
    IRequestHandler<DeleteDownloadWorkerTasksByDownloadTaskIdCommand, Result>
{
    public DeleteDownloadWorkerTasksByDownloadTaskIdCommandHandler(ILog log, PlexRipperDbContext dbContext) : base(log, dbContext) { }

    public async Task<Result> Handle(DeleteDownloadWorkerTasksByDownloadTaskIdCommand command, CancellationToken cancellationToken)
    {
        var downloadWorkerTasks = await _dbContext.DownloadWorkerTasks.AsTracking()
            .Where(x => x.DownloadTaskId == command.DownloadTaskId)
            .ToListAsync(cancellationToken);
        if (!downloadWorkerTasks.Any())
        {
            return Result.Fail(
                    $"Could not find any {nameof(DownloadWorkerTask)}s assigned to {nameof(DownloadTask)} with id: {command.DownloadTaskId}")
                .LogWarning();
        }

        _dbContext.DownloadWorkerTasks.RemoveRange(downloadWorkerTasks);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}