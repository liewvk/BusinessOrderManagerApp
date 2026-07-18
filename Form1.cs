using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using System.Drawing.Printing;
using System.IO;


namespace BusinessOrderManagerApp
{
    public partial class Form1 : Form
    {
        private string connectionString =
    @"Server=(localdb)\MSSQLLocalDB;Database=BusinessDB;Trusted_Connection=True;TrustServerCertificate=True;";

        private PrintDocument printDocument = new PrintDocument();

        private int printRowIndex = 0;
        private void txtSearch_TextChanged(object sender, EventArgs e)
        {

        }

        private void LoadOrders()
        {
            string sql = @"SELECT Id,
                          OrderNumber,
                          CustomerName,
                          ProductName,
                          Quantity,
                          UnitPrice,
                          TotalAmount,
                          OrderStatus,
                          OrderDate
                   FROM SalesOrders
                   ORDER BY Id DESC";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(sql, connection))
                    {
                        DataTable table = new DataTable();
                        adapter.Fill(table);

                        dgvOrders.DataSource = table;

                        FormatGrid();
                        UpdateSummary();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading orders.\n\n" + ex.Message,
                                "Database Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }
        private void FormatGrid()
        {
            if (dgvOrders.Columns.Count == 0)
            {
                return;
            }

            dgvOrders.Columns["UnitPrice"].DefaultCellStyle.Format = "0.00";
            dgvOrders.Columns["TotalAmount"].DefaultCellStyle.Format = "0.00";
            dgvOrders.Columns["OrderDate"].DefaultCellStyle.Format = "dd/MM/yyyy";
        }

        private void UpdateSummary()
        {
            int recordCount = dgvOrders.Rows.Count;
            decimal grandTotal = 0;

            foreach (DataGridViewRow row in dgvOrders.Rows)
            {
                if (row.Cells["TotalAmount"].Value != null)
                {
                    grandTotal += Convert.ToDecimal(row.Cells["TotalAmount"].Value);
                }
            }

            lblRecordCount.Text = $"Records: {recordCount}";
            lblGrandTotal.Text = $"Grand Total: $ {grandTotal:0.00}";
        }
        private bool ValidateInput(out int quantity, out decimal unitPrice)
        {
            quantity = 0;
            unitPrice = 0;

            if (txtOrderNumber.Text.Trim() == "")
            {
                MessageBox.Show("Please enter the order number.",
                                "Missing Order Number",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);

                txtOrderNumber.Focus();
                return false;
            }

            if (txtCustomerName.Text.Trim() == "")
            {
                MessageBox.Show("Please enter the customer name.",
                                "Missing Customer Name",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);

                txtCustomerName.Focus();
                return false;
            }

            if (txtProductName.Text.Trim() == "")
            {
                MessageBox.Show("Please enter the product name.",
                                "Missing Product Name",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);

                txtProductName.Focus();
                return false;
            }

            if (!int.TryParse(txtQuantity.Text.Trim(), out quantity))
            {
                MessageBox.Show("Please enter a valid quantity.",
                                "Invalid Quantity",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);

                txtQuantity.Focus();
                return false;
            }

            if (quantity <= 0)
            {
                MessageBox.Show("Quantity must be greater than zero.",
                                "Invalid Quantity",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);

                txtQuantity.Focus();
                return false;
            }

            if (!decimal.TryParse(txtUnitPrice.Text.Trim(), out unitPrice))
            {
                MessageBox.Show("Please enter a valid unit price.",
                                "Invalid Unit Price",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);

                txtUnitPrice.Focus();
                return false;
            }

            if (unitPrice < 0)
            {
                MessageBox.Show("Unit price cannot be negative.",
                                "Invalid Unit Price",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);

                txtUnitPrice.Focus();
                return false;
            }

            if (cmbOrderStatus.SelectedIndex < 0 && cmbOrderStatus.Text == "")
            {
                MessageBox.Show("Please select the order status.",
                                "Missing Order Status",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);

                cmbOrderStatus.Focus();
                return false;
            }

            return true;
        }
        private void ClearInputFields()
        {
            txtId.Clear();
            txtOrderNumber.Clear();
            txtCustomerName.Clear();
            txtProductName.Clear();
            txtQuantity.Clear();
            txtUnitPrice.Clear();

            cmbOrderStatus.SelectedIndex = -1;
            dtpOrderDate.Value = DateTime.Today;

            txtOrderNumber.Focus();
        }
        private void ExportDataGridViewToCsv(string filePath)
        {
            StringBuilder csvContent = new StringBuilder();

            for (int i = 0; i < dgvOrders.Columns.Count; i++)
            {
                csvContent.Append(EscapeCsvValue(dgvOrders.Columns[i].HeaderText));

                if (i < dgvOrders.Columns.Count - 1)
                {
                    csvContent.Append(",");
                }
            }

            csvContent.AppendLine();

            foreach (DataGridViewRow row in dgvOrders.Rows)
            {
                if (!row.IsNewRow)
                {
                    for (int i = 0; i < dgvOrders.Columns.Count; i++)
                    {
                        object value = row.Cells[i].Value;

                        csvContent.Append(EscapeCsvValue(value?.ToString() ?? ""));

                        if (i < dgvOrders.Columns.Count - 1)
                        {
                            csvContent.Append(",");
                        }
                    }

                    csvContent.AppendLine();
                }
            }

            File.WriteAllText(filePath, csvContent.ToString(), Encoding.UTF8);
        }
        private string EscapeCsvValue(string value)
        {
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                value = value.Replace("\"", "\"\"");
                return $"\"{value}\"";
            }

            return value;
        }
        private void PrintDocument_PrintPage(object sender, PrintPageEventArgs e)
        {
            Font titleFont = new Font("Segoe UI", 16, FontStyle.Bold);
            Font headerFont = new Font("Segoe UI", 9, FontStyle.Bold);
            Font bodyFont = new Font("Consolas", 9);

            float y = 50;
            float leftMargin = e.MarginBounds.Left;
            float lineHeight = bodyFont.GetHeight(e.Graphics) + 6;

            e.Graphics.DrawString("Sales Order Report", titleFont, Brushes.Black, leftMargin, y);
            y += 35;

            e.Graphics.DrawString($"Date: {DateTime.Now}", bodyFont, Brushes.Black, leftMargin, y);
            y += 25;

            e.Graphics.DrawString("Order No   Customer          Product           Qty   Price     Total     Status",
                                  headerFont,
                                  Brushes.Black,
                                  leftMargin,
                                  y);

            y += 20;

            e.Graphics.DrawLine(Pens.Black, leftMargin, y, e.MarginBounds.Right, y);
            y += 15;

            while (printRowIndex < dgvOrders.Rows.Count)
            {
                DataGridViewRow row = dgvOrders.Rows[printRowIndex];

                if (!row.IsNewRow)
                {
                    string orderNo = row.Cells["OrderNumber"].Value?.ToString() ?? "";
                    string customer = row.Cells["CustomerName"].Value?.ToString() ?? "";
                    string product = row.Cells["ProductName"].Value?.ToString() ?? "";
                    int quantity = Convert.ToInt32(row.Cells["Quantity"].Value);
                    decimal unitPrice = Convert.ToDecimal(row.Cells["UnitPrice"].Value);
                    decimal totalAmount = Convert.ToDecimal(row.Cells["TotalAmount"].Value);
                    string status = row.Cells["OrderStatus"].Value?.ToString() ?? "";

                    if (customer.Length > 15)
                    {
                        customer = customer.Substring(0, 15);
                    }

                    if (product.Length > 15)
                    {
                        product = product.Substring(0, 15);
                    }

                    string line = $"{orderNo,-10} {customer,-16} {product,-16} {quantity,3} {unitPrice,8:0.00} {totalAmount,9:0.00} {status,-10}";

                    e.Graphics.DrawString(line, bodyFont, Brushes.Black, leftMargin, y);

                    y += lineHeight;
                }

                printRowIndex++;

                if (y > e.MarginBounds.Bottom - 80)
                {
                    e.HasMorePages = true;
                    return;
                }
            }

            y += 20;

            e.Graphics.DrawLine(Pens.Black, leftMargin, y, e.MarginBounds.Right, y);
            y += 20;

            e.Graphics.DrawString(lblGrandTotal.Text,
                                  headerFont,
                                  Brushes.Black,
                                  leftMargin,
                                  y);

            e.HasMorePages = false;
        }

        public Form1()
        {
            InitializeComponent();
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            cmbOrderStatus.Items.Add("Pending");
            cmbOrderStatus.Items.Add("Paid");
            cmbOrderStatus.Items.Add("Shipped");
            cmbOrderStatus.Items.Add("Completed");
            cmbOrderStatus.Items.Add("Cancelled");

            cmbFilterStatus.Items.Add("All");
            cmbFilterStatus.Items.Add("Pending");
            cmbFilterStatus.Items.Add("Paid");
            cmbFilterStatus.Items.Add("Shipped");
            cmbFilterStatus.Items.Add("Completed");
            cmbFilterStatus.Items.Add("Cancelled");

            cmbOrderStatus.SelectedIndex = -1;
            cmbFilterStatus.SelectedIndex = 0;

            dtpOrderDate.Value = DateTime.Today;

            printDocument.PrintPage += PrintDocument_PrintPage;

            LoadOrders();
            ClearInputFields();

        }

        private void btnAdd_Click(object sender, EventArgs e)
        {

            if (!ValidateInput(out int quantity, out decimal unitPrice))
            {
                return;
            }

            decimal totalAmount = quantity * unitPrice;

            string sql = @"INSERT INTO SalesOrders
                   (OrderNumber, CustomerName, ProductName, Quantity, UnitPrice, TotalAmount, OrderStatus, OrderDate)
                   VALUES
                   (@OrderNumber, @CustomerName, @ProductName, @Quantity, @UnitPrice, @TotalAmount, @OrderStatus, @OrderDate)";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@OrderNumber", txtOrderNumber.Text.Trim());
                        command.Parameters.AddWithValue("@CustomerName", txtCustomerName.Text.Trim());
                        command.Parameters.AddWithValue("@ProductName", txtProductName.Text.Trim());
                        command.Parameters.AddWithValue("@Quantity", quantity);
                        command.Parameters.AddWithValue("@UnitPrice", unitPrice);
                        command.Parameters.AddWithValue("@TotalAmount", totalAmount);
                        command.Parameters.AddWithValue("@OrderStatus", cmbOrderStatus.Text);
                        command.Parameters.AddWithValue("@OrderDate", dtpOrderDate.Value.Date);

                        connection.Open();

                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Order added successfully.",
                                            "Order Added",
                                            MessageBoxButtons.OK,
                                            MessageBoxIcon.Information);

                            LoadOrders();
                            ClearInputFields();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error adding order.\n\n" + ex.Message,
                                "Database Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }

        }

        private void dgvOrders_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            DataGridViewRow row = dgvOrders.Rows[e.RowIndex];

            txtId.Text = row.Cells["Id"].Value.ToString();
            txtOrderNumber.Text = row.Cells["OrderNumber"].Value.ToString();
            txtCustomerName.Text = row.Cells["CustomerName"].Value.ToString();
            txtProductName.Text = row.Cells["ProductName"].Value.ToString();
            txtQuantity.Text = row.Cells["Quantity"].Value.ToString();
            txtUnitPrice.Text = Convert.ToDecimal(row.Cells["UnitPrice"].Value).ToString("0.00");
            cmbOrderStatus.Text = row.Cells["OrderStatus"].Value.ToString();

            if (DateTime.TryParse(row.Cells["OrderDate"].Value.ToString(), out DateTime orderDate))
            {
                dtpOrderDate.Value = orderDate;
            }

        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (txtId.Text.Trim() == "")
            {
                MessageBox.Show("Please select an order to update.",
                                "No Order Selected",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);

                return;
            }

            if (!ValidateInput(out int quantity, out decimal unitPrice))
            {
                return;
            }

            decimal totalAmount = quantity * unitPrice;

            string sql = @"UPDATE SalesOrders
                   SET OrderNumber = @OrderNumber,
                       CustomerName = @CustomerName,
                       ProductName = @ProductName,
                       Quantity = @Quantity,
                       UnitPrice = @UnitPrice,
                       TotalAmount = @TotalAmount,
                       OrderStatus = @OrderStatus,
                       OrderDate = @OrderDate
                   WHERE Id = @Id";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Id", Convert.ToInt32(txtId.Text));
                        command.Parameters.AddWithValue("@OrderNumber", txtOrderNumber.Text.Trim());
                        command.Parameters.AddWithValue("@CustomerName", txtCustomerName.Text.Trim());
                        command.Parameters.AddWithValue("@ProductName", txtProductName.Text.Trim());
                        command.Parameters.AddWithValue("@Quantity", quantity);
                        command.Parameters.AddWithValue("@UnitPrice", unitPrice);
                        command.Parameters.AddWithValue("@TotalAmount", totalAmount);
                        command.Parameters.AddWithValue("@OrderStatus", cmbOrderStatus.Text);
                        command.Parameters.AddWithValue("@OrderDate", dtpOrderDate.Value.Date);

                        connection.Open();

                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Order updated successfully.",
                                            "Order Updated",
                                            MessageBoxButtons.OK,
                                            MessageBoxIcon.Information);

                            LoadOrders();
                            ClearInputFields();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating order.\n\n" + ex.Message,
                                "Database Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (txtId.Text.Trim() == "")
            {
                MessageBox.Show("Please select an order to delete.",
                                "No Order Selected",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);

                return;
            }

            DialogResult result = MessageBox.Show("Are you sure you want to delete this order?",
                                                  "Confirm Delete",
                                                  MessageBoxButtons.YesNo,
                                                  MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }

            string sql = "DELETE FROM SalesOrders WHERE Id = @Id";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Id", Convert.ToInt32(txtId.Text));

                        connection.Open();

                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Order deleted successfully.",
                                            "Order Deleted",
                                            MessageBoxButtons.OK,
                                            MessageBoxIcon.Information);

                            LoadOrders();
                            ClearInputFields();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error deleting order.\n\n" + ex.Message,
                                "Database Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }

        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            string searchText = txtSearch.Text.Trim();

            if (searchText == "")
            {
                MessageBox.Show("Please enter a customer name to search.",
                                "Missing Search Text",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);

                txtSearch.Focus();
                return;
            }

            string sql = @"SELECT Id,
                          OrderNumber,
                          CustomerName,
                          ProductName,
                          Quantity,
                          UnitPrice,
                          TotalAmount,
                          OrderStatus,
                          OrderDate
                   FROM SalesOrders
                   WHERE CustomerName LIKE @SearchText
                   ORDER BY Id DESC";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(sql, connection))
                    {
                        adapter.SelectCommand.Parameters.AddWithValue("@SearchText", "%" + searchText + "%");

                        DataTable table = new DataTable();
                        adapter.Fill(table);

                        dgvOrders.DataSource = table;

                        FormatGrid();
                        UpdateSummary();

                        if (table.Rows.Count == 0)
                        {
                            MessageBox.Show("No matching order found.",
                                            "Search Result",
                                            MessageBoxButtons.OK,
                                            MessageBoxIcon.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error searching orders.\n\n" + ex.Message,
                                "Database Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }

        }

        private void btnApplyFilter_Click(object sender, EventArgs e)
        {
            string status = cmbFilterStatus.Text;

            if (status == "" || status == "All")
            {
                LoadOrders();
                return;
            }

            string sql = @"SELECT Id,
                          OrderNumber,
                          CustomerName,
                          ProductName,
                          Quantity,
                          UnitPrice,
                          TotalAmount,
                          OrderStatus,
                          OrderDate
                   FROM SalesOrders
                   WHERE OrderStatus = @OrderStatus
                   ORDER BY Id DESC";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(sql, connection))
                    {
                        adapter.SelectCommand.Parameters.AddWithValue("@OrderStatus", status);

                        DataTable table = new DataTable();
                        adapter.Fill(table);

                        dgvOrders.DataSource = table;

                        FormatGrid();
                        UpdateSummary();

                        if (table.Rows.Count == 0)
                        {
                            MessageBox.Show("No orders found for the selected status.",
                                            "Filter Result",
                                            MessageBoxButtons.OK,
                                            MessageBoxIcon.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error filtering orders.\n\n" + ex.Message,
                                "Database Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }

        }

        private void btnClearFilter_Click(object sender, EventArgs e)
        {
            txtSearch.Clear();

            if (cmbFilterStatus.Items.Count > 0)
            {
                cmbFilterStatus.SelectedIndex = 0;
            }

            LoadOrders();

        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            ClearInputFields();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadOrders();
            ClearInputFields();
            txtSearch.Clear();

            if (cmbFilterStatus.Items.Count > 0)
            {
                cmbFilterStatus.SelectedIndex = 0;
            }

        }

        private void btnExportCsv_Click(object sender, EventArgs e)
        {
            if (dgvOrders.Rows.Count == 0)
            {
                MessageBox.Show("There is no data to export.",
                                "No Data",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);

                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "CSV Files (*.csv)|*.csv";
            saveFileDialog.Title = "Export Orders to CSV";
            saveFileDialog.FileName = "SalesOrders.csv";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    ExportDataGridViewToCsv(saveFileDialog.FileName);

                    MessageBox.Show("Orders exported successfully.",
                                    "Export Complete",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error exporting orders.\n\n" + ex.Message,
                                    "Export Error",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error);
                }
            }

        }

        private void btnPrintPreview_Click(object sender, EventArgs e)
        {
            if (dgvOrders.Rows.Count == 0)
            {
                MessageBox.Show("There is no data to preview.",
                                "No Data",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);

                return;
            }

            printRowIndex = 0;

            PrintPreviewDialog previewDialog = new PrintPreviewDialog();
            previewDialog.Document = printDocument;
            previewDialog.Width = 1000;
            previewDialog.Height = 700;

            previewDialog.ShowDialog();

        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Are you sure you want to exit?",
                                      "Confirm Exit",
                                      MessageBoxButtons.YesNo,
                                      MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                Application.Exit();
            }

        }
    }
}





















