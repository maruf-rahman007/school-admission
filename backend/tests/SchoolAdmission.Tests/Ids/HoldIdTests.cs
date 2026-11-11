using System.Text.RegularExpressions;
using SchoolAdmission.Api.Ids;

namespace SchoolAdmission.Tests.Ids;

public class HoldIdTests
{
    private const string Hex32 = "3f2a9c1e0b7d4e5fa6c8d9e0f1a2b3c4";

    [Fact]
    public void Format_AnyGuid_ReturnsPrefixAnd32LowercaseHex()
    {
        var id = HoldId.Format(Guid.NewGuid());

        Assert.Matches(new Regex("^h_[0-9a-f]{32}$"), id);
    }

    [Fact]
    public void TryParse_FormattedId_ReturnsSameGuid()
    {
        var guid = Guid.NewGuid();

        var parsed = HoldId.TryParse(HoldId.Format(guid), out var result);

        Assert.True(parsed);
        Assert.Equal(guid, result);
    }

    [Fact]
    public void TryParse_UppercaseHex_ReturnsTrue()
    {
        var parsed = HoldId.TryParse("h_" + Hex32.ToUpperInvariant(), out var result);

        Assert.True(parsed);
        Assert.Equal("h_" + Hex32, HoldId.Format(result));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("h_")]
    [InlineData("h_3f2a9c1e0b7d4e5fa6c8d9e0f1a2b3c")] // 31 hex
    [InlineData("h_3f2a9c1e0b7d4e5fa6c8d9e0f1a2b3c4d")] // 33 hex
    [InlineData("h_3f2a9c1e-0b7d-4e5f-a6c8-d9e0f1a2b3c4")] // dashed UUID
    [InlineData("h_{3f2a9c1e-0b7d-4e5f-a6c8-d9e0f1a2b3c4}")] // braced UUID
    [InlineData("H_3f2a9c1e0b7d4e5fa6c8d9e0f1a2b3c4")] // uppercase prefix
    [InlineData("c_3f2a9c1e0b7d4e5fa6c8d9e0f1a2b3c4")] // class prefix
    [InlineData("3f2a9c1e0b7d4e5fa6c8d9e0f1a2b3c4")] // no prefix
    [InlineData(" h_3f2a9c1e0b7d4e5fa6c8d9e0f1a2b3c4")] // leading space
    [InlineData("h_3f2a9c1e0b7d4e5fa6c8d9e0f1a2b3c4 ")] // trailing space
    [InlineData("h_3f2a9c1e0b7d4e5fa6c8d9e0f1a2b3cg")] // non-hex letter
    [InlineData("h_৩f2a9c1e0b7d4e5fa6c8d9e0f1a2b3c4")] // Bangla digit
    [InlineData("h_３f2a9c1e0b7d4e5fa6c8d9e0f1a2b3c4")] // full-width digit
    [InlineData("h_3f2a9c1e0b7d4e5fa6c8d9e0f1a2b3cé")] // other non-ASCII
    public void TryParse_InvalidInput_ReturnsFalse(string? value)
    {
        var parsed = HoldId.TryParse(value, out var result);

        Assert.False(parsed);
        Assert.Equal(Guid.Empty, result);
    }
}
