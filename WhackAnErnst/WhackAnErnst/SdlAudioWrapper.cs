using System.Runtime.InteropServices;
using static SDL2.SDL;

namespace WhackAnErnst
{
    public static class SdlAudioWrapper
    {
        [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SDL_OpenAudioDevice")]
        private static extern uint INTERNAL_SDL_OpenAudioDevice(IntPtr device, int iscapture, ref SDL_AudioSpec desired, out SDL_AudioSpec obtained, int allowed_changes);

        static SDL_AudioSpec have, want;
        static uint length, soundDev;
        static IntPtr buffer;

        public static void Init()
        {
            SDL_InitSubSystem(SDL_INIT_AUDIO);
            //Console.WriteLine(SDL_GetNumAudioDevices(0));
            //Console.WriteLine(SDL_GetAudioDeviceName(0, 0));
        }

        public static void DeInit()
        {
            SDL_CloseAudioDevice(soundDev);
        }

        public static void PlaySound(string soundPath)
        {
            SDL_CloseAudioDevice(soundDev);
            SDL_LoadWAV(soundPath, out have, out buffer, out length);
            //soundDev = SDL_OpenAudioDevice(SDL_GetAudioDeviceName(0, 0), 0, ref have, out want, 0);
            soundDev = INTERNAL_SDL_OpenAudioDevice(IntPtr.Zero, 0, ref have, out want, 0);
            //Console.WriteLine(SDL_GetError());
            //Console.WriteLine(soundDev);
            SDL_QueueAudio(soundDev, buffer, length);
            SDL_PauseAudioDevice(soundDev, 0);
        }
    }
}
