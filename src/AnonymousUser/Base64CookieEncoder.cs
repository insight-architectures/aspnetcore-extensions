using System;
using System.Text;
using System.Threading.Tasks;

namespace InsightArchitectures.Extensions.AspNetCore.AnonymousUser
{
    /// <summary>
    /// Default cookie value encoder/decoder. Uses base64 for serialisation.
    /// </summary>
    public class Base64CookieEncoder : ICookieEncoder
    {
        /// <summary>
        /// Deserialises a base64 value into clear text.
        /// <param name="encodedValue">A base64 encoded value.</param>
        /// <returns>Returns null if argument is null, otherwise the decoded value.</returns>
        /// </summary>
        public Task<string> DecodeAsync(string encodedValue)
        {
            if (string.IsNullOrWhiteSpace(encodedValue))
            {
                return Task.FromResult((string)null);
            }

            var bytes = Convert.FromBase64String(encodedValue);

            return Task.FromResult(Encoding.UTF8.GetString(bytes));
        }

        /// <summary>
        /// Serialiases a clear text value into base64.
        /// <param name="value">A clear text value.</param>
        /// <returns>Returns null if argument is null, otherwise the encoded value.</returns>
        /// </summary>
        public Task<string> EncodeAsync(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Task.FromResult((string)null);
            }

            var bytes = Encoding.UTF8.GetBytes(value);

            return Task.FromResult(Convert.ToBase64String(bytes));
        }
    }
}