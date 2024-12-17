namespace ClientApp
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private System.Windows.Forms.Button sendButton;
        private System.Windows.Forms.Button selectionFile;
        private System.Windows.Forms.Label fileName;
        private System.Windows.Forms.PictureBox input;
        private System.Windows.Forms.PictureBox output;
        private System.Windows.Forms.Label message;
        private System.Windows.Forms.CheckBox thread;
        private System.Windows.Forms.Button saveButton;
    }
}

