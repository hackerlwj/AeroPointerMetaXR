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
        // 开始时只显示打开面板按钮
        //openPanelButton.SetActive(true);
        closePanelButton.SetActive(false);
        panel.SetActive(false);
    }

    // 打开面板的方法
    public void OpenPanel()
    {
        openPanelButton.SetActive(false);
        closePanelButton.SetActive(true);
        panel.SetActive(true);
    }

    // 关闭面板的方法
    public void ClosePanel()
    {
        openPanelButton.SetActive(true);
        closePanelButton.SetActive(false);
        panel.SetActive(false);
    }
}