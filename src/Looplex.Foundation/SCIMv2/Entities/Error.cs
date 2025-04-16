using System;

using Looplex.Foundation.Entities;

namespace Looplex.Foundation.SCIMv2.Entities;

public class SCIMv2Exception : Exception
{
  public Error Error { get; private set; }

  public SCIMv2Exception(
    string detail,
    ErrorScimType scimType,
    int status,
    Exception? innerException = null) : base(detail, innerException)
  {
    Error = new Error(detail, scimType, status);
  }

  public SCIMv2Exception(
    string detail,
    int status,
    Exception? innerException = null) : base(detail, innerException)
  {
    Error = new Error(detail, status);
  }
}

public class Error : Actor
{
  public string? Detail { get; private set; }
  public ErrorScimType? ScimType { get; private set; }
  public bool ShouldSerializeScimType => ScimType.HasValue;

  public int Status { get; private set; }

  #region Reflectivity

  public Error() : base()
  {
    Detail = string.Empty;
    Status = 0;
  }

  #endregion

  public Error(
    string detail,
    ErrorScimType scimType,
    int status) : base()
  {
    Detail = detail;
    ScimType = scimType;
    Status = status;
  }

  public Error(
    string detail,
    int status) : base()
  {
    Detail = detail;
    Status = status;
  }
}

public enum ErrorScimType
{
  InvalidFilter,
  InvalidPath,
  InvalidSyntax,
  InvalidValue,
  InvalidVers,
  Mutability,
  NoTarget,
  Sensitive,
  TooMany,
  Uniqueness
}