using FluentAssertions;
using Xunit;
using Lykke.Service.IroncladDecorator.Sessions;
using Microsoft.AspNetCore.DataProtection;
using Moq;

namespace Lykke.Service.IroncladDecorator.Tests
{
    public class ProtectionUtilsTests
    {
        private readonly IDataProtector _dataProtector;

        public ProtectionUtilsTests()
        {
            _dataProtector = MakeDataProtector();
        }

        [Fact]
        public void Protect_WhenStringValueIsPassed_Protects()
        {
            var testData = "gibberish";
            
            var protectedValue = ProtectionUtils.SerializeAndProtect(testData, _dataProtector);

            protectedValue.Should().NotBeNull();
        }

        [Fact]
        public void Unprotect_WhenCalledAfterProtectForString_ReturnsSource()
        {
            var testData = "gibberish";
            
            var protectedValue = ProtectionUtils.SerializeAndProtect(testData, _dataProtector);

            var unprotected = ProtectionUtils.DeserializeAndUnprotect<string>(protectedValue, _dataProtector);

            unprotected.Should().BeEquivalentTo(testData);
        }

        [Fact]
        public void Unprotect_WhenCalledAfterProtectForObject_ReturnsSource()
        {
            var testValue = "gibberish";
            var testData = new UserSession
            {
                Id = testValue
            };
            
            var protectedValue = ProtectionUtils.SerializeAndProtect(testData, _dataProtector);

            var unprotected = ProtectionUtils.DeserializeAndUnprotect<UserSession>(protectedValue, _dataProtector);

            unprotected.Id.Should().BeEquivalentTo(testValue);
        }

        private IDataProtector MakeDataProtector()
        {
            var dataProtector = new Mock<IDataProtector>();

            dataProtector.Setup(x => x.Protect(It.IsAny<byte[]>())).Returns<byte[]>(x => x);
            dataProtector.Setup(x => x.Unprotect(It.IsAny<byte[]>())).Returns<byte[]>(x => x);

            return dataProtector.Object;
        }
    }
}
