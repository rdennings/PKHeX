using FluentAssertions;
using Xunit;

namespace PKHeX.Core.Tests.Saves;

public class Zukan4Tests
{
    // Offsets within the Pokédex block.
    private const int Shellos = 0x108;
    private const int Gastrodon = 0x109;
    private const int Burmy = 0x10A;
    private const int ShayminPt = 0x320;
    private const int GiratinaPt = 0x321;
    private const int ShayminHGSS = 0x33C;
    private const int GiratinaHGSS = 0x33D;

    private const byte None = Zukan4.FORM_NONE;

    private static SAV4 GetSave(GameVersion version) => version switch
    {
        GameVersion.DP => new SAV4DP(),
        GameVersion.Pt => new SAV4Pt(),
        _ => new SAV4HGSS(),
    };

    private static void SeeForms(SAV4 sav, Species species, int offset, params byte[] forms)
    {
        sav.Dex.Data[offset] = 0xFF; // initialized value
        foreach (var form in forms)
            sav.Dex.SetDex(new PK4 { Species = (ushort)species, Form = form });
    }

    [Theory]
    [InlineData(new byte[] { 0 }, 0xFC, 0, None)]
    [InlineData(new byte[] { 1 }, 0xFF, 1, None)]
    [InlineData(new byte[] { 0, 1 }, 0xFE, 0, 1)]
    [InlineData(new byte[] { 1, 0 }, 0xFD, 1, 0)]
    [InlineData(new byte[] { 0, 0 }, 0xFC, 0, None)]
    [InlineData(new byte[] { 1, 0, 1, 0 }, 0xFD, 1, 0)]
    public void TwoFormsMatchGame(byte[] seen, byte expect, byte first, byte second)
    {
        foreach (var version in new[] { GameVersion.DP, GameVersion.Pt, GameVersion.HGSS })
        {
            var sav = GetSave(version);
            SeeForms(sav, Species.Shellos, Shellos, seen);
            sav.Dex.Data[Shellos].Should().Be(expect);
            sav.Dex.GetForms((ushort)Species.Shellos).Should().Equal(first, second);

            SeeForms(sav, Species.Gastrodon, Gastrodon, seen);
            sav.Dex.Data[Gastrodon].Should().Be(expect);
            sav.Dex.GetForms((ushort)Species.Gastrodon).Should().Equal(first, second);
        }
    }

    [Theory]
    [InlineData(GameVersion.Pt, ShayminPt, GiratinaPt)]
    [InlineData(GameVersion.HGSS, ShayminHGSS, GiratinaHGSS)]
    public void TwoFormsLegendaries(GameVersion version, int shaymin, int giratina)
    {
        var sav = GetSave(version);
        SeeForms(sav, Species.Shaymin, shaymin, 1);
        sav.Dex.Data[shaymin].Should().Be(0xFF);
        sav.Dex.GetForms((ushort)Species.Shaymin).Should().Equal(1, None);

        SeeForms(sav, Species.Giratina, giratina, 0, 1);
        sav.Dex.Data[giratina].Should().Be(0xFE);
        sav.Dex.GetForms((ushort)Species.Giratina).Should().Equal(0, 1);
    }

    [Fact]
    public void TwoFormsUnseen()
    {
        var sav = new SAV4Pt();
        sav.Dex.Data[Shellos] = 0xFC;
        sav.Dex.GetForms((ushort)Species.Shellos).Should().Equal(None, None);

        sav.Dex.SetForms((ushort)Species.Shellos, []);
        sav.Dex.Data[Shellos].Should().Be(0xFF);
    }

    [Fact]
    public void NoLegendaryFormsDP()
    {
        var sav = new SAV4DP();
        sav.Dex.GetForms((ushort)Species.Shaymin).Should().BeEmpty();
        sav.Dex.GetForms((ushort)Species.Giratina).Should().BeEmpty();

        var before = sav.Dex.Data.ToArray();
        sav.Dex.SetForms((ushort)Species.Shaymin, [1]);
        sav.Dex.SetForms((ushort)Species.Giratina, [1]);
        sav.Dex.Data.ToArray().Should().Equal(before);
    }

    [Theory]
    [InlineData(GameVersion.DP)]
    [InlineData(GameVersion.Pt)]
    [InlineData(GameVersion.HGSS)]
    public void SpindaFirstSeen(GameVersion version)
    {
        var sav = GetSave(version);
        sav.Dex.SetDex(new PK4 { Species = (ushort)Species.Spinda, PID = 0x12345678 });
        sav.Dex.SpindaPID.Should().Be(0x12345678);

        sav.Dex.SetDex(new PK4 { Species = (ushort)Species.Spinda, PID = 0x87654321 });
        sav.Dex.SpindaPID.Should().Be(0x12345678);
    }

    [Theory]
    [InlineData(new byte[] { 0 }, 0xFC, 0, None, None)]
    [InlineData(new byte[] { 0, 2 }, 0xF8, 0, 2, None)]
    [InlineData(new byte[] { 2, 1, 0 }, 0xC6, 2, 1, 0)]
    public void ThreeFormsRoundTrip(byte[] seen, byte expect, byte first, byte second, byte third)
    {
        var sav = new SAV4Pt();
        SeeForms(sav, Species.Burmy, Burmy, seen);
        sav.Dex.Data[Burmy].Should().Be(expect);
        var forms = sav.Dex.GetForms((ushort)Species.Burmy);
        forms.Should().Equal(first, second, third);

        sav.Dex.SetForms((ushort)Species.Burmy, forms);
        sav.Dex.Data[Burmy].Should().Be(expect);

        // A partial list (as the dex editor saves it) leaves the remaining slots empty.
        sav.Dex.SetForms((ushort)Species.Burmy, seen);
        sav.Dex.Data[Burmy].Should().Be(expect);
    }

    [Fact]
    public void ClearSeenResetsForms()
    {
        var sav = new SAV4Pt();
        SeeForms(sav, Species.Burmy, Burmy, 0, 1);
        sav.Dex.ClearSeen((ushort)Species.Burmy);
        sav.Dex.Data[Burmy].Should().Be(0xFF);

        SeeForms(sav, Species.Shellos, Shellos, 0);
        sav.Dex.ClearSeen((ushort)Species.Shellos);
        sav.Dex.Data[Shellos].Should().Be(0xFF);
    }
}
