using Medimind;
using Medimind.WebREST;

using System;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEngine.UI;

using RESTfulHTTPServer.src.models;
using RESTfulHTTPServer.src.controller;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Medimind.WebREST
{
    // 클라이언트 쪽 응답 코드
    [Serializable]
    public struct Response_Client<T>
    {
        public string code;
        public string message;

        public Response_Client(string code, string message)
        {
            this.code = code;
            this.message = message;
        }
    }

    /// <summary>
    /// Server => Device [Request] 통신에 대한 반응
    /// </summary>
    public class WREST_Request_S2D : WREST_RequestData
    {
        public static WREST_Request_S2D Instance { get; set; }

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        /// <summary>
        /// ServerInit.Start() 내부 routingManager에서 문자열로 호출
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static Response Receive(Request request)
        {
            Response response = new Response();
            string json = request.POSTData;

            UnityEngine.Debug.Log($"Receive Request:\n{json}");

            // API 키 다른 경우
            if (WREST.Header.kAPIKEY != request.apikey)
            {
                string rsMessage = JsonUtility.ToJson(new Response_Client<object>("410", "Authentication Failed"), true);
                WrongCode(rsMessage, HttpStatusCode.Unauthorized, out response);
                UnityEngine.Debug.Log($"WrongCode - HttpStatusCode.Unauthorized(401): \n{json}\n[{WREST.Header.kAPIKEY}]/[{request.apikey}]\n{request.ToString()}");
                return response;
            }

            string state = request.GetParameter("State").Replace("/", ""); // 요청(진행 상태) 가져오기.  ex) /ready => ready

            // State 가져올 수 없는 경우
            if (!Enum.TryParse(state, out WREST_State _state))
            {
                string rsMessage = JsonUtility.ToJson(new Response_Client<object>("400", "Bad Request"), true);
                WrongCode(rsMessage, HttpStatusCode.NotFound, out response);
                UnityEngine.Debug.Log($"WrongCode - HttpStatusCode.NotFound(404): \n{json}");
                return response;
            }

            // 장치 초기화 실패 또는 초기화 미완료 경우
            if (false) // => 임시로 작성
            {
                string rsMessage = JsonUtility.ToJson(new Response_Client<object>("401", "Device Uninitialized"), true);
                WrongCode(rsMessage, HttpStatusCode.Unauthorized, out response);
                UnityEngine.Debug.Log($"WrongCode - HttpStatusCode.OK(401): \n{json}");
                return response;
            }
            else // 정상 수신
            {
                response = MakeResponse(request.Route.Url, json, _state);

                if (response.HTTPStatus == (int)HttpStatusCode.OK)
                {
                    callActionQueue.Enqueue((_state, json));
                }

                return response;
            }
        }
        private static Response MakeResponse(string requestURL, string json, WREST_State _state)
        {
            Response response = new Response();

            (bool, string) kCheck = KeyCheck(json, _state);
            (bool, string) vCheck = ValueCheck(_state);

            try
            {
                Response_Client<object> responseClientData = new Response_Client<object>(); // 서버 쪽으로 보낼 응답 객체 생성

                if (kCheck.Item1 && vCheck.Item1)
                {
                    response.HTTPStatus = (int)HttpStatusCode.OK;

                    responseClientData.code = ResponseCode.Success.code;
                    responseClientData.message = ResponseCode.Success.message;
                }
                else
                {
                    string wrongText = "";

                    if (kCheck.Item1 == false)
                    {
                        wrongText = kCheck.Item2;
                    }
                    if (vCheck.Item1 == false)
                    {
                        wrongText = vCheck.Item2;
                    }
                    ResponseMessage status = ResponseCodeMaker(_state, wrongText);

                    responseClientData.code = status.code;
                    responseClientData.message = status.message;
                }

                response.Content = JsonUtility.ToJson(responseClientData, true);

                UnityEngine.Debug.Log($"Enqueue responseData({requestURL}):\n{json}\n" +
                                   $"[Key: {kCheck.Item1}/{(kCheck.Item2 == "" ? "Correct KeyList" : kCheck.Item2)}]\n" +
                                   $"[Value: {vCheck.Item1}/{(vCheck.Item2 == "" ? "Correct Values" : vCheck.Item2)}]");

                return response;
            }
            catch (Exception e)
            {
                response.Content = "Failed to deseiralised JSON";
                response.HTTPStatus = (int)HttpStatusCode.NotAcceptable;

                UnityEngine.Debug.Log($"<color=red>Error Json:</color>\n{requestURL}\n{json}\n\n{e}\n\n" +
                                   $"unvalid: Failed to deseiralised JSON\n" +
                                   $"[Key: {kCheck.Item1}/{(kCheck.Item2 == "" ? "Correct KeyList" : kCheck.Item2)}]\n" +
                                   $"[Value: {vCheck.Item1}/{(vCheck.Item2 == "" ? "Correct Values" : vCheck.Item2)}]");

                return response;
            }
        }

        /// <summary>
        /// D -> S -> D 로 들어온 Response 검증
        /// </summary>
        /// <param name="requestURL"></param>
        /// <param name="json"></param>
        /// <param name="_state"></param>
        /// <returns></returns>
        public bool ReceiveResponse(string responseJson, WREST_State state)
        {
            bool isValid = false;
            UnityEngine.Debug.Log($"[ReceiveResponse] S->D 응답 수신 검증 :\n{responseJson}");

            // Key / Value Check 수행
            (bool, string) kCheck = KeyCheck(responseJson, state);
            (bool, string) vCheck = ValueCheck(state);

            try
            {
                Response_Client<object> result = new Response_Client<object>();

                if (kCheck.Item1 && vCheck.Item1)
                {
                    result.code = ResponseCode.Success.code;
                    result.message = ResponseCode.Success.message;

                    isValid = true;
                }
                else
                {
                    string wrongText = "";

                    if (kCheck.Item1 == false)
                    {
                        wrongText = kCheck.Item2;
                    }
                    if (vCheck.Item1 == false)
                    {
                        wrongText = vCheck.Item2;
                    }
                    ResponseMessage status = ResponseCodeMaker(state, wrongText);

                    result.code = status.code;
                    result.message = status.message;

                    isValid = false;
                }

                // 응답 내용을 JSON으로 변환
                string resultJson = JsonUtility.ToJson(result, true);

                // 서버로 다시 회신 (비동기 업로드)
                WREST_Request_D2S.Instance.ReportToServerResponse(state, resultJson);

                UnityEngine.Debug.Log($"[ReceiveResponse] 결과 회신:\nisValid : {isValid}\n{resultJson}\n" +
                                      $"[Key: {kCheck.Item1}/{(kCheck.Item2 == "" ? "Correct KeyList" : kCheck.Item2)}]\n" +
                                      $"[Value: {vCheck.Item1}/{(vCheck.Item2 == "" ? "Correct Values" : vCheck.Item2)}]");

            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log($"<color=red>[ReceiveResponse Error]</color>\n{e}" +
                                      $"[Key: {kCheck.Item1}/{(kCheck.Item2 == "" ? "Correct KeyList" : kCheck.Item2)}]\n" +
                                      $"[Value: {vCheck.Item1}/{(vCheck.Item2 == "" ? "Correct Values" : vCheck.Item2)}]");
            }

            return isValid;
        }

        private static string WrongCode(string v, HttpStatusCode code, out Response response)
        {
            response = new Response(WREST.Header.kHTML, v, (int)code, WREST.Header.kAPIKEY);

            return JsonUtility.ToJson(response);
        }


    }
}

