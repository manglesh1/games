using System;
using System.Linq;
using System.Windows.Forms;

namespace ComPortScannerApp
{
    public partial class MainView : Form
    {
        public MainView()
        {
            InitializeComponent();
        }

        private void MainView_Load(object sender, EventArgs e)
        {
            LoadComPorts();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadComPorts();
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            if (dataGridPorts.GetCellCount(DataGridViewElementStates.Selected) > 0)
            {
                try
                {
                    Clipboard.SetDataObject(dataGridPorts.GetClipboardContent(), true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Copy failed: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("No cells selected.", "Copy", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void LoadComPorts()
        {
            var ports = ComPortScanner.GetAvailableComPorts();
            dataGridPorts.DataSource = ports.Select(p => new
            {
                Port = p.Port,
                Name = p.Name,
                DeviceID = p.DeviceID
            }).ToList();
        }
    }
}
