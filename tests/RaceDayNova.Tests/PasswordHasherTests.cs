using RaceDayNova.Api.Data;
using RaceDayNova.Api.Security;

namespace RaceDayNova.Tests;

/// <summary>Pure unit tests (no HTTP, no database) for password hashing and the password policy.</summary>
public class PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void Hash_DoesNotContainThePlainTextPassword()
    {
        var hash = _hasher.Hash("Password123!");
        Assert.DoesNotContain("Password123!", hash);
        Assert.StartsWith("PBKDF2$", hash);
    }

    [Fact]
    public void Hash_UsesARandomSalt_SoTwoHashesOfTheSamePasswordDiffer()
    {
        Assert.NotEqual(_hasher.Hash("Password123!"), _hasher.Hash("Password123!"));
    }

    [Fact]
    public void Verify_ReturnsTrueForTheCorrectPassword()
    {
        var hash = _hasher.Hash("Password123!");
        Assert.True(_hasher.Verify("Password123!", hash));
    }

    [Fact]
    public void Verify_ReturnsFalseForAWrongPassword()
    {
        var hash = _hasher.Hash("Password123!");
        Assert.False(_hasher.Verify("password123!", hash));
        Assert.False(_hasher.Verify("", hash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("PART1_HASH_001")]
    [InlineData("PBKDF2$abc$def$ghi")]
    [InlineData("PBKDF2$210000$not-base64$also-not-base64")]
    public void Verify_ReturnsFalseForMalformedHashes_InsteadOfThrowing(string storedHash)
    {
        Assert.False(_hasher.Verify("Password123!", storedHash));
    }

    [Theory]
    [InlineData("thandi.maseko@racedaynova.co.za")]
    [InlineData("johan.botha@racedaynova.co.za")]
    [InlineData("lebo.molefe@racedaynova.co.za")]
    [InlineData("zanele.khumalo@racedaynova.co.za")]
    public void SeededHashes_VerifyAgainstTheDocumentedDemoPassword(string email)
    {
        Assert.True(_hasher.Verify(SeedData.DemoPassword, SeedData.SeededHashFor(email)));
    }

    [Theory]
    [InlineData("short1", false)]
    [InlineData("alllettersnodigits", false)]
    [InlineData("12345678", false)]
    [InlineData("Password123!", true)]
    [InlineData("abcdefg1", true)]
    public void PasswordPolicy_EnforcesLengthLetterAndDigit(string password, bool expected)
    {
        Assert.Equal(expected, PasswordPolicy.IsValid(password, out _));
    }
}
