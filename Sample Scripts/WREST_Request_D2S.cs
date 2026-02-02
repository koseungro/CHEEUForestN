using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.Events;
using FNI.IS;

namespace Medimind.WebREST
{
    /// <summary>
    /// Device => Server 데이터 전송
    /// </summary>
    public class WREST_Request_D2S : MonoBehaviour
    {
        public static WREST_Request_D2S Instance { get; set; }

        // D -> S 테스트용 임시 서버
        private string mockServerUrl = "http://192.168.35.93:4010";


        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(this.gameObject);
            }
            else
                Debug.LogError($"WREST_Request_D2S Singleton Instnace 생성 오류 : {this.gameObject.name}");

            WREST_Portal.on_DtoS_DeviceStatusHandler.AddEvent((m) => ReportToJsonFormat(m, WREST_Portal.on_DtoS_DeviceStatusHandler));
            WREST_Portal.on_DtoS_UpdateRequestHandler.AddEvent((m) => ReportToJsonFormat(m, WREST_Portal.on_DtoS_UpdateRequestHandler));
            WREST_Portal.on_DtoS_DownloadRequestHandler.AddEvent((m) => ReportToJsonFormat(m, WREST_Portal.on_DtoS_DownloadRequestHandler));
            WREST_Portal.on_DtoS_VerificationHandler.AddEvent((m) => ReportToJsonFormat(m, WREST_Portal.on_DtoS_VerificationHandler));
            WREST_Portal.on_DtoS_RecordUploadHandler.AddEvent((m) => ReportToJsonFormat(m, WREST_Portal.on_DtoS_RecordUploadHandler));
            WREST_Portal.on_DtoS_ProgressHandler.AddEvent((m) => ReportToJsonFormat(m, WREST_Portal.on_DtoS_ProgressHandler));
            WREST_Portal.on_DtoS_CompleteHandler.AddEvent((m) => ReportToJsonFormat(m, WREST_Portal.on_DtoS_CompleteHandler));
            WREST_Portal.on_DtoS_TrainingEndHandler.AddEvent((m) => ReportToJsonFormat(m, WREST_Portal.on_DtoS_TrainingEndHandler));
        }
        private void ReportToJsonFormat(WREST_DeviceStatus data, WREST_Portal.Event<WREST_DeviceStatus> response)
        {
            StartCoroutine(Upload(WREST_State.status, data, response.onResponse));
        }
        private void ReportToJsonFormat(WREST_UpdateRequest data, WREST_Portal.Event<WREST_UpdateRequest> response)
        {
            StartCoroutine(Upload(WREST_State.update, data, response.onResponse));
        }
        private void ReportToJsonFormat(WREST_DownloadRequest data, WREST_Portal.Event<WREST_DownloadRequest> response)
        {
            StartCoroutine(Upload(WREST_State.download, data, response.onResponse));
        }
        private void ReportToJsonFormat(WREST_Verification data, WREST_Portal.Event<WREST_Verification> response)
        {
            StartCoroutine(Upload(WREST_State.verification, data, response.onResponse));
        }
        private void ReportToJsonFormat(WREST_RecordDataUpload data, WREST_Portal.Event<WREST_RecordDataUpload> response)
        {
            StartCoroutine(Upload(WREST_State.upload, data, response.onResponse));
        }
        private void ReportToJsonFormat(WREST_Progress data, WREST_Portal.Event<WREST_Progress> response)
        {
            StartCoroutine(Upload(WREST_State.progress, data, response.onResponse));
        }
        private void ReportToJsonFormat(WREST_Complete data, WREST_Portal.Event<WREST_Complete> response)
        {
            StartCoroutine(Upload(WREST_State.complete, data, response.onResponse));
        }
        private void ReportToJsonFormat(WREST_End data, WREST_Portal.Event<WREST_End> response)
        {
            StartCoroutine(Upload(WREST_State.end, data, response.onResponse));
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        // Server로 Json 데이터 전송
        private IEnumerator Upload<T>(WREST_State state, T data, UnityAction<ResponseMessage> response = null)// where T: WREST_RequestBase
        {
            string toJson = JsonUtility.ToJson(data, true);
            //toJson = toJson.JsonTFChanger();

            UnityEngine.Debug.Log($"D to S Request[{state}]\n{toJson}\n{WREST.URL.SERVER.Get(state)}");


            using (UnityWebRequest request = new UnityWebRequest($"{WREST.URL.SERVER.Get(state)}", WREST.Type.POST.ToString())) 
            {
                byte[] jsonTosend = new UTF8Encoding().GetBytes(toJson);
                request.uploadHandler = new UploadHandlerRaw(jsonTosend);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("content-type", WREST.Header.kJSON);
                request.SetRequestHeader("api-key", WREST.Header.kAPIKEY);
                request.SetRequestHeader("device-key", WREST.Header.kDeviceKey_Test);

#if UNITY_5_6_6
                yield return request.Send();

                yield return new WaitForSeconds(0.1f);

                if (request.isError)
#else
                yield return request.SendWebRequest();
                UnityEngine.Debug.Log($"D to S Request[{state}] Sended.");

                if (request.result != UnityWebRequest.Result.Success &&
                    request.result != UnityWebRequest.Result.InProgress)
#endif
                {
                    UnityEngine.Debug.Log($"[Error Sended WebRequest/F2D]\n" +
                                       $"{toJson}\n{WREST.Header.kAPIKEY}\n{WREST.Header.kDeviceKey_Test}\n" +
                                       $"request.error: {request.error}");
                }
                else
                {
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        UnityEngine.Debug.Log($"Request=>state: {state}\n\n" +
                                         $"{toJson}\n\n" +
                                         $"Response=>\n{request.downloadHandler.text.ToBeauty()}");
                    }
                    //WREST_LogManager.Instance.RequestText_F2D(toJson, state.ToString());
                    //WREST_LogManager.Instance.ResponeText_D2F($"Request=>transactionId: {id}, state: {state}\n\n" +
                    //                                          $"Response=>\n{request.downloadHandler.text.ToBeauty()}", state.ToString());
                }

                string responseText = request.downloadHandler.text;

                if (string.IsNullOrEmpty(responseText))
                {
                    UnityEngine.Debug.Log("서버 응답이 비어 있습니다.");
                    response?.Invoke(new ResponseMessage { code = "500", message = "Empty Response", data = null });
                }
                else
                {
                    try
                    {
                        ResponseMessage baseResponse = JsonUtility.FromJson<ResponseMessage>(responseText);
                        response?.Invoke(baseResponse);

                        bool responseIsValid = false;
                        object parsedData = null;

                        // S -> D 특정 응답 data 수신 시 처리
                        switch (state)
                        {
                            case WREST_State.update:
                                parsedData = ParseResponseData<WREST_UpdateResponse, WREST_UpdateData>(responseText, ref baseResponse, state, out responseIsValid);
                                if (responseIsValid && baseResponse.data is WREST_UpdateData updateData)
                                {
                                    WREST_Portal.on_StoD_UpdateResponseReadyHandler.Action((WREST_UpdateResponse)parsedData);
                                }
                                break;
                            case WREST_State.download:
                                parsedData = ParseResponseData<WREST_DownloadResponse, WREST_DownloadData>(responseText, ref baseResponse, state, out responseIsValid);
                                if (responseIsValid && baseResponse.data is WREST_DownloadData downloadData)
                                {
                                    WREST_Portal.on_StoD_DownloadResponseHandler.Action((WREST_DownloadResponse)parsedData);
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.Log($"[Upload] 응답 파싱 실패: {e.Message}\nResponse Raw:\n{responseText}");
                    }
                }

                request.uploadHandler.Dispose();
                request.downloadHandler.Dispose();
            }
        }

        /// <summary>
        ///  D -> S -> D 로 들어온 Response data 검증 후 응답 회신
        /// </summary>
        /// <param name="state"></param>
        /// <param name="resultJson"></param>
        public void ReportToServerResponse(WREST_State state, string resultJson)
        {
            StartCoroutine(UploadResponseToServer(state, resultJson));
        }

        private IEnumerator UploadResponseToServer(WREST_State state, string resultJson)
        {
            using (UnityWebRequest request = new UnityWebRequest($"{WREST.URL.SERVER.Get(state)}", WREST.Type.POST.ToString()))
            {
                byte[] jsonToSend = new UTF8Encoding().GetBytes(resultJson);
                request.uploadHandler = new UploadHandlerRaw(jsonToSend);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", WREST.Header.kJSON);
                request.SetRequestHeader("api-key", WREST.Header.kAPIKEY);
                request.SetRequestHeader("device-key", WREST.Header.kDeviceKey_Test);

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                    UnityEngine.Debug.Log($"[UploadResponseToServer] D -> S -> D 최종 응답 회신 성공:\n{resultJson}");
                else
                    UnityEngine.Debug.Log($"[UploadResponseToServer] D -> S -> D 최종 응답 회신 실패:\n{resultJson}");
            }
        }

        /// <summary>
        /// D -> S -> D 로 들어온 Response Json 유효성 검증 후 파싱
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="Tdata"></typeparam>
        /// <param name="json"></param>
        /// <param name="baseResponse"></param>
        /// <param name="state"></param>
        /// <param name="isValid"> Json 데이터 Key, Value 유효성 </param>
        public static T ParseResponseData<T, Tdata>(string json, ref ResponseMessage baseResponse, WREST_State state, out bool isValid) where T : WREST_ResponseStoDBase<Tdata>, new()
        {
            isValid = WREST_Request_S2D.Instance.ReceiveResponse(json, state);

            if (isValid == false)
                return null;

            T parsed = JsonUtility.FromJson<T>(json);
            baseResponse.data = parsed.data;
            return parsed;
        }
    }
}
