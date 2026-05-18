using OpenCvSharp.Internal.Vectors;
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
        int GetPluginNum();
        void SetPluginNum(int componetNum);
        IPlugin LoadPlugin(int pluginIndex);
        public void DetachPlugin();
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

        public int GetPluginNum()
        {
            return configModel.pluginNum;
        }

        public void SetPluginNum(int componentNum)
        {
            configModel.pluginNum = componentNum;
        }

        public string GetPluginNameFromIndex(int index)
        {
            string pluginName = configModel.pluginPaths[index];
            // 전체 경로에서 확장자 .dll 및 경로 Plugins\ 를 합하여 총 12개 문자 제거
            pluginName = pluginName.Substring(8, pluginName.Length - 12);

            return pluginName;
        }

        public IPlugin LoadPlugin(int pluginIndex)
        {
            string pluginPath = configModel.pluginPaths[pluginIndex];

            return DLLLoader.CreateCommand<IPlugin>(pluginPath);
        }

        public void DetachPlugin()
        {

        }
    }
}
