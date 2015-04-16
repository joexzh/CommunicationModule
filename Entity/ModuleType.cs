using System;
using System.Collections.Generic;

using System.Text;

namespace CommunicationModule.Entity
{
    public enum ModuleType
    {
        //注册，各模块刚连接时发送
        Register,
        //配置模块
        Config,
        //解析模块
        Analysis,
        //gnss解算模块
        Gnss
    };
}
