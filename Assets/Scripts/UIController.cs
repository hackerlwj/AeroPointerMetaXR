using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public GameObject closePanelButton;
    public GameObject openPanelButton;
    public GameObject panel;

    // Start is called before the first frame update
    void Start()
    {
        // ��ʼʱֻ��ʾ����尴ť
        //openPanelButton.SetActive(true);
        closePanelButton.SetActive(false);
        panel.SetActive(false);
    }

    // �����ķ���
    public void OpenPanel()
    {
        openPanelButton.SetActive(false);
        closePanelButton.SetActive(true);
        panel.SetActive(true);
    }

    // �ر����ķ���
    public void ClosePanel()
    {
        openPanelButton.SetActive(true);
        closePanelButton.SetActive(false);
        panel.SetActive(false);
    }
}