namespace Looplex.Samples.Application;

/// <summary>
/// Filter parameters for Pad stored procedures
/// </summary>
public class PadFilterParameters
{
    public string? Ids { get; set; }
    public string? Uuids { get; set; }
    public string? Name { get; set; }
    public bool? Active { get; set; }
    public int? Status { get; set; }
    public DateTime? CreatedBegin { get; set; }
    public DateTime? CreatedEnd { get; set; }
    public DateTime? UpdatedBegin { get; set; }
    public DateTime? UpdatedEnd { get; set; }
}

/// <summary>
/// Filter parameters for Note stored procedures
/// </summary>
public class NoteFilterParameters
{
    public string? Ids { get; set; }
    public string? Uuids { get; set; }
    public string? Text { get; set; }
    public string? PadGuids { get; set; }
    public bool? Active { get; set; }
    public int? Status { get; set; }
    public DateTime? CreatedBegin { get; set; }
    public DateTime? CreatedEnd { get; set; }
    public DateTime? UpdatedBegin { get; set; }
    public DateTime? UpdatedEnd { get; set; }
}



