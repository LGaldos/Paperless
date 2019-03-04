using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Client;
using System.Threading;
using System.IO;
using System.Windows.Media.Imaging;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Browser;

namespace ESMA.Paperless.FileUploaderVisor.v15
{
    public partial class MainPage : UserControl
    {
        private ClientContext context;
        private Web web;
        public List<FileStructure> fileList = new List<FileStructure>();

        private BackgroundWorker bw = new BackgroundWorker();
        private BackgroundWorker bwf = new BackgroundWorker();

        private int number = 0;

        //InitParams prevazane zo SilverLight definicie
        string webUrl = string.Empty;
        string libraryName = string.Empty;
        string libraryURL = string.Empty;
        string subfolderName = string.Empty;
        double maxFileSize = 0;
        double maxSize = 0;
        string allowTypesWebpart = string.Empty;
        string[] allowTypes = new string[0];
        int maxFiles = 0;
        private Boolean blnMessageFinish = false;

        public class FileStructure
        {
            public string FileName { get; set; }
            public string FileLabel { get; set; }
            public string FileSize { get; set; }
            public bool FileOverwrite { get; set; }
            public FileInfo FileInfo { get; set; }
            public string FileStatus { get; set; }
        }


        public MainPage()
        {
            InitializeComponent();

            //Events
            this.UploadButton.Click += new RoutedEventHandler(btnUpload_Click);
            this.ClearButton.Click += new RoutedEventHandler(btnClear_Click);


            this.dg.AllowDrop = true;
            this.dg.Drop += new DragEventHandler(dg_Drop);
            this.textDrop.AllowDrop = true;
            this.textDrop.Drop += new DragEventHandler(dg_Drop);

            //Timer Jobs
            bw.WorkerReportsProgress = true;
            bw.WorkerSupportsCancellation = true;
            bw.DoWork += new DoWorkEventHandler(bw_DoWork);

            bwf.WorkerReportsProgress = true;
            bwf.WorkerSupportsCancellation = true;
            bwf.DoWork += new DoWorkEventHandler(bwf_DoWork);

            try
            {
                webUrl = App.Current.Host.InitParams["WebUrl"];

                libraryURL = string.IsNullOrEmpty(App.Current.Host.InitParams["LibraryURL"]) ? "" : App.Current.Host.InitParams["LibraryURL"].Replace("%2c", ",");
                libraryName = string.IsNullOrEmpty(App.Current.Host.InitParams["LibraryName"]) ? "" : App.Current.Host.InitParams["LibraryName"].Replace("%2c", ",");
                subfolderName = string.IsNullOrEmpty(App.Current.Host.InitParams["SubfolderName"]) ? "" : App.Current.Host.InitParams["SubfolderName"].Replace("%2c", ",");

                allowTypesWebpart = string.IsNullOrEmpty(App.Current.Host.InitParams["AllowTypes"]) ? "" : App.Current.Host.InitParams["AllowTypes"].Replace("%2c", ",");
                if (!string.IsNullOrEmpty(allowTypesWebpart))
                    allowTypes = allowTypesWebpart.ToLower().Split(';');

                maxFileSize = string.IsNullOrEmpty(App.Current.Host.InitParams["MaxFileSize"]) ? 0 : Convert.ToDouble(App.Current.Host.InitParams["MaxFileSize"]);
                maxSize = string.IsNullOrEmpty(App.Current.Host.InitParams["MaxSize"]) ? 0 : Convert.ToDouble(App.Current.Host.InitParams["MaxSize"]);
                maxFiles = string.IsNullOrEmpty(App.Current.Host.InitParams["MaxFiles"]) ? 0 : Convert.ToInt32(App.Current.Host.InitParams["MaxFiles"]);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }


            try
            {
                context = new ClientContext(webUrl);
                web = context.Web;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        #region <BUTTONS>

        private void btnUpload_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(libraryName))
                MessageBox.Show("Error to get the 'Library Name'");
            else
            {
                fileList = (List<FileStructure>)dg.ItemsSource;
                number = 0;

                if (fileList.Count > 0)
                {
                    HideComponents();

                    progressBar1.Value = number;
                    progressBar1.Minimum = number;
                    progressBar1.Maximum = fileList.Count;

                    UploadFile(fileList[number].FileInfo, libraryURL, subfolderName, fileList[number].FileOverwrite, libraryName);

                    //Initialite Btn + GridView
                    UploadButton.Visibility = Visibility.Collapsed;
                    ClearButton.Visibility = Visibility.Collapsed;
                    dg.IsEnabled = false;
                }
            }
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            ClearAllInformation();
        }

        private void ClearAllInformation()
        {
            fileList.Clear();
            dg.ItemsSource = null;
            dg.ItemsSource = fileList;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var ctl = e.OriginalSource as Button;
            if (null != ctl)
            {
                var fileS = ctl.DataContext as FileStructure;
                if (null != fileS)
                {
                    FileStructure fileRemove = fileList.First(s => s.FileName == fileS.FileName);
                    fileList.Remove(fileRemove);

                    dg.ItemsSource = null;
                    dg.ItemsSource = fileList;

                    if (fileList.Count == 0)
                        textDrop.Visibility = Visibility.Visible;

                }
            }
        }

        private void CloseButton_Click_1(object sender, RoutedEventArgs e)
        {
            var Script = HtmlPage.Document.CreateElement("script");
            Script.SetAttribute("type", "text/javascript");
            Script.SetProperty("text", "function CloseDialog() { window.frameElement.commitPopup().close();}");
            HtmlPage.Document.DocumentElement.AppendChild(Script);
            HtmlPage.Window.Invoke("CloseDialog");
        }

        #endregion

        #region <CONTROL MANAGEMENT>

        //----------------------------------------------------------
        //TIMER JOBS
        //----------------------------------------------------------
        private void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            Dispatcher.BeginInvoke(() =>
            {
            });
        }

        private void bwf_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            Dispatcher.BeginInvoke(() =>
            {
            });
        }

        //----------------------------------------------------------
        //HIDE CONTROLS
        //----------------------------------------------------------
        private void UnhideComponents()
        {
            //dg.IsEnabled = true;
            UploadButton.IsEnabled = true;
            ClearButton.IsEnabled = true;
            progressBar1.Visibility = Visibility.Collapsed;

            txtMessage.Visibility = Visibility.Visible;

            if (blnMessageFinish == false)
            {
                txtMessage.Foreground = new SolidColorBrush(Colors.Green);
                txtMessage.Text = "Files uploaded correctly.";
            }
            else
            {
                txtMessage.Foreground = new SolidColorBrush(Colors.Red);
                txtMessage.Text = "Error uploading files. Please, try again.";
            }
        }

        private void HideComponents()
        {
            dg.IsEnabled = false;
            UploadButton.IsEnabled = false;
            ClearButton.IsEnabled = false;
            progressBar1.Visibility = Visibility.Visible;
        }


        #endregion


        void dg_Drop(object sender, DragEventArgs e)
        {

            if (e.Data != null)
            {
                FileInfo[] files = e.Data.GetData(DataFormats.FileDrop) as FileInfo[];

                foreach (FileInfo fileInfo in files)
                {
                    try
                    {
                        double fileSize = Math.Round((fileInfo.Length / 1024f) / 1024f, 2);
                        double size = 0;

                        if (string.IsNullOrEmpty(fileInfo.Extension))
                        {
                            MessageBox.Show("Folder can not be imported!");
                            continue;
                        }

                        if (!maxFileSize.Equals(0))
                        {
                            if (fileSize > maxFileSize)
                            {
                                MessageBox.Show(string.Format("Maximum File size is: {0}MB. File {1} has {2}MB!", maxFileSize.ToString(), fileInfo.Name, fileSize.ToString()));
                                continue;
                            }
                        }

                        if (!maxSize.Equals(0))
                        {
                            foreach (FileStructure fileS in fileList)
                            {
                                size += Math.Round((fileS.FileInfo.Length / 1024f) / 1024f, 2);
                            }
                            size += fileSize;

                            if (size > maxSize)
                            {
                                MessageBox.Show(string.Format("Maximum size for all files is: {0}MB!.Review what files have been uploaded.", maxSize));
                                continue;
                            }
                        }

                        if (!maxFiles.Equals(0))
                        {
                            if (fileList.Count >= maxFiles)
                            {
                                MessageBox.Show(string.Format("Maximum files to upload: {0}!", maxFiles));
                                continue;
                            }
                        }


                        if (!allowTypes.Length.Equals(0))
                        {
                            if (!allowTypes.Contains(fileInfo.Extension.ToLower()))
                            {
                                MessageBox.Show(string.Format("Not allowed file type. {0}", fileInfo.Name));
                                continue;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }

                    CreateFileStructure(fileInfo);
                }

                dg.ItemsSource = null;
                dg.ItemsSource = fileList;

                if (!fileList.Count.Equals(0))
                    textDrop.Visibility = Visibility.Collapsed;
            }
        }

        private void CreateFileStructure(FileInfo fileInfo)
        {

            try
            {
                bool fileExists = false;

                if (fileList.Count > 0)
                    fileExists = fileList.Any(s => s.FileName == fileInfo.Name);

                if (fileExists == false)
                {
                    if (fileInfo.Length < 1048576)
                        fileList.Add(new FileStructure() { FileName = fileInfo.Name, FileInfo = fileInfo, FileSize = Math.Round((fileInfo.Length / 1024f), 2).ToString() + "KB", FileStatus = "Not Send", FileLabel = "", FileOverwrite = true });
                    else
                        fileList.Add(new FileStructure() { FileName = fileInfo.Name, FileInfo = fileInfo, FileSize = Math.Round((fileInfo.Length / 1024f) / 1024f, 2).ToString() + "MB", FileStatus = "Not Send", FileLabel = "", FileOverwrite = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void UploadFile(FileInfo fileToUpload, string libraryURL, string subfolderPath, bool fileOverwrite, string libraryName)
        {
            try
            {

                Stream str = null;
                Int32 strLen, strRead;

                str = fileToUpload.OpenRead();
                //strLen = Convert.ToInt32(str.Length);

                //byte[] strArr = new byte[strLen];
                //strRead = str.Read(strArr, 0, strLen);

                List destinationList = web.Lists.GetByTitle(libraryName);

                if (libraryURL.Contains("/Forms/AllItems.aspx"))
                    libraryURL = libraryURL.Replace("/Forms/AllItems.aspx", null);

                if (libraryURL.Contains("/"))
                {
                    string[] inf = libraryURL.Split('/');
                    libraryURL = inf[inf.Length - 1];
                }

                var fciFileToUpload = new FileCreationInformation();
                //fciFileToUpload.Content = strArr;
                fciFileToUpload.ContentStream = str;

                string uploadLocation = fileToUpload.Name;

                if (!string.IsNullOrEmpty(subfolderPath))
                    uploadLocation = string.Format("{0}/{1}", subfolderPath, uploadLocation);

                uploadLocation = string.Format("{0}/{1}/{2}", webUrl, libraryURL, uploadLocation);
                //MessageBox.Show(uploadLocation);

                fciFileToUpload.Url = uploadLocation;
                fciFileToUpload.Overwrite = fileOverwrite;

                Microsoft.SharePoint.Client.File clFileToUpload = destinationList.RootFolder.Files.Add(fciFileToUpload);


                context.Load(web);
                context.Load(clFileToUpload);
                context.ExecuteQueryAsync(OnLoadingSucceeded, OnLoadingFailed);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                UnhideComponents();
            }
        }

        private void OnLoadingSucceeded(Object sender, ClientRequestSucceededEventArgs args)
        {
            Dispatcher.BeginInvoke(FileUploaded);
        }

        private void OnLoadingFailed(object sender, ClientRequestFailedEventArgs args)
        {
            Dispatcher.BeginInvoke(FileNotUploaded);
        }

        private void FileUploaded()
        {
            try
            {
                fileList[number].FileStatus = "Done";


                Dispatcher.BeginInvoke(() =>
                {
                    dg.ItemsSource = null;
                    dg.ItemsSource = fileList;

                    progressBar1.Value = number;
                });



                number++;

                if (number < fileList.Count)
                    UploadFile(fileList[number].FileInfo, libraryURL, subfolderName, fileList[number].FileOverwrite, libraryName);
                else if (number == fileList.Count)
                    UnhideComponents();


            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                UnhideComponents();
            }

        }

        private void FileNotUploaded()
        {
            try
            {

                blnMessageFinish = true;

                fileList[number].FileStatus = "Failed!";

                Dispatcher.BeginInvoke(() =>
                {
                    dg.ItemsSource = null;
                    dg.ItemsSource = fileList;
                    progressBar1.Value = number;
                });


                number++;
                if (number < fileList.Count)
                    UploadFile(fileList[number].FileInfo, libraryURL, subfolderName, fileList[number].FileOverwrite, libraryName);
                else if (number == fileList.Count)
                    UnhideComponents();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                UnhideComponents();
            }
        }

        private void NextUploadAfterSuccess(Object sender, ClientRequestSucceededEventArgs args)
        {
            Dispatcher.BeginInvoke(NextUploadAfterSuccessAsync);
        }

        private void NextUploadAfterSuccessAsync()
        {
            number++;

            if (number < fileList.Count)
                UploadFile(fileList[number].FileInfo, libraryURL, subfolderName, fileList[number].FileOverwrite, libraryName);
            else if (number == fileList.Count)
                UnhideComponents();

        }

        private void nextUploadAfterNotSuccess(object sender, ClientRequestFailedEventArgs args)
        {
            Dispatcher.BeginInvoke(NextUploadAfterNotSuccessAsync);
        }

        private void NextUploadAfterNotSuccessAsync()
        {
            MessageBox.Show("Label can not be changed. File Found for some Reason Label not changed! Moving to next File!");

            number++;
            if (number < fileList.Count)
                UploadFile(fileList[number].FileInfo, libraryURL, subfolderName, fileList[number].FileOverwrite, libraryName);
            else if (number == fileList.Count)
                UnhideComponents();

        }


    }

}
