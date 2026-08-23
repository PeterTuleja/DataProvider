using Xunit;

namespace DataProvider.Tests
{
    /// <summary>
    /// ExecuteScalar&lt;T&gt; dostane z ADO.NET typ stlpca, nie typ, ktory pyta volajuci.
    /// Testy drzia konverziu, ktora to zosuladuje.
    /// </summary>
    public class PrevedNaTypTests
    {
        [Fact]
        public void SumaZFloatStlpcaSaPrevedieNaDecimal()
        {
            // SUM(C100_Mnozstvo) nad float stlpcom pride ako Double - unboxing na decimal by spadol
            object hodnota = 12.5d;

            Assert.Equal(12.5m, DataProviderBase.PrevedNaTyp<decimal>(hodnota));
        }

        [Fact]
        public void NulaZIsnullNadPrazdnouMnozinouSaPrevedieNaDecimal()
        {
            object hodnota = 0d;

            Assert.Equal(0m, DataProviderBase.PrevedNaTyp<decimal>(hodnota));
        }

        [Fact]
        public void NepresnostFloatuSaZaokruhliNaDecimal()
        {
            object hodnota = 0.1d + 0.2d;   // 0.30000000000000004

            Assert.Equal(0.3m, DataProviderBase.PrevedNaTyp<decimal>(hodnota));
        }

        [Fact]
        public void DecimalOstaneBezKonverzie()
        {
            object hodnota = 12.5m;

            Assert.Equal(12.5m, DataProviderBase.PrevedNaTyp<decimal>(hodnota));
        }

        [Fact]
        public void PocetZCountOstaneInt()
        {
            object hodnota = 7;

            Assert.Equal(7, DataProviderBase.PrevedNaTyp<int>(hodnota));
        }

        [Fact]
        public void SmallintSaPrevedieNaShort()
        {
            object hodnota = (short)3;

            Assert.Equal((short)3, DataProviderBase.PrevedNaTyp<short>(hodnota));
        }

        [Fact]
        public void IntSaPrevedieNaDecimal()
        {
            object hodnota = 42;

            Assert.Equal(42m, DataProviderBase.PrevedNaTyp<decimal>(hodnota));
        }

        [Fact]
        public void NullOstaneNull()
        {
            // DBNull uz predtym normalizuje ExecuteScalarInternal na null
            Assert.Null(DataProviderBase.PrevedNaTyp<decimal>(null));
        }

        [Fact]
        public void HodnotaMimoRozsahuCielovehoTypuVyhodiVynimku()
        {
            object hodnota = 40000;   // nezmesti sa do short

            Assert.Throws<OverflowException>(() => DataProviderBase.PrevedNaTyp<short>(hodnota));
        }
    }
}
