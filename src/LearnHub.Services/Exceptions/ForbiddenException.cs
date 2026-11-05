namespace LearnHub.Services.Exceptions;

public class ForbiddenException(string message) : AppException(message, 403, "FORBIDDEN");
