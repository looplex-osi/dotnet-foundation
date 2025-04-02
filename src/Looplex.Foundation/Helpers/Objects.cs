using System.Runtime.CompilerServices;

namespace Looplex.Foundation.Helpers;

public static class Objects
{
  public static string GetCallerName(this object input, [CallerMemberName] string memberName = "")
  {
    return memberName; 
  }
}