namespace G5.PlayVsBots
{
    partial class PlayVsBotsForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this._gameTableControl = new G5.Controls.GameTableControl();
            this.SuspendLayout();
            // 
            // _gameTableControl
            // 
            this._gameTableControl.Location = new System.Drawing.Point(12, 12);
            this._gameTableControl.Name = "_gameTableControl";
            this._gameTableControl.Size = new System.Drawing.Size(750, 511);
            this._gameTableControl.TabIndex = 0;
            // 
            // PlayVsBotsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(775, 536);
            this.Controls.Add(this._gameTableControl);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "PlayVsBotsForm";
            this.Text = "Play vs Bots";
            this.ResumeLayout(false);

        }

        #endregion

        private Controls.GameTableControl _gameTableControl;
    }
}