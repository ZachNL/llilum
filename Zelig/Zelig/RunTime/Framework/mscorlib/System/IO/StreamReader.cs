// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Class:  StreamReader
**
**
** Purpose: For reading text from streams in a particular 
** encoding.
**
**
===========================================================*/

using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
//using System.Runtime.Versioning;

namespace System.IO
{
    // This class implements a TextReader for reading characters to a Stream.
    // This is designed for character input in a particular Encoding, 
    // whereas the Stream class is designed for byte input and output.  
    // 
    [Serializable()]
    public class StreamReader : TextReader
    {
////    // StreamReader.Null is threadsafe.
////    public new static readonly StreamReader Null = new NullStreamReader();

        // Using a 1K byte buffer and a 4K FileStream buffer works out pretty well
        // perf-wise.  On even a 40 MB text file, any perf loss by using a 4K
        // buffer is negated by the win of allocating a smaller byte[], which 
        // saves construction time.  This does break adaptive buffering,
        // but this is slightly faster.
        internal const int DefaultBufferSize = 1024;  // Byte buffer size
        private const int DefaultFileStreamBufferSize = 4096;
        private const int MinBufferSize = 128;

        private bool _closable;  // For Console.In. We should consider exposing a Closable bit perhaps on stream at some point.

        private Stream stream;
        private Encoding encoding;
        private Decoder decoder;
        private byte[] byteBuffer;
        private char[] charBuffer;
        private byte[] _preamble;   // Encoding's preamble, which identifies this encoding.
        private int charPos;
        private int charLen;
        // Record the number of valid bytes in the byteBuffer, for a few checks.
        private int byteLen;
        // This is used only for preamble detection
        private int bytePos;

        // This is the maximum number of chars we can get from one call to 
        // ReadBuffer.  Used so ReadBuffer can tell when to copy data into
        // a user's char[] directly, instead of our internal char[].
        private int _maxCharsPerBuffer;

        // We will support looking for byte order marks in the stream and trying
        // to decide what the encoding might be from the byte order marks, IF they
        // exist.  But that's all we'll do.  
        private bool _detectEncoding;

        // Whether we must still check for the encoding's given preamble at the
        // beginning of this file.
        private bool _checkPreamble;

        // Whether the stream is most likely not going to give us back as much 
        // data as we want the next time we call it.  We must do the computation
        // before we do any byte order mark handling and save the result.  Note
        // that we need this to allow users to handle streams used for an 
        // interactive protocol, where they block waiting for the remote end 
        // to send a response, like logging in on a Unix machine.
        private bool _isBlocked;

        // StreamReader by default will ignore illegal UTF8 characters. We don't want to 
        // throw here because we want to be able to read ill-formed data without choking. 
        // The high level goal is to be tolerant of encoding errors when we read and very strict 
        // when we write. Hence, default StreamWriter encoding will throw on error.   

        internal StreamReader()
        {
        }

        public StreamReader( Stream stream ) : this( stream, true )
        {
        }

        public StreamReader( Stream stream                           ,
                             bool   detectEncodingFromByteOrderMarks ) : this( stream, Encoding.UTF8, detectEncodingFromByteOrderMarks, DefaultBufferSize )
        {
        }

        public StreamReader( Stream   stream   ,
                             Encoding encoding ) : this( stream, encoding, true, DefaultBufferSize )
        {
        }

        public StreamReader( Stream   stream                           ,
                             Encoding encoding                         ,
                             bool     detectEncodingFromByteOrderMarks ) : this( stream, encoding, detectEncodingFromByteOrderMarks, DefaultBufferSize )
        {
        }

        // Creates a new StreamReader for the given stream.  The 
        // character encoding is set by encoding and the buffer size, 
        // in number of 16-bit characters, is set by bufferSize.  
        // 
        // Note that detectEncodingFromByteOrderMarks is a very
        // loose attempt at detecting the encoding by looking at the first
        // 3 bytes of the stream.  It will recognize UTF-8, little endian
        // unicode, and big endian unicode text, but that's it.  If neither
        // of those three match, it will use the Encoding you provided.
        // 
        public StreamReader( Stream   stream                           ,
                             Encoding encoding                         ,
                             bool     detectEncodingFromByteOrderMarks ,
                             int      bufferSize                       )
        {
            if(stream == null || encoding == null)
            {
#if EXCEPTION_STRINGS
                throw new ArgumentNullException( (stream == null ? "stream" : "encoding") );
#else
                throw new ArgumentNullException();
#endif
            }

            if(!stream.CanRead)
            {
#if EXCEPTION_STRINGS
                throw new ArgumentException( Environment.GetResourceString( "Argument_StreamNotReadable" ) );
#else
                throw new ArgumentException();
#endif
            }

            if(bufferSize <= 0)
            {
#if EXCEPTION_STRINGS
                throw new ArgumentOutOfRangeException( "bufferSize", Environment.GetResourceString( "ArgumentOutOfRange_NeedPosNum" ) );
#else
                throw new ArgumentOutOfRangeException();
#endif
            }

            Init( stream, encoding, detectEncodingFromByteOrderMarks, bufferSize );
        }

        // For non closable streams such as Console.In
        internal StreamReader( Stream   stream                           ,
                               Encoding encoding                         ,
                               bool     detectEncodingFromByteOrderMarks ,
                               int      bufferSize                       ,
                               bool     closable                         ) : this( stream, encoding, detectEncodingFromByteOrderMarks, bufferSize )
        {
            _closable = closable;
        }

////    [ResourceExposure( ResourceScope.Machine )]
////    [ResourceConsumption( ResourceScope.Machine )]
        public StreamReader( String path ) : this( path, true )
        {
        }

////    [ResourceExposure( ResourceScope.Machine )]
////    [ResourceConsumption( ResourceScope.Machine )]
        public StreamReader( String path                             ,
                             bool   detectEncodingFromByteOrderMarks ) : this( path, Encoding.UTF8, detectEncodingFromByteOrderMarks, DefaultBufferSize )
        {
        }

////    [ResourceExposure( ResourceScope.Machine )]
////    [ResourceConsumption( ResourceScope.Machine )]
        public StreamReader( String   path     ,
                             Encoding encoding ) : this( path, encoding, true, DefaultBufferSize )
        {
        }

////    [ResourceExposure( ResourceScope.Machine )]
////    [ResourceConsumption( ResourceScope.Machine )]
        public StreamReader( String   path                             ,
                             Encoding encoding                         ,
                             bool     detectEncodingFromByteOrderMarks ) : this( path, encoding, detectEncodingFromByteOrderMarks, DefaultBufferSize )
        {
        }

////    [ResourceExposure( ResourceScope.Machine )]
////    [ResourceConsumption( ResourceScope.Machine )]
        public StreamReader( String   path                             ,
                             Encoding encoding                         ,
                             bool     detectEncodingFromByteOrderMarks ,
                             int      bufferSize                       )
        {
            // Don't open a Stream before checking for invalid arguments,
            // or we'll create a FileStream on disk and we won't close it until
            // the finalizer runs, causing problems for applications.
            if(path == null || encoding == null)
            {
#if EXCEPTION_STRINGS
                throw new ArgumentNullException( (path == null ? "path" : "encoding") );
#else
                throw new ArgumentNullException();
#endif
            }

            if(path.Length == 0)
            {
#if EXCEPTION_STRINGS
                throw new ArgumentException( Environment.GetResourceString( "Argument_EmptyPath" ) );
#else
                throw new ArgumentException();
#endif
            }

            if(bufferSize <= 0)
            {
#if EXCEPTION_STRINGS
                throw new ArgumentOutOfRangeException( "bufferSize", Environment.GetResourceString( "ArgumentOutOfRange_NeedPosNum" ) );
#else
                throw new ArgumentOutOfRangeException();
#endif
            }

            Stream stream = CreateFile( path );

            Init( stream, encoding, detectEncodingFromByteOrderMarks, bufferSize );
        }

        private void Init( Stream   stream                           ,
                           Encoding encoding                         ,
                           bool     detectEncodingFromByteOrderMarks ,
                           int      bufferSize                       )
        {
            this.stream = stream;
            this.encoding = encoding;
            decoder = encoding.GetDecoder();
            if(bufferSize < MinBufferSize) bufferSize = MinBufferSize;
            byteBuffer = new byte[bufferSize];
            _maxCharsPerBuffer = encoding.GetMaxCharCount( bufferSize );
            charBuffer = new char[_maxCharsPerBuffer];
            byteLen = 0;
            bytePos = 0;
            _detectEncoding = detectEncodingFromByteOrderMarks;
            _preamble = encoding.GetPreamble();
            _checkPreamble = (_preamble.Length > 0);
            _isBlocked = false;
            _closable = true; // ByDefault all streams are closable unless explicitly told otherwise
        }

////    [ResourceExposure( ResourceScope.Machine )]
////    [ResourceConsumption( ResourceScope.Machine )]
////    [MethodImpl( MethodImplOptions.InternalCall )]
        private static Stream CreateFile( String path )
        {
            FileStream f = new FileStream( path, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultFileStreamBufferSize ); //, FileOptions.SequentialScan );
            return f;
        }

        public override void Close()
        {
            Dispose( true );
        }

        protected override void Dispose( bool disposing )
        {
            // Dispose of our resources if this StreamReader is closable.
            // Note that Console.In should not be closable.
            try
            {
                // Note that Stream.Close() can potentially throw here. So we need to 
                // ensure cleaning up internal resources, inside the finally block.  
                if(Closable && disposing && (stream != null))
                {
                    stream.Close();
                }
            }
            finally
            {
                if(Closable && (stream != null))
                {
                    stream = null;
                    encoding = null;
                    decoder = null;
                    byteBuffer = null;
                    charBuffer = null;
                    charPos = 0;
                    charLen = 0;
                    base.Dispose( disposing );
                }
            }
        }

        public virtual Encoding CurrentEncoding
        {
            get { return encoding; }
        }

        public virtual Stream BaseStream
        {
            get { return stream; }
        }

        internal bool Closable
        {
            get { return _closable; }
            //set { _closable = value; }
        }

        // DiscardBufferedData tells StreamReader to throw away its internal
        // buffer contents.  This is useful if the user needs to seek on the
        // underlying stream to a known location then wants the StreamReader
        // to start reading from this new point.  This method should be called
        // very sparingly, if ever, since it can lead to very poor performance.
        // However, it may be the only way of handling some scenarios where 
        // users need to re-read the contents of a StreamReader a second time.
        public void DiscardBufferedData()
        {
            byteLen = 0;
            charLen = 0;
            charPos = 0;
            decoder = encoding.GetDecoder();
            _isBlocked = false;
        }

        public bool EndOfStream
        {
            get
            {
                if(stream == null)
                {
                    __Error.ReaderClosed();
                }

                if(charPos < charLen)
                {
                    return false;
                }

                // This may block on pipes!
                int numRead = ReadBuffer();
                return numRead == 0;
            }
        }

        public override int Peek()
        {
            if(stream == null)
            {
                __Error.ReaderClosed();
            }

            if(charPos == charLen)
            {
                if(_isBlocked || ReadBuffer() == 0) return -1;
            }

            return charBuffer[charPos];
        }

        public override int Read()
        {
            if(stream == null)
            {
                __Error.ReaderClosed();
            }

            if(charPos == charLen)
            {
                if(ReadBuffer() == 0) return -1;
            }

            int result = charBuffer[charPos];
            charPos++;
            return result;
        }

        public override int Read( [In, Out] char[] buffer, int index, int count )
        {
            if(stream == null)
            {
                __Error.ReaderClosed();
            }

            if(buffer == null)
            {
#if EXCEPTION_STRINGS
                throw new ArgumentNullException( "buffer", Environment.GetResourceString( "ArgumentNull_Buffer" ) );
#else
                throw new ArgumentNullException();
#endif
            }

            if(index < 0 || count < 0)
            {
#if EXCEPTION_STRINGS
                throw new ArgumentOutOfRangeException( (index < 0 ? "index" : "count"), Environment.GetResourceString( "ArgumentOutOfRange_NeedNonNegNum" ) );
#else
                throw new ArgumentOutOfRangeException();
#endif
            }

            if(buffer.Length - index < count)
            {
#if EXCEPTION_STRINGS
                throw new ArgumentException( Environment.GetResourceString( "Argument_InvalidOffLen" ) );
#else
                throw new ArgumentException();
#endif
            }

            int charsRead = 0;
            // As a perf optimization, if we had exactly one buffer's worth of 
            // data read in, let's try writing directly to the user's buffer.
            bool readToUserBuffer = false;
            while(count > 0)
            {
                int n = charLen - charPos;
                if(n == 0) n = ReadBuffer( buffer, index + charsRead, count, out readToUserBuffer );
                if(n == 0) break;  // We're at EOF
                if(n > count) n = count;
                if(!readToUserBuffer)
                {
                    Buffer.InternalBlockCopy( charBuffer, charPos * 2, buffer, (index + charsRead) * 2, n * 2 );
                    charPos += n;
                }
                charsRead += n;
                count -= n;
                // This function shouldn't block for an indefinite amount of time,
                // or reading from a network stream won't work right.  If we got
                // fewer bytes than we requested, then we want to break right here.
                if(_isBlocked)
                {
                    break;
                }
            }
            return charsRead;
        }

        public override String ReadToEnd()
        {
            if(stream == null)
            {
                __Error.ReaderClosed();
            }

            // Call ReadBuffer, then pull data out of charBuffer.
            StringBuilder sb = new StringBuilder( charLen - charPos );
            do
            {
                sb.Append( charBuffer, charPos, charLen - charPos );
                charPos = charLen;  // Note we consumed these characters
                ReadBuffer();
            } while(charLen > 0);

            return sb.ToString();
        }

        // Trims n bytes from the front of the buffer.
        private void CompressBuffer( int n )
        {
            BCLDebug.Assert( byteLen >= n, "CompressBuffer was called with a number of bytes greater than the current buffer length.  Are two threads using this StreamReader at the same time?" );
            Buffer.InternalBlockCopy( byteBuffer, n, byteBuffer, 0, byteLen - n );
            byteLen -= n;
        }

        private void DetectEncoding()
        {
            if(byteLen < 2)
            {
                return;
            }

////        _detectEncoding = false;
////        bool changedEncoding = false;
////        if(byteBuffer[0] == 0xFE && byteBuffer[1] == 0xFF)
////        {
////            // Big Endian Unicode
////
////            encoding = new UnicodeEncoding( true, true );
////            CompressBuffer( 2 );
////            changedEncoding = true;
////        }
////        else if(byteBuffer[0] == 0xFF && byteBuffer[1] == 0xFE)
////        {
////            // Little Endian Unicode, or possibly little endian UTF32
////            if(byteLen >= 4 && byteBuffer[2] == 0 && byteBuffer[3] == 0)
////            {
////                encoding = new UTF32Encoding( false, true );
////                CompressBuffer( 4 );
////            }
////            else
////            {
////                encoding = new UnicodeEncoding( false, true );
////                CompressBuffer( 2 );
////            }
////            changedEncoding = true;
////        }
////        else if(byteLen >= 3 && byteBuffer[0] == 0xEF && byteBuffer[1] == 0xBB && byteBuffer[2] == 0xBF)
////        {
////            // UTF-8
////            encoding = Encoding.UTF8;
////            CompressBuffer( 3 );
////            changedEncoding = true;
////        }
////        else if(byteLen >= 4 && byteBuffer[0] == 0 && byteBuffer[1] == 0 &&
////                 byteBuffer[2] == 0xFE && byteBuffer[3] == 0xFF)
////        {
////            // Big Endian UTF32
////            encoding = new UTF32Encoding( true, true );
////            changedEncoding = true;
////        }
////        else if(byteLen == 2)
////        {
////            _detectEncoding = true;
////        }
////
////        // Note: in the future, if we change this algorithm significantly,
////        // we can support checking for the preamble of the given encoding.
////
////        if(changedEncoding)
////        {
////            decoder = encoding.GetDecoder();
////            _maxCharsPerBuffer = encoding.GetMaxCharCount( byteBuffer.Length );
////            charBuffer = new char[_maxCharsPerBuffer];
////        }
        }

        // Trims the preamble bytes from the byteBuffer. This routine can be called multiple times
        // and we will buffer the bytes read until the preamble is matched or we determine that
        // there is no match. If there is no match, every byte read previously will be available 
        // for further consumption. If there is a match, we will compress the buffer for the 
        // leading preamble bytes
        private bool IsPreamble()
        {
            if(!_checkPreamble)
            {
                return _checkPreamble;
            }

            BCLDebug.Assert( bytePos <= _preamble.Length, "_compressPreamble was called with the current bytePos greater than the preamble buffer length.  Are two threads using this StreamReader at the same time?" );
            int len = (byteLen >= (_preamble.Length)) ? (_preamble.Length - bytePos) : (byteLen - bytePos);

            for(int i = 0; i < len; i++, bytePos++)
            {
                if(byteBuffer[bytePos] != _preamble[bytePos])
                {
                    bytePos = 0;
                    _checkPreamble = false;
                    break;
                }
            }

            BCLDebug.Assert( bytePos <= _preamble.Length, "possible bug in _compressPreamble.  Are two threads using this StreamReader at the same time?" );

            if(_checkPreamble)
            {
                if(bytePos == _preamble.Length)
                {
                    // We have a match
                    CompressBuffer( _preamble.Length );
                    bytePos = 0;
                    _checkPreamble = false;
                    _detectEncoding = false;
                }
            }

            return _checkPreamble;
        }

        private int ReadBuffer()
        {
            charLen = 0;
            charPos = 0;

            if(!_checkPreamble)
            {
                byteLen = 0;
            }

            do
            {
                if(_checkPreamble)
                {
                    BCLDebug.Assert( bytePos <= _preamble.Length, "possible bug in _compressPreamble.  Are two threads using this StreamReader at the same time?" );
                    int len = stream.Read( byteBuffer, bytePos, byteBuffer.Length - bytePos );
                    BCLDebug.Assert( len >= 0, "Stream.Read returned a negative number!  This is a bug in your stream class." );

                    if(len == 0)
                    {
                        // EOF but we might have buffered bytes from previous 
                        // attempts to detecting preamble that needs to decoded now
                        if(byteLen > 0)
                            charLen += decoder.GetChars( byteBuffer, 0, byteLen, charBuffer, charLen );

                        return charLen;
                    }

                    byteLen += len;
                }
                else
                {
                    BCLDebug.Assert( bytePos == 0, "bytePos can be non zero only when we are trying to _checkPreamble.  Are two threads using this StreamReader at the same time?" );
                    byteLen = stream.Read( byteBuffer, 0, byteBuffer.Length );
                    BCLDebug.Assert( byteLen >= 0, "Stream.Read returned a negative number!  This is a bug in your stream class." );

                    if(byteLen == 0)  // We're at EOF
                        return charLen;
                }

                // _isBlocked == whether we read fewer bytes than we asked for.
                // Note we must check it here because CompressBuffer or 
                // DetectEncoding will screw with byteLen.
                _isBlocked = (byteLen < byteBuffer.Length);

                // Check for preamble before detect encoding. This is not to override the
                // user suppplied Encoding for the one we implicitly detect. The user could
                // customize the encoding which we will loose, such as ThrowOnError on UTF8
                if(IsPreamble())
                    continue;

                // If we're supposed to detect the encoding and haven't done so yet,
                // do it.  Note this may need to be called more than once.
                if(_detectEncoding && byteLen >= 2)
                    DetectEncoding();

                charLen += decoder.GetChars( byteBuffer, 0, byteLen, charBuffer, charLen );
            } while(charLen == 0);

            //Console.WriteLine("ReadBuffer called.  chars: "+charLen);
            return charLen;
        }


        // This version has a perf optimization to decode data DIRECTLY into the 
        // user's buffer, bypassing StreamWriter's own buffer.
        // This gives a > 20% perf improvement for our encodings across the board,
        // but only when asking for at least the number of characters that one
        // buffer's worth of bytes could produce.
        // This optimization, if run, will break SwitchEncoding, so we must not do 
        // this on the first call to ReadBuffer.  
        private int ReadBuffer( char[] userBuffer, int userOffset, int desiredChars, out bool readToUserBuffer )
        {
            charLen = 0;
            charPos = 0;

            if(!_checkPreamble)
                byteLen = 0;

            int charsRead = 0;

            // As a perf optimization, we can decode characters DIRECTLY into a
            // user's char[].  We absolutely must not write more characters 
            // into the user's buffer than they asked for.  Calculating 
            // encoding.GetMaxCharCount(byteLen) each time is potentially very 
            // expensive - instead, cache the number of chars a full buffer's 
            // worth of data may produce.  Yes, this makes the perf optimization 
            // less aggressive, in that all reads that asked for fewer than AND 
            // returned fewer than _maxCharsPerBuffer chars won't get the user 
            // buffer optimization.  This affects reads where the end of the
            // Stream comes in the middle somewhere, and when you ask for 
            // fewer chars than than your buffer could produce.
            readToUserBuffer = desiredChars >= _maxCharsPerBuffer;

            do
            {
                if(_checkPreamble)
                {
                    BCLDebug.Assert( bytePos <= _preamble.Length, "possible bug in _compressPreamble.  Are two threads using this StreamReader at the same time?" );
                    int len = stream.Read( byteBuffer, bytePos, byteBuffer.Length - bytePos );
                    BCLDebug.Assert( len >= 0, "Stream.Read returned a negative number!  This is a bug in your stream class." );

                    if(len == 0)
                    {
                        // EOF but we might have buffered bytes from previous 
                        // attempts to detecting preamble that needs to decoded now
                        if(byteLen > 0)
                        {
                            if(readToUserBuffer)
                            {
                                charsRead += decoder.GetChars( byteBuffer, 0, byteLen, userBuffer, userOffset + charsRead );
                                charLen = 0;  // StreamReader's buffer is empty.
                            }
                            else
                            {
                                charsRead = decoder.GetChars( byteBuffer, 0, byteLen, charBuffer, charsRead );
                                charLen += charsRead;  // Number of chars in StreamReader's buffer.
                            }
                        }
                        return charsRead;
                    }

                    byteLen += len;
                }
                else
                {
                    BCLDebug.Assert( bytePos == 0, "bytePos can be non zero only when we are trying to _checkPreamble.  Are two threads using this StreamReader at the same time?" );
                    byteLen = stream.Read( byteBuffer, 0, byteBuffer.Length );
                    BCLDebug.Assert( byteLen >= 0, "Stream.Read returned a negative number!  This is a bug in your stream class." );

                    if(byteLen == 0)  // EOF
                    {
                        return charsRead;
                    }
                }

                // _isBlocked == whether we read fewer bytes than we asked for.
                // Note we must check it here because CompressBuffer or 
                // DetectEncoding will screw with byteLen.
                _isBlocked = (byteLen < byteBuffer.Length);

                // Check for preamble before detect encoding. This is not to override the
                // user suppplied Encoding for the one we implicitly detect. The user could
                // customize the encoding which we will loose, such as ThrowOnError on UTF8
                // Note: we don't need to recompute readToUserBuffer optimization as IsPreamble
                // doesn't change the encoding or affect _maxCharsPerBuffer
                if(IsPreamble())
                {
                    continue;
                }

                // On the first call to ReadBuffer, if we're supposed to detect the encoding, do it.
                if(_detectEncoding && byteLen >= 2)
                {
                    DetectEncoding();
                    // DetectEncoding changes some buffer state.  Recompute this.
                    readToUserBuffer = desiredChars >= _maxCharsPerBuffer;
                }

                charPos = 0;
                if(readToUserBuffer)
                {
                    charsRead += decoder.GetChars( byteBuffer, 0, byteLen, userBuffer, userOffset + charsRead );
                    charLen = 0;  // StreamReader's buffer is empty.
                }
                else
                {
                    charsRead = decoder.GetChars( byteBuffer, 0, byteLen, charBuffer, charsRead );
                    charLen += charsRead;  // Number of chars in StreamReader's buffer.
                }
            } while(charsRead == 0);

            _isBlocked &= charsRead < desiredChars;

            //Console.WriteLine("ReadBuffer: charsRead: "+charsRead+"  readToUserBuffer: "+readToUserBuffer);
            return charsRead;
        }


        // Reads a line. A line is defined as a sequence of characters followed by
        // a carriage return ('\r'), a line feed ('\n'), or a carriage return
        // immediately followed by a line feed. The resulting string does not
        // contain the terminating carriage return and/or line feed. The returned
        // value is null if the end of the input stream has been reached.
        //
        public override String ReadLine()
        {
            if(stream == null)
            {
                __Error.ReaderClosed();
            }

            if(charPos == charLen)
            {
                if(ReadBuffer() == 0) return null;
            }

            StringBuilder sb = null;
            do
            {
                int i = charPos;
                do
                {
                    char ch = charBuffer[i];
                    // Note the following common line feed chars:
                    // \n - UNIX   \r\n - DOS   \r - Mac
                    if(ch == '\r' || ch == '\n')
                    {
                        String s;
                        if(sb != null)
                        {
                            sb.Append( charBuffer, charPos, i - charPos );
                            s = sb.ToString();
                        }
                        else
                        {
                            s = new String( charBuffer, charPos, i - charPos );
                        }
                        charPos = i + 1;
                        if(ch == '\r' && (charPos < charLen || ReadBuffer() > 0))
                        {
                            if(charBuffer[charPos] == '\n') charPos++;
                        }
                        return s;
                    }
                    i++;
                } while(i < charLen);

                i = charLen - charPos;

                if(sb == null) sb = new StringBuilder( i + 80 );
                sb.Append( charBuffer, charPos, i );
            } while(ReadBuffer() > 0);

            return sb.ToString();
        }

        // No data, class doesn't need to be serializable.
        // Note this class is threadsafe.
        private class NullStreamReader : StreamReader
        {
            internal NullStreamReader() : base( Stream.Null, Encoding.Unicode, false, 1 )
            {
            }

            public override Stream BaseStream
            {
                get { return Stream.Null; }
            }

            public override Encoding CurrentEncoding
            {
                get { return Encoding.Unicode; }
            }

            protected override void Dispose( bool disposing )
            {
                // Do nothing - this is essentially unclosable.
            }

            public override int Peek()
            {
                return -1;
            }

            public override int Read()
            {
                return -1;
            }

            public override int Read( char[] buffer, int index, int count )
            {
                return 0;
            }

            public override String ReadLine()
            {
                return null;
            }

            public override String ReadToEnd()
            {
                return String.Empty;
            }
        }
    }
}
