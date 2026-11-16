using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace EpinelPS.Models;

[Table("UserCompletedScenarios")]
public class UserCompletedScenarioEntity
{
    [Key]
    public long Id { get; set; }

    public ulong UserId { get; set; }
    public string ScenarioId { get; set; } = "";

    public GameUser User { get; set; } = null!;
}
