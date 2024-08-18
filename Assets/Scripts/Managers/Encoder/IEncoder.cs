namespace Managers.Encoder
{
    public interface IEncoder
    {
        public void Encode(string path);
        /// <summary>
        /// Decode an encoded file
        /// </summary>
        /// <param name="path">The path of the encoded file.</param>
        /// <returns>The path of the decoded file.</returns>
        public string Decode(string path);
    }
}