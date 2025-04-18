using System.Windows.Forms;

namespace ComPortScannerApp
{
    partial class MainView
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.DataGridView dataGridPorts;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Button btnCopy;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.dataGridPorts = new System.Windows.Forms.DataGridView();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.btnCopy = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridPorts)).BeginInit();
            this.SuspendLayout();

            // 
            // dataGridPorts
            // 
            this.dataGridPorts.AllowUserToAddRows = false;
            this.dataGridPorts.AllowUserToDeleteRows = false;
            this.dataGridPorts.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                                        | System.Windows.Forms.AnchorStyles.Left)
                                        | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridPorts.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridPorts.Location = new System.Drawing.Point(12, 60);
            this.dataGridPorts.Name = "dataGridPorts";
            this.dataGridPorts.ReadOnly = true;
            this.dataGridPorts.RowTemplate.Height = 28;
            this.dataGridPorts.Size = new System.Drawing.Size(760, 480);
            this.dataGridPorts.TabIndex = 0;

            // Bigger font
            this.dataGridPorts.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.dataGridPorts.DefaultCellStyle.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.dataGridPorts.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);

            // Enable cell selection and copying
            this.dataGridPorts.SelectionMode = DataGridViewSelectionMode.CellSelect;
            this.dataGridPorts.MultiSelect = true;
            this.dataGridPorts.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;

            // 
            // btnRefresh
            // 
            this.btnRefresh.Location = new System.Drawing.Point(12, 12);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(160, 40);
            this.btnRefresh.TabIndex = 1;
            this.btnRefresh.Text = "🔄 Refresh Ports";
            this.btnRefresh.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);

            // 
            // btnCopy
            // 
            this.btnCopy.Location = new System.Drawing.Point(180, 12);
            this.btnCopy.Name = "btnCopy";
            this.btnCopy.Size = new System.Drawing.Size(160, 40);
            this.btnCopy.TabIndex = 2;
            this.btnCopy.Text = "📋 Copy Selected";
            this.btnCopy.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.btnCopy.UseVisualStyleBackColor = true;
            this.btnCopy.Click += new System.EventHandler(this.btnCopy_Click);

            // 
            // MainView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 561);
            this.Controls.Add(this.btnCopy);
            this.Controls.Add(this.btnRefresh);
            this.Controls.Add(this.dataGridPorts);
            this.Name = "MainView";
            this.Text = "COM Port Scanner";
            this.Load += new System.EventHandler(this.MainView_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridPorts)).EndInit();
            this.ResumeLayout(false);
        }
    }
}
