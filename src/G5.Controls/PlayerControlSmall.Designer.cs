namespace G5.Controls
{
    partial class PlayerControlSmall
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.labelStatus = new System.Windows.Forms.Label();
            this.labelStack = new System.Windows.Forms.Label();
            this.labelName = new System.Windows.Forms.Label();
            this.pictureHH1 = new System.Windows.Forms.PictureBox();
            this.pictureHH2 = new System.Windows.Forms.PictureBox();
            this.labelPosition = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pictureHH1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureHH2)).BeginInit();
            this.SuspendLayout();
            // 
            // labelStatus
            // 
            this.labelStatus.AutoSize = true;
            this.labelStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelStatus.ForeColor = System.Drawing.Color.Coral;
            this.labelStatus.Location = new System.Drawing.Point(3, 32);
            this.labelStatus.Name = "labelStatus";
            this.labelStatus.Size = new System.Drawing.Size(51, 15);
            this.labelStatus.TabIndex = 5;
            this.labelStatus.Text = "Folded";
            // 
            // labelStack
            // 
            this.labelStack.AutoSize = true;
            this.labelStack.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelStack.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.labelStack.Location = new System.Drawing.Point(3, 16);
            this.labelStack.Name = "labelStack";
            this.labelStack.Size = new System.Drawing.Size(36, 16);
            this.labelStack.TabIndex = 4;
            this.labelStack.Text = "$200";
            // 
            // labelName
            // 
            this.labelName.AutoSize = true;
            this.labelName.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelName.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.labelName.Location = new System.Drawing.Point(3, 0);
            this.labelName.Name = "labelName";
            this.labelName.Size = new System.Drawing.Size(82, 16);
            this.labelName.TabIndex = 3;
            this.labelName.Text = "Nemandza";
            // 
            // pictureHH1
            // 
            this.pictureHH1.Image = Resources._card_back;
            this.pictureHH1.Location = new System.Drawing.Point(60, 16);
            this.pictureHH1.Name = "pictureHH1";
            this.pictureHH1.Size = new System.Drawing.Size(26, 38);
            this.pictureHH1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureHH1.TabIndex = 6;
            this.pictureHH1.TabStop = false;
            // 
            // pictureHH2
            // 
            this.pictureHH2.Image = Resources._card_back;
            this.pictureHH2.Location = new System.Drawing.Point(89, 16);
            this.pictureHH2.Name = "pictureHH2";
            this.pictureHH2.Size = new System.Drawing.Size(26, 38);
            this.pictureHH2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureHH2.TabIndex = 7;
            this.pictureHH2.TabStop = false;
            // 
            // labelPosition
            // 
            this.labelPosition.AutoSize = true;
            this.labelPosition.Location = new System.Drawing.Point(86, 3);
            this.labelPosition.Name = "labelPosition";
            this.labelPosition.Size = new System.Drawing.Size(21, 13);
            this.labelPosition.TabIndex = 8;
            this.labelPosition.Text = "BB";
            // 
            // PlayerControlSmall
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.DimGray;
            this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Controls.Add(this.labelPosition);
            this.Controls.Add(this.pictureHH2);
            this.Controls.Add(this.pictureHH1);
            this.Controls.Add(this.labelStatus);
            this.Controls.Add(this.labelStack);
            this.Controls.Add(this.labelName);
            this.Name = "PlayerControlSmall";
            this.Size = new System.Drawing.Size(134, 54);
            ((System.ComponentModel.ISupportInitialize)(this.pictureHH1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureHH2)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelStatus;
        private System.Windows.Forms.Label labelStack;
        private System.Windows.Forms.Label labelName;
        private System.Windows.Forms.PictureBox pictureHH1;
        private System.Windows.Forms.PictureBox pictureHH2;
        private System.Windows.Forms.Label labelPosition;
    }
}
