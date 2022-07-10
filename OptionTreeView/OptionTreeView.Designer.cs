namespace OptionTreeView
{
    partial class OptionTreeView
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
            this.SplitContainer1 = new System.Windows.Forms.SplitContainer();
            this.OptionLeftView = new System.Windows.Forms.TreeView();
            ((System.ComponentModel.ISupportInitialize)(this.SplitContainer1)).BeginInit();
            this.SplitContainer1.Panel1.SuspendLayout();
            this.SplitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // SplitContainer1
            // 
            this.SplitContainer1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.SplitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SplitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.SplitContainer1.Location = new System.Drawing.Point(0, 0);
            this.SplitContainer1.Name = "SplitContainer1";
            // 
            // SplitContainer1.Panel1
            // 
            this.SplitContainer1.Panel1.Controls.Add(this.OptionLeftView);
            // 
            // SplitContainer1.Panel2
            // 
            this.SplitContainer1.Panel2.AutoScroll = true;
            this.SplitContainer1.Size = new System.Drawing.Size(508, 322);
            this.SplitContainer1.SplitterDistance = 160;
            this.SplitContainer1.TabIndex = 0;
            // 
            // OptionLeftView
            // 
            this.OptionLeftView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.OptionLeftView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.OptionLeftView.FullRowSelect = true;
            this.OptionLeftView.Location = new System.Drawing.Point(0, 0);
            this.OptionLeftView.Name = "OptionLeftView";
            this.OptionLeftView.ShowLines = false;
            this.OptionLeftView.ShowPlusMinus = false;
            this.OptionLeftView.ShowRootLines = false;
            this.OptionLeftView.Size = new System.Drawing.Size(158, 320);
            this.OptionLeftView.TabIndex = 0;
            this.OptionLeftView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.OptionLeftView_AfterSelect);
            // 
            // OptionTreeView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.SplitContainer1);
            this.Name = "OptionTreeView";
            this.Size = new System.Drawing.Size(508, 322);
            this.Load += new System.EventHandler(this.OptionTreeView_Load);
            this.SplitContainer1.Panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.SplitContainer1)).EndInit();
            this.SplitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer SplitContainer1;
        private System.Windows.Forms.TreeView OptionLeftView;
    }
}
