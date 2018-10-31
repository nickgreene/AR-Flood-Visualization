//************************************************************************
//Credit to danielesuppo Jul 27, 2018
//https://forum.unity.com/threads/disable-enable-and-reset-arcore.542552/
//************************************************************************

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class mARSessionManager : MonoBehaviour
{

    [SerializeField] GameObject arCoreSessionPrefab;
    private GameObject newArCoreSessionPrefab;
    private GoogleARCore.ARCoreSession arcoreSession;


    private void Start()
    {
        newArCoreSessionPrefab = Instantiate(arCoreSessionPrefab, Vector3.zero, Quaternion.identity);
        arcoreSession = newArCoreSessionPrefab.GetComponent<GoogleARCore.ARCoreSession>();
        arcoreSession.enabled = true;
    }



    public void Reset()
    {
        StartCoroutine(CreateANewSession());
    }

    IEnumerator CreateANewSession()
    {
        //Destroy
        arcoreSession.enabled = false;
        if (newArCoreSessionPrefab != null)
            Destroy(newArCoreSessionPrefab);

        yield return new WaitForEndOfFrame();

        //Create a new one
        newArCoreSessionPrefab = Instantiate(arCoreSessionPrefab, Vector3.zero, Quaternion.identity);
        arcoreSession = newArCoreSessionPrefab.GetComponent<GoogleARCore.ARCoreSession>();
        arcoreSession.enabled = true;
    }
}