using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.IO.Compression;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using Microsoft.Win32;

namespace WarriorLauncher
{
    public partial class Form1 : Form
    {
        static string baseUrl = "http://warrior.rip";
        static string apiUrl = "http://api.warrior.rip";
        static string version = "1.0.0";
        static string client = "2016";
        static string installPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Warrior\\" + client;

        public Form1()
        {
            InitializeComponent();
        }

        private void UpdateClient(bool doInstall)
        {
            label1.Text = "Getting the latest version...";

            string recievedClientData;
            try
            {
                recievedClientData = new WebClient().DownloadString(apiUrl + "/client/" + client);
            }
            catch
            {
                label1.Text = "Error: Can't connect.";
                MessageBox.Show("Could not connect to Warrior, please contact us if this persists.", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }

            string versionString = "false";
            string downloadUrl = "false";

            try
            {
                JObject clientData = JObject.Parse(recievedClientData);
                
                downloadUrl = (string)clientData["url"];
                versionString = (string)clientData["version"];

            }
            catch
            {
                MessageBox.Show("Could not parse JSON, this is usually a serverside issue. Please contact us if this problem persists.", "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }


            if (doInstall)
            {
                // Assuming we're in the temp folder or the user knows what they're doing, start replacing files.
                label1.Text = "Downloading newest version...";

                WebClient webClient = new WebClient();
                string tempZipArchivePath = Path.GetTempPath() + client + ".zip";

                webClient.DownloadProgressChanged += (s, e) =>
                {
                    progressBar1.Value = e.ProgressPercentage;
                };

                webClient.DownloadFileCompleted += (s, e) =>
                {
                    label1.Text = "Extracting files...";
                    progressBar1.Value = 50;

                    try
                    {
                        if (Directory.Exists(installPath))
                        {
                            Directory.Delete(installPath, true);
                        }

                        ZipFile.ExtractToDirectory(tempZipArchivePath, installPath);
                    }
                    catch (Exception exc)
                    {
                        MessageBox.Show("Error occurred while attempting to extract the client to its proper directory. (" + exc.Message + ") \n\n" + installPath, "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Close();
                        return;
                    }

                    label1.Text = "Setting up URI...";
                    try
                    {
                        var classesKey = Registry.CurrentUser.OpenSubKey(@"Software\Classes", true);

                        var key = classesKey.CreateSubKey("warriorlauncher");
                        key.CreateSubKey("DefaultIcon").SetValue("", installPath + "\\WarriorLauncher.exe,1");
                        key.SetValue("", "warriorlauncher:Protocol");
                        key.SetValue("URL Protocol", "");
                        key.CreateSubKey(@"shell\open\command").SetValue("", '"' + installPath + "\\WarriorLauncher.exe" + '"' + "-token %1");
                        key.Close();
                    }
                    catch
                    {
                        MessageBox.Show("Couldn't setup URI, this means the client will probably not launch.", "URI Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    label1.Text = "Clean up...";
                    if (File.Exists(tempZipArchivePath))
                    {
                        File.Delete(tempZipArchivePath);
                    }

                    MessageBox.Show("Warrior updated successfully.", "Update Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Close();
                    return;
                };

                try
                {
                    webClient.DownloadFileAsync(new Uri(baseUrl + "/client/download/" + client), tempZipArchivePath);
                }
                catch
                {
                    MessageBox.Show("Could not get latest client files from the website.", "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Close();
                    return;
                }
            }   
            else
            {
                // Clone the installer to the temp folder so we can actually install.

                string currentExecutablePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string copiedExecutablePath = Path.GetTempPath() + "WarriorLauncher" + client + ".exe";

                File.Copy(currentExecutablePath, copiedExecutablePath, true);

                var process = new Process
                {
                    StartInfo =
                    {
                        FileName = copiedExecutablePath,
                        Arguments = "-update"
                    }
                };
                process.Start();

                Close();
                return;
            }
        }

        private async void Form1_Shown(object sender, EventArgs e)
        {
            string[] args = Environment.GetCommandLineArgs();

            if (args.Length < 2)
            {
                UpdateClient(false);
                return;
            }



            else if (args[1] == "-update")
            {
                UpdateClient(true);
                return;
            }

            else if (args[1] != "-token")
            {
                UpdateClient(false);
                return;
            }

            else if (!File.Exists(installPath + "\\WarriorPlayerBeta.exe"))
            {
                MessageBox.Show("The client couldn't be found, sot it will be installed.\nInstall Path: " + installPath, "Client Missing", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateClient(false);
                return;
            }

            label1.Text = "Checking version...";
            progressBar1.Value = 50;

            string recievedClientData;
            try
            {
                recievedClientData = new WebClient().DownloadString(apiUrl + "/client/" + client);
            }
            catch
            {
                label1.Text = "Error: Can't connect.";
                MessageBox.Show("Could not connect to Warrior, please contact us if this persists.", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }

            string versionString = "false";
            string downloadUrl = "false";
            string sha256 = "none";

            try
            {
                JObject clientData = JObject.Parse(recievedClientData);

                versionString = (string)clientData["version"];
                downloadUrl = (string)clientData["url"];
            }
            catch
            {
                MessageBox.Show("Could not parse JSON, this is usually a serverside issue. Please contact us if this problem persists.", "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }

            if (versionString == version)
            {
                label1.Text = "Launching Warrior " + client + "...";

                var process = new Process
                {
                    StartInfo =
                    {
                        FileName = installPath + "\\WarriorPlayerBeta.exe",
                        Arguments = ("-a " + '"' + Form1.baseUrl + '"' + " -t 123 -j " + '"' + Form1.baseUrl + "/game/join/" + args[2].Split(':')[1] + '"')
                    }
                };
                process.Start();

                progressBar1.Value = 100;
                await Task.Delay(5000);

                Close();
                return;
            }
            else
            {
                label1.Text = "New version found, updating: " + version + " -> " + versionString;
                await Task.Delay(5000);
                UpdateClient(false);
                return;
            }
        }
    }
}
