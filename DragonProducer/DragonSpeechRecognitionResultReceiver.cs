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
    public class DragonSpeechRecognitionResultReceiver : IProducer<IStreamingSpeechRecognitionResult>, IDisposable, IPipeUtilUsers
    {
        private const string listener_pipe_name = "dragon_processed_text_pipe";
        private PipeListener listener;

        private const string destination_start_stop_pipe_name = "dragon_start_stop_pipe";
        private PipeSender start_stop_sender;

        public Emitter<IStreamingSpeechRecognitionResult> Out { get; private set; }
        public Receiver<bool> isAcceptingData { get; private set; }
        
        private bool isUsing;

        public DragonSpeechRecognitionResultReceiver(Pipeline pipeline)
        {
            pipeline.RegisterPipelineStartHandler(this, this.OnPipelineStart);
            this.Out = pipeline.CreateEmitter<IStreamingSpeechRecognitionResult>(this, nameof(DragonSpeechRecognitionResultReceiver));
            this.listener = new PipeListener(PassOnToPipeline, listener_pipe_name, this);
            this.start_stop_sender = new PipeSender(destination_start_stop_pipe_name);
            this.isAcceptingData = pipeline.CreateReceiver<bool>(this, TellDragonToStartOrStopListening, nameof(this.isAcceptingData));
        }

        public void Dispose()
        {
            Console.WriteLine("[DragonSpeechRecognizerResultReceiver] Dispose");
            listener.Dispose();
            start_stop_sender.Dispose();
        }

        public void OnPipelineStart()
        {
            new Task(() => listener.Initialize()).Start();
            new Task(() => start_stop_sender.Initialize()).Start();
            isUsing = true;
        }

        public void Stop()
        {
            isUsing = false;
        }

        private void PassOnToPipeline(string result)
        {
            Out.Post(new StreamingSpeechRecognitionResult(true, result, 1), DateTime.Now);
        }

        public void ReconnectSenders()
        {
            start_stop_sender.Reconnect();
        }

        public void TellDragonToStartOrStopListening(bool isAccepting)
        {
            var startOrNot = isAccepting ? "start" : "stop";
            Console.WriteLine($"[DragonSpeechRecognizerResultReceiver] telling dragon to {startOrNot} listening");
            start_stop_sender.Send(isAccepting ? "1" : "0");
        }
    }
}
