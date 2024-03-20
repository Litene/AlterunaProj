using System.Collections;
using System.Collections.Generic;
using Alteruna;
using UnityEngine;

public class StartGame : MonoBehaviour
{
    public void StartGameNow() {
        GameObject.Find("Multiplayer").GetComponent<Multiplayer>().LoadScene("SampleScene");
    }
}
