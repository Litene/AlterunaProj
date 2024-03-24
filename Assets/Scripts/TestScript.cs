using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Alteruna;
public class TestScript : AttributesSync {

	[ContextMenu("we have a button")]private void Yes() {
		BroadcastRemoteMethod();
	}

	[SynchronizableMethod] public void Sync() {
		Debug.Log("We are here");
	}	
}

