// Polyfill required for C# 9+ records and init-only setters on .NET Framework 4.x
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
