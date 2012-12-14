using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

class mouse_wrapper
{
    [DllImport("user32.dll")]
    public static extern void mouse_event(uint dwFlags, Int32 dx, Int32 dy, uint dwData, IntPtr dwExtraInfo);

    [Flags]
    public enum MouseEventFlags : uint
    {
        LEFTDOWN =      0x00000002,
        LEFTUP =        0x00000004,
        MIDDLEDOWN =    0x00000020,
        MIDDLEUP =      0x00000040,
        MOVE =          0x00000001,
        ABSOLUTE =      0x00008000,
        RIGHTDOWN =     0x00000008,
        RIGHTUP =       0x00000010,
        WHEEL =         0x00000800,
        XDOWN =         0x00000080,
        XUP =           0x00000100
    }

    //dwData to specify XButton during Xup and XDOWN
    public enum MouseEventDataXButtons : uint
    {
        XBUTTON1 = 0x00000001,
        XBUTTON2 = 0x00000002
    }
}
public class mouse_event : MonoBehaviour {


    public Vector3 scale;
    public Vector3 inputBias;
    public Vector3 outputBias;
    public Transform transformToFollow1;
    public Transform transformToFollow2;
    public int x;
    public int y;
    Vector3 target;
    public Vector3 p1_p, p2_p, v1, v2;
    public float mag;
    public bool common = false;
    bool wasCommon = false;
    public float thresh = .1f;

    public bool filter = true;
    public float alpha = .8f;
    public float beta = .5f;
    public Vector3 s0, s1, b0, b1;
    // Use this for initialization
    public Vector3 input, output, filterout;
    
	void Start () {
	
	}
	
	void FixedUpdate () {
        if ((transformToFollow1 != null) && (transformToFollow2 != null))
        {
            Vector3 p1 = transformToFollow1.position;
            Vector3 p2 = transformToFollow2.position;
            float dt = Time.fixedDeltaTime;



             v1 = (p1 - p1_p) / dt;
             v2 = (p2 - p2_p) / dt;
            //mag = Vector3.Magnitude(v1 - v2);
             mag = Mathf.Abs(v1.y - v2.y);
            
            common = mag < thresh;
            if (!common && wasCommon)
            {
                if (p1.x < p2.x)
                {
                    //p1 is on the left
                    if (p1.y < p2.y)
                    //p1 is clicking
                    {
                        Debug.Log("LEFT CLICK");
                    }
                    else
                    {
                        Debug.Log("RIGHT CLICK");
                    }
                }
                else
                {
                    //p1 is on the right
                    if (p1.y < p2.y)
                    //p1 is clicking
                    {
                        Debug.Log("RIGHT CLICK (P1 on right)");
                    }
                    else
                    {
                        Debug.Log("LEFT CLICK (P1 on right)");
                    }
                }
            }
            if (common)
            {
                Vector3 average = (p1 + p2) / 2;
                input = Vector3.Scale(scale, (average + inputBias));
                s1 = alpha * input + (1 - alpha) * s0;
                b1 = beta * s1 + (1 - beta) * b0;
                b0 = b1;
                s0 = s1;
                filterout = 2 * s1 - b1;
                output = filterout + outputBias;
                x = (Int32)(output.x);
                y = (Int32)(output.y);
                mouse_wrapper.MouseEventFlags m = mouse_wrapper.MouseEventFlags.ABSOLUTE | mouse_wrapper.MouseEventFlags.MOVE;
                mouse_wrapper.mouse_event((uint)m, x, y, 0, IntPtr.Zero);
            
            }
            wasCommon = common;
            p1_p = p1;
            p2_p = p2;
        }
	}
    public bool setInputBias = true;
    public bool stopafterfirst = true;
    public void FingersSet()
    {
        Vector3 average = transformToFollow1.position + transformToFollow2.position;
        if (setInputBias)
        {
            inputBias = average;
            if (stopafterfirst)
            {
                setInputBias = false;
            }
        }
    }
}   
