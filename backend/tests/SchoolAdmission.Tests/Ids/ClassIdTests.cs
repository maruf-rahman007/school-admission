using System.Text.RegularExpressions;
using SchoolAdmission.Api.Ids;

namespace SchoolAdmission.Tests.Ids;

public class ClassIdTests
{
    private const string Hex32 = "3f2a9c1e0b7d4e5fa6c8d9e0f1a2b3c4";

    [Fact]
    public void Format_AnyGuid_ReturnsPrefixAnd32LowercaseHex()
    {
        var id = ClassId.Format(Guid.NewGuid());

        Assert.Matches(new Regex("^c_[0-9a-f]{32}$"), id);
    }

    [Fact]
    public void TryParse_FormattedId_ReturnsSameGuid()
    {
        var guid = Guid.NewGuid();

        var parsed = ClassId.TryParse(ClassId.Format(guid), out var result);

        Assert.True(parsed);
        Assert.Equal(guid, result);
    }

    [Fact]
    public void TryParse_UppercaseHex_ReturnsTrue()
    {
        var parsed = ClassId.TryParse("c_" + Hex32.ToUpperInvariant(), out var result);

        Assert.True(parsed);
        Assert.Equal("c_" + Hex32, ClassId.Format(result));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("c_")]
    [InlineData("c_3f2a9c1e0b7d4e5fa6c8d9e0f1a2b3c")] // 31 hex
    [InlineData("c_3f2a9c1e0b7d4e5fa6c8d9e0f1a2b3c4d")] // 33 hex
    [InlineData("c_3f2a9c1e-0b7d-4e5f-a6c8-d9e0f1a2b3c4")] // dashed UUID
    [InlineData("c_{3f2a9c1e-0b7d-4e5f-a6c8-d9e0f1a2b3c4}")] // braced UUID
    [InlineData("C_3f2a9c1e0b7d4e5fa6c8d9e0f1a2b3c4")] // uppercase prefix
    [InlineData("h_3f2a9c1e0b7d4e5fa6c8d9e0f1a2b3c4")] // hold prefix
    [InlineData("3f2a9c1e0b7d4e5fa6c8d9e0f1a2b3c4")] // no prefix
    [InlineData(" c_3f2a9c1e0b7d4e5fa6c8d9e0f1a2b3c4")] // leading space
    [InlineData("c_3f2a9c1e0b7d4e5fa6c8d9e0f1a2b3c4 ")] // trailing space
    [InlineData("c_3f2a9c1e0b7d4e5fa6c8d9e0f1a2b3cg")] // non-hex letter
    [InlineData("c_৩f2a9c1e0b7d4e5fa6c8d9e0f1a2b3c4")] // Bangla digit
    [InlineData("c_３f2a9c1e0b7d4e5fa6c8d9e0f1a2b3c4")] // full-width digit
    [InlineData("c_3f2a9c1e0b7d4e5fa6c8d9e0f1a2b3cé")] // other non-ASCII
    public void TryParse_InvalidInput_ReturnsFalse(string? value)
    {
        var parsed = ClassId.TryParse(value, out var result);

        Assert.False(parsed);
        Assert.Equal(Guid.Empty, result);
    }
}
