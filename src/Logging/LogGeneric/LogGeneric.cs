using Logging.Interface;
using Serilog;
using Log = Logging.Log2.Log;

namespace Logging.LogGeneric;

public class LogGeneric<T> : Log, ILog<T> where T : class
{
    public LogGeneric(ILogger logger) : base(logger)
    {
        ClassType = typeof(T);
    }
}