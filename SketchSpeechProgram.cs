namespace NU.Kiosk
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Speech;
    using Microsoft.Psi.Visualization.Client;
    using NU.Kqml;
    using log4net;
    using log4net.Config;
    using System.Reflection;
    using Microsoft.Psi.Components;

    public static class SketchSpeechProgram
    {
        private const string AppName = "SketchSpeech";

        static void Main(string[] args)
        {
            string facilitatorIP = "127.0.0.1";
            int facilitatorPort = 9000;
            int localPort = 6000;
            
            bool exit = false;
            while (!exit)
            {
                Console.WriteLine("Press 1 to run, q to quit...");
                ConsoleKey key = Console.ReadKey().Key;
                Console.WriteLine();
                switch (key)
                {
                    case ConsoleKey.D1:
                        RunPipeline(facilitatorIP, facilitatorPort, localPort);
                        break;
                    case ConsoleKey.Q:
                        exit = true;
                        break;
                    default:
                        exit = false;
                        break;
                }
            }
        }

        private static void RunPipeline(string facilitatorIP, int facilitatorPort, int localPort)
        {
            using (Pipeline pipeline = Pipeline.Create())
            {
                Speech.DragonSpeechRecognitionResultReceiver DragonProducer = null;
                IProducer<IStreamingSpeechRecognitionResult> finalResults;
                SocketStringConsumer kqml = null;
                SketchingInputTextPreProcessor preproc = new SketchingInputTextPreProcessor(pipeline);
                var responder = new Speech.SketchingResponder(pipeline);

                Console.WriteLine("Using Dragon Producer. Dragon acquires audio on its own.");
                DragonProducer = new Speech.DragonSpeechRecognitionResultReceiver(pipeline);
                finalResults = DragonProducer.Out.Where(result => result.IsFinal);
                responder.StateChanged.Select(state => state == "listening").PipeTo(DragonProducer.isAcceptingData);

                var recognitionResult = finalResults.Select(r =>
                {
                    var ssrResult = r as IStreamingSpeechRecognitionResult;
                    Console.WriteLine($"{ssrResult.Text} (confidence: {ssrResult.Confidence})");
                    return ssrResult;
                });

                // Send final speech results to be preprocessed, this will filter based on confidence and
                //  will remove some *passive (*unsure about word choice) utterances such as um and ah
                recognitionResult.PipeTo(preproc.In);

                // Send processed user input (utterance) to responder so the string can be sent to kqml
                preproc.PipeTo(responder.UserInput);
                
                Console.WriteLine("Setting up connection to Companion");
                int facilitatorPort_num = Convert.ToInt32(facilitatorPort);
                int localPort_num = Convert.ToInt32(localPort);
                Console.WriteLine("Your Companion IP address is: " + facilitatorIP);
                Console.WriteLine("Your Companion port is: " + facilitatorPort);
                Console.WriteLine("Your local port is: " + localPort);
                kqml = new SocketStringConsumer(pipeline, facilitatorIP, facilitatorPort_num, localPort_num);
                // SketchingResponder creates kqml requests when it recieves user input (takes string from utterance),
                //  piping that to kqml recieve will connect to companions and ask for our task
                responder.KQMLRequest.PipeTo(kqml);

                // Register an event handler to catch pipeline errors
                pipeline.PipelineCompletionEvent += PipelineCompletionEvent;

                // Run the pipeline
                pipeline.RunAsync();
                Console.ReadKey();
                Console.WriteLine($"Press any key to exit...");
            }
        }

        private static void PipelineCompletionEvent(object sender, PipelineCompletionEventArgs e) {
            Console.WriteLine($"Pipeline execution completed with {e.Errors.Count} errors");
            if (e.Errors.Count > 0) {
                foreach (var error in e.Errors) {
                    Console.WriteLine($"error: {error}");
                }
            }
        }
    }
}
