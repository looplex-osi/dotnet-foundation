using System.Collections.Generic;

namespace Looplex.Protocols.HTTP.Ports;

public interface ISCIMv2Response
{
    string[] Schemas { get; set; }
    int TotalResults { get; set; }
    int ItemsPerPage { get; set; }
    int StartIndex { get; set; }
    object? Data { get; set; }
    List<object> Resources { get; set; }
}

