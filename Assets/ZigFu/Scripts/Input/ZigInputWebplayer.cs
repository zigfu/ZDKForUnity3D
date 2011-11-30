using UnityEngine;
using System.Collections;

#region Javascript
class WebplayerJavascript
{
    // WARNING - change only if you know what you doing (take off every zig)
    public const string getPlugin = @"function getZigPlugin() {
		return document.getElementById('zigPluginObject');
	}
    ";

    public const string addHandler = @"function XBrowserAddHandler(target,eventName,handlerName)
	{
		if ( target.attachEvent )
			target.attachEvent('on' + eventName, handlerName);
		else if ( target.addEventListener )
			target.addEventListener(eventName, handlerName, false);
		else
			target['on' + eventName] = handlerName;
	}
    ";

    public const string pluginLoaded = @"function zigPluginLoaded() {
		XBrowserAddHandler(getZigPlugin(), 'NewFrame', onZigNewFrame);
        }
    ";

    public const string onUpdate = @"function onZigNewFrame()
	{{
		var plug = getZigPlugin();
		var unity = unityObject.getObjectById('unityPlayer');
		unity.SendMessage('{0}', 'NewData', JSON.stringify([plug.users, plug.hands]));
	}}
    ";
    public const string injectPlugin = @"function injectPlugin(callback) {
		zigPluginLoadedCB = callback;" +
        "\nvar html = '<object id=\"zigPluginObject\" type=\"application/x-zig\" width=\"0\" height=\"0\">';\n" +
        "html += '<param name=\"onload\" value=\"zigPluginLoadedCB\" />';\n" +
        @"html += '</object>';
		var newDiv = document.createElement('div');
		newDiv.innerHTML = html;
		document.body.appendChild(newDiv);
	}
    ";

    static public string getInjectJS(string listenerName)
    {
        return WebplayerJavascript.getPlugin
                + WebplayerJavascript.addHandler
                + WebplayerJavascript.pluginLoaded
                + string.Format(WebplayerJavascript.onUpdate, listenerName)
                + WebplayerJavascript.injectPlugin
                + "injectPlugin(zigPluginLoaded);\n";
    }
}

#endregion

public class ZigInputWebplayer : MonoBehaviour {

    public ZigUserTracker UserTracker;
    public void doInjectPlugin() {
        Application.ExternalEval(WebplayerJavascript.getInjectJS(gameObject.name));
    }

	// Use this for initialization
	void Start () {
        if (!Application.isWebPlayer) {
            print("Only web player supported by ZigInputWebplayer!");
            return;
        }
        else {
            doInjectPlugin();
        }

	}

    // needed for some disparity between the output of the JSON decoder and our direct OpenNI layer
    static void Intify(ArrayList list, string property)
    {
        foreach (Hashtable obj in list) {
            obj[property] = int.Parse(obj[property].ToString());
        }
    }
    // called from javascript, giving us new data from the plugin
    void NewData(string param)
    {
        try {
            ArrayList data = (ArrayList)JSON.JsonDecode(param);
            ArrayList users = (ArrayList)data[0];
            ArrayList hands = (ArrayList)data[1];
            Intify(users, "id");
            Intify(hands, "id");
            Intify(hands, "userid");
            UserTracker.UpdateData(users, hands);
        }
        catch (System.Exception ex) {
            Logger.Log(ex.ToString()); // the only we we'll actually see the exception
        }
    }
}
