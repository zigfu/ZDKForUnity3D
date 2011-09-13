using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;

public class GL {
    static internal GLFunctions implementation;
    static GL() 
    {
#if UNITY_STANDALONE_OSX
        implementation = new GLMac();
#elif UNITY_STANDALONE_WINDOWS
        implementation = new GLWin();
#else // assume editor - other platforms will be solved later
        if (Environment.OSVersion.Platform == PlatformID.MacOSX) {
            implementation = new GLMac();
        }
        else {
            implementation = new GLWin();
        }
#endif
    }

    public static void BindTexture(Int32 target, Int32 texture)
    {
        implementation.BindTexture(target, texture);
    }
    public static void TexSubImage2D(Int32 target, Int32 level, Int32 xoffset, Int32 yoffset, Int32 width, Int32 height, Int32 format, Int32 type, IntPtr pixels)
    {
        implementation.TexSubImage2D(target, level, xoffset, yoffset, width, height, format, type, pixels);
    }


    public const Int32 RGB = 0x1907;
    public const Int32 RGBA = 0x1908;
    public const Int32 TEXTURE_2D = 0x0DE1;
    public const Int32 UNSIGNED_BYTE = 0x1401;

    internal interface GLFunctions {
        void BindTexture(Int32 target, Int32 texture);
        void TexSubImage2D(Int32 target, Int32 level, Int32 xoffset, Int32 yoffset, Int32 width, Int32 height, Int32 format, Int32 type, IntPtr pixels);
    }

    internal class GLMac : GLFunctions
    {

        public void TexSubImage2D(Int32 target, Int32 level, Int32 xoffset, Int32 yoffset, Int32 width, Int32 height, Int32 format, Int32 type, IntPtr pixels)
        {
            glTexSubImage2D(target, level, xoffset, yoffset, width, height, format, type, pixels);
        }
        public void BindTexture(Int32 target, Int32 texture) {
            glBindTexture(target, texture);
        }

        internal const string glLibrary = "/System/Library/Frameworks/OpenGL.framework/OpenGL";
        [DllImport(glLibrary, EntryPoint = "glBindTexture", ExactSpelling = true)]
        extern public static void glBindTexture(Int32 target, Int32 texture);

        [DllImport(glLibrary, EntryPoint = "glTexSubImage2D", ExactSpelling = true)]
        extern public static void glTexSubImage2D(Int32 target, Int32 level, Int32 xoffset, Int32 yoffset, Int32 width, Int32 height, Int32 format, Int32 type, IntPtr pixels);
    }
    internal class GLWin : GLFunctions
    {
        public void TexSubImage2D(Int32 target, Int32 level, Int32 xoffset, Int32 yoffset, Int32 width, Int32 height, Int32 format, Int32 type, IntPtr pixels)
        {
            glTexSubImage2D(target, level, xoffset, yoffset, width, height, format, type, pixels);
        }
        public void BindTexture(Int32 target, Int32 texture) {
            glBindTexture(target, texture);
        }
        internal const string glLibrary = "opengl32.dll";
        [DllImport(glLibrary, EntryPoint = "glBindTexture", ExactSpelling = true)]
        extern public static void glBindTexture(Int32 target, Int32 texture);

        [DllImport(glLibrary, EntryPoint = "glTexSubImage2D", ExactSpelling = true)]
        extern public static void glTexSubImage2D(Int32 target, Int32 level, Int32 xoffset, Int32 yoffset, Int32 width, Int32 height, Int32 format, Int32 type, IntPtr pixels);
    }
}
