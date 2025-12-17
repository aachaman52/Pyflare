/*
 * PyFlare Engine - Platform Bindings
 * C# to native C interop layer
 */

using System;
using System.Runtime.InteropServices;

namespace PyFlare.Engine.Platform
{
    /// <summary>
    /// P/Invoke bindings to native platform layer
    /// </summary>
    public static class NativePlatform
    {
        private const string NATIVE_LIB = "native";

        // ====================================================================
        // WINDOW MANAGEMENT
        // ====================================================================

        [DllImport(NATIVE_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern int native_init_window(int width, int height, 
            [MarshalAs(UnmanagedType.LPStr)] string title, 
            int fullscreen, int vsync);

        [DllImport(NATIVE_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void native_destroy_window();

        [DllImport(NATIVE_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void native_update();

        [DllImport(NATIVE_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void native_present();

        [DllImport(NATIVE_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern int native_is_window_open();

        [DllImport(NATIVE_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void native_get_window_size(out int width, out int height);

        [DllImport(NATIVE_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern double native_get_delta_time();

        // ====================================================================
        // OPENGL FUNCTIONS
        // ====================================================================

        [DllImport(NATIVE_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void native_clear(float r, float g, float b, float a);

        [DllImport(NATIVE_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint native_create_shader(
            [MarshalAs(UnmanagedType.LPStr)] string vertexSrc,
            [MarshalAs(UnmanagedType.LPStr)] string fragmentSrc);

        [DllImport(NATIVE_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void native_use_shader(uint shaderId);

        [DllImport(NATIVE_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void native_delete_shader(uint shaderId);

        // ====================================================================
        // UTILITY FUNCTIONS
        // ====================================================================

        [DllImport(NATIVE_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern long native_get_memory_usage();

        [DllImport(NATIVE_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern double native_get_time();
    }

    /// <summary>
    /// High-level C# wrapper for platform functionality
    /// </summary>
    public class Platform
    {
        private static bool initialized = false;
        private static int windowWidth;
        private static int windowHeight;

        public static bool Initialize(int width, int height, string title, 
            bool fullscreen = false, bool vsync = true)
        {
            if (initialized)
            {
                Console.WriteLine("Platform already initialized");
                return true;
            }

            int result = NativePlatform.native_init_window(
                width, height, title,
                fullscreen ? 1 : 0, vsync ? 1 : 0
            );

            if (result == 1)
            {
                initialized = true;
                windowWidth = width;
                windowHeight = height;
                Console.WriteLine($"Platform initialized: {width}x{height}");
                return true;
            }

            Console.WriteLine("Failed to initialize platform");
            return false;
        }

        public static void Shutdown()
        {
            if (initialized)
            {
                NativePlatform.native_destroy_window();
                initialized = false;
                Console.WriteLine("Platform shutdown complete");
            }
        }

        public static void Update()
        {
            if (!initialized) return;
            NativePlatform.native_update();
        }

        public static void Present()
        {
            if (!initialized) return;
            NativePlatform.native_present();
        }

        public static bool IsWindowOpen()
        {
            if (!initialized) return false;
            return NativePlatform.native_is_window_open() == 1;
        }

        public static void GetWindowSize(out int width, out int height)
        {
            if (initialized)
            {
                NativePlatform.native_get_window_size(out width, out height);
                windowWidth = width;
                windowHeight = height;
            }
            else
            {
                width = 0;
                height = 0;
            }
        }

        public static int GetWindowWidth()
        {
            GetWindowSize(out int w, out int h);
            return w;
        }

        public static int GetWindowHeight()
        {
            GetWindowSize(out int w, out int h);
            return h;
        }

        public static double GetDeltaTime()
        {
            if (!initialized) return 0.0;
            return NativePlatform.native_get_delta_time();
        }

        public static void Clear(float r, float g, float b, float a = 1.0f)
        {
            if (!initialized) return;
            NativePlatform.native_clear(r, g, b, a);
        }

        public static long GetMemoryUsage()
        {
            return NativePlatform.native_get_memory_usage();
        }

        public static double GetTime()
        {
            return NativePlatform.native_get_time();
        }

        public static bool IsInitialized() => initialized;
    }

    /// <summary>
    /// Shader management wrapper
    /// </summary>
    public class Shader
    {
        private uint shaderId;
        private bool isValid;

        public Shader(string vertexSource, string fragmentSource)
        {
            shaderId = NativePlatform.native_create_shader(vertexSource, fragmentSource);
            isValid = shaderId != 0;

            if (!isValid)
            {
                Console.WriteLine("Failed to create shader");
            }
        }

        public void Use()
        {
            if (isValid)
            {
                NativePlatform.native_use_shader(shaderId);
            }
        }

        public void Dispose()
        {
            if (isValid)
            {
                NativePlatform.native_delete_shader(shaderId);
                isValid = false;
            }
        }

        public bool IsValid() => isValid;
        public uint GetId() => shaderId;
    }

    /// <summary>
    /// Performance monitoring utilities
    /// </summary>
    public class Performance
    {
        private static double lastFrameTime = 0;
        private static double[] frameTimes = new double[60];
        private static int frameIndex = 0;
        private static int frameCount = 0;

        public static void Update()
        {
            double currentTime = Platform.GetTime();
            double deltaTime = currentTime - lastFrameTime;
            lastFrameTime = currentTime;

            frameTimes[frameIndex] = deltaTime;
            frameIndex = (frameIndex + 1) % frameTimes.Length;
            frameCount++;
        }

        public static double GetAverageFPS()
        {
            if (frameCount == 0) return 0;

            double sum = 0;
            int count = Math.Min(frameCount, frameTimes.Length);
            for (int i = 0; i < count; i++)
            {
                sum += frameTimes[i];
            }

            double avgDelta = sum / count;
            return avgDelta > 0 ? 1.0 / avgDelta : 0;
        }

        public static double GetCurrentFPS()
        {
            double deltaTime = Platform.GetDeltaTime();
            return deltaTime > 0 ? 1.0 / deltaTime : 0;
        }

        public static long GetMemoryUsageMB()
        {
            return Platform.GetMemoryUsage() / (1024 * 1024);
        }

        public static void PrintStats()
        {
            Console.WriteLine($"FPS: {GetCurrentFPS():F1} (Avg: {GetAverageFPS():F1})");
            Console.WriteLine($"Memory: {GetMemoryUsageMB()} MB");
            Console.WriteLine($"Delta Time: {Platform.GetDeltaTime() * 1000:F2} ms");
        }
    }
}
