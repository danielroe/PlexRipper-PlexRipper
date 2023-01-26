﻿using System.Runtime.CompilerServices;
using Logging.LogStatic;
using Serilog.Events;

// ReSharper disable once CheckNamespace
// Needs to be in the same namespace as the FluentResults package
namespace FluentResults;

/// <summary>
/// Part of the <see cref="ResultExtensions"/> class - Logging related functionality.
/// </summary>
public static partial class ResultExtensions
{
    #region Implementations

    private static void LogByType(
        LogEventLevel logLevel,
        string message,
        Exception e = null,
        string memberName = default!,
        string sourceFilePath = default!,
        int sourceLineNumber = default!)
    {
        switch (logLevel)
        {
            case LogEventLevel.Verbose:
                LogStatic.Verbose(e, message, memberName, sourceFilePath, sourceLineNumber);
                break;
            case LogEventLevel.Debug:
                LogStatic.Debug(e, message, memberName, sourceFilePath, sourceLineNumber);
                break;
            case LogEventLevel.Information:
                LogStatic.Information(e, message, memberName, sourceFilePath, sourceLineNumber);
                break;
            case LogEventLevel.Warning:
                LogStatic.Warning(message, memberName, sourceFilePath, sourceLineNumber);
                break;
            case LogEventLevel.Error:
                LogStatic.Error(e, message, memberName, sourceFilePath, sourceLineNumber);
                break;
            case LogEventLevel.Fatal:
                LogStatic.Fatal(e, message, memberName, sourceFilePath, sourceLineNumber);
                break;
        }
    }

    private static Result<T> LogResult<T>(
        this Result<T> result,
        LogEventLevel logLevel,
        Exception e = null,
        string memberName = "",
        string sourceFilePath = "")
    {
        LogReasons(result.ToResult(), logLevel, e, memberName, sourceFilePath);

        return result;
    }

    private static Result LogResult(
        this Result result,
        LogEventLevel logLevel,
        Exception e = null,
        string memberName = "",
        string sourceFilePath = "")
    {
        LogReasons(result, logLevel, e, memberName, sourceFilePath);

        return result;
    }

    private static void LogReasons(
        this Result result,
        LogEventLevel logLevel,
        Exception e = null,
        string memberName = "",
        string sourceFilePath = "")
    {
        foreach (var success in result.Successes)
        {
            LogByType(logLevel, success.Message, null, memberName, sourceFilePath);

            if (success.Metadata.Any())
            {
                foreach (var entry in success.Metadata)
                    LogByType(logLevel, $"{entry.Key} - {entry.Value}", null, memberName, sourceFilePath);
            }
        }

        foreach (var error in result.Errors)
        {
            LogByType(logLevel, error.Message, null, memberName, sourceFilePath);

            if (error.Metadata.Any())
            {
                foreach (var entry in error.Metadata)
                    LogByType(logLevel, $"{entry.Key} - {entry.Value}", null, memberName, sourceFilePath);
            }

            foreach (var errorReason in error.Reasons)
            {
                LogByType(logLevel, "--" + errorReason.Message, null, memberName, sourceFilePath);
                if (errorReason.Metadata.Any())
                {
                    foreach (var entry in errorReason.Metadata)
                        LogByType(logLevel, $"--{entry.Key} - {entry.Value}", null, memberName, sourceFilePath);
                }

                foreach (var childErrorReason in errorReason.Reasons)
                {
                    LogByType(logLevel, "----" + childErrorReason.Message, null, memberName, sourceFilePath);
                    if (childErrorReason.Metadata.Any())
                    {
                        foreach (var entry in childErrorReason.Metadata)
                            LogByType(logLevel, $"----MetaData: {entry.Key} - {entry.Value}", null, memberName, sourceFilePath);
                    }
                }
            }

            if (error is ExceptionalError exceptional)
            {
                var exception = exceptional.Exception;
                LogByType(logLevel, "Exception", null, memberName, sourceFilePath);
                LogByType(logLevel, $"--{exception.Message} - {exception.Source}", null, memberName, sourceFilePath);
                if (exception.InnerException is not null)
                {
                    exception = exception.InnerException;
                    LogByType(logLevel, $"----{exception.Message} - {exception.Source}", null, memberName, sourceFilePath);
                    if (exception.InnerException is not null)
                    {
                        exception = exception.InnerException.InnerException;
                        if (exception is not null)
                            LogByType(logLevel, $"-------{exception.Message} - {exception.Source}", null, memberName, sourceFilePath);
                    }
                }
            }
        }

        if (e != null)
            LogByType(logLevel, string.Empty, e, memberName, sourceFilePath);
    }

    #endregion

    #region Result Signatures

    /// <summary>
    /// Logs all nested reasons and metadata on Log.Verbose().
    /// </summary>
    /// <param name="result">The result to use for logging.</param>
    /// <param name="memberName">The function name where the result happened.</param>
    /// <param name="sourceFilePath">The path to the source.</param>
    /// <returns>The result unchanged.</returns>
    public static Result LogVerbose(
        this Result result,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "")
    {
        return LogResult(result, LogEventLevel.Verbose, null, memberName, sourceFilePath);
    }

    /// <summary>
    /// Logs all nested reasons and metadata on Log.Debug().
    /// </summary>
    /// <param name="result">The result to use for logging.</param>
    /// <param name="memberName">The function name where the result happened.</param>
    /// <param name="sourceFilePath">The path to the source.</param>
    /// <returns>The result unchanged.</returns>
    public static Result LogDebug(
        this Result result,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "")
    {
        return LogResult(result, LogEventLevel.Debug, null, memberName, sourceFilePath);
    }

    /// <summary>
    /// Logs all nested reasons and metadata on Log.Information().
    /// </summary>
    /// <param name="result">The result to use for logging.</param>
    /// <param name="memberName">The function name where the result happened.</param>
    /// <param name="sourceFilePath">The path to the source.</param>
    /// <returns>The result unchanged.</returns>
    public static Result LogInformation(
        this Result result,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "")
    {
        return LogResult(result, LogEventLevel.Information, null, memberName, sourceFilePath);
    }

    /// <summary>
    /// Logs all nested reasons and metadata on Log.Warning().
    /// </summary>
    /// <param name="result">The result to use for logging.</param>
    /// <param name="memberName">The function name where the result happened.</param>
    /// <param name="sourceFilePath">The path to the source.</param>
    /// <returns>The result unchanged.</returns>
    public static Result LogWarning(
        this Result result,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "")
    {
        return LogResult(result, LogEventLevel.Warning, null, memberName, sourceFilePath);
    }

    /// <summary>
    /// Logs all nested reasons and metadata on Log.Error().
    /// </summary>
    /// <param name="result">The result to use for logging.</param>
    /// <param name="e">The optional exception which can be passed to Log.Error().</param>
    /// <param name="memberName">The function name where the result happened.</param>
    /// <param name="sourceFilePath">The path to the source.</param>
    /// <returns>The result unchanged.</returns>
    public static Result LogError(
        this Result result,
        Exception e = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "")
    {
        return LogResult(result, LogEventLevel.Error, e, memberName, sourceFilePath);
    }

    /// <summary>
    /// Logs all nested reasons and metadata on Log.Fatal().
    /// </summary>
    /// <param name="result">The result to use for logging.</param>
    /// <param name="e">The optional exception which can be passed to Log.Error().</param>
    /// <param name="memberName">The function name where the result happened.</param>
    /// <param name="sourceFilePath">The path to the source.</param>
    /// <returns>The result unchanged.</returns>
    public static Result LogFatal(
        this Result result,
        Exception e = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "")
    {
        return LogResult(result, LogEventLevel.Fatal, e, memberName, sourceFilePath);
    }

    #endregion

    #region Result<T> Signatures

    /// <summary>
    /// Logs all nested reasons and metadata on Log.Verbose().
    /// </summary>
    /// <param name="result">The result to use for logging.</param>
    /// <param name="memberName">The function name where the result happened.</param>
    /// <param name="sourceFilePath">The path to the source.</param>
    /// <typeparam name="T">The result type.</typeparam>
    /// <returns>The result unchanged.</returns>
    public static Result<T> LogVerbose<T>(
        this Result<T> result,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "")
    {
        return LogResult(result, LogEventLevel.Verbose, null, memberName, sourceFilePath);
    }

    /// <summary>
    /// Logs all nested reasons and metadata on Log.Debug().
    /// </summary>
    /// <param name="result">The result to use for logging.</param>
    /// <param name="memberName">The function name where the result happened.</param>
    /// <param name="sourceFilePath">The path to the source.</param>
    /// <typeparam name="T">The result type.</typeparam>
    /// <returns>The result unchanged.</returns>
    public static Result<T> LogDebug<T>(
        this Result<T> result,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "")
    {
        return LogResult(result, LogEventLevel.Debug, null, memberName, sourceFilePath);
    }

    /// <summary>
    /// Logs all nested reasons and metadata on Log.Information().
    /// </summary>
    /// <param name="result">The result to use for logging.</param>
    /// <param name="memberName">The function name where the result happened.</param>
    /// <param name="sourceFilePath">The path to the source.</param>
    /// <typeparam name="T">The result type.</typeparam>
    /// <returns>The result unchanged.</returns>
    public static Result<T> LogInformation<T>(
        this Result<T> result,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "")
    {
        return LogResult(result, LogEventLevel.Information, null, memberName, sourceFilePath);
    }

    /// <summary>
    /// Logs all nested reasons and metadata on Log.Warning().
    /// </summary>
    /// <param name="result">The result to use for logging.</param>
    /// <param name="memberName">The function name where the result happened.</param>
    /// <param name="sourceFilePath">The path to the source.</param>
    /// <typeparam name="T">The result type.</typeparam>
    /// <returns>The result unchanged.</returns>
    public static Result<T> LogWarning<T>(
        this Result<T> result,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "")
    {
        return LogResult(result, LogEventLevel.Warning, null, memberName, sourceFilePath);
    }

    /// <summary>
    /// Logs all nested reasons and metadata on Log.Error().
    /// </summary>
    /// <param name="result">The result to use for logging.</param>
    /// <param name="e">The optional exception which can be passed to Log.Error().</param>
    /// <param name="memberName">The function name where the result happened.</param>
    /// <param name="sourceFilePath">The path to the source.</param>
    /// <typeparam name="T">The result type.</typeparam>
    /// <returns>The result unchanged.</returns>
    public static Result LogError<T>(
        this Result<T> result,
        Exception e = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "")
    {
        return LogResult(result, LogEventLevel.Error, e, memberName, sourceFilePath).ToResult();
    }

    /// <summary>
    /// Logs all nested reasons and metadata on Log.Fatal().
    /// </summary>
    /// <param name="result">The result to use for logging.</param>
    /// <param name="e">The optional exception which can be passed to Log.Error().</param>
    /// <param name="memberName">The function name where the result happened.</param>
    /// <param name="sourceFilePath">The path to the source.</param>
    /// <typeparam name="T">The result type.</typeparam>
    /// <returns>The result unchanged.</returns>
    public static Result LogFatal<T>(
        this Result<T> result,
        Exception e = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "")
    {
        return LogResult(result, LogEventLevel.Fatal, e, memberName, sourceFilePath).ToResult();
    }

    #endregion
}