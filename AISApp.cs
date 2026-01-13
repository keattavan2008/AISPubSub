using AVEVA.IntegrationService.DataAPI.SDK;
using AVEVA.IntegrationService.DataAPI.SDK.ApiClient;
using AVEVA.IntegrationService.DataAPI.SDK.Models;
using Microsoft.Data.SqlClient;
using System.Data;


namespace AISPubSub
{
    public partial class AISApp : Form
    {
        //Configuration related
        private static string host = "https://eu.ais.connect.aveva.com/data";
        private static string AccessToken = string.Empty;
        public string SelectedDataSource = string.Empty;
        public string SelectedAcknId = string.Empty;
        public string SelectedTableName = string.Empty;

        //Clients
        private HealthCheckClient? healthCheckClient;
        private DataApiClient? dataApiClient;
        private SignalRPubSubClient? signalRPubSubClient;

        //Authentication related
        public static string TBUserId = string.Empty;
        public static string TBPassword = string.Empty;


        //Database related
        private static readonly string BaseDir = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly string DbPath = Path.Combine(BaseDir, "identifier.sqlite");
        private static readonly string ConnectionString = $"Data Source={DbPath}";
        private SqlConnectionStringBuilder? builder;
        private DataAccess DataAccess;


        public AISApp()
        {
            InitializeComponent();

            DataAccess = new DataAccess(ConnectionString);
            DataAccess.InitializeDatabase();

        }

        // Initialize connection to Data API and load data sources
        private async Task ConnectIntialize()
        {
            CancellationTokenSource cancelToken = new();
            try
            {
                host = tbHostUrl.Text.TrimEnd('/') + "/";

                dataApiClient = new DataApiClient(host, !string.IsNullOrEmpty(AccessToken) ? AuthenticationType.Connect : AuthenticationType.NTLM,
                    waitingTimeInMinutesForLiveData: 10, AccessToken, new CancellationTokenSource());

                Log("Data API Client initialized");

                var res = await dataApiClient.GetDataSources(AccessToken);
                cBDatasource.Items.Clear();

                var names = res.Tables.Cast<DataTable>()
                           .SelectMany(t => t.AsEnumerable())
                           .Select(r => r.Field<string>("Name"))
                           .Where(n => !string.IsNullOrEmpty(n));

                if (!names.Any())
                {
                    Log("No Data Sources found. or Access issue");
                    return;
                }
                cBDatasource.Items.AddRange([.. names!]);

                //foreach (DataTable table in res.Tables)
                //{
                //    foreach (DataRow row in table.Rows)
                //    {
                //        if (row.Table.Columns.Contains("Name") && row["Name"] != DBNull.Value)
                //        {
                //            string dataSourceName = row["Name"].ToString();
                //            cBDatasource.Items.Add(dataSourceName);
                //        }
                //    }
                //}

                Log($"Loaded {cBDatasource.Items.Count} Data Sources across {res.Tables.Count} categories.");
            }
            catch (Exception cx)
            {

                Log(cx.Message);
            }

        }

        // Health check for the API
        private async void HealthCheck()
        {
            try
            {
                if (string.IsNullOrEmpty(host))
                {
                    MessageBox.Show("Please select a valid Host URL");
                    return;
                }

                healthCheckClient = HealthCheckClientFactory.CreateHealthCheckHttpClient(host, AccessToken);

                var apiHealthCheck = await healthCheckClient.HealthCheckAPI(host);

                if (apiHealthCheck == System.Net.HttpStatusCode.OK)
                {
                    Log("AIS Data API is Healthy");
                    pBStatus.Image = Properties.Resources.circle;
                }
                else
                {
                    Log("Please check the API is running");
                    pBStatus.Image = Properties.Resources.mark;
                }
            }
            catch (Exception e)
            {
                Log("Error during Health Check: " + e.Message);
                pBStatus.Image = Properties.Resources.mark;

            }
        }

        // Set up subscription to a data source
        public async Task SetUpSubscription(string dataSource)
        {

            host = tbHostUrl.Text.TrimEnd('/') + "/";

            if (signalRPubSubClient == null)
            {
                signalRPubSubClient = await SignalRHubConnectionFactory.CreatePubSubClient(null, new HubConnectionManager(),
                    !string.IsNullOrEmpty(AccessToken) ? AuthenticationType.Connect : AuthenticationType.NTLM, host, token: AccessToken);

                Log(string.Format("Pubsub client created"));

            }

            await signalRPubSubClient.Unsubscribe(dataSource);
            signalRPubSubClient.MessagePublished -= SignalRPubSubClient_MessagePublished;

            await signalRPubSubClient.Subscribe(dataSource);
            Log(string.Format("Pubsub client subscribed to {0}", dataSource));

            signalRPubSubClient.MessagePublished += SignalRPubSubClient_MessagePublished;

        }

        // Unsubscribe from a data source
        public async Task Unsubscribe(string dataSource)
        {
            signalRPubSubClient!.MessagePublished -= SignalRPubSubClient_MessagePublished;
            await signalRPubSubClient.Unsubscribe(dataSource);
            Log(string.Format("Pubsub client unsubscribed from {0}", dataSource));
        }

        // Event handler for message published
        public async void SignalRPubSubClient_MessagePublished(object? sender, AVEVA.IntegrationService.DataAPI.SDK.Event.PubSubMessageEventArgs e)
        {

            try
            {
                Log($"Ack: {e.PubSubMessage.AcknowledgementId} Topic: {e.PubSubMessage.Topic} CreatedTime: {e.PubSubMessage.CreatedTime}");

                if (e.PubSubMessage.AcknowledgementId == null) return;
                // var tableName = ExtractTableName(e.PubSubMessage.Context);
                Log($"Message received for {e.PubSubMessage.Datasource}");

                // Try the manual fetch to bypass the SDK's internal serialization bug
                var baseResult = await dataApiClient!.GetTableDataByAcknowledgementId(
                    e.PubSubMessage.Datasource,
                    e.PubSubMessage.AcknowledgementId,
                    AccessToken);

                if (baseResult?.Rows == null) return;
                Log($"Received {baseResult.Rows.Count} rows for table '{baseResult.TableName}'");
                // Await this to ensure DB operations complete properly
                if (builder == null)
                {
                    builder = new SqlConnectionStringBuilder
                    {
                        InitialCatalog = tBDatabase.Text,
                        DataSource = tBServerInstance.Text,
                        UserID = TBUserId,
                        Password = TBPassword,
                        IntegratedSecurity = false,
                        ConnectTimeout = 30,
                        TrustServerCertificate = true
                    };
                }
                await DataAccess.InsertDataIntoSqlServerAsync(baseResult, baseResult.TableName, builder);
            }
            catch (Exception ex)
            {
                Log($"Error retrieving/inserting table '{e.PubSubMessage.Topic}': {ex.Message}");
            }

        }

        //Input Box for Access Token
        public static string ShowInputBox(string Prompt, string Title)
        {

            var promptForm = new Form
            {
                Width = 500,
                Height = 150,
                Text = Title
            };

            var textLabel = new Label() { Left = 50, Top = 20, Text = Prompt };
            var inputBox = new TextBox() { Left = 160, Top = 20, Width = 300 };
            var confirmation = new Button() { Text = @"OK", Left = 350, Width = 100, Top = 70, DialogResult = DialogResult.OK };
            confirmation.Click += (sender, e) => { promptForm.Close(); };

            promptForm.Controls.Add(confirmation);
            promptForm.Controls.Add(textLabel);
            promptForm.Controls.Add(inputBox);


            var dialogResult = promptForm.ShowDialog();

            return dialogResult == DialogResult.OK ? inputBox.Text : string.Empty;

        }

        //Get SQL Authentication
        public static void ShowAthuBox(string Label1, string Label2)
        {

            var promptForm = new Form
            {
                Width = 500,
                Height = 200,
                Text = "Authentication"
            };

            var textLabel = new Label() { Left = 50, Top = 20, Text = Label1 };
            var textLabel1 = new Label() { Left = 50, Top = 60, Text = Label2 };
            var inputBox = new TextBox() { Left = 160, Top = 20, Width = 300 };
            var inputBox1 = new TextBox() { Left = 160, Top = 60, Width = 300 };
            var confirmation = new Button() { Text = @"OK", Left = 350, Width = 100, Top = 90, DialogResult = DialogResult.OK };
            confirmation.Click += (sender, e) => { promptForm.Close(); };

            promptForm.Controls.Add(confirmation);
            promptForm.Controls.Add(textLabel);
            promptForm.Controls.Add(inputBox);
            promptForm.Controls.Add(textLabel1);
            promptForm.Controls.Add(inputBox1);


            var dialogResult = promptForm.ShowDialog();

            if (dialogResult == DialogResult.OK)
            {
                TBUserId = inputBox.Text;
                TBPassword = inputBox1.Text;
            }

        }

        private void BAuth_Click(object sender, EventArgs e)
        {
            ShowAthuBox("UserName", "Password");

        }

        // Test SQL Server Connection
        private void BTestConnection_Click(object sender, EventArgs e)
        {
            Log("Checking the SQL Connection...");

            try
            {
                //sqlConnectionStringBuilder.InitialCatalog = tBDatabase.Text;
                SqlConnectionStringBuilder builder = new()
                {
                    InitialCatalog = tBDatabase.Text,
                    DataSource = tBServerInstance.Text,
                    UserID = TBUserId,
                    Password = TBPassword,
                    IntegratedSecurity = false,
                    ConnectTimeout = 30,
                    TrustServerCertificate = true
                };

                Log(builder.ConnectionString);

                SqlConnection sqlConnection = new(builder.ConnectionString);
                sqlConnection.Open();
                var connectionstatus = sqlConnection.State;
                Log(connectionstatus.ToString());
                sqlConnection.Close();
                Log($"SQL Connection Successful {connectionstatus.ToString()}");
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }

        }

        private void AISApp_Load(object sender, EventArgs e)
        {
            RBNone_Click(sender, e);
        }

        private void RBSQLServer_Click(object sender, EventArgs e)
        {

            this.LabNotes.Text = string.Empty;
            this.tBDatabase.Enabled = true;
            this.tBServerInstance.Enabled = true;
            this.bTestConnection.Enabled = true;
            this.bAuth.Enabled = true;
        }

        private void RBSQLite_Click(object sender, EventArgs e)
        {
            this.LabNotes.Text = "SQLite Db will be created locally.";
            this.LabNotes.ForeColor = System.Drawing.Color.Blue;
            this.tBServerInstance.Enabled = false;
            this.tBServerInstance.Text = string.Empty;
            this.tBDatabase.Enabled = false;
            this.tBDatabase.Text = string.Empty;
            this.bTestConnection.Enabled = false;
            this.bAuth.Enabled = false;
        }

        private void RBNone_Click(object sender, EventArgs e)
        {
            this.LabNotes.Text = string.Empty;
            this.tBServerInstance.Enabled = false;
            this.tBServerInstance.Text = string.Empty;
            this.tBDatabase.Enabled = false;
            this.tBDatabase.Text = string.Empty;
            this.bAuth.Enabled = false;
            this.bTestConnection.Enabled = false;
        }

        // Access Token Authentication
        private void BToken_Click(object sender, EventArgs e)
        {
            try
            {
                AccessToken = ShowInputBox(Prompt: "Enter Access Token", Title: "Access Token");
                if (!string.IsNullOrEmpty(AccessToken))
                {
                    _ = ConnectIntialize();
                    HealthCheck();
                }
                else
                {
                    Log("Access Token is empty");
                }
            }
            catch (Exception tx)
            {

                Log(tx.Message);
            }

        }

        private void BNTLM_Click(object sender, EventArgs e)
        {
            AccessToken = string.Empty;
            _ = ConnectIntialize();
            HealthCheck();
        }

        private void RBHybird_Click(object sender, EventArgs e)
        {
            this.tbHostUrl.Text = "https://eu.ais.connect.aveva.com/data";
        }

        private void RBLocal_Click(object sender, EventArgs e)
        {
            this.tbHostUrl.Text = string.Empty;
        }

        // Log messages to the log box
        public void Log(string logMessage)
        {
            Invoke((MethodInvoker)(() => logBox.Items.Add(logMessage)));
            Invoke((MethodInvoker)(() => logBox.SelectedIndex = logBox.Items.Count - 1));

        }

        // Load Acknowledgement IDs for the selected data source
        private async void BAcknList_ClickAsync(object sender, EventArgs e)
        {
            try
            {

                if (string.IsNullOrEmpty(SelectedDataSource))
                {
                    Log("Please select a Data Source first.");
                    return;
                }

                IEnumerable<Acknowledgement> ackList = await dataApiClient!.GetAcknowledgements(SelectedDataSource, !string.IsNullOrEmpty(AccessToken) ? AccessToken : null);

                cBAckn.Items.Clear();

                if (ackList == null || !ackList.Any())
                {
                    Log("No Acknowledgements found for this source.");
                    return;
                }

                var names = ackList
                    .Select(a => a.AcknowledgementId)
                    .Where(id => !string.IsNullOrEmpty(id))
                    .ToArray();

                cBAckn.Items.AddRange(names);

                Log($"Loaded {names.Length} Acknowledgements.");
            }
            catch (Exception ax)
            {
                Log($"Error: {ax.Message}");
            }

        }

        // Load Tables for the selected data source
        private async void BGetTables_Click(object sender, EventArgs e)
        {
            try
            {

                if (string.IsNullOrEmpty(SelectedDataSource))
                {
                    Log("Please select a Data Source first.");
                    return;
                }

                Log($"Fetching tables for Data Source: {SelectedDataSource}");

                DataTable tableResult = await dataApiClient!.GetTables(SelectedDataSource, liveData: false, AccessToken);

                Log("Table fetch completed.");

                cBTable.Items.Clear();

                if (tableResult == null || tableResult.Rows.Count == 0)
                {
                    Log("No tables found for this source.");
                    return;
                }

                Log($"Total columns in table result: {tableResult.Columns.Count}");

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
                Log($"Error: {ax.Message}");
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
                Log("Please select a Data Source first.");
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
                    Log("Please select an Acknowledgement ID first.");
                    return;
                }

                Log($"Fetching data for Acknowledgement ID: {ackid}");

                var tableResult = await dataApiClient!.GetTableDataByAcknowledgementId(
                    cBDatasource.Text,
                    ackid,
                    !string.IsNullOrEmpty(AccessToken) ? AccessToken : null);

                Log("Data fetch completed." + tableResult.Columns.Count);
            }
            catch (Exception ax)
            {

                Log(ax.Message);
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

                Log($"Total Acknowledgements {cBAcknList.Count}");

                var deleteAcknIds = await dataApiClient!.DeleteAcknowledgements(SelectedDataSource, cBAcknList, !string.IsNullOrEmpty(AccessToken) ? AccessToken : null);

                Log($"Deleted Acknowledgements {deleteAcknIds.StatusCode}");
                Log($"Status Message {deleteAcknIds.Message}");

            }
            catch (Exception dx)
            {
                Log(dx.Message);
            }
        }
    }
}
