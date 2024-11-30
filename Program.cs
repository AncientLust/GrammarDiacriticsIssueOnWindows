using NWaves.Audio;
using NWaves.Operations;
using NWaves.Signals;
using Vosk;

namespace GrammarDiacriticsIssueOnWindows;

class Program
{
    private static readonly char[] _delimeters = [' ', ',', '.', '!', '?'];
    
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.InputEncoding = System.Text.Encoding.UTF8;
        
        var targetSampleRate = 16000;
        var targetText = "Czy mogę ci pomóc?";
        
        var model = new Model("../../../Models/Polish");
        var words = ExtractWords(targetText);
        var grammar = "[\"" + string.Join("\",\"", words) + "\"]";
        using var recognizer = new VoskRecognizer(model, targetSampleRate, grammar);
        Vosk.Vosk.SetLogLevel(0);
        recognizer.SetMaxAlternatives(0);
        recognizer.SetWords(false);
        
        var wavFilePath = "../../../czy_moge_ci_pomoc.Linear16";
        using var fileStream = new FileStream(wavFilePath, FileMode.Open, FileAccess.Read);
        var waveFile = new WaveFile(fileStream);
        
        var signal = waveFile.Signals[0];

        if (signal.SamplingRate != targetSampleRate)
        {
            signal = Operation.Resample(signal, targetSampleRate);
        }

        var pcmBytes = ConvertSignalToPcm(signal);

        var recognizedText = recognizer.AcceptWaveform(pcmBytes, pcmBytes.Length)
            ? recognizer.Result()
            : recognizer.PartialResult();

        Console.WriteLine("Target text: " + targetText);
        Console.WriteLine("Recognized text: " + recognizedText);
        
        Console.WriteLine("Done.");
    }
    
    private static byte[] ConvertSignalToPcm(DiscreteSignal signal)
    {
        var samples = signal.Samples;
        var pcmBytes = new byte[samples.Length * 2];
        
        for (var i = 0; i < samples.Length; i++)
        {
            var sample = (short)(samples[i] * short.MaxValue);
            pcmBytes[2 * i] = (byte)(sample & 0xFF);
            pcmBytes[2 * i + 1] = (byte)((sample >> 8) & 0xFF);
        }
        
        return pcmBytes;
    }
    
    private static List<string> ExtractWords(string text)
    {
        return text
            .ToLower()
            .Split(_delimeters,' ', StringSplitOptions.RemoveEmptyEntries)
            .ToList();
    }
}