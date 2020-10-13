using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Example_UI : MonoBehaviour
{
    private Example_01 example_01;
    private Example_02 example_02;
    private Example_03 example_03;

    public Text text;

    // Start is called before the first frame update
    void Start()
    {
        example_01 = new Example_01();
        example_02 = new Example_02();
        example_03 = new Example_03();
        text.text = example_01.Read();
    }

    public void OnBtn1()
    {
        text.text = example_01.Read();
    }
    public void OnBtn2()
    {
        text.text = example_02.Read();
    }
    public void OnBtn3()
    {
        text.text = example_03.Read();

    }
}
