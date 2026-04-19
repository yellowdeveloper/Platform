using PluginBase;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace Platform.Utils
{
    class DLLLoader : AssemblyLoadContext
    {
        private AssemblyDependencyResolver _resolver;

        public DLLLoader(string pluginPath)
        {
            _resolver = new AssemblyDependencyResolver(pluginPath);
        }

        public static T CreateCommand<T> (string relativePath) where T : class
        {
            string pluginLocation = System.IO.Path.GetFullPath(relativePath);
            if (!File.Exists(pluginLocation))
            {
                // TODO :: REVEAL "DLL DOESNT EXIST ERROR"
                return null;
            }

            DLLLoader loader = new(pluginLocation);
            Assembly plugAssembly = loader.LoadFromAssemblyName(new(Path.GetFileNameWithoutExtension(pluginLocation)));

            var pluginType = plugAssembly.GetTypes().FirstOrDefault(t => typeof(T).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            if (pluginType != null)
            {
                return Activator.CreateInstance(pluginType) as T;
            }

            return null;
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            string assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            return null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            string libraryPath =  _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (libraryPath != null)
            {
                return LoadUnmanagedDllFromPath(libraryPath);
            }

            return IntPtr.Zero;
        }
    }
}
