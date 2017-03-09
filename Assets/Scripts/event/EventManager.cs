using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventManager : MonoBehaviour {

	#region C# properties

	public static EventManager Instance {
		get { return _instance; }
		set { }
	}

	#endregion

	#region variables

	private static EventManager _instance = null;
	private Dictionary<EVENT_TYPE, List<IListener>> _listeners = new Dictionary<EVENT_TYPE, List<IListener>> ();

	#endregion

	#region methods

	void Awake () {
		if (_instance == null) {
			_instance = this;
			DontDestroyOnLoad (gameObject);
		} else {
			DestroyImmediate (this);
		}
	}

	public void AddListener (EVENT_TYPE eventType, IListener listener) {
		List<IListener> listenList = null;
		if (_listeners.TryGetValue (eventType, out listenList)) {
			listenList.Add (listener);
			return;
		}

		listenList = new List<IListener> ();
		listenList.Add (listener);
		_listeners.Add (eventType, listenList);
	}

	public void PostNotification (EVENT_TYPE eventType, Component sender, System.Object param = null) {
		List<IListener> listenList = null;
		if(!_listeners.TryGetValue (eventType, out listenList)) {
			return;
		}
		for (int i = 0; i < listenList.Count; i++) {
			if (!listenList[i].Equals (null)) {
				listenList [i].OnEvent (eventType, sender, param);
			}
		}
	}

	public void RemoveEvent (EVENT_TYPE eventType) {
		_listeners.Remove (eventType);
	}

	public void RemoveRedundancies () {
		Dictionary<EVENT_TYPE, List<IListener>> tmpListeners = new Dictionary<EVENT_TYPE, List<IListener>> ();
		foreach (KeyValuePair<EVENT_TYPE, List<IListener>> item in _listeners) {
			for (int i = item.Value.Count - 1; i >= 0; i--) {
				if (item.Value [i].Equals (null)) {
					item.Value.RemoveAt (i);
				}
			}
			if (item.Value.Count > 0) {
				tmpListeners.Add (item.Key, item.Value);
			}
		}
		_listeners = tmpListeners;
	}

	void OnLevelWasLoaded () {
		RemoveRedundancies ();
	}

	#endregion
}
