namespace LearnHub.Data.Interfaces;

public interface IModificationTracked
{
    DateTime? UpdatedAt { get; set; }
    string? UpdatedBy { get; set; }
}
