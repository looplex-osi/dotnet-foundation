using System.Runtime.CompilerServices;

namespace Looplex.Foundation;

public static class ObjectHelper
{
  public static string GetCallerName(this object input, [CallerMemberName] string memberName = "")
  {
    return memberName;
  }
}