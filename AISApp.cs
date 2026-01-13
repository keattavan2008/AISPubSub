using System.Data;
using System.Net;
using AISPubSub.Properties;
using AVEVA.IntegrationService.DataAPI.SDK;
using AVEVA.IntegrationService.DataAPI.SDK.ApiClient;
using AVEVA.IntegrationService.DataAPI.SDK.Event;
using AVEVA.IntegrationService.DataAPI.SDK.Models;
using Microsoft.Data.SqlClient;
using Serilog;
using Serilog.Events;

namespace AISPubSub
{
    public partial class AisApp : Form
    {
        //Configuration related
        private static string _host = "https://eu.ais.connect.aveva.com/data";
        private static string _accessToken = string.Empty;
        public string SelectedDataSource = string.Empty;
        public string SelectedAcknId = string.Empty;
        public string SelectedTableName = string.Empty;

        //Clients
        private HealthCheckClient? _healthCheckClient;
        private DataApiClient? _dataApiClient;
        private SignalRPubSubClient? _signalRPubSubClient;

        //Authentication related
        public static string TbUserId = string.Empty;
        public static string TbPassword = string.Empty;


        //Database related
        private static readonly string BaseDir = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly string DbPath = Path.Combine(BaseDir, "identifier.sqlite");
        private static readonly string ConnectionString = $"Data Source={DbPath}";
        private SqlConnectionStringBuilder? _builder;
        private DataAccess _dataAccess;
        

        public AisApp()
        {
            InitializeComponent();

            _dataAccess = new DataAccess(ConnectionString);
            _dataAccess.InitializeDatabase();
            logBox.DrawMode = DrawMode.OwnerDrawFixed;
            
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Sink(new ListBoxSink(logBox))
                .WriteTo.File("logs/log.txt", rollingInterval:  RollingInterval.Day)
                .CreateLogger();

            Log.Information("AIS Pub Sub Client started");
            
        }

        // Initialize connection to Data API and load data sources
        private async Task ConnectIntialize()
        {
            
            try
            {
                _host = tbHostUrl.Text.TrimEnd('/') + "/";

                _dataApiClient = new DataApiClient(_host, !string.IsNullOrEmpty(_accessToken) ? AuthenticationType.Connect : AuthenticationType.NTLM,
                    waitingTimeInMinutesForLiveData: 10, _accessToken, new CancellationTokenSource());

                Log.Information("Data API Client initialized");

                var res = await _dataApiClient.GetDataSources(_accessToken);
                cBDatasource.Items.Clear();

                var names = res.Tables.Cast<DataTable>()
                           .SelectMany(t => t.AsEnumerable())
                           .Select(r => r.Field<string>("Name"))
                           .Where(n => !string.IsNullOrEmpty(n));

                var enumerable = names as string[] ?? names.ToArray();
                if (!enumerable.Any())
                {
                    Log.Information("No Data Sources found. or Access issue");
                    return;
                }
                cBDatasource.Items.AddRange([.. enumerable!]);

                //foreach (DataTable table in res.Tables)
                //{
                //    foreach (DataRow row in table.Rows)`
                //    {
                //        if (row.Table.Columns.Contains("Name") && row["Name"] != DBNull.Value)
                //        {
                //            string dataSourceName = row["Name"].ToString();
                //            cBDatasource.Items.Add(dataSourceName);
                //        }
                //    }
                //}

                Log.Information($"Loaded {cBDatasource.Items.Count} Data Sources across {res.Tables.Count} categories.");
            }
            catch (Exception cx)
            {

                Log.Error(cx.Message);
            }

        }

        // Health check for the API
        private async void HealthCheck()
        {
            try
            {
                if (string.IsNullOrEmpty(_host))
                {
                    MessageBox.Show(@"Please select a valid Host URL");
                    return;
                }

                _healthCheckClient = HealthCheckClientFactory.CreateHealthCheckHttpClient(_host, _accessToken);

                var apiHealthCheck = await _healthCheckClient.HealthCheckAPI(_host);

                if (apiHealthCheck == HttpStatusCode.OK)
                {
                    Log.Information("AIS Data API is Healthy");
                    pBStatus.Image = Resources.circle;
                }
                else
                {
                    Log.Information("Please check the API is running");
                    pBStatus.Image = Resources.mark;
                }
            }
            catch (Exception e)
            {
                Log.Error("Error during Health Check: " + e.Message);
                pBStatus.Image = Resources.mark;

            }
        }

        // Set up subscription to a data source
        public async Task SetUpSubscription(string dataSource)
        {

            _host = tbHostUrl.Text.TrimEnd('/') + "/";

            if (_signalRPubSubClient == null)
            {
                _signalRPubSubClient = await SignalRHubConnectionFactory.CreatePubSubClient(null, new HubConnectionManager(),
                    !string.IsNullOrEmpty(_accessToken) ? AuthenticationType.Connect : AuthenticationType.NTLM, _host, token: _accessToken);

                Log.Information("Pubsub client created");

            }

            await _signalRPubSubClient.Unsubscribe(dataSource);
            _signalRPubSubClient.MessagePublished -= SignalRPubSubClient_MessagePublished;

            await _signalRPubSubClient.Subscribe(dataSource);
            Log.Information(string.Format("Pubsub client subscribed to {0}", dataSource));

            _signalRPubSubClient.MessagePublished += SignalRPubSubClient_MessagePublished;

        }

        // Unsubscribe from a data source
        public async Task Unsubscribe(string dataSource)
        {
            _signalRPubSubClient!.MessagePublished -= SignalRPubSubClient_MessagePublished;
            await _signalRPubSubClient.Unsubscribe(dataSource);
            Log.Information(string.Format("Pubsub client unsubscribed from {0}", dataSource));
        }

        // Event handler for message published
        public async void SignalRPubSubClient_MessagePublished(object? sender, PubSubMessageEventArgs e)
        {

            try
            {
                Log.Information($"Ack: {e.PubSubMessage.AcknowledgementId} Topic: {e.PubSubMessage.Topic} CreatedTime: {e.PubSubMessage.CreatedTime}");

                if (e.PubSubMessage.AcknowledgementId == null) return;
                // var tableName = ExtractTableName(e.PubSubMessage.Context);
                Log.Information($"Message received for {e.PubSubMessage.Datasource}");

                // Try the manual fetch to bypass the SDK's internal serialization bug
                var baseResult = await _dataApiClient!.GetTableDataByAcknowledgementId(
                    e.PubSubMessage.Datasource,
                    e.PubSubMessage.AcknowledgementId,
                    _accessToken);

                if (baseResult?.Rows == null) return;
                Log.Information($"Received {baseResult.Rows.Count} rows for table '{baseResult.TableName}'");
                // Await this to ensure DB operations complete properly
                if (_builder == null)
                {
                    _builder = new SqlConnectionStringBuilder
                    {
                        InitialCatalog = tBDatabase.Text,
                        DataSource = tBServerInstance.Text,
                        UserID = TbUserId,
                        Password = TbPassword,
                        IntegratedSecurity = false,
                        ConnectTimeout = 30,
                        TrustServerCertificate = true
                    };
                }
                await _dataAccess.InsertDataIntoSqlServerAsync(baseResult, baseResult.TableName, _builder);
            }
            catch (Exception ex)
            {
                Log.Error($"Error retrieving/inserting table '{e.PubSubMessage.Topic}': {ex.Message}");
            }

        }

        //Input Box for Access Token
        public static string ShowInputBox(string prompt, string title)
        {

            var promptForm = new Form
            {
                Width = 500,
                Height = 150,
                Text = title
            };

            var textLabel    = new Label { Left = 50, Top = 20, Text = prompt };
            var inputBox     = new TextBox { Left = 160, Top = 20, Width = 300 };
            var confirmation = new Button { Text = @"OK", Left = 350, Width = 100, Top = 70, DialogResult = DialogResult.OK };
            confirmation.Click += (_, _) => { promptForm.Close(); };

            promptForm.Controls.Add(confirmation);
            promptForm.Controls.Add(textLabel);
            promptForm.Controls.Add(inputBox);


            var dialogResult = promptForm.ShowDialog();

            return dialogResult == DialogResult.OK ? inputBox.Text : string.Empty;

        }

        //Get SQL Authentication
        public static void ShowAthuBox(string label1, string label2)
        {

            var promptForm = new Form
            {
                Width = 500,
                Height = 200,
                Text = @"Authentication"
            };

            var textLabel    = new Label { Left = 50, Top = 20, Text = label1 };
            var textLabel1   = new Label { Left = 50, Top = 60, Text = label2 };
            var inputBox     = new TextBox { Left = 160, Top = 20, Width = 300 };
            var inputBox1    = new TextBox { Left = 160, Top = 60, Width = 300 };
            var confirmation = new Button { Text = @"OK", Left = 350, Width = 100, Top = 90, DialogResult = DialogResult.OK };
            confirmation.Click += (_, _) => { promptForm.Close(); };

            promptForm.Controls.Add(confirmation);
            promptForm.Controls.Add(textLabel);
            promptForm.Controls.Add(inputBox);
            promptForm.Controls.Add(textLabel1);
            promptForm.Controls.Add(inputBox1);


            var dialogResult = promptForm.ShowDialog();

            if (dialogResult == DialogResult.OK)
            {
                TbUserId = inputBox.Text;
                TbPassword = inputBox1.Text;
            }

        }

        private void BAuth_Click(object sender, EventArgs e)
        {
            ShowAthuBox("UserName", "Password");

        }

        // Test SQL Server Connection
        private void BTestConnection_Click(object sender, EventArgs e)
        {
            Log.Information("Checking the SQL Connection...");

            try
            {
                //sqlConnectionStringBuilder.InitialCatalog = tBDatabase.Text;
                SqlConnectionStringBuilder builder = new()
                {
                    InitialCatalog = tBDatabase.Text,
                    DataSource = tBServerInstance.Text,
                    UserID = TbUserId,
                    Password = TbPassword,
                    IntegratedSecurity = false,
                    ConnectTimeout = 30,
                    TrustServerCertificate = true
                };

                Log.Information(builder.ConnectionString);

                SqlConnection sqlConnection = new(builder.ConnectionString);
                sqlConnection.Open();
                var connectionstatus = sqlConnection.State;
                Log.Information(connectionstatus.ToString());
                sqlConnection.Close();
                Log.Information($"SQL Connection Successful {connectionstatus.ToString()}");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }

        }

        private void AISApp_Load(object sender, EventArgs e)
        {
            RBNone_Click(sender, e);
        }

        private void RBSQLServer_Click(object sender, EventArgs e)
        {

            LabNotes.Text = string.Empty;
            tBDatabase.Enabled = true;
            tBServerInstance.Enabled = true;
            bTestConnection.Enabled = true;
            bAuth.Enabled = true;
        }

        private void RBSQLite_Click(object sender, EventArgs e)
        {
            LabNotes.Text = @"SQLite Db will be created locally.";
            LabNotes.ForeColor = Color.Blue;
            tBServerInstance.Enabled = false;
            tBServerInstance.Text = string.Empty;
            tBDatabase.Enabled = false;
            tBDatabase.Text = string.Empty;
            bTestConnection.Enabled = false;
            bAuth.Enabled = false;
        }

        private void RBNone_Click(object sender, EventArgs e)
        {
            LabNotes.Text = string.Empty;
            tBServerInstance.Enabled = false;
            tBServerInstance.Text = string.Empty;
            tBDatabase.Enabled = false;
            tBDatabase.Text = string.Empty;
            bAuth.Enabled = false;
            bTestConnection.Enabled = false;
        }

        // Access Token Authentication
        private void BToken_Click(object sender, EventArgs e)
        {
            try
            {
                _accessToken = ShowInputBox(prompt: "Enter Access Token", title: "Access Token");
                if (!string.IsNullOrEmpty(_accessToken))
                {
                    _ = ConnectIntialize();
                    HealthCheck();
                }
                else
                {
                    Log.Information("Access Token is empty");
                }
            }
            catch (Exception tx)
            {

                Log.Error(tx.Message);
            }

        }

        private void BNTLM_Click(object sender, EventArgs e)
        {
            _accessToken = string.Empty;
            _ = ConnectIntialize();
            HealthCheck();
        }

        private void RBHybird_Click(object sender, EventArgs e)
        {
            tbHostUrl.Text = @"https://eu.ais.connect.aveva.com/data";
        }

        private void RBLocal_Click(object sender, EventArgs e)
        {
            tbHostUrl.Text = string.Empty;
        }

        // Load Acknowledgement IDs for the selected data source
        private async void BAcknList_ClickAsync(object sender, EventArgs e)
        {
            try
            {

                if (string.IsNullOrEmpty(SelectedDataSource))
                {
                    Log.Information("Please select a Data Source first.");
                    return;
                }

                IEnumerable<Acknowledgement> ackList = await _dataApiClient!.GetAcknowledgements(SelectedDataSource, !string.IsNullOrEmpty(_accessToken) ? _accessToken : null);

                cBAckn.Items.Clear();

                if (ackList == null || !ackList.Any())
                {
                    Log.Information("No Acknowledgements found for this source.");
                    return;
                }

                var names = ackList
                    .Select(a => a.AcknowledgementId)
                    .Where(id => !string.IsNullOrEmpty(id))
                    .ToArray();

                cBAckn.Items.AddRange(names);

                Log.Information($"Loaded {names.Length} Acknowledgements.");
            }
            catch (Exception ax)
            {
                Log.Error(ax.Message);
            }

        }

        // Load Tables for the selected data source
        private async void BGetTables_Click(object sender, EventArgs e)
        {
            try
            {

                if (string.IsNullOrEmpty(SelectedDataSource))
                {
                    Log.Information("Please select a Data Source first.");
                    return;
                }

                Log.Information($"Fetching tables for Data Source: {SelectedDataSource}");

                DataTable tableResult = await _dataApiClient!.GetTables(SelectedDataSource, liveData: false, _accessToken);

                Log.Information("Table fetch completed.");

                cBTable.Items.Clear();

                if (tableResult == null || tableResult.Rows.Count == 0)
                {
                    Log.Information("No tables found for this source.");
                    return;
                }

                Log.Information($"Total columns in table result: {tableResult.Columns.Count}");

                // Using LINQ to extract strings from the DataRow collection
                //var names = tableResult.AsEnumerable()
                //    .Select(row => row.Field<string>("Name")) // Ensure column name matches exactly
                //    .Where(name => !string.IsNullOrEmpty(name))
                //    .ToArray();

                //cBTable.Items.AddRange(names!);

                //Log($"Loaded {names.Length} tables.");
            }
            catch (Exception ax)
            {
                Log.Error(ax.Message);
            }

        }

        private void BClear_Click(object sender, EventArgs e)
        {
            logBox.Items.Clear();
        }

        // Subscribe or Unsubscribe to/from the selected data source
        private void CbSubscribe_Click(object sender, EventArgs e)
        {

            if (string.IsNullOrEmpty(SelectedDataSource))
            {
                Log.Information("Please select a Data Source first.");
                return;
            }

            _ = cbSubscribe.Checked ? SetUpSubscription(SelectedDataSource) : Unsubscribe(SelectedDataSource);

        }

        // Fetch data by Acknowledgement ID
        private async void BGetDataByAckn_ClickAsync(object sender, EventArgs e)
        {
            try
            {
                string ackid = cBAckn.Text;

                if (string.IsNullOrEmpty(ackid))
                {
                    Log.Information("Please select an Acknowledgement ID first.");
                    return;
                }

                Log.Information($"Fetching data for Acknowledgement ID: {ackid}");

                var tableResult = await _dataApiClient!.GetTableDataByAcknowledgementId(
                    cBDatasource.Text,
                    ackid,
                    !string.IsNullOrEmpty(_accessToken) ? _accessToken : null);

                Log.Information("Data fetch completed." + tableResult.Columns.Count);
            }
            catch (Exception ax)
            {

                Log.Error(ax.Message);
            }
        }

        private void BGetDataByTable_Click(object sender, EventArgs e)
        {

        }

        private void BGetAcknIdByTable_Click(object sender, EventArgs e)
        {

        }


        private void cBDatasource_SelectedIndexChanged(object sender, EventArgs e)
        {
            SelectedDataSource = cBDatasource.Text;
        }

        private void cBAckn_SelectedIndexChanged(object sender, EventArgs e)
        {
            SelectedAcknId = cBAckn.Text;
        }

        private async void Button1_Click(object sender, EventArgs e)
        {
            try
            {
                List<string> cBAcknList = new();
                foreach (var item in cBAckn.Items)
                {
                    cBAcknList.Add(item.ToString()!);
                }

                Log.Information($"Total Acknowledgements {cBAcknList.Count}");

                var deleteAcknIds = await _dataApiClient!.DeleteAcknowledgements(SelectedDataSource, cBAcknList, !string.IsNullOrEmpty(_accessToken) ? _accessToken : null);

                Log.Information($"Deleted Acknowledgements {deleteAcknIds.StatusCode}");
                Log.Information($"Status Message {deleteAcknIds.Message}");

            }
            catch (Exception dx)
            {
                Log.Error(dx.Message);
            }
        }

        private void logBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            // Safety check for empty lists
            if (e.Index < 0) return;

            // Get the item and cast it back to our LogEntry
            if (logBox.Items[e.Index] is LogEntry entry)
            {
                // Pick the color
                Color textColor = entry.Level switch
                {
                    LogEventLevel.Error or LogEventLevel.Fatal => Color.Red,
                    LogEventLevel.Warning => Color.DarkOrange,
                    LogEventLevel.Debug => Color.Gray,
                    _ => Color.Black
                };

                e.DrawBackground();

                using (Brush brush = new SolidBrush(textColor))
                {
                    // Center the text slightly for better alignment in ListBox
                    e.Graphics.DrawString(entry.Message, e.Font ?? this.Font, brush, e.Bounds);
                }

                e.DrawFocusRectangle();
            }
        }
    }
}
