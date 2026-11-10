namespace LearnHub.Data.Interfaces;

public interface ICreationTracked
{
    DateTime CreatedAt { get; set; }
    string? CreatedBy { get; set; }
}
