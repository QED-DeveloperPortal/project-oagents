using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.AI.DevTeam.Plugins.Mockaco.Model;

public class AppCatalogMethod
{
  public AppCatalogMethod()
  {
    Id = "";
    GroupCode = "";
    DomainName = "";
    MethodType = "";
    MethodPrefix = "";
    Method = "";
    MethodDescription = "";
    FileName = "";
  }

  public string Id { get; set; } // Guid Identifier
  public string GroupCode { get; set; }
  public string DomainName { get; set; }
  public string MethodType { get; set; }
  public string MethodPrefix { get; set; }
  public string Method { get; set; }
  public string MethodDescription { get; set; }
  public string FileName { get; set; }
}
