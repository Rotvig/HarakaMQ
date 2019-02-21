using AutoFixture;
using AutoFixture.AutoFakeItEasy;
using AutoFixture.Xunit2;

namespace HarakaMQ.UnitTests.Utils
{
    public class AutoFakeItEasyDataAttribute : AutoDataAttribute
    {
        public AutoFakeItEasyDataAttribute() : base(() =>
            new Fixture()
                .Customize(new AutoFakeItEasyCustomization()))
        {
            
        }
    }
}