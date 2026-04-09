using System.ComponentModel.DataAnnotations;

namespace TrafagSalesExporter.Models;

public class HanaServer
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; }

    public string Username { get; set; } = string.Empty;

    public string EncryptedPassword { get; set; } = string.Empty;

    public List<Site> Sites { get; set; } = [];
}
