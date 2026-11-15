using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EpinelPS.Models;

public class ServerSecrets
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; } = 1;

    public byte[] LauncherTokenKey { get; set; } = [];
    public byte[] EncryptionTokenKey { get; set; } = [];
}
