using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Looplex.Foundation.Helpers;

public static class Objects
{
  public static string GetCallerName(this object input, [CallerMemberName] string memberName = "")
  {
    return memberName; 
  }

  public static TProp? GetPropertyValue<TProp>(this object resource, string propertyName)
  {
    PropertyInfo? prop = resource.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
    if (prop != null)
    {
      object? val = prop.GetValue(resource);
      if (val is TProp castVal)
      {
        return castVal;
      }

      return (TProp)Convert.ChangeType(val, typeof(TProp));
    }

    return default;
  }
}