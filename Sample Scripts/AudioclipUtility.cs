using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Text;
using System.Security.Cryptography;

public enum SaveType
{
    Byte,
    Wav,
    MP3,
}
public static class AudioClipUtility
{
    public static AudioClip LastConvertedClip { get { return lastConvertedClip; } }
    public static int Frequency { get { return kSampleRate; } }

    public const int kWAVE_HEADER_SIZE = 44;
    public const int kRescaleFactor = 32767;
    /// <summary>
    /// 오디오 녹음 샘플레이트
    /// </summary>
    public const int kSampleRate
#if UNITY_EDITOR
    = 16000; // 44100 -> 16000
#elif UNITY_ANDROID || UNITY_IOS
    = 16000; // 24000 -> 16000
#else
    = 16000; // 44100 -> 16000
#endif
    public const int kChannels = 1;

    private static AudioClip lastConvertedClip;
    // 암호화 키
    private static readonly string myKey = "Dhw7pY3F4R2o9tS6";
    // 초기화 벡터
    private static readonly string myIv = "Dtt7oG3F424o5r91";

    public static byte[] GenerateKey(string password)
    {
        return SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(password));
    }

    public static void Save(this AudioClip clip, SaveType saveType, string path, string filename, bool useEncrypt = false)
    {
        string fullName = Path.Combine(path, filename);

        switch (saveType)
        {
            case SaveType.Byte:
                fullName += ".data";
                byte[] convert = ClipToBinary(clip);
                File.WriteAllBytes(fullName, convert);
                break;
            case SaveType.Wav:
                if (useEncrypt)
                {
                    fullName += ".dat";
                    AudioclipToWav.SaveEncrypt(clip, fullName, GenerateKey(myKey));
                }
                else
                {
                    fullName += ".wav";
                    AudioclipToWav.Save(clip, fullName);
                }
                break;
            case SaveType.MP3:
                fullName += ".mp3";
                AudioclipToMP3.Save(clip, fullName);
                break;
            default:
                break;
        }
    }
    /// <summary>
    /// float 샘플 데이터 => 16bit PCM 데이터 변환
    /// </summary>
    /// <param name="clip"></param>
    /// <param name="addLastLength"></param>
    /// <returns></returns>
    public static byte[] ClipToBinary(this AudioClip clip, bool addLastLength = false)
    {
        lastConvertedClip = clip;

        float[] samples = new float[clip.samples * kChannels];
        clip.GetData(samples, 0);

        short[] intData = new short[samples.Length];
        List<byte> intToByteList = new List<byte>();

        for (int cnt = 0; cnt < samples.Length; cnt++)
        {
            intData[cnt] = (short)(samples[cnt] * kRescaleFactor);
            byte[] byteArr = BitConverter.GetBytes(intData[cnt]);
            intToByteList.AddRange(byteArr);
        }

        if (addLastLength)
            intToByteList.AddRange(new byte[Frequency * 2]);

        return intToByteList.ToArray();
        //return floatToByte;
    }

    /// <summary>
    /// 녹음된 음성에서 앞뒤 무음 구간 제거
    /// </summary>
    /// <param name="clip"></param>
    /// <param name="min"></param>
    /// <returns></returns>
    public static AudioClip TrimSilence(this AudioClip clip, float min)
    {
        float[] samples = new float[clip.samples];

        clip.GetData(samples, 0);

        return TrimSilence(new List<float>(samples), min, clip.channels, clip.frequency, false);
    }
    public static AudioClip TrimSilence(List<float> samples, float min, int channels, int hz)
    {
        return TrimSilence(samples, min, channels, hz, false);
    }
    public static AudioClip TrimSilence(List<float> samples, float min, int channels, int hz, bool stream)
    {
        int i;

        for (i = 0; i < samples.Count; i++)
        {
            if (Mathf.Abs(samples[i]) > min)
            {
                break;
            }
        }

        samples.RemoveRange(0, i);

        for (i = samples.Count - 1; i > 0; i--)
        {
            if (Mathf.Abs(samples[i]) > min)
            {
                break;
            }
        }

        samples.RemoveRange(i, samples.Count - i);

        AudioClip clip = AudioClip.Create("TempClip", samples.Count, channels, hz, stream);

        clip.SetData(samples.ToArray(), 0);

        return clip;
    }
}

public static class AudioclipToWav
{
    /// <summary>
    /// 파일 저장
    /// </summary>
    /// <param name="filename">저장 경로, 확장자 제외</param>
    /// <param name="clip">변경할 오디오 클림</param>
    /// <returns></returns>
    public static bool Save(AudioClip clip, string filename)
    {
        if (clip == null)
        {
            Debug.Log("AudioClip is Null");
            return false;
        }
        else
        {
            try
            {
                if (!filename.ToLower().EndsWith(".wav"))
                {
                    filename += ".wav";
                }

                string folderPath = Path.GetDirectoryName(filename);

                if (Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                using (FileStream fileStream = new FileStream(filename, FileMode.Create))
                {
                    byte[] converted = Convert(clip);

                    fileStream.Write(converted, 0, converted.Length);
                }

                return true;
            }
            catch (UnityException e)
            {
                Debug.Log(e.Message);
                return false;
            }
        }
    }

    /// <summary>
    /// AudioClip 파일을 암호화 => dat 파일로 저장
    /// </summary>
    /// <param name="filename">저장 경로, 확장자 제외</param>
    /// <param name="clip">변경할 오디오 클림</param>
    /// <returns></returns>
    public static bool SaveEncrypt(AudioClip clip, string filename, byte[] key)
    {
        if (clip == null)
        {
            Debug.Log("AudioClip is Null");
            return false;
        }
        else
        {
            try
            {
                if (!filename.ToLower().EndsWith(".dat"))
                {
                    filename += ".dat";
                }

                string folderPath = Path.GetDirectoryName(filename);

                if (Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                byte[] converted = Convert(clip);

                // 암호화
                byte[] iv = new byte[16];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(iv);
                }

                byte[] encrypted = AESUtility.Encrypt(converted, key, iv);

                using (FileStream fs = new FileStream(filename, FileMode.Create))
                {
                    fs.Write(iv, 0, iv.Length);
                    fs.Write(encrypted, 0, encrypted.Length);
                }

                DecryptDat(filename, filename.Replace(".dat", ".wav"), key);
                return true;
            }
            catch (UnityException e)
            {
                Debug.Log(e.Message);
                return false;
            }
        }
    }

    /// <summary>
    /// .dat 파일 복호화 => wav 파일 저장
    /// </summary>
    public static void DecryptDat(string datFileName, string wavPath, byte[] key)
    {
        if (!datFileName.ToLower().EndsWith(".dat"))
        {
            Debug.Log($"File Name을 확인해 주십시오 : {datFileName}");
            return;
        }

        if (!wavPath.ToLower().EndsWith(".wav"))
        {
            wavPath += ".wav";
        }

        // 복호화
        byte[] fileBytes = File.ReadAllBytes(datFileName);

        byte[] iv = new byte[16];
        Array.Copy(fileBytes, 0, iv, 0, 16);

        byte[] encrypted = new byte[fileBytes.Length - 16];
        Array.Copy(fileBytes, 16, encrypted, 0, encrypted.Length);

        byte[] waveBytes = AESUtility.Decrypt(encrypted, key, iv);

        using (FileStream fileStream = new FileStream(wavPath, FileMode.Create))
        {
            fileStream.Write(waveBytes, 0, waveBytes.Length);
        }

    }

    /// <summary>
    /// wav header 설정
    /// </summary>
    /// <param name="data">해더를 추가하고 싶은 데이터</param>
    /// <param name="clip">오디어 정보를 읽어올 클립</param>
    /// <returns></returns>
    public static byte[] Convert(AudioClip clip)
    {
        byte[] clipToBinary = clip.ClipToBinary();
        //header의 크는 44로 정해져 있음

        List<byte> header = new List<byte>();

        header.AddRange(Header(clipToBinary.Length, clip.frequency, clip.channels, clip.samples)); // 헤더 추가

        header.AddRange(clipToBinary); // PCM 데이터 추가

        return header.ToArray();
    }
    /// <summary>
    /// wav header 설정
    /// </summary>
    /// <param name="data">해더를 추가하고 싶은 데이터</param>
    /// <param name="clip">오디어 정보를 읽어올 클립</param>
    /// <returns></returns>
    public static byte[] Header(int dataSize, int hz, int channels, int samples)
    {
        //header의 크는 44로 정해져 있음

        List<byte> header = new List<byte>();
        /*riff         */
        header.AddRange(System.Text.Encoding.UTF8.GetBytes("RIFF"));
        /*chunkSize    */
        header.AddRange(Tobyte(dataSize - 8, 4));
        /*wave         */
        header.AddRange(System.Text.Encoding.UTF8.GetBytes("WAVE"));
        /*fmt          */
        header.AddRange(System.Text.Encoding.UTF8.GetBytes("fmt "));
        /*subChunk1    */
        header.AddRange(Tobyte(16, 4));
        /*audioFormat  */
        header.AddRange(Tobyte(1, 2));
        /*numChannels  */
        header.AddRange(Tobyte(channels, 2));
        /*sampleRate   */
        header.AddRange(Tobyte(hz, 4));
        /*byteRate     */
        header.AddRange(Tobyte(hz * channels * 2, 4));
        /*blockAlign   */
        header.AddRange(Tobyte((ushort)(channels * 2), 2));
        /*bitsPerSample*/
        header.AddRange(Tobyte(16, 2));
        /*datastring   */
        header.AddRange(System.Text.Encoding.UTF8.GetBytes("data"));
        /*subChunk2    */
        header.AddRange(Tobyte(samples * channels * 2, 4));

        return header.ToArray();
    }
    /// <summary>
    /// header의 크기를 맞추기 위해 사용
    /// </summary>
    /// <param name="value">변경할 값</param>
    /// <param name="count">값 크기 정의</param>
    /// <returns></returns>
    private static byte[] Tobyte(int value, int count)
    {
        byte[] values = new byte[count];
        byte[] convert = BitConverter.GetBytes(value);

        for (int cnt = 0; cnt < count; cnt++)
        {
            values[cnt] = convert[cnt];
        }

        return values;
    }
}

public static class AudioclipToMP3
{
    /// <summary>
    /// AudioClip을 MP3로 변환하여 저장한다.
    /// </summary>
    /// <param name="clip"></param>
    /// <param name="path"></param>
    public static bool Save(AudioClip clip, string filename, int bitRate = 128)
    {
        if (clip == null)
        {
            Debug.Log("AudioClip is Null");
            return false;
        }
        else
        {
            try
            {
                if (!filename.ToLower().EndsWith(".mp3"))
                    filename = filename + ".mp3";

                string folderPath = Path.GetDirectoryName(filename);

                if (Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                using (FileStream fileStream = new FileStream(filename, FileMode.Create))
                {
                    byte[] converted = Convert(clip, bitRate);

                    fileStream.Write(converted, 0, converted.Length);
                }

                return true;
            }
            catch (UnityException e)
            {
                Debug.Log(e.Message);
                return false;
            }
        }
    }
    /// <summary>
    /// AudioClip을 MP3 바이너리 데이터로 변환
    /// </summary>
    /// <param name="clip">변환할 데이터</param>
    /// <returns></returns>
    public static byte[] Convert(AudioClip clip, int bitRate = 128)
    {
        byte[] clipToBinery = clip.ClipToBinary(true);

        MemoryStream retMs = new MemoryStream();
        //MemoryStream ms = new MemoryStream(clipToBinery);
        //RawSourceWaveStream rdr = new RawSourceWaveStream(ms, new WaveFormat(AudioClipUtility.kSampleRate, 16, AudioClipUtility.kChannels));

        //LameMP3FileWriter wtr = new LameMP3FileWriter(retMs, rdr.WaveFormat, bitRate);

        //rdr.CopyTo(wtr);

        return retMs.ToArray();
    }

}