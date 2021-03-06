// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Utf8.Tests
{
    public class Utf8EncoderTests
    {
        private const ushort Utf16HighSurrogateFirstCodePoint = 0xD800;
        private const ushort Utf16HighSurrogateLastCodePoint = 0xDBFF;
        private const ushort Utf16LowSurrogateFirstCodePoint = 0xDC00;
        private const ushort Utf16LowSurrogateLastCodePoint = 0xDFFF;

        private const byte Utf8OneByteLastCodePoint = 0x7F;
        private const ushort Utf8TwoBytesLastCodePoint = 0x7FF;
        private const ushort Utf8ThreeBytesLastCodePoint = 0xFFFF;

        private const int CharLength = 999;

        [Fact]
        public void TestEncodingInputBufferEmpty()
        {
            string unicodeString = "";
            ReadOnlySpan<char> characters = unicodeString.AsSpan();
            int expectedBytesWritten = GetByteCount(characters);
            char[] charArray = characters.ToArray();
            byte[] utf8BufferExpected = new byte[Encoding.UTF8.GetByteCount(charArray)];

            Encoding.UTF8.GetBytes(charArray, 0, characters.Length, utf8BufferExpected, 0);
            byte[] utf8Buffer = new byte[expectedBytesWritten + 25];
            Span<byte> span = new Span<byte>(utf8Buffer);
            int charactersConsumed;
            int bytesWritten;
            Assert.True(TextEncoder.Utf8.TryEncode(characters, span, out charactersConsumed, out bytesWritten));

            Assert.Equal(0, charactersConsumed);
            Assert.Equal(expectedBytesWritten, bytesWritten);
        }

        [Fact]
        public void TestEncodingOutputBufferEmpty()
        {
            string unicodeString = GenerateValidString(CharLength, 0, Utf8ThreeBytesLastCodePoint);
            ReadOnlySpan<char> characters = unicodeString.AsSpan();
            char[] charArray = characters.ToArray();
            byte[] utf8BufferExpected = new byte[Encoding.UTF8.GetByteCount(charArray)];

            Encoding.UTF8.GetBytes(charArray, 0, characters.Length, utf8BufferExpected, 0);
            byte[] utf8Buffer = new byte[0];
            Span<byte> span = new Span<byte>(utf8Buffer);
            int charactersConsumed;
            int bytesWritten;
            Assert.False(TextEncoder.Utf8.TryEncode(characters, span, out charactersConsumed, out bytesWritten));

            Assert.Equal(0, charactersConsumed);
            Assert.Equal(0, bytesWritten);
        }

        [Fact]
        public void TestEncodingOutputBufferTooSmall()
        {
            string unicodeString = GenerateValidString(CharLength, 0, Utf8ThreeBytesLastCodePoint);
            ReadOnlySpan<char> characters = unicodeString.AsSpan();
            int expectedBytesWritten = GetByteCount(characters);
            char[] charArray = characters.ToArray();
            byte[] utf8BufferExpected = new byte[Encoding.UTF8.GetByteCount(charArray)];

            Encoding.UTF8.GetBytes(charArray, 0, characters.Length, utf8BufferExpected, 0);
            byte[] utf8Buffer = new byte[500];
            Span<byte> span = new Span<byte>(utf8Buffer);
            int charactersConsumed;
            int bytesWritten;
            Assert.False(TextEncoder.Utf8.TryEncode(characters, span, out charactersConsumed, out bytesWritten));

            Assert.Equal(499, bytesWritten);

            byte[] utf8BufferRemaining = new byte[expectedBytesWritten - bytesWritten];
            Span<byte> span2 = new Span<byte>(utf8BufferRemaining);
            int charactersConsumed2;
            int bytesWritten2;
            Assert.True(TextEncoder.Utf8.TryEncode(characters.Slice(charactersConsumed), span2, out charactersConsumed2, out bytesWritten2));

            byte[] utf8BufferActual = span2.ToArray();

            Assert.Equal(unicodeString.Length, charactersConsumed + charactersConsumed2);
            Assert.Equal(utf8BufferExpected.Length, bytesWritten2 + bytesWritten);
            Assert.Equal(utf8BufferExpected.Length, expectedBytesWritten);

            for (int i = bytesWritten; i < utf8BufferExpected.Length; i++)
            {
                Assert.Equal(utf8BufferExpected[i], utf8BufferActual[i - bytesWritten]);
            }
        }

        [Fact]
        public void TestEncodingInputBufferContainsOnlyInvalidData()
        {
            string unicodeString = GenerateOnlyInvalidString(CharLength);
            ReadOnlySpan<char> characters = unicodeString.AsSpan();
            int expectedBytesWritten = GetByteCount(characters);
            byte[] utf8Buffer = new byte[expectedBytesWritten];
            Span<byte> span = new Span<byte>(utf8Buffer);
            int charactersConsumed;
            int bytesWritten;
            Assert.False(TextEncoder.Utf8.TryEncode(characters, span, out charactersConsumed, out bytesWritten));

            Assert.Equal(0, charactersConsumed);
            Assert.Equal(expectedBytesWritten, bytesWritten);
        }

        [Fact]
        public void TestEncodingInputBufferContainsSomeInvalidData()
        {
            string unicodeString = GenerateStringWithInvalidChars(CharLength);
            ReadOnlySpan<char> characters = unicodeString.AsSpan();
            int expectedBytesWritten = GetByteCount(characters);
            byte[] utf8Buffer = new byte[expectedBytesWritten + 100];
            Span<byte> span = new Span<byte>(utf8Buffer);
            int charactersConsumed;
            int bytesWritten;
            Assert.False(TextEncoder.Utf8.TryEncode(characters, span, out charactersConsumed, out bytesWritten));

            Assert.True(charactersConsumed < unicodeString.Length);
            Assert.Equal(expectedBytesWritten, bytesWritten);
        }

        [Fact]
        public void TestEncodingOutputBufferTooLarge()
        {
            string unicodeString = GenerateValidString(CharLength, 0, Utf8ThreeBytesLastCodePoint);
            ReadOnlySpan<char> characters = unicodeString.AsSpan();
            char[] charArray = characters.ToArray();
            byte[] utf8BufferExpected = new byte[Encoding.UTF8.GetByteCount(charArray)];

            Encoding.UTF8.GetBytes(charArray, 0, characters.Length, utf8BufferExpected, 0);

            string unicodeString2 = GenerateValidString(CharLength, 0x0800, Utf8ThreeBytesLastCodePoint);
            ReadOnlySpan<char> characters2 = unicodeString2.AsSpan();
            char[] charArray2 = characters2.ToArray();
            byte[] utf8BufferExpected2 = new byte[Encoding.UTF8.GetByteCount(charArray2)];

            Encoding.UTF8.GetBytes(charArray2, 0, characters2.Length, utf8BufferExpected2, 0);

            int expectedBytesWritten = GetByteCount(characters) + GetByteCount(characters2);
            byte[] utf8Buffer = new byte[expectedBytesWritten];
            Span<byte> span = new Span<byte>(utf8Buffer);
            int charactersConsumed;
            int bytesWritten;
            int charactersConsumed2;
            int bytesWritten2;
            Assert.True(TextEncoder.Utf8.TryEncode(characters, span, out charactersConsumed, out bytesWritten));
            Assert.True(TextEncoder.Utf8.TryEncode(characters2, span.Slice(bytesWritten), out charactersConsumed2, out bytesWritten2));

            byte[] utf8BufferActual = span.ToArray();

            Assert.Equal(unicodeString.Length + unicodeString2.Length, charactersConsumed + charactersConsumed2);
            Assert.Equal(utf8BufferExpected.Length + utf8BufferExpected2.Length, bytesWritten + bytesWritten2);
            Assert.Equal(utf8BufferExpected.Length + utf8BufferExpected2.Length, expectedBytesWritten);

            for (int i = 0; i < utf8BufferExpected.Length; i++)
            {
                Assert.Equal(utf8BufferExpected[i], utf8BufferActual[i]);
            }
            for (int i = 0; i < utf8BufferExpected2.Length; i++)
            {
                Assert.Equal(utf8BufferExpected2[i], utf8BufferActual[i + bytesWritten]);
            }
        }

        [Fact]
        public void TestEncodingEndOnHighSurrogateAndRestart()
        {
            string unicodeString = GenerateValidStringEndsWithHighStartsWithLow(CharLength, false, 0, Utf8OneByteLastCodePoint);
            ReadOnlySpan<char> characters = unicodeString.AsSpan();

            string unicodeString2 = unicodeString + GenerateValidStringEndsWithHighStartsWithLow(CharLength, true, 0, Utf8OneByteLastCodePoint);
            ReadOnlySpan<char> characters2 = unicodeString2.AsSpan();

            int expectedBytesWritten = GetByteCount(characters2);
            byte[] utf8Buffer = new byte[expectedBytesWritten];
            Span<byte> span = new Span<byte>(utf8Buffer);
            int charactersConsumed;
            int bytesWritten;
            int charactersConsumed2;
            int bytesWritten2;
            Assert.False(TextEncoder.Utf8.TryEncode(characters, span, out charactersConsumed, out bytesWritten));
            Assert.True(TextEncoder.Utf8.TryEncode(characters2.Slice(charactersConsumed), span.Slice(bytesWritten), out charactersConsumed2, out bytesWritten2));

            Assert.Equal(unicodeString2.Length, charactersConsumed + charactersConsumed2);
            Assert.Equal(expectedBytesWritten, bytesWritten + bytesWritten2);
        }

        [Fact]
        public void TestEncodingValidStringASCII()
        {
            Assert.True(Validate(0, Utf8OneByteLastCodePoint));  // 1 byte
        }

        [Fact]
        public void TestEncodingValidStringNon_ASCII()
        {
            Assert.True(Validate(0x0080, Utf8TwoBytesLastCodePoint));  // 2 bytes
            Assert.True(Validate(0x0800, 0xD7FF));  // 3 bytes
            Assert.True(Validate(0xE000, Utf8ThreeBytesLastCodePoint));  // 3 bytes
            Assert.True(Validate(0xD800, 0xDFFF));  // 4 bytes (high and low surrogates)
            Assert.True(Validate(0x0000, Utf8ThreeBytesLastCodePoint));  // mixed
        }

        [Fact]
        public void TestEncodingBruteForceAllValidChars()
        {
            string unicodeString = GenerateAllCharString();
            int charLengthOfAllCharacters = ((0xD7FF - 0) + 1) + ((Utf8ThreeBytesLastCodePoint - 0xE000) + 1); // single char
            charLengthOfAllCharacters += 2 * ((Utf16HighSurrogateLastCodePoint - Utf16HighSurrogateFirstCodePoint) + 1) * ((Utf16LowSurrogateLastCodePoint - Utf16LowSurrogateFirstCodePoint) + 1);  //double char
            Assert.Equal(unicodeString.Length, charLengthOfAllCharacters);  // should be equal to 2160640

            ReadOnlySpan<char> characters = unicodeString.AsSpan();
            char[] charArray = characters.ToArray();
            byte[] utf8BufferExpected = new byte[Encoding.UTF8.GetByteCount(charArray)];

            Encoding.UTF8.GetBytes(charArray, 0, characters.Length, utf8BufferExpected, 0);

            int expectedBytesWritten = GetByteCount(characters);
            byte[] utf8Buffer = new byte[expectedBytesWritten];
            Span<byte> span = new Span<byte>(utf8Buffer);
            int charactersConsumed;
            int bytesWritten;
            Assert.True(TextEncoder.Utf8.TryEncode(characters, span, out charactersConsumed, out bytesWritten));

            byte[] utf8BufferActual = span.ToArray();

            Assert.Equal(unicodeString.Length, charactersConsumed);
            Assert.Equal(utf8BufferExpected.Length, bytesWritten);
            Assert.Equal(utf8BufferExpected.Length, expectedBytesWritten);

            for (int i = 0; i < utf8BufferExpected.Length; i++)
            {
                Assert.Equal(utf8BufferExpected[i], utf8BufferActual[i]);
            }
        }


        private bool Validate(int minCodePoint, int maxCodePoint)
        {
            string unicodeString = GenerateValidString(CharLength, minCodePoint, maxCodePoint);
            ReadOnlySpan<char> characters = unicodeString.AsSpan();
            char[] charArray = characters.ToArray();
            byte[] utf8BufferExpected = new byte[Encoding.UTF8.GetByteCount(charArray)];

            Encoding.UTF8.GetBytes(charArray, 0, characters.Length, utf8BufferExpected, 0);
            int expectedBytesWritten = GetByteCount(characters);
            byte[] utf8Buffer = new byte[expectedBytesWritten * 3];
            Span<byte> span = new Span<byte>(utf8Buffer);
            int charactersConsumed;
            int bytesWritten;
            if (!TextEncoder.Utf8.TryEncode(characters, span, out charactersConsumed, out bytesWritten))
            {
                return false;
            }

            byte[] utf8BufferActual = span.ToArray();


            if (charactersConsumed != unicodeString.Length)
            {
                return false;
            }
            if (bytesWritten != utf8BufferExpected.Length)
            {
                return false;
            }
            if (expectedBytesWritten != utf8BufferExpected.Length)
            {
                return false;
            }

            for (int i = 0; i < utf8BufferExpected.Length; i++)
            {
                if (utf8BufferExpected[i] != utf8BufferActual[i])
                {
                    return false;
                }
            }

            return true;
        }

        private string GenerateValidString(int charLength, int minCodePoint, int maxCodePoint)
        {
            Random rand = new Random(42);
            var plainText = new StringBuilder();
            for (int j = 0; j < charLength; j++)
            {
                var val = rand.Next(minCodePoint, maxCodePoint);

                if (j < charLength - 1)
                {
                    while (val >= Utf16LowSurrogateFirstCodePoint && val <= Utf16LowSurrogateLastCodePoint)
                    {
                        val = rand.Next(minCodePoint, maxCodePoint); // skip surrogate characters if they can't be paired
                    }

                    if (val >= Utf16HighSurrogateFirstCodePoint && val <= Utf16HighSurrogateLastCodePoint)
                    {
                        plainText.Append((char)val);    // high surrogate
                        j++;
                        val = rand.Next(Utf16LowSurrogateFirstCodePoint, Utf16LowSurrogateLastCodePoint); // low surrogate
                    }
                }
                else
                {
                    while (val >= Utf16HighSurrogateFirstCodePoint && val <= Utf16LowSurrogateLastCodePoint)
                    {
                        val = rand.Next(0, Utf8ThreeBytesLastCodePoint); // skip surrogate characters if they can't be paired
                    }
                }
                plainText.Append((char)val);
            }
            return plainText.ToString();
        }

        internal static string GenerateOnlyInvalidString(int charLength)
        {
            Random rand = new Random(42);
            var plainText = new StringBuilder();
            for (int j = 0; j < charLength; j++)
            {
                var val = rand.Next(Utf16LowSurrogateFirstCodePoint, Utf16LowSurrogateLastCodePoint);
                plainText.Append((char)val);
            }
            return plainText.ToString();
        }

        private string GenerateStringWithInvalidChars(int charLength)
        {
            Random rand = new Random(42);
            var plainText = new StringBuilder();
            for (int j = 0; j < charLength; j++)
            {
                var val = rand.Next(0, Utf8ThreeBytesLastCodePoint);

                if (j < charLength - 1)
                {
                    while (val >= Utf16LowSurrogateFirstCodePoint && val <= Utf16LowSurrogateLastCodePoint)
                    {
                        val = rand.Next(0, Utf8ThreeBytesLastCodePoint); // skip surrogate characters if they can't be paired
                    }

                    if (val >= Utf16HighSurrogateFirstCodePoint && val <= Utf16HighSurrogateLastCodePoint)
                    {
                        plainText.Append((char)val);    // high surrogate
                        j++;
                        val = rand.Next(0, Utf8ThreeBytesLastCodePoint); // high surrogate may not be paired with a low surrogate (invalid)
                    }
                }
                else
                {
                    // last char should be high surrogate (no low surrogate after, invalid)
                    val = rand.Next(Utf16HighSurrogateFirstCodePoint, Utf16HighSurrogateLastCodePoint);
                }
                plainText.Append((char)val);
            }
            return plainText.ToString();
        }

        private string GenerateValidStringEndsWithHighStartsWithLow(int charLength, bool startsWithLow, int minCodePoint, int maxCodePoint)
        {
            Random rand = new Random(42);
            var plainText = new StringBuilder();
            bool alreadyDone = false;
            for (int j = 0; j < charLength; j++)
            {
                var val = rand.Next(minCodePoint, maxCodePoint);

                if (startsWithLow && !alreadyDone)
                {
                    // first character must be low surrogate
                    val = rand.Next(Utf16LowSurrogateFirstCodePoint, Utf16LowSurrogateLastCodePoint);
                    alreadyDone = true;
                }
                else
                {
                    if (j < charLength - 1)
                    {
                        while (val >= Utf16LowSurrogateFirstCodePoint && val <= Utf16LowSurrogateLastCodePoint)
                        {
                            val = rand.Next(minCodePoint, maxCodePoint); // skip surrogate characters if they can't be paired
                        }

                        if (val >= Utf16HighSurrogateFirstCodePoint && val <= Utf16HighSurrogateLastCodePoint)
                        {
                            plainText.Append((char)val);    // high surrogate
                            j++;
                            val = rand.Next(Utf16LowSurrogateFirstCodePoint, Utf16LowSurrogateLastCodePoint);  // low surrogate
                        }
                    }
                    else
                    {
                        // if first char is valid, last char should be high surrogate (no low surrogate after, invalid)
                        val = startsWithLow ? rand.Next(0, Utf8OneByteLastCodePoint) : rand.Next(Utf16HighSurrogateFirstCodePoint, Utf16HighSurrogateLastCodePoint);
                    }
                }
                plainText.Append((char)val);
            }
            return plainText.ToString();
        }

        private string GenerateAllCharString()
        {
            var plainText = new StringBuilder();
            for (int j = 0; j <= Utf8ThreeBytesLastCodePoint; j++)
            {
                if (j >= Utf16HighSurrogateFirstCodePoint && j <= Utf16HighSurrogateLastCodePoint)
                {
                    // high surrogate, pair it with all low surrogates possible
                    for (int i = Utf16LowSurrogateFirstCodePoint; i <= Utf16LowSurrogateLastCodePoint; i++)
                    {
                        plainText.Append((char)j);
                        plainText.Append((char)i);
                    }
                }
                else if (j >= 0xDC00 && j <= 0xDFFF)
                {
                    continue;   // don't want unpaird low surrogates
                }
                else
                {
                    // characters not reserved for surrogate pairs
                    plainText.Append((char)j);
                }
            }
            return plainText.ToString();    // Length should be 0x10FFFF characters (1114112 in decimal)
        }

        private int GetByteCount(ReadOnlySpan<char> utf16)
        {
            var inputLength = utf16.Length;

            int temp = 0;
            for (int i = 0; i < inputLength; i++)
            {
                char codePoint = utf16[i];
                if (codePoint <= Utf8OneByteLastCodePoint)
                {
                    temp += 1;
                }
                else if (codePoint <= Utf8TwoBytesLastCodePoint)
                {
                    temp += 2;
                }
                else if (codePoint >= Utf16HighSurrogateFirstCodePoint && codePoint <= Utf16HighSurrogateLastCodePoint)
                {
                    i++;
                    if (i >= inputLength)
                    {
                        return temp;
                    }
                    char lowSurrogate = utf16[i];

                    if (lowSurrogate < Utf16LowSurrogateFirstCodePoint || lowSurrogate > Utf16LowSurrogateLastCodePoint)
                    {
                        return temp;
                    }
                    temp += 4;
                }
                else if (codePoint >= Utf16LowSurrogateFirstCodePoint && codePoint <= Utf16LowSurrogateLastCodePoint)
                {
                    return temp;
                }
                else
                {
                    temp += 3;
                }
            }
            return temp;
        }

        public static object[][] TryEncodeFromUTF16ToUTF8TestData = {
            // empty
            new object[] { true, new byte[] { }, new char[]{ (char)0x0050 }, false },
            // multiple bytes
            new object[] { true, new byte[] { 0x50, 0xCF, 0xA8,  0xEA, 0xBF, 0x88, 0xF0, 0xA4, 0xA7, 0xB0 },
                new char[]{ (char)0x0050, (char)0x03E8, (char)0xAFC8, (char)0xD852, (char)0xDDF0 }, true },
        };

        [Theory, MemberData("TryEncodeFromUTF16ToUTF8TestData")]
        public void UTF16ToUTF8EncodingTestForReadOnlySpanOfChar(bool useUtf8Encoder, byte[] expectedBytes, char[] chars, bool expectedReturnVal)
        {
            TextEncoder encoder = useUtf8Encoder ? TextEncoder.Utf8 : TextEncoder.Utf16;
            ReadOnlySpan<char> characters = new ReadOnlySpan<char>(chars);
            Span<byte> buffer = new Span<byte>(new byte[expectedBytes.Length]);
            int bytesWritten;
            int consumed;

            Assert.Equal(expectedReturnVal, encoder.TryEncode(characters, buffer, out consumed, out bytesWritten));
            Assert.Equal(expectedReturnVal ? expectedBytes.Length : 0, bytesWritten);

            if (expectedReturnVal)
            {
                Assert.Equal(characters.Length, consumed);
                Assert.True(AreByteArraysEqual(expectedBytes, buffer.ToArray()));
            }
        }

        public static object[][] TryEncodeFromUnicodeMultipleCodePointsTestData = {
             // empty
            new object[] { true, new byte[] { }, new uint[] { 0x50 }, false },
            new object[] { false, new byte[] { }, new uint[] { 0x50 }, false },
            // multiple bytes
            new object[] { true, new byte[] { 0x50, 0xCF, 0xA8,  0xEA, 0xBF, 0x88, 0xF0, 0xA4, 0xA7, 0xB0 },
                new uint[] { 0x50, 0x3E8, 0xAFC8, 0x249F0 } , true },
            new object[] { false, new byte[] { 0x50, 0x00, 0xE8,  0x03, 0xC8, 0xAF, 0x52, 0xD8, 0xF0, 0xDD },
                new uint[] { 0x50, 0x3E8, 0xAFC8, 0x249F0 } , true },
            // multiple bytes - buffer too small
            new object[] { true, new byte[] { 0x50 },
                new uint[] { 0x50, 0x3E8, 0xAFC8, 0x249F0 } , false },
            new object[] { false, new byte[] { 0x50, 0x00 },
                new uint[] { 0x50, 0x3E8, 0xAFC8, 0x249F0 } , false },
        };

        [Theory, MemberData("TryEncodeFromUnicodeMultipleCodePointsTestData")]
        public void TryEncodeFromUnicodeMultipleCodePoints(bool useUtf8Encoder, byte[] expectedBytes, uint[] codePointsArray, bool expectedReturnVal)
        {
            TextEncoder encoder = useUtf8Encoder ? TextEncoder.Utf8 : TextEncoder.Utf16;
            ReadOnlySpan<uint> codePoints = new ReadOnlySpan<uint>(codePointsArray);
            Span<byte> buffer = new Span<byte>(new byte[expectedBytes.Length]);
            int bytesWritten;
            int consumed;

            Assert.Equal(expectedReturnVal, encoder.TryEncode(codePoints, buffer, out consumed, out bytesWritten));
            Assert.Equal(expectedBytes.Length, bytesWritten);

            if (expectedReturnVal)
            {
                Assert.Equal(codePoints.Length, consumed);
                Assert.True(AreByteArraysEqual(expectedBytes, buffer.ToArray()));
            }
        }

        public static object[][] TryDecodeToUnicodeMultipleCodePointsTestData = {
            //empty
            new object[] { true, new uint[] {}, new byte[] {}, true },
            new object[] { false, new uint[] {}, new byte[] {}, true },
            // multiple bytes
            new object[] { true,
                new uint[] { 0x50, 0x3E8, 0xAFC8, 0x249F0 }, new byte[] { 0x50, 0xCF, 0xA8,  0xEA, 0xBF, 0x88, 0xF0, 0xA4, 0xA7, 0xB0 }, true },
            new object[] { false,
                new uint[] { 0x50, 0x3E8, 0xAFC8, 0x249F0 }, new byte[] {  0x50, 0x00, 0xE8,  0x03, 0xC8, 0xAF, 0x52, 0xD8, 0xF0, 0xDD }, true },
        };

        [Theory, MemberData("TryDecodeToUnicodeMultipleCodePointsTestData")]
        public void TryDecodeToUnicodeMultipleCodePoints(bool useUtf8Encoder, uint[] expectedCodePointsArray, byte[] inputBytesArray, bool expectedReturnVal)
        {
            TextEncoder encoder = useUtf8Encoder ? TextEncoder.Utf8 : TextEncoder.Utf16;
            Span<uint> expectedCodePoints = new Span<uint>(expectedCodePointsArray);
            Span<byte> inputBytes = new Span<byte>(inputBytesArray);
            Span<uint> codePoints = new Span<uint>(new uint[expectedCodePoints.Length]);
            int bytesWritten;
            int consumed;

            Assert.Equal(expectedReturnVal, encoder.TryDecode(inputBytes, codePoints, out consumed, out bytesWritten));

            if (expectedReturnVal)
            {
                Assert.Equal(inputBytes.Length, consumed);
                Assert.True(AreCodePointArraysEqual(expectedCodePoints.ToArray(), codePoints.ToArray()));
            }

        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void BruteTestingRoundtripEncodeDecodeAllUnicodeCodePoints(bool useUtf8Encoder)
        {
            TextEncoder encoder = useUtf8Encoder ? TextEncoder.Utf8 : TextEncoder.Utf16;
            const uint maximumValidCodePoint = 0x10FFFF;
            uint[] expectedCodePoints = new uint[maximumValidCodePoint + 1];
            for (uint i = 0; i <= maximumValidCodePoint; i++)
            {
                if (i >= 0xD800 && i <= 0xDFFF)
                {
                    expectedCodePoints[i] = 0; // skip surrogate characters
                }
                else
                {
                    expectedCodePoints[i] = i;
                }
            }

            ReadOnlySpan<uint> expectedCodePointsSpan = new ReadOnlySpan<uint>(expectedCodePoints);
            uint maxBytes = 4 * (maximumValidCodePoint + 1);
            Span<byte> buffer = new Span<byte>(new byte[maxBytes]);
            int bytesEncoded;
            int consumed;
            Assert.True(encoder.TryEncode(expectedCodePointsSpan, buffer, out consumed, out bytesEncoded));

            buffer = buffer.Slice(0, bytesEncoded);
            Span<uint> codePoints = new Span<uint>(new uint[maximumValidCodePoint + 1]);
            int written;
            Assert.True(encoder.TryDecode(buffer, codePoints, out consumed, out written));

            Assert.Equal(bytesEncoded, consumed);
            Assert.Equal(maximumValidCodePoint + 1, (uint)written);

            for (int i = 0; i <= maximumValidCodePoint; i++)
            {
                Assert.Equal(expectedCodePointsSpan[i], codePoints[i]);
            }
        }
        
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void EncodeAllUnicodeCodePoints(bool useUtf8Encoder)
        {
            TextEncoder encoder = useUtf8Encoder ? TextEncoder.Utf8 : TextEncoder.Utf16;
            Encoding systemEncoder = useUtf8Encoder ? Encoding.UTF8 : Encoding.Unicode;
            const uint maximumValidCodePoint = 0x10FFFF;
            uint[] codePoints = new uint[maximumValidCodePoint + 1];

            var plainText = new StringBuilder();
            for (int i = 0; i <= maximumValidCodePoint; i++)
            {
                if (i >= 0xD800 && i <= 0xDFFF)
                {
                    codePoints[i] = 0; // skip surrogate characters
                    plainText.Append((char)0); // skip surrogate characters
                }
                else
                {
                    codePoints[i] = (uint)i;

                    if (i > 0xFFFF)
                    {
                        plainText.Append(char.ConvertFromUtf32(i));
                    }
                    else
                    {
                        plainText.Append((char)i);
                    }
                }
            }

            ReadOnlySpan<uint> codePointsSpan = new ReadOnlySpan<uint>(codePoints);
            uint maxBytes = 4 * (maximumValidCodePoint + 1);
            Span<byte> buffer = new Span<byte>(new byte[maxBytes]);
            int bytesWritten;
            int consumed;
            Assert.True(encoder.TryEncode(codePointsSpan, buffer, out consumed, out bytesWritten));

            string unicodeString = plainText.ToString();
            ReadOnlySpan<char> characters = unicodeString.AsSpan();
            int byteCount = systemEncoder.GetByteCount(unicodeString);
            byte[] buff = new byte[byteCount];
            Span<byte> expectedBuffer;
            char[] charArray = characters.ToArray();

            systemEncoder.GetBytes(charArray, 0, characters.Length, buff, 0);
            expectedBuffer = new Span<byte>(buff);

            int minLength = Math.Min(expectedBuffer.Length, buffer.Length);

            for (int i = 0; i < minLength; i++)
            {
                Assert.Equal(expectedBuffer[i], buffer[i]);
            }
        }

        public bool AreByteArraysEqual(byte[] arrayOne, byte[] arrayTwo)
        {
            if (arrayOne.Length != arrayTwo.Length) return false;

            for (int i = 0; i < arrayOne.Length; i++)
            {
                if (arrayOne[i] != arrayTwo[i]) return false;
            }

            return true;
        }

        public bool AreCodePointArraysEqual(uint[] arrayOne, uint[] arrayTwo)
        {
            if (arrayOne.Length != arrayTwo.Length) return false;

            for (int i = 0; i < arrayOne.Length; i++)
            {
                if (!arrayOne[i].Equals(arrayTwo[i])) return false;
            }

            return true;
        }

        public static object[][] EnsureCodeUnitsOfStringTestCases = {
            // empty
            new object[] { new byte[0],""},
            // ascii
            new object[] { new byte[] { 0x61 }, "a"},
            new object[] { new byte[] { 0x61, 0x62, 0x63 }, "abc"},
            new object[] { new byte[] { 0x41, 0x42, 0x43, 0x44 }, "ABCD"},
            new object[] { new byte[] { 0x30, 0x31, 0x32, 0x33, 0x34 }, "01234"},
            new object[] { new byte[] { 0x20, 0x2c, 0x2e, 0x0d, 0x0a, 0x5b, 0x5d, 0x3c, 0x3e, 0x28, 0x29 },  " ,.\r\n[]<>()"},
            // edge cases for multibyte characters
            new object[] { new byte[] { 0x7f }, "\u007f"},
            new object[] { new byte[] { 0xc2, 0x80 }, "\u0080"},
            new object[] { new byte[] { 0xdf, 0xbf }, "\u07ff"},
            new object[] { new byte[] { 0xe0, 0xa0, 0x80 }, "\u0800"},
            new object[] { new byte[] { 0xef, 0xbf, 0xbf }, "\uffff"},
            // ascii mixed with multibyte characters
            // 1 code unit + 2 code units
            new object[] { new byte[] { 0x61, 0xc2, 0x80 }, "a\u0080"},
            // 2 code units + 1 code unit
            new object[] { new byte[] { 0xc2, 0x80, 0x61 }, "\u0080a"},
            // 1 code unit + 2 code units + 1 code unit
            new object[] { new byte[] { 0x61, 0xc2, 0x80, 0x61 }, "a\u0080a"},
            // 3 code units + 2 code units
            new object[] { new byte[] { 0xe0, 0xa0, 0x80, 0xc2, 0x80 }, "\u0800\u0080"},
            // 2 code units + 3 code units
            new object[] { new byte[] { 0xc2, 0x80, 0xe0, 0xa0, 0x80 }, "\u0080\u0800"},
            // 2 code units + 3 code units
            new object[] { new byte[] { 0xc2, 0x80, 0x61, 0xef, 0xbf, 0xbf }, "\u0080a\uffff"},
            // 1 code unit + 2 code units + 3 code units
            new object[] { new byte[] { 0x61, 0xc2, 0x80, 0xef, 0xbf, 0xbf }, "a\u0080\uffff"},
            // 2 code units + 3 code units + 1 code unit
            new object[] { new byte[] { 0xc2, 0x80, 0xef, 0xbf, 0xbf, 0x61 }, "\u0080\uffffa"},
            // 1 code unit + 2 code units + 3 code units
            new object[] { new byte[] { 0x61, 0xc2, 0x80, 0x61, 0xef, 0xbf, 0xbf, 0x61 }, "a\u0080a\uffffa"}
            // TODO: Add case with 4 byte character - it is impossible to do using string literals, need to create it using code point
        };
        [Theory, MemberData("EnsureCodeUnitsOfStringTestCases")]
        public void EnsureCodeUnitsOfStringByEnumeratingBytes(byte[] expectedBytes, string str)
        {
            var utf8String = new Utf8String(str);
            Assert.Equal(expectedBytes.Length, utf8String.Length);
            Utf8String.Enumerator e = utf8String.GetEnumerator();

            int i = 0;
            while (e.MoveNext())
            {
                Assert.True(i < expectedBytes.Length);
                Assert.Equal(expectedBytes[i], (byte)e.Current);
                i++;
            }
            Assert.Equal(expectedBytes.Length, i);
        }

        [Theory, MemberData("EnsureCodeUnitsOfStringTestCases")]
        public void EnsureCodeUnitsOfStringByIndexingBytes(byte[] expectedBytes, string str)
        {
            var utf8String = new Utf8String(str);
            Assert.Equal(expectedBytes.Length, utf8String.Length);

            for (int i = 0; i < utf8String.Length; i++)
            {
                Assert.Equal(expectedBytes[i], (byte)utf8String[i]);
            }
        }

        public static object[] PartialEncodeDecodeUtf8ToUtf16TestCases = new object[]
        {
            //new object[]
            //{
            //    /* output buffer size */, /* consumed on first pass */,
            //    new byte[] { /* UTF-8 encoded input data */ },
            //    new byte[] { /* expected output first pass */ },
            //    new byte[] { /* expected output second pass */ },
            //},
            new object[]
            {
                4, 2,
                new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F },
                new byte[] { 0x48, 0x00, 0x65, 0x00 },
                new byte[] { 0x6C, 0x00, 0x6C, 0x00, 0x6F, 0x00 },
            },
            new object[]
            {
                5, 6,
                new byte[] { 0xE6, 0xA8, 0x99, 0xE6, 0xBA, 0x96, 0xE8, 0x90, 0xAC, 0xE5, 0x9C, 0x8B, 0xE7, 0xA2, 0xBC },
                new byte[] { 0x19, 0x6A, 0x96, 0x6E },
                new byte[] { 0x2C, 0x84, 0x0B, 0x57, 0xBC, 0x78 },
            },
        };

        [Theory, MemberData("PartialEncodeDecodeUtf8ToUtf16TestCases")]
        public void TryPartialUtf8ToUtf16EncodingTest(int outputSize, int expectedConsumed, byte[] inputBytes, byte[] expected1, byte[] expected2)
        {
            int written;
            int consumed;

            var input = new Span<byte>(inputBytes);
            var output = new Span<byte>(new byte[outputSize]);

            Assert.False(TextEncoder.Utf16.TryEncode(input, output, out consumed, out written));
            Assert.Equal(expected1.Length, written);
            Assert.Equal(expectedConsumed, consumed);
            Assert.True(AreByteArraysEqual(expected1, output.Slice(0, written).ToArray()));

            input = input.Slice(consumed);
            output = new Span<byte>(new byte[expected2.Length]);
            Assert.True(TextEncoder.Utf16.TryEncode(input, output, out consumed, out written));
            Assert.Equal(expected2.Length, written);
            Assert.Equal(inputBytes.Length - expectedConsumed, consumed);
            Assert.True(AreByteArraysEqual(expected2, output.ToArray()));
        }

        [Theory, MemberData("PartialEncodeDecodeUtf8ToUtf16TestCases")]
        public void TryPartialUtf8ToUtf16DecodingTest(int outputSize, int expectedConsumed, byte[] inputBytes, byte[] expected1, byte[] expected2)
        {
            int written;
            int consumed;

            var input = new Span<byte>(inputBytes);
            var output = new Span<byte>(new byte[outputSize]);

            Assert.False(TextEncoder.Utf8.TryDecode(input, output.NonPortableCast<byte, char>(), out consumed, out written));
            Assert.Equal(expected1.Length, written * sizeof(char));
            Assert.Equal(expectedConsumed, consumed);
            Assert.True(AreByteArraysEqual(expected1, output.Slice(0, written * sizeof(char)).ToArray()));

            input = input.Slice(consumed);
            output = new Span<byte>(new byte[expected2.Length]);
            Assert.True(TextEncoder.Utf8.TryDecode(input, output.NonPortableCast<byte, char>(), out consumed, out written));
            Assert.Equal(expected2.Length, written * sizeof(char));
            Assert.Equal(inputBytes.Length - expectedConsumed, consumed);
            Assert.True(AreByteArraysEqual(expected2, output.ToArray()));
        }

        public static object[] PartialEncodeDecodeUtf16ToUtf8TestCases = new object[]
        {
            //new object[]
            //{
            //    /* output buffer size */, /* consumed on first pass */,
            //    new char[] { /* UTF-16 encoded input data */ },
            //    new byte[] { /* expected output first pass */ },
            //    new byte[] { /* expected output second pass */ },
            //},
            new object[]
            {
                2, 2,
                new char[] { '\u0048', '\u0065', '\u006C', '\u006C', '\u006F' },
                new byte[] { 0x48, 0x65 },
                new byte[] { 0x6C, 0x6C, 0x6F },
            },
            new object[]
            {
                7, 2,
                new char[] { '\u6A19', '\u6E96', '\u842C', '\u570B', '\u78BC' },
                new byte[] { 0xE6, 0xA8, 0x99, 0xE6, 0xBA, 0x96 },
                new byte[] { 0xE8, 0x90, 0xAC, 0xE5, 0x9C, 0x8B, 0xE7, 0xA2, 0xBC },
            },
        };

        [Theory, MemberData("PartialEncodeDecodeUtf16ToUtf8TestCases")]
        public void TryPartialUtf16ToUtf8EncodingTest(int outputSize, int expectedConsumed, char[] inputBytes, byte[] expected1, byte[] expected2)
        {
            int written;
            int consumed;

            var input = new Span<char>(inputBytes);
            var output = new Span<byte>(new byte[outputSize]);

            Assert.False(TextEncoder.Utf8.TryEncode(input, output, out consumed, out written));
            Assert.Equal(expected1.Length, written);
            Assert.Equal(expectedConsumed, consumed);
            Assert.True(AreByteArraysEqual(expected1, output.Slice(0, written).ToArray()));

            input = input.Slice(consumed);
            output = new Span<byte>(new byte[expected2.Length]);
            Assert.True(TextEncoder.Utf8.TryEncode(input, output, out consumed, out written));
            Assert.Equal(expected2.Length, written);
            Assert.Equal(inputBytes.Length - expectedConsumed, consumed);
            Assert.True(AreByteArraysEqual(expected2, output.ToArray()));
        }

        [Theory, MemberData("PartialEncodeDecodeUtf16ToUtf8TestCases")]
        public void TryPartialUtf16ToUtf8DecodingTest(int outputSize, int expectedConsumed, char[] inputBytes, byte[] expected1, byte[] expected2)
        {
            int written;
            int consumed;

            var input = new Span<char>(inputBytes).AsBytes();
            var output = new Span<byte>(new byte[outputSize]);

            Assert.False(TextEncoder.Utf16.TryDecode(input, output, out consumed, out written));
            Assert.Equal(expected1.Length, written);
            Assert.Equal(expectedConsumed, consumed);
            Assert.True(AreByteArraysEqual(expected1, output.Slice(0, written).ToArray()));

            input = input.Slice(consumed * sizeof(char));
            output = new Span<byte>(new byte[expected2.Length]);
            Assert.True(TextEncoder.Utf16.TryDecode(input, output, out consumed, out written));
            Assert.Equal(expected2.Length, written);
            Assert.Equal(inputBytes.Length - expectedConsumed, consumed);
            Assert.True(AreByteArraysEqual(expected2, output.ToArray()));
        }
    }
}
