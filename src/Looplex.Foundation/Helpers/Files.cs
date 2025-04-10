using System.Collections.Generic;
using System.IO;

namespace Looplex.Foundation.Helpers;

public class Files
{
  public static IEnumerable<KeyValuePair<string, string>> LoadEnv(string path)
  {
    if (!File.Exists(path))
    {
      throw new FileNotFoundException($"Environment file not found at path: {path}");
    }
    
    string[] lines;
    try
    {
      lines = File.ReadAllLines(path);
    }
    catch (IOException ex)
    {
      throw new IOException($"Error reading environment file: {ex.Message}", ex);
    }

    foreach (var line in lines)
    {
      if (string.IsNullOrWhiteSpace(line) || line.Trim().StartsWith("#"))
        continue;

      var parts = line.Split(['='], 2);
      if (parts.Length == 2)
      {
        var key = parts[0].Trim();
        var value = parts[1].Trim();
        yield return new KeyValuePair<string, string>(key, value);
      }
    }
  }
}