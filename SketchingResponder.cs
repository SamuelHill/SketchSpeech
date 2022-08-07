using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Speech.Synthesis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using log4net;
using Microsoft.Psi;
using Microsoft.Psi.Components;
using NU.Kiosk.SharedObject;


namespace NU.Kiosk.Speech
{
    public class SketchingResponder
    {
        //private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public enum DialogState
        {
            Sleeping,
            Listening,
            Thinking,
            Speaking,
            Waiting
        }

        private readonly Pipeline pipeline;

        public SketchingResponder(Pipeline pipeline)
        {
            this.pipeline = pipeline;
            this.UserInput = pipeline.CreateReceiver<Utterance>(this, ReceiveUserInput, nameof(this.UserInput));
            this.KQMLRequest = pipeline.CreateEmitter<string>(this, nameof(this.KQMLRequest));
            this.StateChanged = pipeline.CreateEmitter<string>(this, nameof(this.StateChanged));
        }

        public Receiver<Utterance> UserInput { get; private set; }
        public Emitter<string> KQMLRequest { get; private set; }
        public Emitter<string> StateChanged { get; private set; }

        private void ReceiveUserInput(Utterance arg1, Envelope arg2)
        {
            Console.WriteLine($"[ReceiverUserInput] Utterance received: {arg1.Text}");
            KQMLRequest.Post(arg1.Text, arg2.OriginatingTime);
            updateState(DialogState.Listening, arg2.OriginatingTime);
        }

        private void updateState(DialogState newState, DateTime dt)
        {
            StateChanged.Post("listening", DateTime.Now);
        }
    }
}