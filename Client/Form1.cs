using System.Drawing;
using System;
using System.Net;
using System.Windows.Forms;
using System.Net.Sockets;

namespace ClientApp
{
    public partial class Form1 : Form
    {

        private IPAddress _localAddress = IPAddress.Parse("127.0.0.1");
        private int _port = 8888;

        private Client1 _client;

        private Bitmap _input;
        private bool _thread;

        public Form1()
        {
            InitializeComponent();

            try
            {
                _client = new Client1(new IPEndPoint(_localAddress, _port));
            }
            catch
            {
                MessageBox.Show("Не удалось подключиться к серверу");
                selectionFile.Enabled = false;
            }

            message.Text = "Время обработки: -";
            sendButton.Enabled = false;
        }

        private void InitializeComponent()
        {
            this.sendButton = new System.Windows.Forms.Button();
            this.selectionFile = new System.Windows.Forms.Button();
            this.fileName = new System.Windows.Forms.Label();
            this.input = new System.Windows.Forms.PictureBox();
            this.output = new System.Windows.Forms.PictureBox();
            this.message = new System.Windows.Forms.Label();
            this.thread = new System.Windows.Forms.CheckBox();
            this.saveButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.input)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.output)).BeginInit();
            this.SuspendLayout();
            // 
            // sendButton
            // 
            this.sendButton.Location = new System.Drawing.Point(310, 760);
            this.sendButton.Size = new System.Drawing.Size(400, 100);
            this.sendButton.Text = "Отправить";
            this.sendButton.UseVisualStyleBackColor = true;
            this.sendButton.Click += new System.EventHandler(this.sendButton_Click);
            // 
            // selectionFile
            // 
            this.selectionFile.Location = new System.Drawing.Point(50, 110);
            this.selectionFile.Size = new System.Drawing.Size(200, 50);
            this.selectionFile.Text = "Выбрать файл";
            this.selectionFile.UseVisualStyleBackColor = true;
            this.selectionFile.Click += new System.EventHandler(this.selectionFile_Click);
            // 
            // fileName
            // 
            this.fileName.AutoSize = true;
            this.fileName.Location = new System.Drawing.Point(260, 130);
            this.fileName.Size = new System.Drawing.Size(300, 20);
            this.fileName.Text = "Файл не выбран";

            // 
            // input
            // 
            this.input.BackColor = System.Drawing.SystemColors.ControlDark;
            this.input.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.input.Location = new System.Drawing.Point(50, 200);
            this.input.Size = new System.Drawing.Size(943, 403);
            this.input.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.input.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;

            // 
            // output
            // 
            this.output.BackColor = System.Drawing.SystemColors.ControlDark;
            this.output.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.output.Location = new System.Drawing.Point(930, 200);
            this.output.Size = new System.Drawing.Size(943, 403);
            this.output.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.output.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            // 
            // message
            // 
            this.message.AutoSize = true;
            this.message.Location = new System.Drawing.Point(800, 800);
            this.message.Size = new System.Drawing.Size(300, 30);
            this.message.Text = "Время обработки: -";
            this.message.Font = new Font(this.message.Font.FontFamily, 14);
            // 
            // thread
            // 
            this.thread.AutoSize = true;
            this.thread.Location = new System.Drawing.Point(445, 740);
            this.thread.Size = new System.Drawing.Size(200, 40);
            this.thread.Text = "Многопоточность";
            this.thread.UseVisualStyleBackColor = true;
            this.thread.CheckedChanged += new System.EventHandler(this.thread_CheckedChanged);
            // 
            // saveButton
            // 
            this.saveButton.Location = new System.Drawing.Point(1330, 760);
            this.saveButton.Size = new System.Drawing.Size(400, 100);
            this.saveButton.Text = "Сохранить";
            this.saveButton.UseVisualStyleBackColor = true;
            this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
            // 
            // Form1
            // 
            this.ClientSize = new System.Drawing.Size(1920, 1080);
            this.Controls.Add(this.saveButton);
            this.Controls.Add(this.thread);
            this.Controls.Add(this.message);
            this.Controls.Add(this.output);
            this.Controls.Add(this.input);
            this.Controls.Add(this.fileName);
            this.Controls.Add(this.selectionFile);
            this.Controls.Add(this.sendButton);
            this.Name = "Form1";
            this.Text = "Image Processing Client";
            this.WindowState = FormWindowState.Maximized;
            ((System.ComponentModel.ISupportInitialize)(this.input)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.output)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void selectionFile_Click(object sender, System.EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image Files (*.jpeg;*.jpg;*.png)|*.jpeg;*.jpg;*.png";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string fileNameStr = openFileDialog.FileName;
                    _input = new Bitmap(fileNameStr);

                    fileName.Text = fileNameStr;
                    input.Image = _input;

                    sendButton.Enabled = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при загрузке файла: " + ex.Message);
                }
            }
            else
            {
                input.Image = null;
                fileName.Text = "Файл не выбран";
                sendButton.Enabled = false;
            }
        }

        private async void sendButton_Click(object sender, System.EventArgs e)
        {
            saveButton.Enabled = false;
            (Bitmap outputBitmap, double time) = await _client.SendImage(_input, _thread);
            output.Image = outputBitmap;
            message.Text = $"Время обработки: {Math.Round(time, 2)} ms.";
            saveButton.Enabled = true;
        }

        private void thread_CheckedChanged(object sender, EventArgs e)
        {
            _thread = thread.Checked;
        }

        private void output_Click(object sender, EventArgs e)
        {

        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            if (output.Image != null)
            {
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "JPEG Image|*.jpg|PNG Image|*.png|Bitmap Image|*.bmp";
                    saveFileDialog.Title = "Save an Image File";
                    saveFileDialog.DefaultExt = "jpg";
                    saveFileDialog.AddExtension = true;

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        string filePath = saveFileDialog.FileName;
                        output.Image.Save(filePath);
                    }
                }
            }
            else
            {
                MessageBox.Show("No image to save.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
