using Microsoft.Psi;
using Microsoft.Psi.Components;
using Microsoft.Psi.Speech;
using NU.Kiosk.SharedObject;
using log4net;
using System.Reflection;
using System;

namespace NU.Kqml
{
    public class SketchingInputTextPreProcessor : ConsumerProducer<IStreamingSpeechRecognitionResult, Utterance>
    {
        //private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public SketchingInputTextPreProcessor(Pipeline pipeline) : base(pipeline) {}

        protected override void Receive(IStreamingSpeechRecognitionResult result, Envelope e)
        {
            var message = result.Text.ToLower();

            double confidence = result.Confidence.Value;
            if (confidence < 0.3) {
                Console.WriteLine($"Received Speech Input \"{message}\" with confidence {confidence}; discarding...");
                message = "(Unintelligible)";
            } else {
                Console.WriteLine($"Received Speech Input \"{message}\" with confidence {confidence}; ");
            }
            
            switch (message)
            {
                case "okay":
                case "hm":
                case "um":
                case "ah":
                case "cool":
                case "huh?":
                case "wow!":
                case "Huck you":
                case "bye":
                case "bye bye":
                    // Filter out a few things
                    Console.WriteLine($"Discarding message: {message}");
                    break;
                case "":
                    message = "(Unintelligible)";
                    goto default;
                default:
                    this.Out.Post(new Utterance(message, confidence, StringResultSource.speech), e.Time);
                    break;
            }
        }
    }
}
