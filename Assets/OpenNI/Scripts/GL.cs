using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;

public class GL {
#if UNITY_STANDALONE_OSX
    internal const string glLibrary = "/System/Library/Frameworks/OpenGL.framework/OpenGL";
#else
    internal const string glLibrary = "opengl32.dll";
#endif

    public const Int32 RGB = 0x1907;
    public const Int32 RGBA = 0x1908;
    public const Int32 TEXTURE_2D = 0x0DE1;
    public const Int32 UNSIGNED_BYTE = 0x1401;

    [DllImport(glLibrary, EntryPoint = "glBindTexture", ExactSpelling = true)]
    extern public static void BindTexture(Int32 target, Int32 texture);

    [DllImport(glLibrary, EntryPoint = "glTexSubImage2D", ExactSpelling = true)]
    extern public static void TexSubImage2D(Int32 target, Int32 level, Int32 xoffset, Int32 yoffset, Int32 width, Int32 height, Int32 format, Int32 type, IntPtr pixels);
}
