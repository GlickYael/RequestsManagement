namespace RequestsManagement.Enums
{
    public static class UpdateStatusResultExtensions
    {
        public static string ToMessage(this UpdateStatusResult result) => result switch
        {
            UpdateStatusResult.NotFound => "Request was not found.",
            UpdateStatusResult.InvalidStatus => "Invalid status value or unsupported status transition.",
            UpdateStatusResult.Conflict => "Concurrency conflict: the request has been modified by another user.",
            _ => "An unexpected error occurred during update."
        };
    }
}
