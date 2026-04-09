using System.ComponentModel.DataAnnotations;

namespace TrafagSalesExporter.Models;

public class HanaServer
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 30015;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
