namespace WhackAnErnst
{
    public static class Sounds
    {
        private static Random _r = new();

        public const string Dat = "sounds/dat.wav";
        public const string Bop = "sounds/bop.wav";
        public const string Dunk = "sounds/dunk.wav";
        public const string Au01 = "sounds/au01.wav";
        public const string Au02 = "sounds/au02.wav";
        public const string Au03 = "sounds/au03.wav";
        public const string Au04 = "sounds/au04.wav";
        public const string Au05 = "sounds/au05.wav";
        public const string Au06 = "sounds/au06.wav";
        public const string Ready = "sounds/ready.wav";
        public const string Set = "sounds/set.wav";
        public const string Go = "sounds/go.wav";
        public const string GameOver = "sounds/gameover.wav";

        public static string RandomAu()
        {
            return $"sounds/au0{_r.Next(1, 7)}.wav";
        }
    }
}
