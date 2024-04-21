using System;
using System.Reflection;
using System.IO;
using System.Linq;
using UnityEngine;

namespace TownOfHostForE.Modules;

//SP Thx SNR!!!!!!!!!
public static class WavManager
{
	public static AudioClip loadAudioClipFromWavResources(string path, string clipName = "UNNAMED_TOR_AUDIO_CLIP")
	{
		Assembly assembly = Assembly.GetExecutingAssembly();
		Stream stream = assembly.GetManifestResourceStream(path);
		return ToAudioClip(stream, clipName);
	}
	public static AudioClip ToAudioClip(Stream memoryStream, string audioClipName="WavNoneClipName")
    {
        // RIFF
        var riffBytes = new byte[4];
        memoryStream.Read(riffBytes);
        if (riffBytes[0] != 0x52 || riffBytes[1] != 0x49 || riffBytes[2] != 0x46 || riffBytes[3] != 0x46) throw new ArgumentException("fileBytes is not the correct Wav file format.");

		// chunk size
		var chunkSizeBytes = new byte[4];
        memoryStream.Read(chunkSizeBytes);
        var chunkSize = BitConverter.ToInt32(chunkSizeBytes);

        // WAVE
        var wavBytes = new byte[4];
        memoryStream.Read(wavBytes);
        if (wavBytes[0] != 0x57 || wavBytes[1] != 0x41 || wavBytes[2] != 0x56 || wavBytes[3] != 0x45) throw new ArgumentException("fileBytes is not the correct Wav file format.");

        // fmt
        var fmtBytes = new byte[4];
        memoryStream.Read(fmtBytes);
        if (fmtBytes[0] != 0x66 || fmtBytes[1] != 0x6d || fmtBytes[2] != 0x74 || fmtBytes[3] != 0x20) throw new ArgumentException("fileBytes is not the correct Wav file format.");

		// fmtSize
		var fmtSizeBytes = new byte[4];
        memoryStream.Read(fmtSizeBytes);
        var fmtSize = BitConverter.ToInt32(fmtSizeBytes);

		// AudioFormat
		var audioFormatBytes = new byte[2];
        memoryStream.Read(audioFormatBytes);
        var isPCM = audioFormatBytes[0] == 0x1 && audioFormatBytes[1] == 0x0;

		// NumChannels   Mono = 1, Stereo = 2
		var numChannelsBytes = new byte[2];
        memoryStream.Read(numChannelsBytes);
        var channels = (int)BitConverter.ToUInt16(numChannelsBytes);

		// SampleRate
		var sampleRateBytes = new byte[4];
        memoryStream.Read(sampleRateBytes);
        var sampleRate = BitConverter.ToInt32(sampleRateBytes);

		// ByteRate (=SampleRate * NumChannels * BitsPerSample/8)
		var byteRateBytes = new byte[4];
        memoryStream.Read(byteRateBytes);

		// BlockAlign (=NumChannels * BitsPerSample/8)
		var blockAlignBytes = new byte[2];
        memoryStream.Read(blockAlignBytes);

		// BitsPerSample
		var bitsPerSampleBytes = new byte[2];
        memoryStream.Read(bitsPerSampleBytes);
        var bitPerSample = BitConverter.ToUInt16(bitsPerSampleBytes);

		// Discard Extra Parameters
		if (fmtSize > 16) memoryStream.Seek(fmtSize - 16, SeekOrigin.Current);

		// Data
		var subChunkIDBytes = new byte[4];
        memoryStream.Read(subChunkIDBytes);

        // If fact exists, discard fact
        if (subChunkIDBytes[0] == 0x66 && subChunkIDBytes[1] == 0x61 && subChunkIDBytes[2] == 0x63 && subChunkIDBytes[3] == 0x74)
        {
            var factSizeBytes = new byte[4];
            memoryStream.Read(factSizeBytes);
            var factSize = BitConverter.ToInt32(factSizeBytes);
            memoryStream.Seek(factSize, SeekOrigin.Current);
            memoryStream.Read(subChunkIDBytes);
        }

        //LISTだったら
        if (subChunkIDBytes[0] == 0x4C && subChunkIDBytes[1] == 0x49 && subChunkIDBytes[2] == 0x53 && subChunkIDBytes[3] == 0x54)
        {
            var factSizeBytes = new byte[4];
            memoryStream.Read(factSizeBytes);
            var factSize = BitConverter.ToInt32(factSizeBytes);
            memoryStream.Seek(factSize, SeekOrigin.Current);
            memoryStream.Read(subChunkIDBytes);

            //ここまでがいらないので新しいの取得
            factSizeBytes = new byte[4];
            memoryStream.Read(factSizeBytes);
            factSize = BitConverter.ToInt32(factSizeBytes);
            memoryStream.Seek(factSize, SeekOrigin.Current);
            memoryStream.Read(subChunkIDBytes);

        }

        if (subChunkIDBytes[0] != 0x64 || subChunkIDBytes[1] != 0x61 || subChunkIDBytes[2] != 0x74 || subChunkIDBytes[3] != 0x61) throw new ArgumentException("fileBytes is not the correct Wav file format.");

		// dataSize (=NumSamples * NumChannels * BitsPerSample/8)
		var dataSizeBytes = new byte[4];
        memoryStream.Read(dataSizeBytes);var dataSize = BitConverter.ToInt32(dataSizeBytes);

        var data = new byte[dataSize];
        memoryStream.Read(data);
        memoryStream.Close();
        memoryStream.Dispose();
        return CreateAudioClip(data, channels, sampleRate, bitPerSample, audioClipName);
    }
    private static AudioClip CreateAudioClip(byte[] data, int channels, int sampleRate, UInt16 bitPerSample, string audioClipName)
    {
        Logger.Info(bitPerSample.ToString(), "BITPAR");
        var audioClipData = bitPerSample switch
        {
            8 => Create8BITAudioClipData(data),
            16 => Create16BITAudioClipData(data),
            32 => Create32BITAudioClipData(data),
            _ => throw new ArgumentException($"bitPerSample is not supported : bitPerSample = {bitPerSample}")
        };
        Logger.Info(audioClipData.Length.ToString()+":Length","wav");
        var audioClip = AudioClip.Create(audioClipName, (audioClipData.Length / 2) - 30, channels, sampleRate, false);
        audioClip.SetData(audioClipData, 0);
        return audioClip;
    }
    
    private static float[] Create8BITAudioClipData(byte[] data)
        => data.Select((x, i) => (float)data[i] / sbyte.MaxValue).ToArray();

    private static float[] Create16BITAudioClipData(byte[] data)
    {
        var audioClipData = new float[data.Length / 2];
        var memoryStream = new MemoryStream(data);

        for (var i = 0; ; i++)
        {
            var target = new byte[2];
            var read = memoryStream.Read(target);

            if (read <= 0) break;

            audioClipData[i] = (float)BitConverter.ToInt16(target) / short.MaxValue;
        }

        return audioClipData;
    }

    private static float[] Create32BITAudioClipData(byte[] data)
    {
        var audioClipData = new float[data.Length / 4];
        var memoryStream = new MemoryStream(data);

        for (var i = 0; ; i++)
        {
            var target = new byte[4];
            var read = memoryStream.Read(target);

            if (read <= 0) break;

            audioClipData[i] = (float)BitConverter.ToInt32(target) / int.MaxValue;
        }

        return audioClipData;
    }
}