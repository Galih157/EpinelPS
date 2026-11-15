using System.ComponentModel.DataAnnotations;

namespace EpinelPS.Models;

public class EventBanner
{
    [Key]
    public string BannerId { get; set; } = "";

    public bool IsActive { get; set; }
}
