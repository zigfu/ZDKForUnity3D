using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WebplayerLogger : MonoBehaviour {
	// singleton
    static WebplayerLogger instance;
    public static WebplayerLogger Instance
    {
        get
        {
            if (null == instance) {
                instance = FindObjectOfType(typeof(WebplayerLogger)) as WebplayerLogger;
                if (null == instance) {
                    GameObject container = new GameObject();
                    DontDestroyOnLoad(container);
                    container.name = "LoggerContainer";
                    instance = container.AddComponent<WebplayerLogger>();
                }
                DontDestroyOnLoad(instance);
            }
            return instance;
        }
    }

	public static void Log(string s)
	{
		Instance.log(s);
	}
	
	public int maxItems = 10;
	
	List<string> logEntries = new List<string>();
	
	public void log(string str)
	{
		logEntries.Add(str);
		if (logEntries.Count > maxItems) {
			logEntries.RemoveRange(0, logEntries.Count - maxItems);
		}
	}

    public bool showLogger = false;
	void OnGUI()
	{
        if (showLogger)
        {
            GUILayout.Box("Log:");
            foreach (string s in logEntries)
            {
                GUILayout.Label(s);
            }
        }
	}
}
