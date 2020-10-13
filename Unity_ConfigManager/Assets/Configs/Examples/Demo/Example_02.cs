using System.Collections.Generic;
using UnityEngine;

public class Example_02 : Example_01
{
#pragma warning disable
    [Config]
    private float CFG_NewConfig;
#pragma warning restore
    public Example_02() : base() { }

    public override string Read()
    {
        return base.Read() + "CFG_NewConfig: " + CFG_NewConfig + "\n";
    }
}