using log4net;
using Microsoft.Psi;
using Microsoft.Psi.Audio;
using Microsoft.Psi.Components;
using Microsoft.Psi.Speech;
using System;
using System.Configuration;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Timers;

namespace NU.Kiosk.Speech
{

    public class DragonSpeechRecognizerClient : ClientBase<IAudioStreamReceiver>, 
        IConsumerProducer<AudioBuffer, IStreamingSpeechRecognitionResult>, IConsumer<AudioBuffer>, IProducer<IStreamingSpeechRecognitionResult>, IDisposable
    {
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public DragonSpeechRecognizerClient(Pipeline pipeline)
            : base(new ServiceEndpoint(ContractDescription.GetContract(typeof(IAudioStreamReceiver)),
                new NetNamedPipeBinding(), new EndpointAddress(ConfigurationManager.AppSettings["DragonSpeechRecognizerServer_EndpointAddr"])))
        {
            pipeline.RegisterPipelineStartHandler(this, this.OnPipelineStart);
            this.In = pipeline.CreateReceiver<AudioBuffer>(this, SendAudioData, nameof(DragonSpeechRecognizerClient));
            this.Out = pipeline.CreateEmitter<IStreamingSpeechRecognitionResult>(this, nameof(DragonSpeechRecognizerClient)+"Out");
            this.UtteranceBegan = pipeline.CreateEmitter<UtteranceBeganEventArgs>(this, nameof(DragonSpeechRecognizerClient)+"UtteranceBegan");
            this.UtteranceEnded = pipeline.CreateEmitter<UtteranceEndedEventArgs>(this, nameof(DragonSpeechRecognizerClient)+"UtteranceEnded");

            Accumulator = new SimpleAudioBuffer(new byte[AccumulatorCapacityInBytes]);
            AccumulatorCurrentSize = 0;
            AccumulatorCleaner = new System.Timers.Timer(1000);
            AccumulatorCleaner.AutoReset = true;
            AccumulatorCleaner.Elapsed += ResetAccumulatorByTimer;

            SpeechRecResultAndEventsReceiver.RecognitionResultMethods += ReceiveRecognitionResult;
            SpeechRecResultAndEventsReceiver.UtteranceBeganMethods += ReceiveUtteranceBeganSignal;
            SpeechRecResultAndEventsReceiver.UtteranceEndedMethods += ReceiveUtteranceEndedSignal;

            consecutiveFailureCount = 0;
            TriesSkippedConsecutiveAfterFailure = 0;
            serviceHost = new ServiceHost(typeof(SpeechRecResultAndEventsReceiver));
            serviceHost.Open();

            ((ICommunicationObject)this).Faulted += new EventHandler(delegate
            {
                Console.WriteLine("Service faulted");
            });

        }

        ServiceHost serviceHost = null;
        IAudioStreamReceiver chann;
        private int consecutiveFailureCount;
        private int TriesSkippedConsecutiveAfterFailure;

        public Receiver<AudioBuffer> In { get; private set; }
        public Emitter<IStreamingSpeechRecognitionResult> Out { get; private set; }
        public Emitter<UtteranceBeganEventArgs> UtteranceBegan { get; private set; }
        public Emitter<UtteranceEndedEventArgs> UtteranceEnded { get; private set; }

        private SimpleAudioBuffer Accumulator;
        private uint AccumulatorCurrentSize;
        private System.Timers.Timer AccumulatorCleaner;
        const uint AccumulatorCapacityInBytes = 5000;

        private void ReceiveRecognitionResult(string Result, DateTime Timestamp)
        {
            this.Out.Post(new StreamingSpeechRecognitionResult(true, Result, 1), Timestamp);
        }

        private void ReceiveUtteranceBeganSignal(DateTime Timestamp)
        {
            this.UtteranceBegan.Post(new UtteranceBeganEventArgs(), Timestamp);
        }

        private void ReceiveUtteranceEndedSignal(DateTime Timestamp)
        {
            this.UtteranceEnded.Post(new UtteranceEndedEventArgs(), Timestamp);
        }

        public void SendAudioData(AudioBuffer buffer, Envelope e)
        {
            AccumulatorCleaner.Stop();
            if (AccumulatorCurrentSize + buffer.Length > AccumulatorCapacityInBytes)
            {
                SendOffLastBatchOfData(e.OriginatingTime, false);
                ResetAccumulator();
            }
            Array.Copy(buffer.Data, 0, Accumulator.Data, AccumulatorCurrentSize, buffer.Length);
            AccumulatorCurrentSize += (uint)buffer.Length;
            AccumulatorCleaner.Start();
        }

        private void SendOffLastBatchOfData(DateTime originatingTime, bool endOfKnownTransmission)
        {
            if (consecutiveFailureCount > 0)
            {
                if (++TriesSkippedConsecutiveAfterFailure >= 30)
                {
                    consecutiveFailureCount = 0;
                    TriesSkippedConsecutiveAfterFailure = 0;
                }
                else
                {
                    return;
                }
            }

            // enqueue Accumulator, restart
            Accumulator.Stamp(AccumulatorCurrentSize, originatingTime, endOfKnownTransmission);

            try
            {
                chann.AcceptAudio(Accumulator);
                consecutiveFailureCount = 0;
                TriesSkippedConsecutiveAfterFailure = 0;
            }
            catch //(Exception e)
            {
                Console.WriteLine($"Channel fault. Stream data packet is discarded. Channel is recreated. ");// {e}");
                chann = this.ChannelFactory.CreateChannel();
                consecutiveFailureCount++;
                // keep going; this throws away the stream result
            }
        }

        private void ResetAccumulatorByTimer(object sender, ElapsedEventArgs e)
        {
            AccumulatorCleaner.Stop();
            SendOffLastBatchOfData(e.SignalTime, true);
            ResetAccumulator();
        }

        private void ResetAccumulator()
        {
            Accumulator = new SimpleAudioBuffer(new byte[AccumulatorCapacityInBytes]); // timestamp will be overridden
            AccumulatorCurrentSize = 0;
        }

        public void Dispose()
        {
            if (serviceHost != null)
            {
                SpeechRecResultAndEventsReceiver.RecognitionResultMethods -= ReceiveRecognitionResult;
                serviceHost.Abort();
                serviceHost.Close();
            }
            this.Abort();
            this.Close();
        }

        private void BlockUntilServiceIsAlive()
        {
            bool HasStarted = false;
            while (true)
            {
                try
                {
                    if (!HasStarted)
                    {
                        _log.Debug($"Waiting for Dragon-side service to come online...");
                    }
                    else
                    {
                        Console.WriteLine($"Waiting for Dragon-side service to come online...");
                    }

                    chann = this.ChannelFactory.CreateChannel();
                    chann.IsAlive();
                }
                catch 
                {   
                    HasStarted = true;
                    System.Threading.Thread.Sleep(1000);
                    continue;
                }
                break;
            }
            // Getting ride of the Channel failure message 
            chann = this.ChannelFactory.CreateChannel();
            _log.Debug($"Dragon is online.");
        }

        public void OnPipelineStart()
        {
            BlockUntilServiceIsAlive();
        }

        public void Stop()
        {
            // Do nothing
        }
    }

    public class UtteranceBeganEventArgs : EventArgs
    {
        public UtteranceBeganEventArgs() { }
    }

    public class UtteranceEndedEventArgs : EventArgs
    {
        public UtteranceEndedEventArgs() { }
    }
}
