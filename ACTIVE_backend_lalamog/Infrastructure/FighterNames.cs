using System.Text.RegularExpressions;

namespace Lalamog.Api.Infrastructure;

public static class FighterNames
{
    public const string TakenMessage = "That fighter name is already taken. Choose another; aliases can be shared.";

    public static string Normalize(string value)
    {
        if (value.Length < 2 || value.Length > 50 || !Regex.IsMatch(value, @"\A[A-Za-z0-9][A-Za-z0-9._-]*\z"))
        {
            throw new ApiException(400, "Use 2–50 letters, numbers, dots, underscores or hyphens for your fighter name.");
        }
        return value.ToUpperInvariant();
    }
}
