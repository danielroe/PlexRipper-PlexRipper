using System.Runtime.CompilerServices;
using Logging.Interface;
using Serilog.Core;
using Serilog.Events;

namespace Logging.Log2;

public partial class Log : ILog
{
    #region Debug

    /// <inheritdoc/>
    [MessageTemplateFormatMethod("messageTemplate")]
    public LogEvent DebugLine(
        string messageTemplate,
        [CallerMemberName] string memberName = default!,
        [CallerFilePath] string sourceFilePath = default!,
        [CallerLineNumber] int sourceLineNumber = default!)
    {
        // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
        return Write(LogEventLevel.Debug, messageTemplate, memberName!, sourceFilePath!, sourceLineNumber);
    }

    /// <inheritdoc/>
    [MessageTemplateFormatMethod("messageTemplate")]
    public LogEvent Debug(
        Exception ex,
        string messageTemplate,
        [CallerMemberName] string memberName = default!,
        [CallerFilePath] string sourceFilePath = default!,
        [CallerLineNumber] int sourceLineNumber = default!)
    {
        // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
        return Write(LogEventLevel.Debug, messageTemplate, ex, memberName!, sourceFilePath!, sourceLineNumber);
    }

    /// <inheritdoc/>
    [MessageTemplateFormatMethod("messageTemplate")]
    public LogEvent Debug<T>(
        string messageTemplate,
        T propertyValue = default!,
        [CallerMemberName] string memberName = default!,
        [CallerFilePath] string sourceFilePath = default!,
        [CallerLineNumber] int sourceLineNumber = default!)
    {
        // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
        return Write(LogEventLevel.Debug, messageTemplate, memberName!, sourceFilePath!, sourceLineNumber, propertyValue);
    }

    /// <inheritdoc/>
    [MessageTemplateFormatMethod("messageTemplate")]
    public LogEvent Debug<T0, T1>(
        string messageTemplate,
        T0 propertyValue0,
        T1 propertyValue1,
        [CallerMemberName] string memberName = default!,
        [CallerFilePath] string sourceFilePath = default!,
        [CallerLineNumber] int sourceLineNumber = default!)
    {
        // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
        return Write(LogEventLevel.Debug, messageTemplate, memberName!, sourceFilePath!, sourceLineNumber, propertyValue0, propertyValue1);
    }

    /// <inheritdoc/>
    [MessageTemplateFormatMethod("messageTemplate")]
    public LogEvent Debug<T0, T1, T2>(
        string messageTemplate,
        T0 propertyValue0,
        T1 propertyValue1,
        T2 propertyValue2,
        [CallerMemberName] string memberName = default!,
        [CallerFilePath] string sourceFilePath = default!,
        [CallerLineNumber] int sourceLineNumber = default!)
    {
        // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
        return Write(LogEventLevel.Debug, messageTemplate, memberName!, sourceFilePath!, sourceLineNumber, propertyValue0, propertyValue1, propertyValue2);
    }

    [MessageTemplateFormatMethod("messageTemplate")]
    public LogEvent Debug<T0, T1, T2, T3>(
        string messageTemplate,
        T0 propertyValue0,
        T1 propertyValue1,
        T2 propertyValue2,
        T3 propertyValue3,
        [CallerMemberName] string memberName = default!,
        [CallerFilePath] string sourceFilePath = default!,
        [CallerLineNumber] int sourceLineNumber = default!)
    {
        // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
        return Write(LogEventLevel.Debug, messageTemplate, memberName!, sourceFilePath!, sourceLineNumber, propertyValue0, propertyValue1, propertyValue2,
            propertyValue3);
    }

    [MessageTemplateFormatMethod("messageTemplate")]
    public LogEvent Debug<T0, T1, T2, T3, T4>(
        string messageTemplate,
        T0 propertyValue0,
        T1 propertyValue1,
        T2 propertyValue2,
        T3 propertyValue3,
        T4 propertyValue4,
        [CallerMemberName] string memberName = default!,
        [CallerFilePath] string sourceFilePath = default!,
        [CallerLineNumber] int sourceLineNumber = default!)
    {
        // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
        return Write(LogEventLevel.Debug, messageTemplate, memberName!, sourceFilePath!, sourceLineNumber, propertyValue0, propertyValue1, propertyValue2,
            propertyValue3, propertyValue4);
    }

    [MessageTemplateFormatMethod("messageTemplate")]
    public LogEvent Debug<T0, T1, T2, T3, T4, T5>(
        string messageTemplate,
        T0 propertyValue0,
        T1 propertyValue1,
        T2 propertyValue2,
        T3 propertyValue3,
        T4 propertyValue4,
        T5 propertyValue5,
        [CallerMemberName] string memberName = default!,
        [CallerFilePath] string sourceFilePath = default!,
        [CallerLineNumber] int sourceLineNumber = default!)
    {
        // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
        return Write(LogEventLevel.Debug, messageTemplate, memberName!, sourceFilePath!, sourceLineNumber, propertyValue0, propertyValue1, propertyValue2,
            propertyValue3, propertyValue4, propertyValue5);
    }

    #endregion
}