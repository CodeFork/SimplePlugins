﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using SimplePlugins.Loaders;
using SimplePlugins;
using System.IO;
using System.Threading;
using System.Reflection;
using SimplePlugins.Exceptions;

namespace SamplePluggableApp
{
    public partial class MainView : Form
    {        
        public MainView()
        {
            InitializeComponent();

            //Plugins will reside in this folder
            this.PluginFolder = Path.Combine(System.Windows.Forms.Application.StartupPath, "Plugins");

            //Create a new instance of PluginLoader to manage the loading and managing plugins
            this.PluginLoader = new PluginLoader();

            //This can be used to tell the plugin loader where to look for any dependancies of plugins if the .Net framework is unable to locate them.
            this.PluginLoader.AssemblyResolutionPaths.Add(Application.StartupPath);
            this.PluginLoader.AssemblyResolutionPaths.Add(Path.Combine(Application.StartupPath, "bin"));

            //Enable default shadow copy behaviour for the plugin loader.
            //When set to true all plugins and dependancies will be shadow copied by the .Net framework before loading.
            //This will allow updating/replacing plugin files even when they are loaded.
            this.PluginLoader.ShadowCopyEnabled = true;

            //This event fires when PluginLoaderBase.Probe is complete.
            this.PluginLoader.ProbeComplete += new PluginLoaderBase.ProbeCompleteEventHandler(PluginLoader_ProbeComplete);

            //This event fires before PluginLoaderBase.Load creates the AppDomain object which will be used to load the plugin from the plugin assembly.
            this.PluginLoader.PluginLoading += new PluginLoaderBase.PluginLoadingEventHandler(PluginLoader_PluginLoading);
        
            //This event fires when PluginLoaderBase.Load completes loading the plugin from the plugin assembly.
            this.PluginLoader.PluginLoaded += new PluginLoaderBase.PluginLoadedEventHandler(PluginLoader_PluginLoaded);

            //This event fires when PluginLoaderBase.Load completes the execution and unloading of the loaded plugin.
            this.PluginLoader.PluginUnloaded += new PluginLoaderBase.PluginUnloadedEventHandler(PluginLoader_PluginUnloaded);
}

        public Console Console
        {
            //The user control used to display messages on the main form
            get { return cslConsole; }
        }

        void PluginLoader_ProbeComplete(object sender, PluginLoaderBase.ProbeCompleteEventArgs e)
        {
            Console.WriteInfo("Plugin probe completed with '" + Convert.ToString(e.InfoList.Count) + "' detections.");
        }

        void PluginLoader_PluginLoading(object sender, PluginLoaderBase.PluginLoadingEventArgs e)
        {
            //You may make changes to the domain setup used to load the current plugin here i.e e.DomainSetup.ApplicationName, e.DomainSetup.CachePath, etc. 
            //Or you can cancel the loading by setting e.Cancel = true

            Console.WriteInfo("Loading plugin from file '" + e.AssemblyFileName + "'...");            
        }

        void PluginLoader_PluginLoaded(object sender, PluginLoaderBase.PluginLoadedEventArgs e)
        {
            Console.WriteInfo("Executing plugin '" + e.Info.ToString() + "'...");
        }

        void PluginLoader_PluginUnloaded(object sender, PluginLoaderBase.PluginUnloadedEventArgs e)
        {
            //Check if the plugin was unloaded due to an exception
            //The PluginException class wraps any unhandled exception in a serializable wrapper.
            //The PluginException class also exposes all InnerExceptions of the actual exception through PluginException.InnerException as well 
            //This makes sure that unhandled non-serializable exceptions do not cause unexpected AppDomain leaks in the main application. 
            if (e.Results.UnhandledException != null)
                Console.WriteException("The plugin '" + e.Info.ToString() + "' was unloaded due to unhandled exception: " + e.Results.UnhandledException.ToString());
            else if (e.Results.ExecutionDenied)
            {
                //Plugins can be denied execution by the PluginLoader object according to the specified value in PluginBase.ExecutionMode property. 
                //This checks which execution modes prevented the plugin from being executed.
                if (e.Info.ExecutionMode == PluginBase.ExecutionModes.AsynchronousExclusive || e.Info.ExecutionMode == PluginBase.ExecutionModes.SynchronousExclusive)
                    Console.WriteInfo("The plugin '" + e.Info.ToString() + "' was unloaded since it is intended to be executed exclusively. Please make sure no other plugin is executing and try again.");
                else if (e.Info.ExecutionMode == PluginBase.ExecutionModes.AsynchronousSingleton || e.Info.ExecutionMode == PluginBase.ExecutionModes.SynchronousSingleton)
                    Console.WriteInfo("The plugin '" + e.Info.ToString() + "' was unloaded since only one instance of it is intended to be executed at a time. Please make sure no other instance of the same plugin is executing and try again.");
                else
                    Console.WriteInfo("The plugin '" + e.Info.ToString() + "' was unloaded.");
            }
            else
            {
                //Each plugin may return results in the form of a PluginParameters object exposed by the PluginLoaderBase.PluginUnloadedEventArgs.Results property
                //This object can be checked for values returned by the plugin like so,
                if (e.Results.Contains("ReturnValue"))
                    Console.WriteInfo("The plugin '" + e.Info.ToString() + "' was unloaded successfully with the return value: " + Convert.ToString(e.Results.Get("ReturnValue")));
                else
                    Console.WriteInfo("The plugin '" + e.Info.ToString() + "' was unloaded successfully");
            }
        }

        private PluginLoader PluginLoader { get; set; }

        private string PluginFolder { get; set; }

        private void MainView_Load(object sender, EventArgs e)
        {
            Console.WriteInfo("Probing for plugins in plugins folder '" + this.PluginFolder + "'...");

            //Probe for plugins in the plugins folder and add them to the ListBox to be displayed.
            //PluginLoaderBase.Probe will return a list of PluginInfo objects which contain information about each available plugin
            PluginInfoList pluginInfoList = this.PluginLoader.Probe(this.PluginFolder, SearchOption.AllDirectories);
            for (int i = 0; i < pluginInfoList.Count; i++)
                lbPlugins.Items.Add(pluginInfoList[i]);
        }

        private void lbPlugins_DoubleClick(object sender, EventArgs e)
        {
            if (lbPlugins.SelectedItem != null)
            {
                //Get the PluginInfo object selected by the user for execution. 
                PluginInfo template = (PluginInfo)lbPlugins.SelectedItem;
                try
                {
                    //Create a PluginParameters object and add any parameters to be passed to the plugin
                    PluginParameters args = new PluginParameters();
                    args.Add("Param1", "The quick brown fox jumped over the lazy dog");
                    args.Add("Param2", 100);
                    args.Add("Param3", 7.321);

                    //Tell the plugin loader to load and execute the plugin
                    this.PluginLoader.Load(template.FileName, args);
                }
                catch (PluginException ex)
                {
                    Console.WriteException(ex);
                }
            }
        }

        private void MainView_FormClosing(object sender, FormClosingEventArgs e)
        {
            //Stop event firing in PluginLoader 
            this.PluginLoader.EventsEnabled = false;

            //Notify all plugins and give them a chance to clean up before exiting the main application
            //This will cause the PluginBase.OnUnloadNotification of each loaded plugin to be called
            this.PluginLoader.NotifyUnloadAll();
            //Thread.Sleep(2000); //Give all plugins two seconds to unload cleanly before exiting.
        }

        private void btnUnloadAll_Click(object sender, EventArgs e)
        {
            //This will cause the PluginBase.OnUnloadNotification of each loaded plugin to be called
            this.PluginLoader.NotifyUnloadAll();
        }

        private void btnShowLoadedAssemblies_Click(object sender, EventArgs e)
        {
            //Display all assemblies which have been loaded to the current AppDomain to check if any domain leaks have occurred.
            string assemblies = "The following assemblies are loaded in the current app domain,\n";
            Assembly[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly a in loadedAssemblies)
                assemblies += a.FullName + "\n";

            this.Console.WriteInfo(assemblies);
        }

        private void btnShowLoadedPlugins_Click(object sender, EventArgs e)
        {
            //Show all plugins which are currently loaded and their instance counts
            if (this.PluginLoader.LoadedPlugins.Count > 0)
            {                
                Dictionary<string, int> pluginInstanceCounts = new Dictionary<string, int>();
                foreach (PluginInfo info in this.PluginLoader.LoadedPlugins)
                {
                    string key = info.ToString();
                    if (pluginInstanceCounts.ContainsKey(key))
                        pluginInstanceCounts[key] = pluginInstanceCounts[key] + 1;
                    else
                        pluginInstanceCounts.Add(key, 1);
                }

                string line = "The following plugins are currently loaded,\n";
                foreach(string key in pluginInstanceCounts.Keys)
                    line += Convert.ToString(pluginInstanceCounts[key]) + " instances of '" + key + "'\n";

                Console.WriteInfo(line);
            }
            else
                Console.WriteInfo("No plugins are currently loaded.");
        }

    }
}
