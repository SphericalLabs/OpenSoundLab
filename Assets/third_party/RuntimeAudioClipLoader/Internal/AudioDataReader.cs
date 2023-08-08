using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace RuntimeAudioClipLoader.Internal
{
	/// <summary>
	/// This is a custom modified NAudio.Wave.AudioFileReader.
	/// Has added support for Ogg with NVorbis.NAudioSupport.VorbisWaveReader
	/// Has addes support for mannaged MP3 with NLayer.NAudioSupport.Mp3FrameDecompressor
	/// </summary>
	public partial class AudioDataReader : WaveStream, ISampleProvider
	{
		WaveStream readerStream; // the waveStream which we will use for all positioning
		readonly SampleChannel sampleChannel; // sample provider that gives us most stuff we need
		readonly int destBytesPerSample;
		readonly int sourceBytesPerSample;
		readonly long length;

		static Type managedMp3DecoderType;

		public static bool IsNativeMp3DecoderAvailable { get { return Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor; } }
		public static bool IsManagedMp3DecoderAvailable { get { return managedMp3DecoderType != null; } }

		public UsedDecoder UsedDecoder { get; private set; }

		static AudioDataReader()
		{
			try
			{
				var assembly = Assembly.Load("NLayer");
				if (assembly != null)
					managedMp3DecoderType = assembly.GetType("NLayer.NAudioSupport.Mp3FrameDecompressor", false, true);
			}
			catch
			{
				managedMp3DecoderType = null;
			}
		}

		public static IMp3FrameDecompressor CreateMp3FrameDecoder(WaveFormat waveFormat, PreferredDecoder preferredDecoder)
		{
			var useMannaged = false;

			if (IsNativeMp3DecoderAvailable && IsManagedMp3DecoderAvailable)
				useMannaged = preferredDecoder == PreferredDecoder.PreferManaged;
			else if (IsNativeMp3DecoderAvailable)
				useMannaged = false;
			else if (IsManagedMp3DecoderAvailable)
				useMannaged = true;
			else
				throw new Exception("Nor native nor mannager MP3 decoder is available.");

			if (useMannaged)
			{
				//return new NLayer.NAudioSupport.Mp3FrameDecompressor(waveFormat); // compiler might not know the type if NLAyer is disabled
				return (IMp3FrameDecompressor)Activator.CreateInstance(managedMp3DecoderType, waveFormat);
			}
			else
			{
				return new AcmMp3FrameDecompressor(waveFormat);
			}
		}


		/// <summary>
		/// Initializes a new instance of AudioFileReader
		/// </summary>
		/// <param name="stream">The file to open</param>
		public AudioDataReader(Stream stream, SelectDecoder format, PreferredDecoder preferredDecoder)
		{
			readerStream = CreateReaderStream(stream, format, preferredDecoder);
			sourceBytesPerSample = (readerStream.WaveFormat.BitsPerSample / 8) * readerStream.WaveFormat.Channels;
			sampleChannel = new SampleChannel(readerStream, false);
			destBytesPerSample = 4 * sampleChannel.WaveFormat.Channels;
			length = SourceToDest(readerStream.Length);
		}

		static SelectDecoder[] autoDetectFormatOrder = new SelectDecoder[] { SelectDecoder.WAV, SelectDecoder.MP3, SelectDecoder.AIFF, SelectDecoder.Ogg };

		/// <summary>
		/// Creates the reader stream, supporting all filetypes in the core NAudio library,
		/// and ensuring we are in PCM format
		/// </summary>
		/// <param name="stream">File Name</param>
		WaveStream CreateReaderStream(Stream stream, SelectDecoder format, PreferredDecoder preferredDecoder)
		{
			WaveStream readerStream = null;
			switch (format)
			{
				case SelectDecoder.WAV:
					readerStream = new WaveFileReader(stream);
					if (readerStream.WaveFormat.Encoding != WaveFormatEncoding.Pcm && readerStream.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
					{
						readerStream = WaveFormatConversionStream.CreatePcmStream(readerStream);
						readerStream = new BlockAlignReductionStream(readerStream);
					}
					break;
				case SelectDecoder.MP3:
					readerStream = new Mp3FileReader(stream, waveFormat => CreateMp3FrameDecoder(waveFormat, preferredDecoder));
					break;
				case SelectDecoder.AIFF:
					readerStream = new AiffFileReader(stream);
					break;
				case SelectDecoder.Ogg:
					readerStream = new NVorbis.NAudioSupport.VorbisWaveReader(stream);
					break;
				case SelectDecoder.AutoDetect:
					UsedDecoder = UsedDecoder.None;
					// try to create reader for all formats
					foreach (var tryFormat in autoDetectFormatOrder)
					{
						stream.Seek(0, SeekOrigin.Begin);
						try
						{
							readerStream = CreateReaderStream(stream, tryFormat, preferredDecoder);
						}
						catch { }
						if (readerStream != null)
						{
							UsedDecoder = (UsedDecoder)tryFormat;
							break;
						}
					}
					if (readerStream == null)
						throw new Exception("Failed to figure out " + typeof(SelectDecoder) + ".");
					break;
			}
			return readerStream;
		}

		/// <summary>
		/// WaveFormat of this stream
		/// </summary>
		public override WaveFormat WaveFormat
		{
			get { return sampleChannel.WaveFormat; }
		}

		/// <summary>
		/// Length of this stream (in bytes)
		/// </summary>
		public override long Length
		{
			get { return length; }
		}

		/// <summary>
		/// Position of this stream (in bytes)
		/// </summary>
		public override long Position
		{
			get { return SourceToDest(readerStream.Position); }
			set { readerStream.Position = DestToSource(value); }
		}

		/// <summary>
		/// Reads from this wave stream
		/// </summary>
		/// <param name="buffer">Audio buffer</param>
		/// <param name="offset">Offset into buffer</param>
		/// <param name="count">Number of bytes required</param>
		/// <returns>Number of bytes read</returns>
		public override int Read(byte[] buffer, int offset, int count)
		{
			var waveBuffer = new WaveBuffer(buffer);
			int samplesRequired = count / 4;
			int samplesRead = Read(waveBuffer.FloatBuffer, offset / 4, samplesRequired);
			return samplesRead * 4;
		}

		/// <summary>
		/// Reads audio from this sample provider
		/// </summary>
		/// <param name="buffer">Sample buffer</param>
		/// <param name="offset">Offset into sample buffer</param>
		/// <param name="count">Number of samples required</param>
		/// <returns>Number of samples read</returns>
		public int Read(float[] buffer, int offset, int count)
		{
			return sampleChannel.Read(buffer, offset, count);
		}

		/// <summary>
		/// Gets or Sets the Volume of this AudioFileReader. 1.0f is full volume
		/// </summary>
		public float Volume
		{
			get { return sampleChannel.Volume; }
			set { sampleChannel.Volume = value; }
		}

		/// <summary>
		/// Helper to convert source to dest bytes
		/// </summary>
		long SourceToDest(long sourceBytes)
		{
			return destBytesPerSample * (sourceBytes / sourceBytesPerSample);
		}

		/// <summary>
		/// Helper to convert dest to source bytes
		/// </summary>
		long DestToSource(long destBytes)
		{
			return sourceBytesPerSample * (destBytes / destBytesPerSample);
		}

		/// <summary>
		/// Disposes this AudioFileReader
		/// </summary>
		/// <param name="disposing">True if called from Dispose</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (readerStream != null)
				{
					readerStream.Dispose();
					readerStream = null;
				}
			}
			base.Dispose(disposing);
		}
	}
}