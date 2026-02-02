using Medimind;
using Medimind.IO;
using Medimind.WebREST;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// WebRESTApi와 관련된 코드 작성
/// </summary>
public partial class Main : MonoBehaviour
{
    public ConnectServer connectServer;
    public WREST_Ready readyData;
    private List<SequenceData> currentTraingList = new List<SequenceData>();
    public static string ServerURL { get; private set; }

    /// <summary>
    /// WebREST에서 사용할 Start함수
    /// </summary>
    private void OnStartWebREST()
    {
        connectServer.StartConnect();

        // S -> D 이벤트 등록
        WREST_Portal.on_StoD_DeviceConfigHandler.AddEvent(CheckInServer);
        WREST_Portal.on_StoD_UpdateResponseReadyHandler.AddEvent(CheckState);
        WREST_Portal.on_StoD_DownloadResponseHandler.AddEvent(DownloadData);
        WREST_Portal.on_StoD_TrainingReadyHandler.AddEvent(CheckUser);
        WREST_Portal.on_StoD_TrainingStartHandler.AddEvent(TrainingStart);
        WREST_Portal.on_StoD_ForceQuitHandler.AddEvent(ForceQuit);

    }

    /// <summary>
    /// Device Key 설정
    /// </summary>
    /// <returns></returns>
    public static string SetDeviceKey()
    {
        string deviceKey = DeviceInfoUtils.GetHashedAndroidId();
        WREST.Header.kDeviceKey = deviceKey;

        Debug.Log($"<color=cyan>Set Device Key</color> : {deviceKey}");

        return deviceKey;
    }

    /// <summary>
    /// 서버 URL이 설정되어 있는지 확인 후 디바이스 상태 전송 시작
    /// </summary>
    public static void CheckServerURL()
    {
        //  WREST.URL.SERVER의 domain이 설정되어 있는지 체크 
        // -> 설정되어 있으면 바로 디바이스 상태 전송 단계로
        // -> 설정되어 있지 않으면 서버 IP 설정 필요
        if (WREST.URL.ReadServerURL(false))
        {
            WREST_Device.Instance.StartSendDeviceStatus();
        }
        else
            Debug.LogError("Server IP가 설정되어 있지 않습니다.");

    }

    /// <summary>
    /// 초기 서버 URL 설정 (S -> D)
    /// </summary>
    /// #### Server -> Client(Oculus) Request 통신 불가로 미사용 ####
    public void CheckInServer(WREST_DeviceConfig config)
    {
        MD_Path.CheckMediPath();

        FNI_Json json_SERVER = new FNI_Json($"{MD_Path.Medimind}/URL_Server.json");
        WREST_URL server = new WREST_URL();
        server.domain = config.serverIP;

        json_SERVER.Save(server, true); // server config 파일 생성

        UnityEngine.Debug.Log($"<color=cyan>서버 URL 설정 완료</color> : [{config}]\n => Server Url : {server.Get()}\n");
    }
    /// <summary>
    /// 서버 설정된 상태에서 장치의 상태를 반복해서 보냄 (D -> S)
    /// </summary>
    public void OnDeviceState()
    {
        WREST_Device.Instance.StartSendDeviceStatus();
    }
    /// <summary>
    /// [[[앱 실행 시]]] 1번만 실행, 신규 데이터가 있는지 업데이트 확인 (D -> S)
    /// </summary>
    public void OnCheckUpdate()
    {
        // TODO : 업데이트 요청할 리소스 목록 객체 데이터화해서 전송
        WREST_UpdateRequest updateRequest = new WREST_UpdateRequest();
        updateRequest.data.resourceList = ResourceMgr.Instance.ConvertItoWREST();

        WREST_Portal.on_DtoS_UpdateRequestHandler.Action(updateRequest); // 임시 작성(요청할 리소스 목록 적용 필요)
    }
    /// <summary>
    /// <see cref="OnCheckUpdate"/>함수에서 서버로 업데이트 확인 시 피드백을 처리할 함수 (S -> D)
    /// </summary>
    public void CheckState(WREST_UpdateResponse updateResponse)
    {
        // 업데이트할 목록 있는지 Check => Download OR 바로 Initialization
        bool isUpdateRequire = false;
        for (int i = 0; i < updateResponse.data.resourceList.Length; i++)
        {
            if (updateResponse.data.resourceList[i].isUpdateRequired == true)
            {
                isUpdateRequire = true;
                break;
            }
        }

        if (isUpdateRequire) // 다운로드 실행
        {
            OnUpdateDownLoad();
        }
        else // 초기화
        {

        }

    }
    /// <summary>
    /// <see cref="CheckState"/>함수에서 처리 후 다운로드 해야 하는 데이터가 있을 때 사용할 함수 (D -> S)
    /// </summary>
    public void OnUpdateDownLoad()
    {
        // 다운로드 요청
        WREST_Portal.on_DtoS_DownloadRequestHandler.Action(new WREST_DownloadRequest());
    }
    /// <summary>
    /// <see cref="OnUpdateDownLoad"/>함수에서 다운로드를 요청한 후 다운로드를 처리 할 함수 (S -> D)
    /// </summary>
    public void DownloadData(WREST_DownloadResponse downloadResponse)
    {
        // TODO: 다운로드 실행에 대한 내용(Adressable)

    }

    /// <summary>
    /// [[[훈련 준비]]] 훈련을 시작하기 전 본인 확인 하는 단계 (S -> D)
    /// </summary>
    public void CheckUser(WREST_Ready ready)
    {
        //Todo: 본인확인 화면의 띄우는 내용
        WREST_UserInfo wrestUserInfo = ready.userInfo;
        WREST_UserData.Instance.SetWRESTToData(wrestUserInfo);

        readyData = ready;

        User_Popup.Instance.ShowPopupByType(User_Popup.PopupType.UserInfo, OnCheckUserComplate);

        UnityEngine.Debug.Log($"<color=cyan>본인 확인 정보 수신 완료</color>");
    }

    /// <summary>
    /// 서버에 본인 확인 완료를 전송하기 위한 함수[Verification] (D -> S)
    /// </summary>
    public void OnCheckUserComplate()
    {
        WREST_Portal.on_DtoS_VerificationHandler.Action(new WREST_Verification() { userInfo = WREST_UserData.web_userInfo });
    }

    /// <summary>
    /// [[[훈련 시작]]] (S -> D)
    /// Main.cs 파일에서 사용중임, 아래 함수명으로 이벤트 등록할 것
    /// </summary>
    public void TrainingStart(WREST_Start start)
    {
        TrainingStart(readyData.trainingList);
    }

    /// <summary>
    /// [[[훈련 시작]]]
    /// </summary>
    public void TrainingStart(string[] contents)
    {
        currentTraingList = new List<SequenceData>();

        for (int cnt = 0; cnt < contents.Length; cnt++)
        {
            currentTraingList.Add(allContentsList.Find(x => x.contentsID == contents[cnt]));
        }
        datamanager.Init(currentTraingList[0].contentsID);
        sequence.StartSequence(currentTraingList);
    }

    public void NextTraining()
    {
        UnLoadAllResources();
        datamanager.Init(sequence.CurrentSequence.contentsID);
        background.UnLoad(sequence.NextSequence, true);
    }
    /// <summary>
    /// 씬데이터를 로드하기 위해 호출(<see cref="SequenceManager"/>에서 호출)
    /// Main.cs 파일에서 사용중임, 아래 함수명으로 이벤트 등록할 것
    /// </summary>
    //public void OnLoadSceneData(SceneContainerData sceneContainer, UnityAction onEnded){}

    /// <summary>
    /// 훈련 진행률 갱신, 행위 완료시 호출(<see cref="SequenceManager"/>에서 호출) (D -> S)
    /// </summary>
    public void OnTrainingStateUpdate(string contentsID, float progress)
    {
        WREST_Progress progressData = new WREST_Progress(contentsID, progress);

        WREST_Portal.on_DtoS_ProgressHandler.Action(progressData);
    }
    /// <summary>
    /// 훈련 완료시 호출(<see cref="SequenceManager"/>에서 호출) (D -> S)
    /// </summary>
    public void OnTrainingComplete()
    {
        WREST_Complete completeData = new WREST_Complete();

        completeData.contentsId = SequenceManager.Instance.CurrentSequence.contentsID;

        WREST_Portal.on_DtoS_CompleteHandler.Action(completeData);

        datamanager.AllComplateAndSendData(()=>
        {
            User_Popup.Instance.ShowPopupByType(User_Popup.PopupType.NextStep, NextTraining);
        });        
    }
    /// <summary>
    /// 모든 시퀀스 진행 완료시 서버로 최종 완료 신호 전송 (D -> S)
    /// </summary>
    public void TrainingEnded()
    {
        WREST_Portal.on_DtoS_TrainingEndHandler.Action(new WREST_End());

        datamanager.AllComplateAndSendData(()=>
        {
            User_Popup.Instance.ShowPopupByType(User_Popup.PopupType.Confirm, TrainingReset);
        });
        
        //Todo: 종료 절차
        TrainingReset();
    }
    /// <summary>
    /// [[[강제 종료]]] 콘텐츠 강제 종료 (S -> D)
    /// </summary>
    /// <param name="forceQuit"></param>
    public void ForceQuit(WREST_ForceQuit forceQuit)
    {
        TrainingReset();
    }
    /// <summary>
    /// 트레이닝을 완전히 끝내고 초기상태로 만든다.
    /// </summary>
    public void TrainingReset()
    {
        UnLoadAllResources();
        background.UnLoad();
        sequence.ResetSequenceManager();
    }
    /// <summary>
    /// 
    /// </summary>
    public void TrainingStop()
    {
        datamanager.AllComplateAndSendData(() =>
        {
            if (sequence.ISLastSequence)
            {
                WREST_Portal.on_DtoS_TrainingEndHandler.Action(new WREST_End());

                sequence.ResetSequenceManager();
                UnLoadAllResources();

                User_Popup.Instance.ShowPopupByType(User_Popup.PopupType.Confirm, TrainingReset);
            }
            else
            {
                WREST_Complete completeData = new WREST_Complete();
                completeData.data.userInfo = WREST_UserData.web_userInfo;
                WREST_Portal.on_DtoS_CompleteHandler.Action(completeData);

                sequence.Forced_StopTrainingSequence();
                UnLoadAllResources();

                User_Popup.Instance.ShowPopupByType(User_Popup.PopupType.NextStep, NextTraining);
            }
        });
    }
    public void Test_NextScene()
    {
        UnLoadAllResources();
        sequence.Forced_NextScene();
    }
}
