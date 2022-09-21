namespace OPack.Internal
{
    using System;

    /// <summary> The public interface partially lifted from Python's struct API. </summary>
    internal interface IPacker
    {
        /// <summary>
        /// Return the size of the struct (and hence of the bytes object produced by
        /// <see cref="Pack(string, int, object[])" />) corresponding to the format string format.
        /// </summary>
        /// <param name="format"> The format to be used for packing. </param>
        /// <returns> The size of the struct. </returns>
        int CalcSize(string format);

        /// <summary>
        /// Convert an array of objects to a little endian or big endian byte array, while following
        /// the specified format.
        /// </summary>
        /// <param name="format"> A "struct.unpack"-compatible format string. </param>
        /// <param name="offset"> Where to start packing in the provided <paramref name="items" />. </param>
        /// <param name="items"> An array of items to convert to a byte array. </param>
        /// <returns> A byte array of packed elements. </returns>
        byte[] Pack(string format, int offset = 0, params object[] items);

        /// <summary>
        /// Convert a byte array into an array of numerical value types based on Python's
        /// "struct.unpack" protocol.
        /// </summary>
        /// <param name="format"> A "struct.unpack"-compatible format string. </param>
        /// <param name="offset"> Where to start unpacking in the provided <paramref name="bytes" />. </param>
        /// <param name="bytes"> An array of bytes to convert to objects. </param>
        /// <returns> Array of objects. </returns>
        /// <remarks>
        /// You are responsible for casting the objects in the array back to their proper types.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// If <paramref name="format" /> doesn't correspond to the length of <paramref name="bytes" />.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="format" /> is null or empty, or if <paramref name="bytes" /> is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// If an invalid character for struct.unpack is found in <paramref name="format" />.
        /// </exception>
        object[] Unpack(string format, int offset = 0, params byte[] bytes);
    }
}