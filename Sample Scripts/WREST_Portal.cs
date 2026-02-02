/// 이벤트 기반 통신 허브(중앙 포털)
/// 서버 - 디바이스 간 요청 흐름을 이벤트로 연결

using UnityEngine.Events;

namespace Medimind.WebREST
{
    public class WREST_Portal
    {
        public class Event<T>
        {
            // 요청이 발생할 때 호출할 이벤트
            public UnityAction<T> onEvent;
            // 그에 대한 응답을 받을 이벤트
            public UnityAction<ResponseMessage> onResponse;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="applyEvent"></param>
            public void AddEvent(UnityAction<T> applyEvent)
            {
                onEvent += applyEvent;
            }
            /// <summary>
            /// 등록된 이벤트를 실행 합니다.
            /// </summary>
            /// <param name="actionValue">이벤트 실행 시 넘길 데이터</param>
            /// <param name="onResponse">데이터 피드백용 이벤트</param>
            public void Action(T actionValue, UnityAction<ResponseMessage> onResponse = null)
            {
                this.onResponse = onResponse;
                onEvent?.Invoke(actionValue);

                if (onEvent == null)
                    UnityEngine.Debug.Log($"[{actionValue.ToString()}] 의 Event가 null 입니다.");
            }
        }

        // Server => Device
        /// <summary>
        /// Server => Device 디바이스 확인/ 서버 URL 설정
        /// </summary>
        public static Event<WREST_DeviceConfig> on_StoD_DeviceConfigHandler = new Event<WREST_DeviceConfig>();
        /// <summary>
        /// Server => Device 업데이트 확인 응답
        /// </summary>
        public static Event<WREST_UpdateResponse> on_StoD_UpdateResponseReadyHandler = new Event<WREST_UpdateResponse>();
        /// <summary>
        /// Server => Device 콘텐츠 다운로드 응답
        /// </summary>
        public static Event<WREST_DownloadResponse> on_StoD_DownloadResponseHandler = new Event<WREST_DownloadResponse>();
        /// <summary>
        /// Server => Device 훈련 준비
        /// </summary>
        public static Event<WREST_Ready> on_StoD_TrainingReadyHandler = new Event<WREST_Ready>();
        /// <summary>
        /// Server => Device 훈련 시작
        /// </summary>
        public static Event<WREST_Start> on_StoD_TrainingStartHandler = new Event<WREST_Start>();        
        /// <summary>
        /// Server => Device 강제 종료 수신(디바이스 종료)
        /// </summary>
        public static Event<WREST_ForceQuit> on_StoD_ForceQuitHandler = new Event<WREST_ForceQuit>();

        // Device => Server
        /// <summary>
        /// Device => Server 디바이스 상태 전송
        /// </summary>
        public static Event<WREST_DeviceStatus> on_DtoS_DeviceStatusHandler = new Event<WREST_DeviceStatus>();
        /// <summary>
        /// Device => Server 업데이트 확인 요청
        /// </summary>
        public static Event<WREST_UpdateRequest> on_DtoS_UpdateRequestHandler = new Event<WREST_UpdateRequest>();
        /// <summary>
        /// Device => Server 콘텐츠 다운로드 요청
        /// </summary>
        public static Event<WREST_DownloadRequest> on_DtoS_DownloadRequestHandler = new Event<WREST_DownloadRequest>();
        /// <summary>
        /// Device => Server 본인 확인 완료
        /// </summary>
        public static Event<WREST_Verification> on_DtoS_VerificationHandler = new Event<WREST_Verification>();
        /// <summary>
        /// Device => Server 녹음 데이터 전송
        /// </summary>
        public static Event<WREST_RecordDataUpload> on_DtoS_RecordUploadHandler = new Event<WREST_RecordDataUpload>();
        /// <summary>
        /// Device => Server 훈련 진행 상태
        /// </summary>
        public static Event<WREST_Progress> on_DtoS_ProgressHandler = new Event<WREST_Progress>();
        /// <summary>
        /// Device => Server 훈련 완료
        /// </summary>
        public static Event<WREST_Complete> on_DtoS_CompleteHandler = new Event<WREST_Complete>();
        /// <summary>
        /// Device => Server 훈련 종료(디바이스 종료)
        /// </summary>
        public static Event<WREST_End> on_DtoS_TrainingEndHandler = new Event<WREST_End>();
    }
}