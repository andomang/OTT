using System.Drawing;
using PersonalPro;
using Xunit;

public class LayoutCalculatorTests
{
    private static long Area(Rectangle[] rects) => rects.Sum(r => (long)r.Width * r.Height);

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void 모든_칸이_겹치지_않고_영역을_채운다(int count)
    {
        var rects = LayoutCalculator.Compute(count, 1001, 777, -1, 0.5, 0.5);
        Assert.Equal(count, rects.Length);
        Assert.Equal(1001L * 777, Area(rects));
    }

    [Theory]
    [InlineData(2, 0)]
    [InlineData(3, 1)]
    [InlineData(4, 3)]
    public void 크게_보기도_영역을_채운다(int count, int main)
    {
        var rects = LayoutCalculator.Compute(count, 1000, 701, main, 0.72, 0.5);
        Assert.Equal(1000L * 701, Area(rects));
        Assert.Equal(new Rectangle(0, 0, 720, 701), rects[main]);
    }

    [Fact]
    public void 세_칸이면_세번째가_아래_전체를_차지한다()
    {
        var rects = LayoutCalculator.Compute(3, 1000, 600, -1, 0.5, 0.5);
        Assert.Equal(new Rectangle(0, 300, 1000, 300), rects[2]);
    }

    [Fact]
    public void 칸이_없으면_빈_배열() =>
        Assert.Empty(LayoutCalculator.Compute(0, 100, 100, -1, 0.5, 0.5));

    [Theory]
    [InlineData(0.0, 0.2)]
    [InlineData(1.0, 0.8)]
    [InlineData(0.4, 0.4)]
    public void 경계선은_범위_안으로_제한된다(double input, double expected) =>
        Assert.Equal(expected, LayoutCalculator.ClampSplit(input));
}

public class OttValidatorTests
{
    private static readonly string[] Names = { "NETFLIX", "웨이브" };

    [Fact]
    public void 올바른_입력은_통과한다() =>
        Assert.Null(OttValidator.Validate("Pinterest", "https://www.pinterest.co.kr", "", Names));

    [Theory]
    [InlineData("a,b", "https://x.com")]
    [InlineData("a|b", "https://x.com")]
    [InlineData("a", "https://x.com/a,b")]
    public void 구분자는_거부한다(string name, string url) =>
        Assert.NotNull(OttValidator.Validate(name, url, "", Names));

    [Theory]
    [InlineData("http")]
    [InlineData("httpfoo")]
    [InlineData("ftp://x.com")]
    [InlineData("javascript:alert(1)")]
    public void 잘못된_URL은_거부한다(string url) =>
        Assert.NotNull(OttValidator.Validate("a", url, "", Names));

    [Fact]
    public void 같은_이름은_대소문자_무관하게_거부한다() =>
        Assert.NotNull(OttValidator.Validate("netflix", "https://x.com", "", Names));

    [Fact]
    public void 수정할_때_자기_이름은_허용한다() =>
        Assert.Null(OttValidator.Validate("웨이브", "https://x.com", "웨이브", Names));
}

public class PresetFormatTests
{
    [Fact]
    public void 저장한_값을_그대로_읽는다()
    {
        string?[] slots = { "NETFLIX", null, "YOUTUBE", null };
        Assert.Equal(slots, PresetFormat.Parse(PresetFormat.Serialize(slots)));
    }

    [Fact]
    public void 줄바꿈과_짧은_파일도_4칸으로_읽는다() =>
        Assert.Equal(new string?[] { "A", null, null, null }, PresetFormat.Parse("A\r\n"));

    [Fact]
    public void 다섯번째_값은_무시한다() =>
        Assert.Equal(4, PresetFormat.Parse("A,B,C,D,E").Length);
}

public class PathNamesTests
{
    [Theory]
    [InlineData("a/b", "a_b")]
    [InlineData("a:b*c", "a_b_c")]
    [InlineData("..", "_")]
    [InlineData("넷플릭스", "넷플릭스")]
    public void 경로에_위험한_문자를_바꾼다(string input, string expected) =>
        Assert.Equal(expected.Replace('_', '_'), PathNames.Sanitize(input));
}

public class OttOrderingTests
{
    [Fact]
    public void 저장된_순서대로_정렬하고_나머지는_뒤에_붙는다()
    {
        var sorted = OttOrdering.Sort(new[] { "A", "B", "C", "D" }, x => x, new List<string> { "C", "A" });
        Assert.Equal(new[] { "C", "A", "B", "D" }, sorted);
    }

    [Fact]
    public void 한_칸_위로_옮긴다() =>
        Assert.Equal(new[] { "B", "A", "C" }, OttOrdering.Move(new[] { "A", "B", "C" }, "B", -1));

    [Theory]
    [InlineData("A", -1)]
    [InlineData("C", 1)]
    [InlineData("없음", 1)]
    public void 끝을_넘으면_그대로다(string name, int delta) =>
        Assert.Equal(new[] { "A", "B", "C" }, OttOrdering.Move(new[] { "A", "B", "C" }, name, delta));
}

public class StatsAggregatorTests
{
    private static readonly Dictionary<string, Dictionary<string, int>> Data = new()
    {
        ["2026-10-01"] = new() { ["A"] = 100, ["B"] = 50 },
        ["2026-10-20"] = new() { ["B"] = 200 }
    };

    [Fact]
    public void 전체_기간을_합산해_큰_순서로_반환한다()
    {
        var rows = StatsAggregator.Total(Data, null);
        Assert.Equal(new[] { ("B", 250), ("A", 100) }, rows);
    }

    [Fact]
    public void 시작일_이후만_합산한다() =>
        Assert.Equal(new[] { ("B", 200) }, StatsAggregator.Total(Data, "2026-10-02"));

    [Fact]
    public void 넘치는_항목은_기타로_합친다()
    {
        var rows = new List<(string, int)> { ("A", 4), ("B", 3), ("C", 2), ("D", 1) };
        Assert.Equal(new[] { ("A", 4), ("기타", 6) }, StatsAggregator.FoldOther(rows, 2));
    }

    [Theory]
    [InlineData(30, "1분 미만")]
    [InlineData(90, "1분")]
    [InlineData(3900, "1시간 5분")]
    public void 시간을_읽기_좋게_표시한다(int seconds, string expected) =>
        Assert.Equal(expected, StatsAggregator.FormatDuration(seconds));
}
