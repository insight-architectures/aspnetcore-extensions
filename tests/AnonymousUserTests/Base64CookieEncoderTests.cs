using System;
using System.Threading.Tasks;
using InsightArchitectures.AnonymousUser;
using NUnit.Framework;

namespace AnonymousUserTests
{
    public class Base64CookieEncoderTests
    {
        [Test, MoqAutoData]
        public async Task EncodingAndDecodingShouldReturnInitialValue(Base64CookieEncoder sut, string initialValue)
        {
            var encodedValue = await sut.EncodeAsync(initialValue);
            var decodedValue = await sut.DecodeAsync(encodedValue);

            Assert.AreEqual(initialValue, decodedValue);
        }

        [Test, MoqAutoData]
        public async Task EncodingShouldReturnBase64String(Base64CookieEncoder sut, string decodedValue)
        {
            var encodedValue = await sut.EncodeAsync(decodedValue);

            Assert.IsTrue(IsBase64String(encodedValue));

            bool IsBase64String(string value)
            {
                Span<byte> buffer = stackalloc byte[value.Length];
                return Convert.TryFromBase64String(value, buffer, out _);
            }
        }

        [Test, MoqAutoData]
        public async Task NullInputShouldReturnNull(Base64CookieEncoder sut)
        {
            Assert.IsNull(await sut.EncodeAsync(null));
            Assert.IsNull(await sut.DecodeAsync(null));
        }
    }
}