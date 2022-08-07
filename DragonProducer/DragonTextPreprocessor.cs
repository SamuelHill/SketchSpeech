using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Psi;
using Microsoft.Psi.Audio;
using Microsoft.Psi.Components;
using Microsoft.Psi.Speech;
using NU.Kiosk.Speech;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NU.Kiosk.SharedObject;

namespace NU.Kiosk.Speech
{
    public class DragonTextPreprocessor : ConsumerProducer<IStreamingSpeechRecognitionResult, Utterance>
    {
        private static Regex apo_s = new Regex(@"[']s");
        private static Regex course_num1 = new Regex(@"(\d)\s+(\d)0\s+(\d)");
        private static Regex course_num2 = new Regex(@"(\d)\s+(\d+)");

        private const string listener_pipe_name = "dragon_processed_text_pipe";
        private PipeListener listener;

        public Receiver<string> UiInput { get; private set; }
        public Emitter<Utterance> Out { get; private set; }

        private bool isUsing;


        public DragonTextPreprocessor(Pipeline pipeline) : base(pipeline)
        {
            this.Out = pipeline.CreateEmitter<Utterance>(this, nameof(DragonTextPreprocessor));
            this.UiInput = pipeline.CreateReceiver<string>(this, ReceiveUiInput, nameof(this.UiInput));
        }


        private void ReceiveUiInput(string arg1, Envelope arg2)
        {
            process(arg1, StringResultSource.ui, arg2.OriginatingTime);
        }

        protected override void Receive(IStreamingSpeechRecognitionResult res, Envelope e)
        {
            process(res.Text, StringResultSource.speech, DateTime.Now);
        }

        private void process(string arg1, StringResultSource source, DateTime time)
        {
            string message = arg1;
            var lower = message.ToLower();
            if (lower.StartsWith("where") || lower.StartsWith("what") || lower.StartsWith("how")
                || lower.StartsWith("who") || lower.StartsWith("is") || lower.StartsWith("are")
                || lower.StartsWith("when") || lower.StartsWith("will") || lower.StartsWith("can ")
                || lower.StartsWith("could ") || lower.StartsWith("does ") || lower.StartsWith("do ")
                || lower.StartsWith("would ") || lower.StartsWith("have ") || lower.StartsWith("has "))
            {
                if (!lower.EndsWith("?"))
                {
                    message += "?";
                }
            }
            else if (lower.StartsWith("show") || lower.StartsWith("tell"))
            {
                if (!lower.EndsWith("."))
                {
                    message += ".";
                }
            }
            if (lower.Contains("where's") || lower.Contains("what's") || lower.Contains("who's") || lower.Contains("when's")
                || lower.Contains("how's")
                )
            {
                message = apo_s.Replace(message, m => " is");
            }

            var x = course_num1.Replace(message, m => string.Format("{0}{1}{2}", m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value));
            var updated_text = course_num2.Replace(x, m => string.Format("{0}{1}", m.Groups[1].Value, m.Groups[2].Value));

            Out.Post(new Utterance(updated_text, 1.0, source), DateTime.Now);
        }

    }
}
