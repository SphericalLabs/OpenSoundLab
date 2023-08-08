using System;
using System.IO;

namespace RuntimeAudioClipLoader.Internal
{
	/// <summary>
	/// Stream used inside <see cref="InternetRadio"/>
	/// </summary>
	public class ReadFullyStream : Stream
	{
		readonly Stream sourceStream;
		long position; // psuedo-position
		readonly byte[] readAheadBuffer;
		int readAheadLength;
		int readAheadOffset;

		public ReadFullyStream(Stream sourceStream)
		{
			this.sourceStream = sourceStream;
			readAheadBuffer = new byte[4096];
		}

		public override bool CanRead
		{
			get { return true; }
		}

		public override bool CanSeek
		{
			get { return false; }
		}

		public override bool CanWrite
		{
			get { return false; }
		}

		public override void Flush()
		{
			throw new InvalidOperationException();
		}

		public override long Length
		{
			get { return position; }
		}

		public override long Position
		{
			get
			{
				return position;
			}
			set
			{
				throw new InvalidOperationException();
			}
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			int bytesRead = 0;
			while (bytesRead < count)
			{
				int readAheadAvailableBytes = readAheadLength - readAheadOffset;
				int bytesRequired = count - bytesRead;
				if (readAheadAvailableBytes > 0)
				{
					int toCopy = Math.Min(readAheadAvailableBytes, bytesRequired);
					Array.Copy(readAheadBuffer, readAheadOffset, buffer, offset + bytesRead, toCopy);
					bytesRead += toCopy;
					readAheadOffset += toCopy;
				}
				else
				{
					readAheadOffset = 0;
					readAheadLength = sourceStream.Read(readAheadBuffer, 0, readAheadBuffer.Length);
					//Debug.WriteLine(String.Format("Read {0} bytes (requested {1})", readAheadLength, readAheadBuffer.Length));
					if (readAheadLength == 0)
					{
						break;
					}
				}
			}
			position += bytesRead;
			return bytesRead;
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new InvalidOperationException();
		}

		public override void SetLength(long value)
		{
			throw new InvalidOperationException();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new InvalidOperationException();
		}
	}
}