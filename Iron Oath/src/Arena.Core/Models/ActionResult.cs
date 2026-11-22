namespace Arena.Core;

public sealed record ActionResult(bool Success, string Message)
{
    public static ActionResult Fail(string message) => new(false, message);
    public static ActionResult Ok(string message = "") => new(true, message);
}
