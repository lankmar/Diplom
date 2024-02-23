using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BackView : MonoBehaviour
{
    [SerializeField] GameObject _back;

    void Start()
    {
        if (_back == null)
        {
            _back = GameObject.FindAnyObjectByType<BackView>().gameObject;
        }
     
    }

    public void Actived(bool isActive)
    {
        _back.SetActive(isActive);
    }
}
