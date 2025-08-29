using System.ComponentModel.DataAnnotations;

namespace Wookashi.FeatureSwitcher.Node.Core.Configuration;

public class DatabaseOptions
{
    public string Type { get; set; }

    [Required]
    public string ConnectionString { get; set; }
}
