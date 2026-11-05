namespace LearnHub.Services.Exceptions;

public class NotFoundException(string message) : AppException(message, 404, "NOT_FOUND");
