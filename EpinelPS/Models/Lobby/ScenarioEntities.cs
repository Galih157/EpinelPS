using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EpinelPS.Utils;

namespace EpinelPS.Models;

[Table("UserCompletedScenarios")]
public class UserCompletedScenarioEntity
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }
    public string ScenarioId { get; set; } = "";

    public GameUser User { get; set; } = null!;
}
