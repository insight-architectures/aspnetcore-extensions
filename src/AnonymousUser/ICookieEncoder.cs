using System.Threading.Tasks;

namespace InsightArchitectures.AnonymousUser
{
    /// <summary>
    /// Interface for cookie value serialisation.
    /// </summary>
    public interface ICookieEncoder
    {
        /// <summary>
        /// Serialiases a clear text value.
        /// <param name="value">A clear text value</param>
        /// </summary>
        Task<string> EncodeAsync(string value);

        /// <summary>
        /// Deserialises the given value into clear text.
        /// <param name="encodedValue">The serialised value</param>
        /// </summary>
        Task<string> DecodeAsync(string encodedValue);
    }
}