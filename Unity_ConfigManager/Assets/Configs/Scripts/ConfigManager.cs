using System;
using UnityEngine;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using System.Text;

public static class ConfigHelper
{
    /// <summary>
    /// 所有对象读取配置方法
    /// </summary>
    public static void ReadConfig(this object inObj)
    {
        if (inObj != null)
        {
            ConfigManager.Instance.CopyConfig(inObj);
        }
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Struct, AllowMultiple = false)]
public class Config : Attribute
{
    public string ConfigFileName = "Default";
    public Config() { }
    public Config(string inFileName)
    {
        ConfigFileName = inFileName;
    }
}

struct FConfigData
{
    public Type Type;
    public List<FieldInfo> FieldInfoList;
    public List<PropertyInfo> PropertyInfoList;
};

public class ConfigManager : Singleton<ConfigManager>
{
    //注释分隔符
    private const char NOTESIGN = ';';
    //类名修饰
    private const string CLASSSIGNBEGIN = "[";
    private const string CLASSSIGNEND = "]";
    //等于号
    private const char EQUALSIGN = '=';
    //默认配置文件格式
    private const string FORMATSIGN = ".ini";
    //构造体结构
    private const char STRUCTSIGNBEGIN = '(';
    private const char STRUCTSIGNEND = ')';
    private const char STRUCTSPLITSIGN = ',';
    //列表配置
    private const char LISTSPLITSIGN = '|';
    //执行语句
    private const string EXECUTESIGN = "->";
    //配置文件路径
    public static string DirectionPath = UnityEngine.Application.streamingAssetsPath + "//Configs";
    //存取所有类的配置信息Map，初始化结束后清除
    private Dictionary<Type, Dictionary<string, string>> m_ConfigDict = new Dictionary<Type, Dictionary<string, string>>();
    //当前类型存在的配置信息列表
    private Dictionary<Type, List<FConfigData>> m_ConfigDataMap = new Dictionary<Type, List<FConfigData>>();
    //当前数据具体存在位置
    private Dictionary<Type, Dictionary<string, object>> m_FinnalDataMap = new Dictionary<Type, Dictionary<string, object>>();
    //缓存每个Type中配置的List列表，初始化结束后清除
    private Dictionary<Type, Dictionary<string, object>> m_ListDict = new Dictionary<Type, Dictionary<string, object>>();

    /// <summary>
    /// 初始化
    /// </summary>
    public override void Initialize()
    {
        if (!Directory.Exists(DirectionPath))
        {
            Debug.Log("不存在该配置路径文件，Path : " + DirectionPath);
            return;
        }

        DirectoryInfo tDirectoryInfo = new DirectoryInfo(DirectionPath);

        Debug.Log("Config 加载！" + (tDirectoryInfo is object));

        if (tDirectoryInfo != null)
        {
            FileInfo[] files = tDirectoryInfo.GetFiles();
            for (int i = 0; i < files.Length; i++)
            {
                if (files[i].FullName.EndsWith(FORMATSIGN))
                {
                    Debug.Log("Config 加载！" + files[i].FullName);
                    ReadFileConfig(files[i]);
                    Debug.Log(File.ReadAllText(files[i].FullName, Encoding.GetEncoding("UTF-8")).ToString());
                }
            }
        }
        InitCacheConfig();
    }

    public void InitCacheConfig()
    {
        foreach (Type type in m_ConfigDict.Keys)
        {
            Attribute attribute = type.GetCustomAttribute(typeof(Config));
            if (attribute != null)
            {
                List<FConfigData> configDatas = GetObjectConfigList(type);
                for (int i = configDatas.Count - 1; i >= 0; i--)
                {
                    List<FieldInfo> fieldInfos = configDatas[i].FieldInfoList;
                    foreach (FieldInfo info in fieldInfos)
                    {
                        if (info.GetCustomAttribute(typeof(Config)) != null)
                        {
                            SetObjectProperty(configDatas[i].Type, info, type);
                        }
                    }
                }
            }
        }
        m_ListDict.Clear();
        m_ConfigDict.Clear();
    }

    public void CopyConfig(object inObj)
    {
        Type type = inObj.GetType();

        while (IsVaildType(type))
        {
            if (m_FinnalDataMap.TryGetValue(type, out Dictionary<string, object> tDict) && tDict != null)
            {
                List<FConfigData> configList = GetObjectConfigList(type);
                for (int i = configList.Count - 1; i >= 0; i--)
                {
                    foreach (FieldInfo fieldInfo in configList[i].FieldInfoList)
                    {
                        Config tConfig = fieldInfo.GetCustomAttribute<Config>();
                        if (tConfig != null && tDict.ContainsKey(fieldInfo.Name))
                        {
                            if (fieldInfo.FieldType.ToString().Contains("System.Collections.Generic.List"))
                            {
                                object objList = tDict[fieldInfo.Name];
                                object selfList = Activator.CreateInstance(fieldInfo.FieldType);
                                fieldInfo.SetValue(inObj, selfList);

                                MethodInfo methodInfo = fieldInfo.FieldType.GetMethod("AddRange", BindingFlags.Instance | BindingFlags.Public);
                                if (methodInfo != null)
                                    methodInfo.Invoke(selfList, new object[] { objList });
                            }
                            else
                            {
                                fieldInfo.SetValue(inObj, tDict[fieldInfo.Name]);
                            }
                        }
                    }
                }
                break;
            }
            else
            {
                type = type.BaseType;
            }
        }
    }

    /// <summary>
    /// 读取文件中的所有配置
    /// </summary>
    private void ReadFileConfig(FileInfo file)
    {
        if (string.IsNullOrEmpty(file.FullName))
            return;

        if (File.Exists(file.FullName))
        {
            FileStream tFS = File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
            StreamReader tReader = new StreamReader(tFS);
            Dictionary<string, string> tConfigDict = null;
            while (!tReader.EndOfStream)
            {
                string tStr = Regex.Replace(tReader.ReadLine(), @"\s", "");

                if (tStr.StartsWith(NOTESIGN.ToString()))
                    continue;

                int signIndex = tStr.IndexOf(NOTESIGN);
                if (signIndex >= 0)
                    tStr = tStr.Substring(0, signIndex);

                //当前行 为类名
                if (tStr.StartsWith(CLASSSIGNBEGIN) && tStr.EndsWith(CLASSSIGNEND))
                {
                    Type tType = Type.GetType(tStr.Substring(1, tStr.Length - 2), false, true);

                    if (tType != null)
                    {
                        Config config = (Config)tType.GetCustomAttribute(typeof(Config));
                        if (config != null && (config.ConfigFileName + FORMATSIGN).Equals(file.Name))
                        {
                            tConfigDict = new Dictionary<string, string>();
                            m_ConfigDict.Add(tType, tConfigDict);
                        }
                        else
                            tConfigDict = null;
                    }
                }
                else
                {
                    if (tConfigDict != null)
                    {
                        if (CheckIsStruct(tStr))
                        {
                            int tFirstEqualSignIndex = tStr.IndexOf(EQUALSIGN);
                            string tKey = tStr.Substring(0, tFirstEqualSignIndex);
                            string tValue = tStr.Substring(tFirstEqualSignIndex + 1, tStr.Length - tFirstEqualSignIndex - 1);

                            if (!string.IsNullOrEmpty(tValue))
                            {
                                if (tConfigDict.ContainsKey(tKey.ToUpper()))
                                    tConfigDict[tKey.ToUpper()] += LISTSPLITSIGN + tValue;
                                else
                                    tConfigDict.Add(tKey.ToUpper(), tValue);
                            }
                        }
                        else
                        {
                            if (tStr.Contains(EXECUTESIGN))
                            {
                                string[] tStrArray = Regex.Split(tStr, EXECUTESIGN, RegexOptions.IgnoreCase);

                                if (tConfigDict.ContainsKey(tStrArray[0].ToUpper()))
                                    tConfigDict[tStrArray[0].ToUpper()] += LISTSPLITSIGN + EXECUTESIGN + tStrArray[1];
                                else
                                    tConfigDict.Add(tStrArray[0].ToUpper(), EXECUTESIGN + tStrArray[1]);
                            }
                            else
                            {
                                string[] tStrArray = tStr.Split(EQUALSIGN);
                                if (tStrArray.Length == 2)
                                {
                                    //Key:0, Value:1
                                    if (tConfigDict.ContainsKey(tStrArray[0].ToUpper()))
                                        tConfigDict[tStrArray[0].ToUpper()] += LISTSPLITSIGN + tStrArray[1];
                                    else
                                        tConfigDict.Add(tStrArray[0].ToUpper(), tStrArray[1]);
                                }
                            }
                        }
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("不存在该文件，Path:" + file.FullName);
        }
    }

    /// <summary>
    /// 读取Config缓存值
    /// </summary>
    /// <param name="inType">类型</param>
    /// <param name="inFieldName">字段名</param>
    /// <returns></returns>
    public object ReadValueByType(Type inType, string inFieldName)
    {
        if (!string.IsNullOrEmpty(inFieldName)) return null;

        if (m_FinnalDataMap.TryGetValue(inType, out Dictionary<string, object> tDict) && tDict != null)
        {
            if (tDict.TryGetValue(inFieldName, out object data))
                return data;
        }
        return null;
    }

    /// <summary>
    /// 是否为有效类型
    /// </summary>
    bool IsVaildType(Type inType)
    {
        return !(inType == typeof(MonoBehaviour) || inType == typeof(System.Object) || inType == typeof(UnityEngine.Object) || inType == typeof(System.ValueType));
    }

    /// <summary>
    /// 获取当前类型需要配置的数据列表
    /// </summary>
    List<FConfigData> GetObjectConfigList(Type inType)
    {
        if (m_ConfigDataMap.TryGetValue(inType, out List<FConfigData> res) && res != null)
            return res;

        List<FConfigData> list = new List<FConfigData>();
        List<FieldInfo> tFieldInfoList;
        List<PropertyInfo> tPropertyInfoList;

        Type tType = inType;

        while (tType != null && tType != typeof(MonoBehaviour) && tType != typeof(System.Object) && tType != typeof(UnityEngine.Object))
        {
            Config config = (Config)tType.GetCustomAttribute(typeof(Config));
            if (config != null && !string.IsNullOrEmpty(config.ConfigFileName))
            {
                FieldInfo[] tFieldInfos = tType.GetFields(BindingFlags.Instance | BindingFlags.DeclaredOnly
                | BindingFlags.NonPublic | BindingFlags.Public);
                PropertyInfo[] tProperties = tType.GetProperties(BindingFlags.Instance | BindingFlags.DeclaredOnly
                    | BindingFlags.NonPublic | BindingFlags.Public);

                tFieldInfoList = new List<FieldInfo>();
                tPropertyInfoList = new List<PropertyInfo>();
                foreach (FieldInfo info in tFieldInfos)
                {
                    if (info.GetCustomAttribute(typeof(Config)) != null)
                    {
                        tFieldInfoList.Add(info);
                    }
                }
                foreach (PropertyInfo info in tProperties)
                {
                    if (info.GetCustomAttribute(typeof(Config)) != null)
                    {
                        tPropertyInfoList.Add(info);
                    }
                }
                list.Add(new FConfigData { Type = tType.UnderlyingSystemType, FieldInfoList = tFieldInfoList, PropertyInfoList = tPropertyInfoList });
            }
            tType = tType.BaseType;
        }

        tFieldInfoList = new List<FieldInfo>();
        tPropertyInfoList = new List<PropertyInfo>();
        for (int i = list.Count - 1; i >= 0; i--)
        {
            FConfigData configData = list[i];
            tFieldInfoList.AddRange(configData.FieldInfoList);
            tPropertyInfoList.AddRange(configData.PropertyInfoList);
            configData.FieldInfoList = tFieldInfoList;
            configData.PropertyInfoList = tPropertyInfoList;
            list[i] = configData;
        }
        m_ConfigDataMap.Add(inType, list);
        return list;
    }

    /// <summary>
    /// 获取List列表存放的数据类型
    /// </summary>
    private Type GetListType(string inTypeStr)
    {
        int startIndex = inTypeStr.LastIndexOf('[') + 1;
        string type = inTypeStr.Substring(startIndex, inTypeStr.LastIndexOf(']') - startIndex);
        return Type.GetType(type, true, true);
    }

    /// <summary>
    /// 设置构造体内字段值
    /// </summary>
    /// <param name="inObj">一个类里的构造体对象</param>
    /// <param name="inValue">值:格式(X=1,Y=2,Z=3)</param>
    private void SetStructProperty(ref object inObj, string inValue)
    {
        string[] tValueArray = inValue.Split(STRUCTSPLITSIGN);

        FieldInfo[] tFieldInfos = inObj.GetType().GetFields(BindingFlags.Instance | BindingFlags.DeclaredOnly
            | BindingFlags.NonPublic | BindingFlags.Public);

        for (int i = 0; i < tValueArray.Length; i++)
        {
            string[] tStrArray = tValueArray[i].Split(EQUALSIGN);

            if (tStrArray.Length == 2)
            {
                for (int tFieldIndex = 0; tFieldIndex < tFieldInfos.Length; tFieldIndex++)
                {
                    if (tFieldInfos[tFieldIndex].Name.ToUpper().Equals(tStrArray[0].ToUpper()))
                    {
                        object value = tFieldInfos[tFieldIndex].FieldType.IsEnum 
                            ? System.Enum.Parse(tFieldInfos[tFieldIndex].FieldType, tStrArray[1]) 
                            : Convert.ChangeType(tStrArray[1], tFieldInfos[tFieldIndex].FieldType);
                        tFieldInfos[tFieldIndex].SetValue(inObj, value);
                    }
                }
            }
        }
    }

    void SetObjectProperty(Type inType, FieldInfo inInfo, Type inTargetType)
    {
        if (m_ConfigDict.TryGetValue(inType, out Dictionary<string, string> tDict) && tDict != null)
        {
            if (tDict.TryGetValue(inInfo.Name.ToUpper(), out string tStr) && !string.IsNullOrEmpty(tStr))
            {
                try
                {
                    if (CheckIsList(tStr, inInfo.FieldType)) 
                    {
                        string[] tStrArray = tStr.Split(LISTSPLITSIGN);
                        Type type = GetListType(inInfo.FieldType.ToString());
                        object entityList = GetListObject(inTargetType, inInfo);

                        //相当于List<T>调用Add方法
                        MethodInfo methodInfo = inInfo.FieldType.GetMethod("Add", BindingFlags.Instance | BindingFlags.Public);

                        for (int i = 0; i < tStrArray.Length; i++)
                        {
                            if (CheckIsExecute(tStrArray[i]))
                            {
                                int len = tStrArray[i].Length;
                                MethodInfo tempMethod = inInfo.FieldType.GetMethod(tStrArray[i].Substring(2, len - 4), BindingFlags.Instance | BindingFlags.Public);
                                if(tempMethod != null)
                                    tempMethod.Invoke(entityList,new object[] { });
                            }
                            else if (CheckIsStruct(tStrArray[i]))
                            {
                                object structObj = Activator.CreateInstance(type);
                                SetStructProperty(ref structObj, tStrArray[i].Substring(1, tStrArray[i].Length - 2));
                                methodInfo.Invoke(entityList, new object[] {
                                    Convert.ChangeType(structObj, type)
                                });
                            }
                            else
                            {
                                methodInfo.Invoke(entityList, new object[] {
                                    type.IsEnum?System.Enum.Parse(type,tStrArray[i]):Convert.ChangeType(tStrArray[i], type)
                                });
                            }
                        }
                        SaveDataToFinnalMap(inTargetType, inInfo.Name, entityList);
                    }
                    else
                    {
                        string[] endStrs = tStr.Split(LISTSPLITSIGN);
                        tStr = endStrs[endStrs.Length - 1];
                        if (CheckIsStruct(tStr))
                        {
                            object structObj = inInfo.FieldType.Assembly.CreateInstance(inInfo.FieldType.FullName);
                            SetStructProperty(ref structObj, tStr.Substring(1, tStr.Length - 2));
                            SaveDataToFinnalMap(inTargetType, inInfo.Name, structObj);
                        }
                        else
                        {
                            object value = inInfo.FieldType.IsEnum ? System.Enum.Parse(inInfo.FieldType, tStr) : Convert.ChangeType(tStr, inInfo.FieldType);
                            SaveDataToFinnalMap(inTargetType, inInfo.Name, value);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning(string.Format("无法将{0}转成{1}类型! 报错:{2}", tStr, inInfo.FieldType.ToString(), e.Message));
                }
            }
        }
    }

    private object GetListObject(Type inTargetType, FieldInfo inFieldInfo)
    {
        if (m_ListDict.TryGetValue(inTargetType, out Dictionary<string, object> tDict))
        {
            if (tDict.TryGetValue(inFieldInfo.Name, out object list))
                return list;
        }
        if (tDict == null)
        {
            tDict = new Dictionary<string, object>();
            m_ListDict.Add(inTargetType, tDict);
        }
        object value = Activator.CreateInstance(inFieldInfo.FieldType);
        tDict.Add(inFieldInfo.Name, value);
        return value;
    }

    void SaveDataToFinnalMap(Type type, string infieldName, object data)
    {
        if (!m_FinnalDataMap.TryGetValue(type, out Dictionary<string, object> tDict) || tDict == null)
        {
            tDict = new Dictionary<string, object>();
            m_FinnalDataMap.Add(type, tDict);
        }
        if (!tDict.ContainsKey(infieldName))
            tDict.Add(infieldName, data);
        else
            tDict[infieldName] = data;
    }

    private bool CheckIsStruct(string inStr)
    {
        return !string.IsNullOrEmpty(inStr) && inStr.Contains(STRUCTSIGNBEGIN.ToString()) && inStr.Contains(STRUCTSIGNEND.ToString()) && inStr.Contains(STRUCTSPLITSIGN.ToString());
    }
    private bool CheckIsList(string inStr, Type inType)
    {
        return !string.IsNullOrEmpty(inStr) && inType.ToString().Contains("System.Collections.Generic.List");
    }
    private bool CheckIsExecute(string inStr)
    {
        return !string.IsNullOrEmpty(inStr) && inStr.Contains(EXECUTESIGN);
    }
}
