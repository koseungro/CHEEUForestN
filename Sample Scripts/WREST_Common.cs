using System;
using System.Collections;
using System.Collections.Generic;

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using Medimind.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Medimind.WebREST
{
    public enum WREST_State
    {
        None,
        config, // 디바이스 등록 및 서버 URL 전달 (S -> D)
        status, // 디바이스 기기 상태 확인 및 상태 전송 (D -> S)
        update, // 콘텐츠 업데이트 필요 여부 확인 및 신규 업데이트 상태 전송 (D -> S -> D)
        download, // 콘텐츠 다운로드 요청 및 다운로드 진행(업데이트 필요 시 호출) (D -> S -> D)
        ready, // 훈련할 내용과 사용자 정보 보냄 (S -> D)
        verification, // 본인 확인 완료 전송 (D -> S)
        start, // 훈련 시작 (S -> D)
        upload, // 녹음 데이터 전송 (D -> S)
        progress, // 훈련 진행 상태 전송 (D -> S)
        complete, // 훈련 준비시 설정된 모든 훈련을 완료함 (D -> S)
        end, // 훈련을 종료함 (S -> D)
        forceQuit, // 훈련 강제 종료 (S -> D)
        saveResult,
    }
    public class WREST
    {
        public enum Type { GET, POST, DELETE, PUT };

        public class Header
        {
            public const string kJSON = "application/json";
            public const string kXML = "application/xml";
            public const string kHTML = "text/html";
            public const string kTEXT = "text/plain";
            public const string kAPIKEY = "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";
            public static string kDeviceKey = "";

            // 연동테스트용 임시 변수
            public const string kDeviceKey_Test = "0000";
        }
        public class URL
        {
            /// <summary>
            /// MetaQuest
            /// </summary>
            public static WREST_URL DEVICE = new WREST_URL();
            /// <summary>
            /// WebServer
            /// </summary>
            public static WREST_URL SERVER = new WREST_URL();

            public static bool SetDeviceURL()
            {
                MD_Path.CheckMediPath();

                FNI_Json json_DEVICE = new FNI_Json(FilePath.Device);

                DEVICE.domain = MD_Device.IP.My;
                DEVICE.port = 4010;
                DEVICE.key = Main.SetDeviceKey();

                json_DEVICE.Save<WREST_URL>(DEVICE, true);
                Debug.Log($"Set DEVICE Json => {FilePath.Device}\n{json_DEVICE.Loaded}");

                return true;

            }

            public static bool ReadServerURL(bool makeServer)
            {
                //SERVER.domain = "192.168.0.66";
                //SERVER.port = 8080;

                FNI_Json json_SERVER = new FNI_Json(FilePath.Server);
                if (json_SERVER.CanLoad)
                {
                    if(makeServer)
                        SERVER = json_SERVER.Load<WREST_URL>();

                    Debug.Log($"Read Server Json => {FilePath.Server}\n{json_SERVER.Loaded}");
                    Debug.Log($"<color=cyan>Server URL</color>: {SERVER.Get()}");

                    return true;
                }
                else
                    return false;
            }
        }
        public class FilePath
        {
            public static string Server => $"{MD_Path.Medimind}/URL_Server.json";
            public static string Device => $"{MD_Path.Medimind}/URL_Device.json";
        }

        public class Time
        {
            public static string Now => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }
        public class Data
        {
            public const string kNoneFloat = "0";
            public const string kNone = "-";
            public const string kYes = "Y";
            public const string kNo = "N";
        }
        public const string kNAMESPACE = "Medimind.WebREST.";

        public static string TransactionId;
    }

    // 실제 서버 주소 관리
    public class WREST_URL
    {
        public bool UsingIDPW => id != "" && pw != "";

        public string domain = "";
        public int port = 4010;
        public string id = "";
        public string pw = "";
        public string key = "";
        public string contextPath = "api/vr";

        public string Get()
        {
            return $"http://{domain}:{port}/{contextPath}";
        }
        public string Get(string state)
        {
            return $"{Get()}/{state}";
        }
        public string Get(WREST_State state)
        {
            return $"{Get()}/{state}";
        }

    }

    // 서버와 클라이언트가 주고받는 Json 데이터 구조 정의
    [Serializable]
    public class WREST_RequestBase // S -> D
    {
        public virtual string[] GetKeyList()
        {
            return new string[] { };
        }
    }

    public class WREST_RequestAdditional : WREST_RequestBase
    {
        // Json Key (고정값)
        public const string kTransactionId = "transactionId";
        public const string kCreateOn = "createOn";

        // Json Value (가변값) => data
        public string transactionId;
        public string createOn;

        public override string[] GetKeyList()
        {
            return new string[] { kTransactionId, kCreateOn };
        }

    }

    [Serializable]
    public class WREST_ResponseStoDBase<T> : WREST_RequestBase // S -> D 응답
    {
        public const string kCode = "code";
        public const string kMessage = "message";

        public string code;
        public string message;
        public T data;

        public override string[] GetKeyList()
        {
            return new string[] {
                kCode,
                kMessage};
        }
    }

    #region 1. 디바이스 등록 S to D
    [Serializable]
    public class WREST_DeviceConfig : WREST_RequestBase
    {
        public const string kServerIP = "serverIP";
        public const string kDeviceKey = "DeviceKey";

        public string serverIP = "";
        public string DeviceKey = "";

        public override string[] GetKeyList()
        {
            return new string[] {
                kServerIP,
                kDeviceKey};
        }
    }
    #endregion

    #region 2. 디바이스 상태 전송 D -> S
    [Serializable]
    public class WREST_DeviceStatus
    {
        public WREST_DeviceInfo deviceInfo = new WREST_DeviceInfo();

    }

    [Serializable]
    public class WREST_DeviceInfo
    {
        public float batteryLevel;
        public bool isHeadset = false;
        public bool isCharging = false;
        public bool isTraining = false;
    }
    #endregion

    #region 3-1. 업데이트 확인 요청 D to S
    [Serializable]
    public class WREST_UpdateRequest
    {
        public WREST_UpdateData data = new WREST_UpdateData();
    }

    [Serializable]
    public class WREST_UpdateData
    {
        public WREST_ResourceData[] resourceList = new WREST_ResourceData[0];
    }

    [Serializable]
    public class WREST_ResourceData
    {
        public string id = "";
        public string version = "";
        public bool isUpdateRequired = false;
    }
    #endregion

    #region 3-2. 업데이트 확인 응답 S to D
    [Serializable]
    public class WREST_UpdateResponse : WREST_ResponseStoDBase<WREST_UpdateData>
    {
        public const string kResourceList = "resourceList";
        public const string kId = "id";
        public const string kVersion = "version";
        public const string kIsUpdateRequired = "isUpdateRequired";

        public override string[] GetKeyList()
        {
            return new string[] {
                kCode,
                kMessage,
                /*kData,*/ kResourceList,
                       kId,
                       kVersion,
                       kIsUpdateRequired,
            };
        }
    }

    #endregion

    #region 4-1. 콘텐츠 다운로드 요청 D -> S
    [Serializable]
    public class WREST_DownloadRequest { }

    #endregion

    #region 4-2. 콘텐츠 다운로드 응답 S -> D
    [Serializable]
    public class WREST_DownloadResponse : WREST_ResponseStoDBase<WREST_DownloadData>
    {
        public const string kDownloadList = "downloadList";

        public override string[] GetKeyList()
        {
            return new string[] {
                kCode,
                kMessage,
                /*kData,*/ kDownloadList};
        }
    }

    [Serializable]
    public class WREST_DownloadData
    {
        public string[] downloadList = new string[0];
    }
    #endregion

    #region 5. 훈련 준비 S -> D
    [Serializable]
    public class WREST_Ready : WREST_RequestBase
    {
        public const string kUserInfo = "userInfo";
        public const string kUserSeq = "userSeq";
        public const string kBirthday = "birthday";
        public const string kUserName = "name";
        public const string kGender = "gender";

        public const string kTrainingList = "trainingList";

        public WREST_UserInfo userInfo = new WREST_UserInfo();
        public string[] trainingList = new string[0];

        public override string[] GetKeyList()
        {
            return new string[] {
                kUserInfo,
                kUserSeq,
                kBirthday,
                kUserName,
                kGender,
                kTrainingList };
        }
    }

    [Serializable]
    public class WREST_UserInfo
    {
        public int userSeq;
        public string birthday;
        public string name;
        public string gender;
        public string age;
    }
    #endregion

    #region 6. 본인 확인 완료 D -> S
    [Serializable]
    public class WREST_Verification
    {
        public WREST_UserInfo userInfo = new WREST_UserInfo();
    }
    #endregion

    #region 7. 훈련 시작 S -> D
    [Serializable]
    public class WREST_Start : WREST_RequestBase { }
    #endregion

    #region 8. 녹음 데이터 전송 D -> S
    [Serializable]
    public class WREST_RecordDataUpload
    {
        public WREST_RecordData data = new WREST_RecordData();
    }

    [Serializable]
    public class WREST_RecordData
    {
        public RecordData recordData;
    }

    #endregion

    #region 9. 훈련 진행 상태 D -> S
    [Serializable]
    public class WREST_Progress
    {
        public string contentsID = "";
        public float progress;

        public WREST_Progress(string cID, float pg)
        {
            contentsID = cID;
            progress = pg;
        }
    }
    #endregion

    #region 10. 훈련 완료 D -> S
    [Serializable]
    public class WREST_Complete
    {
        public string contentsId = "";
        public WREST_TrainingData data = new WREST_TrainingData();

    }
    #endregion

    [Serializable]
    public class WREST_TrainingData
    {
        /// <summary>
        /// 훈련 준비 단계에서 Server에서 들어온 Json 객체 정보
        /// </summary>
        public WREST_UserInfo userInfo;

        public ResultData[] resultData = new ResultData[0];

        //public string time = "";
        //public string id = "";
        //public int value;
    }

    #region 11. 훈련 종료 D -> S
    [Serializable]
    public class WREST_End { }
    #endregion

    #region 12. 강제 종료 S -> D
    [Serializable]
    public class WREST_ForceQuit : WREST_RequestBase { }
    #endregion


    #region reponseMessage

    // 응답 코드 정의
    [Serializable]
    public class ResponseCode
    {
        public static ResponseMessage Success = new ResponseMessage("200", "Success");
        public static ResponseMessage BadRequest = new ResponseMessage("400", "Bad Request", "400");
        public static ResponseMessage DeviceUninitialized = new ResponseMessage("401", "Device Uninitialized");
        public static ResponseMessage PreparingDevice = new ResponseMessage("408", "Preparing Device");
        public static ResponseMessage AuthenticationFailed = new ResponseMessage("410", "Authentication Failed");
        public static ResponseMessage InvalidMessageFormat = new ResponseMessage("420", "Invalid Message Format");
        public static ResponseMessage MandatoryParameterMissing = new ResponseMessage("430", "Mandatory Parameter Missing");
        public static ResponseMessage TypeMismatch = new ResponseMessage("440", "Type Mismatch");
        public static ResponseMessage InternalServerError = new ResponseMessage("500", "Internal Server Error");
        public static ResponseMessage ConnectionFail = new ResponseMessage("610", "Connection Fail");

        /// <summary>
        /// 데이터 포함 성공
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static ResponseMessage SuccessWithData(object data)
        {
            return new ResponseMessage("200", "Success", "200", data);
        }
    }
    [Serializable]
    public struct ResponseMessage
    {
        public string code;
        public string message;
        public string httpStatusCode;
        public object data;

        public ResponseMessage(string code, string message, string httpStatusCode = "200", object data = null)
        {
            this.code = code;
            this.message = message;
            this.httpStatusCode = httpStatusCode;
            this.data = data;
        }

        public T GetData<T>()
        {
            try
            {
                if (data != null)
                {
                    string json = data.ToString();

                    if (json.StartsWith("{") && json.EndsWith("}"))
                        return JsonUtility.FromJson<T>(json);
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log($"[ResponseMessage : S -> D] data 파싱 실패: {e.Message}");
            }
            return default;
        }

        public override string ToString()
        {
            return $"[ResponseMessage]\ncode: {code}\nmessage: {message}\ndata: {data}";
        }
    }
    #endregion

}