using System;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace Agent_Client
{
    public partial class ConfigForm : Form
    {
        private string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

        public ConfigForm()
        {
            InitializeComponent();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            string ip = txtServerIp.Text.Trim();
            if (string.IsNullOrWhiteSpace(ip))
            {
                MessageBox.Show("لطفاً آدرس IP سرور را وارد کنید.");
                return;
            }

            var config = new { ServerIp = ip };
            string json = JsonSerializer.Serialize(config);
            File.WriteAllText(configPath, json);

            MessageBox.Show("آدرس IP با موفقیت ذخیره شد.");
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void ConfigForm_Load(object sender, EventArgs e)
        {
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (config != null && config.ContainsKey("ServerIp"))
                    txtServerIp.Text = config["ServerIp"];
            }
        }
    }
}
