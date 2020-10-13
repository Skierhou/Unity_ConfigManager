# Unity_ConfigManager
# 简介：
提供一份类似虚幻的简易Config配置系统(Unity)  
具体依赖C#反射机制获取并设置存储值  
初始化会存在一定的字符串拼接操作，初始化结束后将所有配置数值存放到字典中占用一定内存  

# 用法：
1.类名前添加[Config]标签标记  
2.在需要的字段前添加[Config]标签标记  
3.在程序运行时调用this.ReadConfig() 即可读到配置  
4.默认配置路径为streamingAssets/Configs/下，默认配置文件名为Default.ini  
5.修改配置信息路径等，以及配置符号通过修改ConfigManager下的变量即可  

# 后期待优化：
1.配置中添加执行语句，简单算式等  
2.代码优化  
