﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SimplePlugins;
using SimplePlugins.Exceptions;
using SamplePluggableApp.SamplePlugin.SampleDependancy;
using System.Configuration;
using System.Threading;

namespace SamplePluggableApp.SamplePlugin
{
    public partial class MainView : Form
    {
        public MainView()
        {
            InitializeComponent();            
        }

        private void MainView_Load(object sender, EventArgs e)
        {
            this.Text = Plugin.Current.FriendlyName;

            Plugin currentPlugin = (Plugin)PluginBase.Current;

            this.lbParameterDisplay.Items.Add("Value of Parameter 1: " + currentPlugin.Param1);
            this.lbParameterDisplay.Items.Add("Value of Parameter 2: " + currentPlugin.Param2.ToString());
            this.lbParameterDisplay.Items.Add("Value of Parameter 3: " + Convert.ToString(currentPlugin.Param3));
        }

        private void btnException_Click(object sender, EventArgs e)
        {
            //This is to demonstrate how unhandled exceptions are caught by PluginLoaderBase object, wrapped in a serializeable wrapper
            //and returned to the pluggable application. This ensures that unexpected non-serializeable exceptions do not leak app domains.
            try
            {
                throw new InvalidCastException(); 
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("This is to demonstrate PluginException.InnerException.", ex);
            }
        }

        private void btnCheckDependancy_Click(object sender, EventArgs e)
        {
            //The assembly containing the class TestDependany is loaded from application root folder and not from the application\plugins folder
            //This demonstrates the purpose of PluginLoaderBase.AssemblyResolutionPaths property
            TestDependancy dep = new TestDependancy();
            MessageBox.Show(dep.TestMethod());
        }

        private void btnReadFromConfigFile_Click(object sender, EventArgs e)
        {
            //Demonstrates how plugins can use their own application configuration file just like a standalone app does
            MessageBox.Show(ConfigurationManager.AppSettings["TestSetting"]);
        }

        private void btnThreadException_Click(object sender, EventArgs e)
        {
            //Demonstrates how plugins can register any additionally created threads and let any unhandled exceptions be handled automatically.
            //When any unhandled exceptions occur in these managed threads the exceptions are added to PluginBase.UnhandledThreadExceptions list and PluginBase.Abort is called where the threadException argument can be used to access the exception.
            //It is recommended to abort the plugin process and let it unload normally when thread exceptions occur

            Thread t = Plugin.Current.CreateManagedThread(new ThreadStart(this.DoAsyncWork));
            t.Start();
            
            this.DoWork();
        }

        private void DoWork()
        {
            while (true) { }
        }

        private void DoAsyncWork()
        {
            Thread.Sleep(1000);
            throw new Exception("This is an exception in a seperate thread");
        }
        
    }
}
