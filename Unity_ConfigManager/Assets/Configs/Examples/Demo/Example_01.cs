using System.Collections.Generic;
using System.Text;
using UnityEngine;

public enum EExampleType
{
    ET_None,
    ET_Test1,
    ET_Test2,
};
public struct FExampleData
{
    public int a;
    public float b;
};

[Config]
public class Example_01
{
    //未赋值的空字段Unity会报Warning
#pragma warning disable
    [Config]
    protected float CFG_Speed;
    [Config]
    protected int CFG_Int;
    [Config]
    protected string CFG_Name;
    [Config]
    protected EExampleType CFG_ExampleType;
    [Config]
    protected FExampleData CFG_ExampleData;
    [Config]
    protected List<string> CFG_List;
    [Config]
    protected List<FExampleData> CFG_StructList;
#pragma warning restore

    public Example_01() 
    {
        //主动调用读取配置
        this.ReadConfig();
    }

    public virtual string Read()
    {
        StringBuilder _res = new StringBuilder();
        _res.Append("CFG_Speed: " + CFG_Speed + "\n");
        _res.Append("CFG_Int: " + CFG_Int + "\n");
        _res.Append("CFG_Name: " + CFG_Name + "\n");
        _res.Append("CFG_ExampleType: " + CFG_ExampleType + "\n");
        _res.Append("CFG_ExampleData: " + CFG_ExampleData.a + "  " + CFG_ExampleData.b + "\n");

        string str = "";
        foreach (var item in CFG_List)
        {
            str += item + ", ";
        }
        _res.Append("CFG_List: " + str + "\n");
        str = "";
        foreach (var item in CFG_StructList)
        {
            str += item.a + " " + item.b + ", ";
        }
        _res.Append("CFG_StructList: " + str + "\n");

        return _res.ToString();
    }
}
