namespace SheepSheep
{
    using System;
    using System.Buffers.Binary;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using OPack.Internal;

    /// <summary> A translation of Python's pack and unpack protocol to C#. </summary>
    public class Packer : IPacker
    {
        private readonly char[] endiannessPrefixes = { '<', '>', '@', '=', '!' };

        /// <summary>
        /// Convert an array of objects to a little endian byte array, and a string that can be used
        /// with <see cref="Unpack(string, int, byte[])" />.
        /// </summary>
        /// <param name="offset"> Where to start packing in the provided <paramref name="items" />. </param>
        /// <param name="items"> An object array of value types to convert. </param>
        /// <returns>
        /// A <see cref="Tuple{T1, T2}" /> Byte array containing the objects provided in binary format.
        /// </returns>
        public static Tuple<byte[], string> AutoPack(int offset = 0, params object[] items)
        {
            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            StringBuilder format = new StringBuilder();

            List<byte> outputBytes = new List<byte>();

            for (int i = offset; i < items.Length; i++)
            {
                var obj = items[i];
                byte[] theseBytes = TypeAgnosticGetBytes(obj, false);
                format.Append(GetFormatSpecifierFor(obj));
                outputBytes.AddRange(theseBytes);
            }

            return Tuple.Create(outputBytes.ToArray(), format.ToString());
        }

        /// <summary> Calculates the size of the <see langword="struct" /> in unmanaged memory. </summary>
        /// <typeparam name="T"> typeof(struct). </typeparam>
        /// <returns> The native size of the struct. </returns>
        public static int NativeCalcSize<T>()
            where T : struct
        {
            return Marshal.SizeOf(typeof(T));
        }

        /// <summary> Packs a <see langword="struct" /> into an array of bytes. </summary>
        /// <typeparam name="T"> typeof(struct). </typeparam>
        /// <param name="target"> The <see langword="struct" /> to pack. </param>
        /// <returns>
        /// The struct packed in a one dimensional array, and a string to be used with <see />.
        /// </returns>
        public static byte[] NativePack<T>(T target)
            where T : struct
        {
            var bufferSize = Marshal.SizeOf(typeof(T));
            IntPtr handle = Marshal.AllocHGlobal(bufferSize);
            Marshal.StructureToPtr(target, handle, true);
            var byteArray = new byte[bufferSize];
            Marshal.Copy(handle, byteArray, 0, bufferSize);
            Marshal.FreeHGlobal(handle);
            return byteArray;
        }

        /// <summary> Unpacks a byte array into a struct. </summary>
        /// <typeparam name="T"> The type of struct. </typeparam>
        /// <param name="byteArrayOfStruct"> The byte array to unpack from. </param>
        /// <returns> An instance of the struct. </returns>
        public static T NativeUnpack<T>(byte[] byteArrayOfStruct)
            where T : struct
        {
            IntPtr handle = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(T)));
            Marshal.Copy(byteArrayOfStruct, 0, handle, Marshal.SizeOf(typeof(T)));
            var structure = Marshal.PtrToStructure<T>(handle);
            Marshal.FreeHGlobal(handle);
            return structure;
        }

        /// <summary> <inheritdoc /> </summary>
        /// <param name="format"> The format to be used for packing. </param>
        /// <returns> The size of the struct. </returns>
        public int CalcSize(string format)
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                throw new ArgumentNullException($"{nameof(format)} cannot be null or empty.");
            }

            this.ThrowIfNativeMode(format);

            format = this.ExpandFormat(format);

            string formatWithoutEndianness = format;

            if (this.endiannessPrefixes.Contains(format[0]))
            {
                formatWithoutEndianness = format.Substring(1);
            }

            int totalByteLength = 0;
            for (int i = 0; i < formatWithoutEndianness.Length; i++)
            {
                switch (formatWithoutEndianness[i])
                {
                    case 'q':
                    case 'Q':
                    case 'd':
                        totalByteLength += sizeof(long);
                        break;

                    case 'i':
                    case 'I':
                    case 'l':
                    case 'L':
                    case 'f':
                        totalByteLength += sizeof(float);
                        break;

                    case 'e':
                    case 'h':
                    case 'H':
                        totalByteLength += sizeof(char);
                        break;

                    case 'c':
                    case 'b':
                    case '?':
                    case 'B':
                    case 'x':
                        totalByteLength += sizeof(bool);
                        break;

                    default:
                        throw new ArgumentException($"Invalid character found in {nameof(format)} string : {formatWithoutEndianness[i]} at position {i}.");
                }
            }

            return totalByteLength;
        }

        /// <summary> <inheritdoc /> </summary>
        /// <param name="format"> A "struct.unpack"-compatible format string. </param>
        /// <param name="offset"> Where to start packing in the provided <paramref name="items" />. </param>
        /// <param name="items"> An array of items to convert to a byte array. </param>
        /// <returns> A byte array of packed elements. </returns>
        public byte[] Pack(string format, int offset = 0, params object[] items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            if (string.IsNullOrWhiteSpace(format))
            {
                throw new ArgumentNullException(nameof(format));
            }

            format = this.ExpandFormat(format, items.Length);

            object[] itemsArray = items;

            if (itemsArray is IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    if (item is IEnumerable innerEnumerable)
                    {
                        itemsArray = innerEnumerable.Cast<object>().ToArray();
                        break;
                    }
                }
            }

            this.ThrowIfNativeMode(format);

            int formatLength = format.Length;
            int formatOffset = 0;

            if (this.endiannessPrefixes.Contains(format[0]))
            {
                formatLength--;
                formatOffset = 1;
            }

            if (formatLength < items.Length)
            {
                throw new ArgumentException($"The number of {nameof(items)} ({items.Length}) provided does not match the total length ({formatLength}) of the ${nameof(format)} string.");
            }

            bool useBigEndian = this.AreWeInBigEndianMode(format);

            List<byte> outputBytes = new List<byte>();

            for (int i = 0; i < format.Length - formatOffset; i++)
            {
                if (i + offset >= itemsArray.Length)
                {
                    break;
                }

                var item = itemsArray[i + offset];

                if (item is null)
                {
                    throw new ArgumentNullException($"{nameof(format)}, at {i + offset}");
                }

                var character = format[i + formatOffset];

                if (character == 'f')
                {
                    var convertedItem = BitConverter.GetBytes(Convert.ToSingle(item, CultureInfo.InvariantCulture));
                    if (useBigEndian != !BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(convertedItem);
                    }

                    outputBytes.AddRange(convertedItem);
                }
                else if (character == 'd')
                {
                    var convertedItem = BitConverter.GetBytes(Convert.ToDouble(item, CultureInfo.InvariantCulture));
                    if (useBigEndian != !BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(convertedItem);
                    }

                    outputBytes.AddRange(convertedItem);
                }
                else if (character == '?' || character == 'B')
                {
                    byte convertedItem = Convert.ToByte(item, CultureInfo.InvariantCulture);
                    outputBytes.Add(convertedItem);
                }
                else if (character == 'b')
                {
                    sbyte convertedItem = Convert.ToSByte(item, CultureInfo.InvariantCulture);
                    outputBytes.Add((byte)convertedItem);
                }
                else if (character == 'i')
                {
                    Span<byte> dest = stackalloc byte[4];
                    if (useBigEndian)
                    {
                        BinaryPrimitives.WriteInt32BigEndian(dest, Convert.ToInt32(item, CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        BinaryPrimitives.WriteInt32LittleEndian(dest, Convert.ToInt32(item, CultureInfo.InvariantCulture));
                    }

                    outputBytes.AddRange(dest.ToArray());
                }
                else if (character == 'I')
                {
                    Span<byte> dest = stackalloc byte[4];
                    if (useBigEndian)
                    {
                        BinaryPrimitives.WriteUInt32BigEndian(dest, Convert.ToUInt32(item, CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        BinaryPrimitives.WriteUInt32LittleEndian(dest, Convert.ToUInt32(item, CultureInfo.InvariantCulture));
                    }

                    outputBytes.AddRange(dest.ToArray());
                }
                else if (character == 'q' || character == 'l')
                {
                    Span<byte> dest = stackalloc byte[8];
                    if (useBigEndian)
                    {
                        BinaryPrimitives.WriteInt64BigEndian(dest, Convert.ToInt64(item, CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        BinaryPrimitives.WriteInt64LittleEndian(dest, Convert.ToInt64(item, CultureInfo.InvariantCulture));
                    }

                    outputBytes.AddRange(dest.ToArray());
                }
                else if (character == 'Q' || character == 'L')
                {
                    Span<byte> dest = stackalloc byte[8];
                    if (useBigEndian)
                    {
                        BinaryPrimitives.WriteUInt64BigEndian(dest, Convert.ToUInt64(item, CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        BinaryPrimitives.WriteUInt64LittleEndian(dest, Convert.ToUInt64(item, CultureInfo.InvariantCulture));
                    }

                    outputBytes.AddRange(dest.ToArray());
                }
                else if (character == 'h')
                {
                    Span<byte> dest = stackalloc byte[2];
                    if (useBigEndian)
                    {
                        BinaryPrimitives.WriteInt16BigEndian(dest, Convert.ToInt16(item, CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        BinaryPrimitives.WriteInt16LittleEndian(dest, Convert.ToInt16(item, CultureInfo.InvariantCulture));
                    }

                    outputBytes.AddRange(dest.ToArray());
                }
                else if (character == 'H')
                {
                    Span<byte> dest = stackalloc byte[2];
                    if (useBigEndian)
                    {
                        BinaryPrimitives.WriteUInt16BigEndian(dest, Convert.ToUInt16(item, CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        BinaryPrimitives.WriteUInt16LittleEndian(dest, Convert.ToUInt16(item, CultureInfo.InvariantCulture));
                    }

                    outputBytes.AddRange(dest.ToArray());
                }
                else
                {
                    throw new InvalidOperationException($"Invalid character '{character}' found in arg {nameof(format)}.");
                }
            }

            return outputBytes.ToArray();
        }

        /// <summary> <inheritdoc /> </summary>
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
        public object[] Unpack(string format, int offset = 0, params byte[] bytes)
        {
            if (bytes is null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            if (string.IsNullOrWhiteSpace(format))
            {
                throw new ArgumentNullException(nameof(format));
            }

            format = this.ExpandFormat(format, bytes.Length);

            var computedSize = this.CalcSize(format);

            if (computedSize < bytes.Length)
            {
                throw new ArgumentException($"The number of {nameof(bytes)} provided ({bytes.Length}) does not match the total length ({computedSize}) of the {nameof(format)} string.");
            }

            this.ThrowIfNativeMode(format);

            bool useBigEndian = this.AreWeInBigEndianMode(format);

            int byteArrayPosition = offset;
            List<object> outputList = new List<object>();
            for (int i = 0; i < format.Length; i++)
            {
                if (this.endiannessPrefixes.Contains(format[i]) && i == 0)
                {
                    continue;
                }

                if (byteArrayPosition >= bytes.Length)
                {
                    break;
                }

                if (format[i] == '?')
                {
                    outputList.Add(bytes[byteArrayPosition] >= 1);
                    byteArrayPosition++;
                }
                else if (format[i] == 'f')
                {
                    var array = new byte[4];
                    Array.Copy(bytes, byteArrayPosition, array, 0, Math.Min(Math.Min(array.Length, bytes.Length - byteArrayPosition), bytes.Length - byteArrayPosition));
                    if (useBigEndian != !BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(array);
                    }

                    var value = BitConverter.ToSingle(array, 0);
                    outputList.Add(value);
                    byteArrayPosition += 4;
                }
                else if (format[i] == 'd')
                {
                    var array = new byte[8];
                    Array.Copy(bytes, byteArrayPosition, array, 0, Math.Min(array.Length, bytes.Length - byteArrayPosition));
                    if (useBigEndian != !BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(array);
                    }

                    var value = BitConverter.ToDouble(array, 0);
                    outputList.Add(value);
                    byteArrayPosition += 8;
                }
                else if (format[i] == 'q')
                {
                    var array = new byte[8];
                    Array.Copy(bytes, byteArrayPosition, array, 0, Math.Min(array.Length, bytes.Length - byteArrayPosition));
                    if (useBigEndian)
                    {
                        outputList.Add(BinaryPrimitives.ReadInt64BigEndian(array));
                    }
                    else
                    {
                        outputList.Add(BinaryPrimitives.ReadInt64LittleEndian(array));
                    }

                    byteArrayPosition += 8;
                }
                else if (format[i] == 'Q')
                {
                    var array = new byte[8];
                    Array.Copy(bytes, byteArrayPosition, array, 0, Math.Min(array.Length, bytes.Length - byteArrayPosition));
                    if (useBigEndian)
                    {
                        outputList.Add(BinaryPrimitives.ReadUInt64BigEndian(array));
                    }
                    else
                    {
                        outputList.Add(BinaryPrimitives.ReadUInt64LittleEndian(array));
                    }

                    byteArrayPosition += 8;
                }
                else if (format[i] == 'l')
                {
                    var array = new byte[4];
                    Array.Copy(bytes, byteArrayPosition, array, 0, Math.Min(array.Length, bytes.Length - byteArrayPosition));
                    if (useBigEndian)
                    {
                        outputList.Add(BinaryPrimitives.ReadInt32BigEndian(array));
                    }
                    else
                    {
                        outputList.Add(BinaryPrimitives.ReadInt32LittleEndian(array));
                    }

                    byteArrayPosition += 4;
                }
                else if (format[i] == 'L' || format[i] == 'I')
                {
                    var array = new byte[4];
                    Array.Copy(bytes, byteArrayPosition, array, 0, Math.Min(array.Length, bytes.Length - byteArrayPosition));
                    if (useBigEndian)
                    {
                        outputList.Add(BinaryPrimitives.ReadUInt32BigEndian(array));
                    }
                    else
                    {
                        outputList.Add(BinaryPrimitives.ReadUInt32LittleEndian(array));
                    }

                    byteArrayPosition += 4;
                }
                else if (format[i] == 'h')
                {
                    var array = new byte[2];
                    Array.Copy(bytes, byteArrayPosition, array, 0, Math.Min(array.Length, bytes.Length - byteArrayPosition));
                    if (useBigEndian)
                    {
                        outputList.Add(BinaryPrimitives.ReadInt16BigEndian(array));
                    }
                    else
                    {
                        outputList.Add(BinaryPrimitives.ReadInt16LittleEndian(array));
                    }

                    byteArrayPosition += 2;
                }
                else if (format[i] == 'H')
                {
                    var array = new byte[2];
                    Array.Copy(bytes, byteArrayPosition, array, 0, Math.Min(array.Length, bytes.Length - byteArrayPosition));
                    if (useBigEndian)
                    {
                        outputList.Add(BinaryPrimitives.ReadUInt16BigEndian(array));
                    }
                    else
                    {
                        outputList.Add(BinaryPrimitives.ReadUInt16LittleEndian(array));
                    }

                    byteArrayPosition += 2;
                }
                else if (format[i] == 'b' || format[i] == 'B')
                {
                    byte[] array = new byte[1];
                    Array.Copy(bytes, byteArrayPosition, array, 0, Math.Min(array.Length, bytes.Length - byteArrayPosition));
                    byte value = array[0];
                    if (format[i] == 'b')
                    {
                        outputList.Add((sbyte)value);
                    }
                    else
                    {
                        outputList.Add(value);
                    }

                    byteArrayPosition++;
                }
                else if (format[i] == 'x')
                {
                    byteArrayPosition++;
                }
                else
                {
                    throw new InvalidOperationException($"Invalid character '{format[i]}' found in arg {nameof(format)}.");
                }
            }

            return outputList.ToArray();
        }

        private static string GetFormatSpecifierFor(object obj)
        {
            if (obj is bool)
            {
                return "?";
            }

            if (obj is double)
            {
                return "d";
            }

            if (obj is float)
            {
                return "f";
            }

            if (obj is int)
            {
                return "i";
            }

            if (obj is uint)
            {
                return "I";
            }

            if (obj is long)
            {
                return "q";
            }

            if (obj is ulong)
            {
                return "Q";
            }

            if (obj is short)
            {
                return "h";
            }

            if (obj is ushort)
            {
                return "H";
            }

            if (obj is byte)
            {
                return "B";
            }

            if (obj is sbyte)
            {
                return "b";
            }

            throw new ArgumentException($"Unsupported object type found: {obj.GetType()}");
        }

        /// <summary>
        /// We use this function to provide an easier way to type-agnostically call the GetBytes
        /// method of the BitConverter class. This means we can have much cleaner code above.
        /// </summary>
        /// <param name="boxedValue"> The numerical value type to pack into an array of bytes. </param>
        /// <param name="isBigEndian"> Do we use little or big endian mode. </param>
        private static byte[] TypeAgnosticGetBytes(object boxedValue, bool isBigEndian)
        {
            if (boxedValue is bool boolean)
            {
                return !boolean
                    ? (new byte[] { 0 })
                    : (new byte[] { 1 });
            }
            else if (boxedValue is int signedInteger)
            {
                Span<byte> holder = stackalloc byte[4];
                if (isBigEndian)
                {
                    BinaryPrimitives.WriteInt32BigEndian(holder, signedInteger);
                }
                else
                {
                    BinaryPrimitives.WriteInt32LittleEndian(holder, signedInteger);
                }

                return holder.ToArray();
            }
            else if (boxedValue is uint unsignedInteger)
            {
                Span<byte> holder = stackalloc byte[4];
                if (isBigEndian)
                {
                    BinaryPrimitives.WriteUInt32BigEndian(holder, unsignedInteger);
                }
                else
                {
                    BinaryPrimitives.WriteUInt32LittleEndian(holder, unsignedInteger);
                }

                return holder.ToArray();
            }
            else if (boxedValue is long signedLongInteger)
            {
                Span<byte> holder = stackalloc byte[8];
                if (isBigEndian)
                {
                    BinaryPrimitives.WriteInt64BigEndian(holder, signedLongInteger);
                }
                else
                {
                    BinaryPrimitives.WriteInt64LittleEndian(holder, signedLongInteger);
                }

                return holder.ToArray();
            }
            else if (boxedValue is ulong unsignedLongInteger)
            {
                Span<byte> holder = stackalloc byte[8];
                if (isBigEndian)
                {
                    BinaryPrimitives.WriteUInt64BigEndian(holder, unsignedLongInteger);
                }
                else
                {
                    BinaryPrimitives.WriteUInt64LittleEndian(holder, unsignedLongInteger);
                }

                return holder.ToArray();
            }
            else if (boxedValue is short signedShort)
            {
                Span<byte> holder = stackalloc byte[2];
                if (isBigEndian)
                {
                    BinaryPrimitives.WriteInt16BigEndian(holder, signedShort);
                }
                else
                {
                    BinaryPrimitives.WriteInt16LittleEndian(holder, signedShort);
                }

                return holder.ToArray();
            }
            else if (boxedValue is ushort unsignedShort)
            {
                Span<byte> holder = stackalloc byte[2];
                if (isBigEndian)
                {
                    BinaryPrimitives.WriteUInt16BigEndian(holder, unsignedShort);
                }
                else
                {
                    BinaryPrimitives.WriteUInt16LittleEndian(holder, unsignedShort);
                }

                return holder.ToArray();
            }
            else if (boxedValue is byte || boxedValue is sbyte)
            {
                return new byte[] { (byte)boxedValue };
            }
            else
            {
                throw new ArgumentException($"Unsupported object type found: {boxedValue.GetType()}. We can pack only numerical value types.");
            }
        }

        private bool AreWeInBigEndianMode(string format)
        {
            var selectedPrefix = '@';

            if (this.endiannessPrefixes.Contains(format[0]))
            {
                selectedPrefix = format[0];
            }

            bool isBigEndian = false;

            if (selectedPrefix == '@' || selectedPrefix == '=')
            {
                isBigEndian = !BitConverter.IsLittleEndian;
            }

            if (selectedPrefix == '<')
            {
                isBigEndian = false;
            }

            if (selectedPrefix == '>' || selectedPrefix == '!')
            {
                isBigEndian = true;
            }

            return isBigEndian;
        }

        private string ExpandFormat(string format, int numberOfElementsInStruct = 0)
        {
            char[] numbers = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
            if (!format.ToCharArray().Any(x => numbers.Contains(x)) && numberOfElementsInStruct <= 0)
            {
                return format;
            }

            if (char.IsDigit(format[format.Length - 1]))
            {
                throw new InvalidOperationException("Repeat count given without format specifier");
            }

            if (!format.ToCharArray().Any(x => numbers.Contains(x)) && this.endiannessPrefixes.Contains(format[0]) && format.Length == 2)
            {
                format = $"{format}{numberOfElementsInStruct}";
            }

            var expandedFormat = new StringBuilder();

            var numberHolder = new StringBuilder();

            string lastCharacter = string.Empty;

            for (int i = format.Length - 1; i >= 0; i--)
            {
                char currentChar = format[i];
                if (numbers.Contains(currentChar))
                {
                    numberHolder.Insert(0, currentChar);
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(lastCharacter) && int.TryParse(numberHolder.ToString(), out var times))
                    {
                        expandedFormat.Remove(expandedFormat.Length - 1, 1);
                        for (int j = 0; j < times; j++)
                        {
                            expandedFormat.Insert(0, lastCharacter);
                        }
                    }

                    lastCharacter = currentChar.ToString(CultureInfo.InvariantCulture);
                    expandedFormat.Insert(0, currentChar);
                }
            }

            return expandedFormat.ToString();
        }

        private void ThrowIfNativeMode(string format)
        {
            if (format[0] == this.endiannessPrefixes[2] ||
                !this.endiannessPrefixes.Contains(format[0]))
            {
                throw new InvalidOperationException("Use Native* methods for native (un)packing of structs");
            }
        }
    }
}