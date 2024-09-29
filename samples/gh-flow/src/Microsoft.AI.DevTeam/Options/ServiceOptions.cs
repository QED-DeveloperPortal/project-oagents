using System.ComponentModel.DataAnnotations;

namespace Microsoft.AI.DevTeam;
public class ServiceOptions
{
    private string _ingesterUrl;

    [Required]
    public string IngesterUrl { get; set; }

    /// <summary>
    /// Local directory from which to load semantic plugins.
    /// </summary>
    public string? SemanticPluginsDirectory { get; set; }

    /// <summary>
    /// Local directory from which to load native plugins.
    /// </summary>
    public string? NativePluginsDirectory { get; set; }
}