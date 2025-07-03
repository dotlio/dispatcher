using System.Runtime.CompilerServices;

namespace DotLio.Dispatcher.Internals;

public struct RequestContext
{
    public readonly string RequestTypeName;
    public readonly Type RequestType;
    public readonly DateTime StartTime;
    
    public RequestContext(Type requestType)
    {
        RequestType = requestType;
        RequestTypeName = requestType.Name;
        StartTime = DateTime.UtcNow;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TimeSpan GetElapsed() => DateTime.UtcNow - StartTime; 
}