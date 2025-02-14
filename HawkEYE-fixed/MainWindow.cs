﻿using CefSharp;
using CefSharp.WinForms;
using HawkEYE_fixed;
using Renci.SshNet;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Threading;


namespace Hawk_Client {
    public partial class MainWindow : Form {

        #region colors
        Color backgroundColor = Color.FromArgb(35, 41, 49);
        Color buttonColor = Color.FromArgb(96, 32, 128);
        Color TrueColor = Color.FromArgb(78, 204, 163);
        Color FalseColor = Color.FromArgb(132, 20, 45);
        #endregion
        #region ssh and connection 
        private String HOST = "";
        private String USER = "";
        private String PASSWORD = "";
        private SshClient sshClient = new SshClient("none", 0, "none", "none");
        public ChromiumWebBrowser mainBrowser,CameraBrowser;
        #endregion
        public MainWindow() {
            InitializeComponent();
            CefSettings settings = new CefSettings();
            settings.BrowserSubprocessPath = @"x86\CefSharp.BrowserSubprocess.exe";
            // Initialize cef with the provided settings
            Cef.Initialize(settings);
            this.scriptSelect.SelectedItem = "Default";
            string path = AppDomain.CurrentDomain.BaseDirectory;
            Thread checkConnection = new Thread(checkConnectionThread);
        }
        #region Control Tab methods
        private void Connect_Click(object sender, EventArgs e) {
            if (sshClient.IsConnected) sshClient.Disconnect();
            int index = this.ConnectAddress.Text.IndexOf("@");
            String address = this.ConnectAddress.Text;
            if (index > 0 || address == "default") {
                if (address == "default") { USER = "root"; HOST = "192.168.1.6"; } //make your life easier
                else{USER = address.Substring(0, index);
                HOST = address.Substring(index + 1); }
                PASSWORD = this.password.Text;
                try {
                    sshClient = new SshClient(HOST, 22, USER, PASSWORD); //default port = 22
                    if (sshClient.IsConnected){ sshClient.Disconnect(); this.ConnectButton.BackColor = FalseColor; return; } //if clicked and connected -> disconnect
                    sshClient.Connect();
                    sshClient.CreateCommand("mkdir ~/.hawk");
                    sshClient.CreateCommand("mkdir ~/.hawk/grips");
                    sshClient.CreateCommand("mkdir ~/.hawk/scripts");
                    sshClient.CreateCommand("mkdir ~/.hawk/cameras");
                }
                catch (Exception) {
                    showError("faild to connect " + USER + " at " + HOST, "Connection error");
                }
                if (sshClient.IsConnected) this.ConnectButton.BackColor = TrueColor;
                else{ this.ConnectButton.BackColor = FalseColor; return; };

                // Create a browser component
                mainBrowser = new ChromiumWebBrowser("http://"+HOST+":1181/stream.mjpg");
                CameraBrowser = new ChromiumWebBrowser("http://" + HOST + ":1181");
                mainBrowser.Location = new Point(893, 79);
                mainBrowser.Size = new Size(640, 480);
                mainBrowser.Dock = DockStyle.None;

                CameraBrowser.Location = new Point(3, 3);
                CameraBrowser.Size = new Size(783, 477);
                // Add it to the form
                this.Controls.Add(mainBrowser);
                this.CameraControl.Controls.Add(CameraBrowser);

            }
            else {
                showError("address should be in template 'usr@host'", "Address not valid");
            }

        }
        private void showError(string text, string title) {
            MessageBox.Show(text, title, MessageBoxButtons.OK, MessageBoxIcon.Error);

        }
        private void cancelHint(object sender, EventArgs e) {
            ((Control)sender).Text = "";
        }

        private void SetIP_Click(object sender, EventArgs e) {
            if (!sshClient.IsConnected) { showError("You are not connected!", "invalid action"); return; }
            String address = this.setIPtext.Text;
            sshClient.CreateCommand("~/HawkEYE/scripts/set_static_ip.sh " + address);
            int index = this.ConnectAddress.Text.IndexOf("@");
            USER = address.Substring(0, index);
            HOST = address.Substring(index + 1);
            if (MessageBox.Show("set IP successfull , press Ok to reboot(recommended) or Cancel to do nothing.", "success", MessageBoxButtons.OKCancel) == DialogResult.OK) sshClient.CreateCommand("reboot");
        }
        private void setDNS_Click(object sender, EventArgs e) {
            if (!sshClient.IsConnected) { showError("You are not connected!", "invalid action"); return; }
            String DNS = this.setIPtext.Text;
            sshClient.CreateCommand("hostnamectl set-hostname " + DNS);
            if (MessageBox.Show("set DNS successfull , press Ok to reboot(recommended) or Cancel to do nothing.", "success", MessageBoxButtons.OKCancel) == DialogResult.OK) sshClient.CreateCommand("reboot");
        }
        private void setDHCP_Click(object sender, EventArgs e) {
            if (!sshClient.IsConnected) { showError("You are not connected!", "invalid action"); return; }
            sshClient.CreateCommand("~/HawkEYE/scripts/remove_static_ip.sh");
            if (MessageBox.Show("static IP removal successfull , press Ok to reboot(recommended) or Cancel to do nothing.", "success", MessageBoxButtons.OKCancel) == DialogResult.OK) sshClient.CreateCommand("reboot");
        }

        private void ChangePass_Click(object sender, EventArgs e) {
            if (!sshClient.IsConnected) { showError("You are not connected!", "invalid action"); return; }
            String pass = this.setPass1.Text;
            if (pass != setPass2.Text) { showError("Password don`t match!", "not equal passwords"); return; }
            sshClient.CreateCommand("~/HawkEYE/scripts/change_password.sh " + pass);
            PASSWORD = pass;
            if (MessageBox.Show("static IP removal successfull , press Ok to reboot(recommended) or Cancel to do nothing.", "success", MessageBoxButtons.OKCancel) == DialogResult.OK) sshClient.CreateCommand("reboot");

        }

        private void Restart_Click(object sender, EventArgs e) {
            if (!sshClient.IsConnected) { showError("You are not connected!", "invalid action"); return; }
            sshClient.RunCommand("pkill -9 python3");
            sshClient.RunCommand("python3 /root/HawkEYE/source/main.py");
            showError("HawkEYE restart successfull", "success");
        }
        private void Reboot(object sender, EventArgs e) {
            if (!sshClient.IsConnected) { showError("You are not connected!", "invalid action"); return; }
            sshClient.RunCommand("reboot");
        }

        #endregion

        private void CheckBox1_CheckedChanged(object sender, EventArgs e) {
            bool ok = !this.MLenable.Checked;
            this.GRIPselecter.Visible = ok;
            this.gripLabel.Visible = ok;
            this.GRIPload.Visible = ok;
            this.GRIPpushButton.Visible = ok;
            this.GRIPdeleteButton.Visible = ok;
            //if (ok) sshClient.RunCommand("python3 ~/HawkEye/source/MLprocess.py");
        }

        private void Refresh_Click(object sender, EventArgs e) {
            if(mainBrowser != null) {
                mainBrowser.Refresh();
            }
        }

        private void ScriptPushButton_Click(object sender, EventArgs e) {
            string buttonType = "scripts"; //can be "scripts" or "grip", depend on the button
            if (sender == GRIPpushButton) buttonType = "grips";
            if (!sshClient.IsConnected) { showError("You are not connected!", "invalid Action"); return; }
            var fileContent = string.Empty;
            var filePath = string.Empty;

            using (OpenFileDialog openFileDialog = new OpenFileDialog()) {
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop); //default path is desktop
                openFileDialog.Filter = "Python files (*.py)|*.py|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK) {
                    //Read the contents of the file into a stream
                    var fileStream = openFileDialog.OpenFile();
                    using (StreamReader reader = new StreamReader(fileStream)) {
                        fileContent = reader.ReadToEnd();
                        ComboBox selecter = buttonType == "scripts" ? scriptSelect : GRIPselecter;
                        selecter.Items.Add(System.IO.Path.GetFileName(openFileDialog.FileName));
                    }
                    sshClient.RunCommand("echo " + fileContent + " > " + "~/.hawk/"+ buttonType +"/" + openFileDialog.SafeFileName);


                }


            }

        }

        private void SaveButton_Click(object sender, EventArgs e) {
            if (!sshClient.IsConnected) { showError("You are not connected!", "invalid Action"); return; }
            string scriptfile = scriptSelect.SelectedItem.ToString(); if (scriptfile == "Default") scriptfile = "processor.py";
            string gripfile = GRIPselecter.SelectedItem.ToString();
            string pipe = "vision";
            string teamnumber = TeamNumberBox.Text; if (teamnumber == "Team Number"){ showError("Insert team number!", "Invalid action");return; }
            string width = WidthBox.Text; if (width == "Width : 120") width = "160";
            string height = HeightBox.Text; if (height == "Height : 160" ) height = "120";
            string fps = FPSbox.Text; if (fps == "FPS-(Frame Per Second) : 30") fps = "30";
            string json = $"{{^grip_file^: \"{gripfile}\",^script_file^: \"{scriptfile}\",^pipe_name^:\"{pipe}\",^team_number^:#{teamnumber}#,^width^:#{width}',^height^:#{height}#,^fps^:#{fps}#}}";
            string js = $"{{^grip_file^: \"{gripfile}\"";
            char c = "'"[0];
            json = json.Replace('^','"').Replace('#',c);
            sshClient.RunCommand($"echo '{json}' > ~/.hawk/config.json");
            updateFileManager();
        }

        private void ScriptDeleteButton_Click(object sender, EventArgs e) {
            if (!sshClient.IsConnected) { showError("You are not connected!", "invalid Action"); return; }
            sshClient.RunCommand("rm ~/.hawk/scripts/" + ((ComboBox)sender).Text);
            updateFileManager();
        }

        private void GRIPdeleteButton_Click(object sender, EventArgs e) {
            if (!sshClient.IsConnected) { showError("You are not connected!", "invalid Action"); return; }
            sshClient.RunCommand("rm ~/.hawk/grips/" + ((ComboBox)sender).Text);
            updateFileManager();
        }

        private void Button1_Click(object sender, EventArgs e) {
            if (!sshClient.IsConnected) { showError("You are not connected!", "invalid Action"); return; }
            ConfigWindow w = new ConfigWindow(HOST);
            w.Show();
        }

        private void ValuesRefresh_Click(object sender, EventArgs e) {
            if (!sshClient.IsConnected) { showError("You are not connected!", "invalid Action"); return; }     
            if (MessageBox.Show("NOTE: refreshing the values will make you recalculate them, this is an automatic process that make your mechine more accurate, Do you want to continue?"
            , "Warning", MessageBoxButtons.OKCancel) == DialogResult.Cancel) return;

            string path = @" ""/root/HawkEye/source"" ";
            var cmd = sshClient.RunCommand("python3 -c 'import os; os.chdir(" + path + "); import MLprocess;'");
            string res = cmd.Result;
            
            res = res.Substring(1,res.Length-3); // remove '[' and ']' for formatting
            MessageBox.Show(res);
            double[] vals = Array.ConvertAll(res.Split(','), Double.Parse);

            Hmin.Text = vals[0].ToString();
            Hmax.Text = vals[1].ToString();

            Smin.Text = vals[2].ToString();
            Smax.Text = vals[3].ToString();

            Vmin.Text = vals[4].ToString();
            Vmax.Text = vals[5].ToString();

        }

        private void updateFileManager() {
            string scripts = sshClient.RunCommand("ls ~/.hawk/scripts").Result;
            string grips = sshClient.RunCommand("ls ~/.hawk/grips").Result;
            scriptSelect.Items.AddRange(scripts.Split(' '));
            GRIPselecter.Items.AddRange(grips.Split(' '));

        }


        public void checkConnectionThread() {
            while(true)
            if (!sshClient.IsConnected) this.ConnectButton.BackColor = FalseColor;
        }
    }
}
