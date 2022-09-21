namespace SheepSheep
{
    using System.Runtime.InteropServices;

    /// <summary> The Packer API for native structs, as a set of extension methods to <see langword="struct" />. </summary>
    public static class StructsExtensions
    {
        /// <summary> Calculates the size of the <see langword="struct" /> in unmanaged memory. </summary>
        /// <typeparam name="T"> typeof(struct). </typeparam>
        /// <param name="target"> The struct to give to <see cref="Marshal.SizeOf{T}(T)" />. </param>
        /// <returns> The native size of the struct. </returns>
        public static int CalcSize<T>(this T target)
            where T : struct
        {
            return Packer.NativeCalcSize<T>();
        }

        /// <summary> Packs a <see langword="struct" /> into an array of bytes. </summary>
        /// <typeparam name="T"> typeof(struct). </typeparam>
        /// <param name="target"> The <see langword="struct" /> to pack. </param>
        /// <returns>
        /// The <see langword="struct" /> packed in a one dimensional array, and a string to be used
        /// with <see />.
        /// </returns>
        public static byte[] Pack<T>(this T target)
            where T : struct
        {
            return Packer.NativePack<T>(target);
        }

        /// <summary> Unpacks a byte array into a struct. </summary>
        /// <typeparam name="T"> The type of struct. </typeparam>
        /// <param name="target"> The <see langword="struct" /> to pack. </param>
        /// <param name="byteArrayOfStruct"> The byte array to unpack from. </param>
        /// <returns> An instance of the struct. </returns>
        public static T Unpack<T>(this T target, byte[] byteArrayOfStruct)
            where T : struct
        {
            return Packer.NativeUnpack<T>(byteArrayOfStruct);
        }
    }
}