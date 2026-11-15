using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EpinelPS.Utils;

namespace EpinelPS.Models;

public class ServerSettings
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; } = 1;

    public LogType LogLevel { get; set; } = LogType.Debug;
    public int MaxInterceptionCount { get; set; } = 3;
    public int ResetHourUtcTime { get; set; } = 20;
}
