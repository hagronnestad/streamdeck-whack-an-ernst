namespace WhackAnErnst
{
    public class Program
    {
        public static async Task Main()
        {
            using var game = new Game();
            await game.StartAsync();
        }
    }
}
