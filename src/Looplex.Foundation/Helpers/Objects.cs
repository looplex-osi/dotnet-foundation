using System;
using System.Collections;
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
    if (resource == null) throw new ArgumentNullException(nameof(resource));
    if (string.IsNullOrEmpty(propertyName))
      throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));

    PropertyInfo? prop = resource.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
    if (prop != null)
    {
      object? val = prop.GetValue(resource);
      if (val is TProp castVal)
      {
        return castVal;
      }

      if (val == null)
      {
        return default;
      }

      try
      {
        return (TProp)Convert.ChangeType(val, typeof(TProp));
      }
      catch (InvalidCastException ex)
      {
        throw new InvalidOperationException(
          $"Cannot convert property '{propertyName}' from {val.GetType()} to {typeof(TProp)}", ex);
      }
      catch (FormatException ex)
      {
        throw new InvalidOperationException(
          $"Cannot convert property '{propertyName}' value '{val}' to {typeof(TProp)}", ex);
      }
    }

    return default;
  }

  public static string? GetFirstEmailValue(this object resource)
  {
    // Assume resource has a property named "Emails" that is an IEnumerable.
    PropertyInfo? emailsProp = resource.GetType().GetProperty("Emails", BindingFlags.Public | BindingFlags.Instance);
    if (emailsProp == null)
    {
      return null;
    }

    if (emailsProp.GetValue(resource) is IEnumerable emails)
    {
      foreach (object? item in emails)
      {
        // Assume each email item has a property "Value".
        PropertyInfo? valueProp = item.GetType().GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
        if (valueProp != null)
        {
          object? val = valueProp.GetValue(item);
          if (val != null)
          {
            return val.ToString();
          }
        }
      }
    }

    return null;
  }
}