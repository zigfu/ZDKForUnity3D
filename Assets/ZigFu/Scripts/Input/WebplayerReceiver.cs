using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

class NewDataEventArgs : EventArgs
{
	public string JsonData {get; private set; }
	public NewDataEventArgs(string jsonData) { JsonData = jsonData; }
}	
	
class WebplayerReceiver : MonoBehaviour
{
	const string jsToInject = @"
	if (typeof webplayerInitZigPlugin == 'undefined') {
		function addHandler(target, eventName, handlerName) {
			if ( target.attachEvent ) target.attachEvent('on' + eventName, handlerName);
			else if ( target.addEventListener ) target.addEventListener(eventName, handlerName, false);
			else target['on' + eventName] = handlerName;
		}

		WebplayerIds = [];
		function webplayerOnNewData(data) {
			var plugin = GetZigObject();
			for (webplayerId in WebplayerIds) {
				var unity = unityObject.getObjectById(webplayerId);
				if (null == unity) continue;
				unity.SendMessage(WebplayerIds[webplayerId], 'NewData', data);
			}
		}
		
		function webplayerZigPluginLoaded(plugin)
		{
			if (undefined === plugin) {
				var plugin = document.getElementById('zigPluginObject');
			}
			addHandler(plugin, 'NewFrame', webplayerOnNewData);
		}

		CachedZigObject = null;
		function GetZigObject()
		{
			if (typeof CachedZigObject == 'undefined') {
				CachedZigObject = null;
			}

			if (null == CachedZigObject) {
				var objs = document.getElementsByTagName('object');
				for (var i=0; i<objs.length; i++) {
					if (objs[i].users !== undefined) {
						CachedZigObject = objs[i];
						break;
					}
				}
			}
			return CachedZigObject;
		}

		function webplayerInitZigPlugin(playerId, objectName)
		{
			zigObject = GetZigObject();
			if (null == zigObject) {" + 
				"var html = '<object id=\"zigPluginObject\" type=\"application/x-zig\" width=\"0\" height=\"0\"><param name=\"onload\" value=\"webplayerZigPluginLoaded\" /></object>';" + 
				@"var newDiv = document.createElement('div');
				WebplayerIds = [];
				newDiv.innerHTML = html;
				document.body.appendChild(newDiv);
				zigObject = document.getElementById('zigPluginObject');
			} else {
				WebplayerIds = [];
				webplayerZigPluginLoaded(zigObject);
			}
			WebplayerIds[playerId] = objectName;
		}
	}
	";
	
	const string GameObjectName = "WebplayerReceiver";
	
	public static WebplayerReceiver Create() {
		GameObject go = new GameObject(GameObjectName);
		DontDestroyOnLoad(go);
		WebplayerReceiver result = go.AddComponent<WebplayerReceiver>();
		string toInject = jsToInject + string.Format("webplayerInitZigPlugin('unityPlayer', '{0}')", GameObjectName);
		Application.ExternalEval(toInject);
		return result;
	}
	
	public event EventHandler<NewDataEventArgs> NewDataEvent;
	
	// called from javascript, giving us new data from the plugin
	void NewData(string param)
	{
        try {
			if (null != NewDataEvent) {
				NewDataEvent.Invoke(this, new NewDataEventArgs(param));
			}
    	}
    	catch (System.Exception ex) {
			// the logger will show exceptions on screen, useful for 
			// webplayer debugging
            Logger.Log(ex.ToString());
    	}
	}
}