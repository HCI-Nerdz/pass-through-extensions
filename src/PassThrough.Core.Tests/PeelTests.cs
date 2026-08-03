using PassThrough.Core;
using Xunit;

namespace PassThrough.Core.Tests;

public class PeelTests
{
    static readonly string[] Defaults = DefaultMetaSuffixes.All.ToArray();

    [Fact]
    public void Peels_json_example()
    {
        var r = Peel.Invoke(@"C:\tmp\appsettings.json.example", Defaults);
        Assert.Equal(["example"], r.Peeled);
        Assert.Equal(".json", r.InnerExt);
    }

    [Fact]
    public void Peels_env_example()
    {
        var r = Peel.Invoke(@"C:\tmp\.env.example", Defaults);
        Assert.Equal(["example"], r.Peeled);
        Assert.Equal(".env", r.InnerExt);
    }

    [Fact]
    public void Peels_old()
    {
        var r = Peel.Invoke(@"C:\tmp\config.yaml.old", Defaults);
        Assert.Equal(["old"], r.Peeled);
        Assert.Equal(".yaml", r.InnerExt);
    }

    [Fact]
    public void Peels_chained_badges()
    {
        var r = Peel.Invoke(@"C:\tmp\notes.txt.example.bak", Defaults);
        Assert.Equal(["bak", "example"], r.Peeled);
        Assert.Equal(".txt", r.InnerExt);
    }

    [Fact]
    public void Does_not_peel_tar_gz()
    {
        var r = Peel.Invoke(@"C:\tmp\archive.tar.gz", Defaults);
        Assert.Empty(r.Peeled);
        Assert.Equal(".gz", r.InnerExt);
    }

    [Fact]
    public void Bare_file_example_has_no_inner_type()
    {
        var r = Peel.Invoke(@"C:\tmp\file.example", Defaults);
        Assert.Equal(["example"], r.Peeled);
        Assert.Null(r.InnerExt);
    }

    [Fact]
    public void Custom_suffix_peels()
    {
        var active = Defaults.Append("mine").ToArray();
        var r = Peel.Invoke(@"C:\tmp\data.json.mine", active);
        Assert.Equal(["mine"], r.Peeled);
        Assert.Equal(".json", r.InnerExt);
    }

    [Fact]
    public void Settings_merges_custom_and_respects_disabled()
    {
        var s = new PassThroughSettings
        {
            CustomSuffixes = ["mine", ".Mine", "gz"],
            DisabledDefaults = ["bak", "old"],
        };
        var active = s.GetActiveSuffixes();
        Assert.Contains("example", active);
        Assert.Contains("mine", active);
        Assert.DoesNotContain("bak", active);
        Assert.DoesNotContain("old", active);
        Assert.DoesNotContain("gz", active);
    }
}
