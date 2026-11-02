using Lalamog.Api.Infrastructure;
using Xunit;

namespace Lalamog.Api.Tests;

public class PasswordHashingTests
{
    [Fact]
    public void NativeHashesAreSaltedAndVerifyOnlyTheCorrectPassword()
    {
        string first = PasswordHashing.Hash("Practice123!Example");
        string second = PasswordHashing.Hash("Practice123!Example");
        Assert.NotEqual(first, second);
        Assert.DoesNotContain("Practice123!Example", first);
        Assert.True(PasswordHashing.Verify(first, "Practice123!Example"));
        Assert.False(PasswordHashing.Verify(first, "wrong"));
    }

    [Theory]
    [InlineData("AAABAgMEBQYHCAkKCwwNDg9RWJYABNCJjjf92tgvvB75e5JhLgfrTigz2rivaw3f9Q==")]
    [InlineData("AQAAAAAAAAPoAAAAEAABAgMEBQYHCAkKCwwNDg9RWJYABNCJjjf92tgvvB75e5JhLgfrTigz2rivaw3f9Q==")]
    [InlineData("AQAAAAEAACcQAAAAEAABAgMEBQYHCAkKCwwNDg/cYu9i+zR7bO6Me3qbN5fTmAVoRd3blqbUJahe45WJZA==")]
    [InlineData("AQAAAAIAAYagAAAAEAABAgMEBQYHCAkKCwwNDg/x+D7MHTUzdF2GX4CN2BrHMo1FJG0yVF0JTgSoDn74Iw==")]
    public void ExistingPasswordVectorsStillVerify(string storedHash)
    {
        Assert.True(PasswordHashing.Verify(storedHash, "Practice123!Example"));
        Assert.False(PasswordHashing.Verify(storedHash, "wrong"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid-base64")]
    [InlineData("AQAAAAI=")]
    [InlineData("Ag==")]
    public void MalformedHashesAreRejected(string? hash)
    {
        Assert.False(PasswordHashing.Verify(hash, "Practice123!Example"));
    }
}
