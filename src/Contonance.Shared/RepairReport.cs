using System.ComponentModel.DataAnnotations;

namespace Contonance.Shared;

public class RepairReport
{
    public Guid Id { get; set; }

    [Required, MinLength(3, ErrorMessage = "Please use a Title bigger than 3 letters."), MaxLength(100, ErrorMessage = "Please use a Title less than 100 letters.")]
    public string Title { get; set; }

    [MaxLength(250)]
    public string Details { get; set; }

    public DateOnly DueDate { get; set; }

    public Severity Severity { get; set; }
}
