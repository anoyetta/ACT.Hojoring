using System;
using System.IO;
using System.Text.Encodings.Web;

namespace ACT.SpecialSpellTimer.RazorModel
{
    /// <summary>
    /// HTML エンコードを行わないエンコーダー
    /// RazorLight で XML を出力する際に、タグがエスケープされるのを防ぐために使用します。
    /// </summary>
    public class NullHtmlEncoder : HtmlEncoder
    {
        public override string Encode(string value)
        {
            return value;
        }

        public override void Encode(TextWriter output, char[] value, int startIndex, int characterCount)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (characterCount == 0) return;

            output.Write(value, startIndex, characterCount);
        }

        public override void Encode(TextWriter output, string value, int startIndex, int characterCount)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (characterCount == 0) return;

            output.Write(value.Substring(startIndex, characterCount));
        }

        public override bool WillEncode(int unicodeScalar)
        {
            return false;
        }

        public override int MaxOutputCharactersPerInputCharacter => 1;

        public override unsafe int FindFirstCharacterToEncode(char* text, int textLength)
        {
            return -1; // No character needs encoding
        }

        public override unsafe bool TryEncodeUnicodeScalar(int unicodeScalar, char* buffer, int bufferLength, out int numberOfCharactersWritten)
        {
            numberOfCharactersWritten = 0;
            return false;
        }
    }
}
