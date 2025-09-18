using System;

namespace Looplex.Foundation.SearchContent.Parser;

/// <summary>
/// Exception thrown when a SCIM filter expression cannot be parsed
/// 
/// Provides detailed information about parsing errors including position and context
/// </summary>
public class FilterParseException : Exception
{
    /// <summary>
    /// Position in the filter expression where the error occurred
    /// </summary>
    public int Position { get; }

    /// <summary>
    /// Line number where the error occurred (for multi-line expressions)
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// Column number where the error occurred
    /// </summary>
    public int Column { get; }

    public FilterParseException(string message) : base(message)
    {
        Position = -1;
        Line = 1;
        Column = 1;
    }

    public FilterParseException(string message, Exception innerException) : base(message, innerException)
    {
        Position = -1;
        Line = 1;
        Column = 1;
    }

    public FilterParseException(string message, int position) : base(message)
    {
        Position = position;
        Line = 1;
        Column = position + 1;
    }

    public FilterParseException(string message, int line, int column, int position) : base(message)
    {
        Line = line;
        Column = column;
        Position = position;
    }

    public override string ToString()
    {
        var baseString = base.ToString();
        
        if (Position >= 0)
        {
            baseString += $"\nPosition: {Position}";
        }
        
        if (Line > 1 || Column > 1)
        {
            baseString += $"\nLine: {Line}, Column: {Column}";
        }
        
        return baseString;
    }
}


