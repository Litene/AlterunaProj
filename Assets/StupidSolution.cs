using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StupidSolution : MonoBehaviour {
    [SerializeField] private GameObject BrowseMenu;
    // Start is called before the first frame update
    public void OnClickedButton() {
        BrowseMenu.SetActive(true);
    }
}
