using System;
using System.ServiceModel;

namespace NU.Kiosk
{
    [ServiceContract]
    public interface ISpeechRecResultAndEventsReceiver
    {
        [OperationContract]
        void ReceiveRecognitionResult(string Result, DateTime Timestamp);

        [OperationContract]
        void ReceiveUtteranceBeganSignal(DateTime Timestamp);

        [OperationContract]
        void ReceiveUtteranceEndedSignal(DateTime Timestamp);
    }
}
