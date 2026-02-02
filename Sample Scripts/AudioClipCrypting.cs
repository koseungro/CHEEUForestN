using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Medimind
{
    public class AudioClipCrypting : Crypting
    {
        //public AudioClip Original { get { return original; } }

        //private AudioClip original;

        public AudioSource decryptTestPlayer;

        public SaveType saveType;

        /// <summary>
        /// 오디오 클립을 Byte배열로 변환 후 암호화 하여 반환 합니다.
        /// </summary>
        /// <param name="clip">암호화 할 정보</param>
        /// <returns></returns>
        public byte[] ClipToEncrypt(AudioClip clip)
        {
            byte[] clipToBinary = clip.ClipToBinary();
            byte[] binaryToX = null;

            switch (saveType)
            {
                case SaveType.Byte:
                    binaryToX = clipToBinary;
                    break;
                case SaveType.Wav:
                    binaryToX = AudioclipToWav.Convert(clip);
                    break;
                case SaveType.MP3:
                    binaryToX = AudioclipToMP3.Convert(clip);
                    break;
            }

            byte[] encryptData = Encrypt(binaryToX, clip.name);

            return encryptData;
        }
        int count;

        /// <summary>
        /// 데이터를 복호화 후 플레이를 합니다.
        /// </summary>
        public void DecryptPlay()
        {
            byte[] decryptedData = Decrypt(encryptData);
            AudioClip decryptClip = BinaryToClip(decryptedData);

            decryptTestPlayer.clip = decryptClip;

            // 복호화 클립 저장 테스트
            AudioClipUtility.Save(decryptClip, SaveType.Wav, DataManager.TestDataPath, decryptClip.name);
            decryptTestPlayer.Play();
        }

        /// <summary>
        /// 바이트 배열을 오디오 클립으로 변경한다. wav header를 제거하는 과정 포함
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public AudioClip BinaryToClip(byte[] data)
        {
            byte[] splitHeader = new byte[data.Length - AudioClipUtility.kWAVE_HEADER_SIZE];
            for (int cnt = AudioClipUtility.kWAVE_HEADER_SIZE; cnt < data.Length; cnt++)
            {
                splitHeader[cnt - AudioClipUtility.kWAVE_HEADER_SIZE] = data[cnt];
            }

            float[] samples = new float[splitHeader.Length / 2];

            for (int cnt = 0; cnt < samples.Length; cnt++)
            {
                short change = System.BitConverter.ToInt16(splitHeader, cnt * 2);
                samples[cnt] = (float)change / (float)AudioClipUtility.kRescaleFactor;
            }

            int sampleCount = splitHeader.Length / 2; // sample 1개(1short) : 2bytes이기 때문 (샘플 수 = byte 길이 / 2)
            AudioClip clip = AudioClip.Create("Decrypt Audio", sampleCount, 1, AudioClipUtility.kSampleRate, false);
            clip.SetData(samples, 0);

            return clip;
        }
    }


#if UNITY_EDITOR
    //[CanEditMultipleObjects]
    //[CustomEditor(typeof(AudioClipCrypting))]
    //public class AudioClipCryptingEditor : Editor
    //{
    //    private AudioClipCrypting Target
    //    {
    //        get
    //        {
    //            if (m_target == null)
    //                m_target = base.target as AudioClipCrypting;

    //            return m_target;
    //        }
    //    }
    //    private AudioClipCrypting m_target;

    //    public override void OnInspectorGUI()
    //    {
    //        base.OnInspectorGUI();
    //        EditorGUI.BeginChangeCheck();

    //        EditorGUILayout.Space();
    //        EditorGUILayout.Space();

    //        EditorGUILayout.BeginHorizontal();
    //        GUI.enabled = Target.decryptTestPlayer.clip != null;
    //        string text = Target.decryptTestPlayer.isPlaying ? "Playing " : "Play ";
    //        if (GUILayout.Button(text + (Target.decryptTestPlayer.clip == null ? "" : (Target.decryptTestPlayer.clip.name + "("+ Target.decryptTestPlayer.clip.length+")"))))
    //        {
    //            Target.decryptTestPlayer.Play();
    //        }
    //        GUI.enabled = true;
    //        EditorGUILayout.EndHorizontal();

    //        //여기까지 검사해서 필드에 변화가 있으면
    //        if (EditorGUI.EndChangeCheck())
    //        {
    //            Undo.RecordObjects(targets, "Changed Update Mode");
    //            //변경이 있을 시 적용된다. 이 코드가 없으면 인스펙터 창에서 변화는 있지만 적용은 되지 않는다.
    //            EditorUtility.SetDirty(Target);
    //        }
    //        serializedObject.ApplyModifiedProperties();
    //        serializedObject.Update();
    //    }
    //}
#endif
}