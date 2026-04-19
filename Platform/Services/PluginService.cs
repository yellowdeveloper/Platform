using Platform.Model;
using Platform.Utils;
using PluginBase;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Platform.Services
{
    interface IPluginService
    {
        int loadPluginPathsAndReturnCount();
        int getPluginNum();
        void setPluginNum(int componetNum);
    }

    internal class PluginService : IPluginService
    {
        private readonly ConfigModel configModel;

        public PluginService(ConfigModel _configModel)
        {
            configModel = _configModel;
        }

        public int loadPluginPathsAndReturnCount()
        {
            string pluginDir = configModel.pluginPath;
            string[] dllFiles = Directory.GetFiles(pluginDir, "*.dll");

            for (int i = 0; i < dllFiles.Length; i++)
            {
                configModel.pluginPaths[i] = dllFiles[i];
            }

            return configModel.pluginPaths.Count;
        }

        public int getPluginNum()
        {
            return configModel.pluginNum;
        }

        public void setPluginNum(int componentNum)
        {
            configModel.pluginNum = componentNum;
        }

        public IPlugin loadPlugin(int pluginIndex)
        {
            string pluginPath = configModel.pluginPaths[pluginIndex];

            return DLLLoader.CreateCommand<IPlugin>(pluginPath);
        }
    }
}
