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
using System.Speech.Synthesis;

namespace NU.Kiosk.Speech
{
    public class DragonTextSpeechSynthesizer : IConsumer<string>, IDisposable, IPipeUtilUsers
    {
        private static Regex apo_s = new Regex(@"[']s");
        private static Regex course_num1 = new Regex(@"(\d)\s+(\d)0\s+(\d)");
        private static Regex course_num2 = new Regex(@"(\d)\s+(\d+)");

        private const string listener_pipe_name = "dragon_processed_text_pipe";

        public Receiver<string> In { get; private set; }
        public Emitter<SynthesizerState> State { get; private set; }

        private const string synth_state_listener_pipe_name = "dragon_synthesizer_state_pipe";
        private const string destination_pipe_name = "dragon_synthesizer_pipe";
        private PipeListener synth_listener;
        private PipeSender synth_sender;

        private bool isUsing;

        public DragonTextSpeechSynthesizer(Pipeline pipeline)
        {
            pipeline.RegisterPipelineStartHandler(this, this.OnPipelineStart);
            this.In = pipeline.CreateReceiver<string>(this, Receive, nameof(DragonTextSpeechSynthesizer));
            this.State = pipeline.CreateEmitter<SynthesizerState>(this, nameof(DragonTextSpeechSynthesizer));

            this.synth_sender = new PipeSender(destination_pipe_name);
            this.synth_listener = new PipeListener(ReceiveSynthesizerStateFromDragon, synth_state_listener_pipe_name, this);

            isUsing = false;
        }

        public void Receive(string message, Envelope arg2)
        {
            if (isUsing)
            {
                Console.WriteLine($"[DragonTextSpeechSynthesizer] sending message to synthesizer: {message}");
                synth_sender.Send(message);
                State.Post(SynthesizerState.Speaking, DateTime.Now);
            }            
        }

        public void Dispose()
        {
            Console.WriteLine("[DragonTextSpeechSynthesizer] Dispose");
            synth_listener.Dispose();
            synth_sender.Dispose();
        }


        public void OnPipelineStart()
        {
            new Task(() => synth_sender.Initialize()).Start();
            new Task(() => synth_listener.Initialize()).Start();
            isUsing = true;
        }

        public void Stop()
        {
            isUsing = false;
        }

        private void ReceiveSynthesizerStateFromDragon(string arg1)
        {
            if (arg1.Equals("Done"))
            {
                State.Post(SynthesizerState.Ready, DateTime.Now);
            }
        }

        public void ReconnectSenders()
        {
            synth_sender.Reconnect();
        }
    }
}
