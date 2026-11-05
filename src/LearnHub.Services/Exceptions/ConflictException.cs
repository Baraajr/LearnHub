namespace LearnHub.Services.Exceptions;

public class ConflictException(string message) : AppException(message, 409, "CONFLICT");
