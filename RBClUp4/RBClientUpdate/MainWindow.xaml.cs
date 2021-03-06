﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.IO.Packaging;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using System.Threading;
using ProcessServiceWorker;
using RBServer.Debug_classes;
using Debug_classes;
using CustomLogger;

namespace RBClientUpdate
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        BackgroundWorker backgroundWorker1;
        BackgroundWorker backgroundWorker2;

        int counter = 4;


        Logger log = new Logger();

        public virtual void Log(string message)
        {
            log.Log(message);
        }

        public virtual void Log(Exception ex, string message)
        {
            log.Log("Error " + ex.Message + " Message:" + message);
        }

        public MainWindow()
        {
#if(DEBUG)
            //int a = 0;
            //DirectoryInfo dir = new DirectoryInfo(@"G:\RRepo\myproject\RBClientUpdate_new\RBClientUpdate\bin\Debug\temp");
            //List<FileInfo> fi = dir.GetFiles("*.*", SearchOption.AllDirectories).ToList<FileInfo>();
            //FileSystemHelper.ReplaseFiles(fi, new DirectoryInfo(@"G:\RRepo\myproject\RBClientUpdate_new\RBClientUpdate\bin\Debug"));
#endif

            InitializeComponent();

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, EventArgs e)
        {
            try
            {
                backgroundWorker1 = new BackgroundWorker();
                backgroundWorker1.WorkerReportsProgress = true;
                backgroundWorker1.DoWork += new DoWorkEventHandler(this.backgroundWorker1_DoWork);
                backgroundWorker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(this.backgroundWorker1_RunWorkerCompleted);
                backgroundWorker1.ProgressChanged += new ProgressChangedEventHandler(backgroundWorker1_ProgressChanged);

                Thread th = new Thread(Automatic_start_updation);
                th.Name = "CountDownThread";
                th.Start();
            }
            catch (Exception ex)
            {
                Log(ex, "MainWindow_Loaded error");
            }
        }

        public void Automatic_start_updation()
        {
            try
            {
                
                for (int i = counter; i >= 0; i--)
                {
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(
                    System.Windows.Threading.DispatcherPriority.Normal,
                    (System.Threading.ThreadStart)delegate { textBlock3.Text = "Обновление через... " + i; });

                    
                    Thread.Sleep(1000);
                }

                
                if (ProcessWorker.GetProcessByName("RBClient").Count() != 0)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(
                    System.Windows.Threading.DispatcherPriority.Normal,
                    (System.Threading.ThreadStart)delegate { textBlock3.Text = "Ждем завершения процесса... "; });
                    Thread.Sleep(1000);
                    Automatic_start_updation();
                    return;
                }
                
                System.Windows.Application.Current.Dispatcher.Invoke(
                   System.Windows.Threading.DispatcherPriority.Normal,
                   (System.Threading.ThreadStart)delegate { backgroundWorker1.RunWorkerAsync(); });

            }
            catch (Exception ex)
            {
                Log(ex, "Automatic_start_updation error");
            }
        }

        public void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }

        public void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            Root.LogEvent += new Root.MessageEventHandler(delegate(object sender1, MessageEventArgs e1)
            {
                File.AppendAllText("RBClientUpdateLog.txt", DateTime.Now.ToShortDateString() + "--" + DateTime.Now.ToShortTimeString() + "--" + e1.Message);
            });
            Root.Operate_Zip((BackgroundWorker)sender, e);
        }


        bool ProcessEnded = false;
        public void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (progressBar1.Value != 100)
            {
                MessageBox.Show("Программа не обновлена!!!");
            }
            else
            {
                //MessageBox.Show("Программа обновлена!!!");
            }
            ProcessEnded = true;
            this.Close();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            backgroundWorker1.RunWorkerAsync();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!ProcessEnded)
            {
                if (MessageBox.Show("Программа не обновлена!!! Все равно закрыть?", "Предупреждение", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                {
                    Environment.Exit(0);
                }
            }
            else
            {
                ProcessWorker.StartProcess("RBClient.exe");
                Environment.Exit(0);
            }
            e.Cancel = true;
        }
    }
}
