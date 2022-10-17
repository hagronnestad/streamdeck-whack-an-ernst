using System.Diagnostics;

namespace WhackAnErnst
{
    public class Program
    {
        public static async Task Main()
        {
            Console.WriteLine("WhackAnErnst Program.Main...");

            // Check if Stream Deck application is running and ask to close it
            var streamDeckFilePath = "";
            var pStreamDeck = GetRunningStreamDeckApplication();
            if (pStreamDeck != null)
            {
                streamDeckFilePath = pStreamDeck.MainModule?.FileName ?? "";
                if (!await AskToCloseStreamDeckApplication(pStreamDeck))
                {
                    return;
                }
            }

            // Start the game
            Console.WriteLine("Starting game...");
            using var game = new Game();
            _ = Task.Run(game.StartAsync);
            Console.WriteLine("Press any key to stop the game...\n");
            Console.ReadKey(true);

            // Ask to restart Stream Deck application if it was closed earlier
            if (!string.IsNullOrWhiteSpace(streamDeckFilePath))
            {
                AskToRestartStreamDeckApplication(streamDeckFilePath);
            }
        }

        private static Process? GetRunningStreamDeckApplication()
        {
            return Process.GetProcesses().FirstOrDefault(x => x.ProcessName.ToLower() == "StreamDeck".ToLower());
        }

        private static async Task<bool> AskToCloseStreamDeckApplication(Process pStreamDeck)
        {
            Console.WriteLine("The official StreamDeck application is running. To play Whack-An-Ernst you have to temporarily close the StreamDeck application.");
            Console.WriteLine("If you continue the application will be closed.");
            Console.Write("Continue? (Y/N): ");
            var answer = Console.ReadKey();
            Console.WriteLine();

            if (answer.KeyChar == 'Y' || answer.KeyChar == 'y')
            {
                Console.WriteLine("Closing the official StreamDeck application...");

                pStreamDeck.Kill();
                await pStreamDeck.WaitForExitAsync(new CancellationTokenSource(3000).Token);

                if (pStreamDeck.HasExited)
                {
                    Console.WriteLine("The application was successfully closed.");
                }
                else
                {
                    Console.WriteLine("Timed out trying to close the application!");
                    return false;
                }
            }
            else
            {
                Console.WriteLine("Manually close the official StreamDeck application and try again.");
                return false;
            }

            return true;
        }

        private static void AskToRestartStreamDeckApplication(string streamDeckFilePath)
        {
            Console.Write("Do you want to restart the official Stream Deck application? (Y/N): ");
            var answer = Console.ReadKey();
            Console.WriteLine();

            if (answer.KeyChar == 'Y' || answer.KeyChar == 'y')
            {
                Console.Write("Starting the official StreamDeck application...");

                Process.Start(streamDeckFilePath);
            }
        }
    }
}
