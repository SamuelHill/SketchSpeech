using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace NU.Kiosk
{
    public delegate void AcquireRecognitionResult(string Result, DateTime Timestamp);
    public delegate void UtteranceBegan(DateTime Timestamp);
    public delegate void UtteranceEnded(DateTime Timestamp);

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession)]
    public class SpeechRecResultAndEventsReceiver : ISpeechRecResultAndEventsReceiver
    {
        public static AcquireRecognitionResult RecognitionResultMethods;
        public static UtteranceBegan UtteranceBeganMethods;
        public static UtteranceEnded UtteranceEndedMethods;

        public void ReceiveRecognitionResult(string Result, DateTime Timestamp)
        {
            // pass on to 
            RecognitionResultMethods(Result, Timestamp);
        }

        public void ReceiveUtteranceBeganSignal(DateTime Timestamp) {
            UtteranceBeganMethods(Timestamp);
        }

        public void ReceiveUtteranceEndedSignal(DateTime Timestamp) {
            UtteranceEndedMethods(Timestamp);
        }
    }
}
