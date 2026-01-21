using System.Data;
using System.Net;
using System.Text.RegularExpressions;
using AISPubSub.Infrastructure.Api;
using AISPubSub.Infrastructure.Database;
using AISPubSub.Infrastructure.Logging;
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
        private string _selectedDataSource = string.Empty;
        private string _selectedAcknId = string.Empty;
        private string _selectedTableName = string.Empty;

        //Clients
        private HealthCheckClient? _healthCheckClient;
        private DataApiClient? _dataApiClient;
        private SignalRPubSubClient? _signalRPubSubClient;

        //Authentication related
        private static string _tbUserId = string.Empty;
        private static string _tbPassword = string.Empty;


        //Database related
        // private static readonly string BaseDir = AppDomain.CurrentDomain.BaseDirectory;
        // private static readonly string DbPath = Path.Combine(BaseDir, "identifier.sqlite");
        // private static readonly string ConnectionString = $"Data Source={DbPath}";
        private SqlConnectionStringBuilder? _builder;
        private readonly DataAccess _dataAccess;
        private readonly ApiService _apiService;
        

        public AisApp(DataAccess dataAccess, ApiService apiService)
        {
            InitializeComponent();
            _dataAccess = dataAccess;
            _apiService = apiService;
            // _dataAccess = new DataAccess(ConnectionString);
            _dataAccess.InitializeDatabase();
            logBox.DrawMode = DrawMode.OwnerDrawFixed;
            
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Sink(new ListBoxSink(logBox))
                .WriteTo.File("logs/log.txt", rollingInterval:  RollingInterval.Day)
                .CreateLogger();

            Log.Information("AIS Pub Sub Client started");
            
            _accessToken = Environment.GetEnvironmentVariable("AIS_ACCESS_TOKEN")!;

            if (String.IsNullOrEmpty(_accessToken))
            {
                HealthCheck();
            }
            
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
                    // _ = ConnectIntialize();
                }
                else
                {
                    Log.Information("Please check the API is running");
                    pBStatus.Image = Resources.mark;
                }
            }
            catch (Exception e)
            {
                Log.Fatal("Error during Health Check: " + e.Message);
                pBStatus.Image = Resources.mark;

            }
        }

        // Set up subscription to a data source
        private async Task SetUpSubscription(string dataSource)
        {

            _host = tbHostUrl.Text.TrimEnd('/') + "/";

            if (_signalRPubSubClient == null)
            {
                _signalRPubSubClient = await SignalRHubConnectionFactory.CreatePubSubClient(null, new HubConnectionManager(),
                    !string.IsNullOrEmpty(_accessToken) ? AuthenticationType.Connect : AuthenticationType.NTLM, _host, token: _accessToken);

                Log.Information("Pub sub client created");

            }

            await _signalRPubSubClient.Unsubscribe(dataSource);
            _signalRPubSubClient.MessagePublished -= SignalRPubSubClient_MessagePublished;

            await _signalRPubSubClient.Subscribe(dataSource);
            Log.Information($"Pub sub client subscribed to {dataSource}");

            _signalRPubSubClient.MessagePublished += SignalRPubSubClient_MessagePublished;

        }

        // Unsubscribe from a data source
        private async Task Unsubscribe(string dataSource)
        {
            _signalRPubSubClient!.MessagePublished -= SignalRPubSubClient_MessagePublished;
            await _signalRPubSubClient.Unsubscribe(dataSource);
            Log.Information($"Pub sub client unsubscribed from {dataSource}");
        }

        // Event handler for message published
        private async void SignalRPubSubClient_MessagePublished(object? sender, PubSubMessageEventArgs e)
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

                ValidateSqlrequest(baseResult, baseResult.TableName);
            }
            catch (Exception ex)
            {
                Log.Error($"Error retrieving/inserting table '{e.PubSubMessage.Topic}': {ex.Message}");
            }

        }
        
        // Validate the SQL Type
        private async void ValidateSqlrequest(DataTable? dataTable, string tableName)
        {
            try
            {
                if (rBSQLite.Checked)
                {
                    await _dataAccess.InsertDataIntoSqliteServerAsync(dataTable, tableName);
                }

                if (rBSQLServer.Checked)
                {
                    _builder ??= new SqlConnectionStringBuilder
                    {
                        ConnectionString = $"Server={tBServerInstance.Text};Database={tBDatabase.Text};Trusted_Connection=True;TrustServerCertificate=True;"
                    };
                        
                    await _dataAccess.InsertDataIntoSqlServerAsync(dataTable!, tableName, _builder);
                }
            }
            catch (Exception es)
            {
                Log.Error(es.Message);
            }
        }

        private async Task<DataTable?> GetTablesByAcknowledgementId()
        {
            try
            {
                if (string.IsNullOrEmpty(_selectedAcknId))
                {
                    Log.Information("Please select an Acknowledgement ID first.");
                    return null;
                }

                Log.Information($"Fetching data for Acknowledgement ID: {_selectedAcknId}");
                
                Log.Debug("debug info: {SelectedDataSource}, {ackId}", _selectedDataSource, _selectedAcknId);
                
                var tableResult = await _dataApiClient!.GetTableDataByAcknowledgementId(_selectedDataSource, _selectedAcknId,
                    !string.IsNullOrEmpty(_accessToken) ? _accessToken : null);

                Log.Information("Data fetch completed. for: {tableName} with no of rows {rowsCount} ",tableResult.TableName, tableResult.Rows.Count);
                
                return tableResult;
            }
            catch (Exception ax)
            {
                Log.Error("Error has found while retrieving date by acknowledgement id: {error}",ax.Message);
            }

            return null;
        }
        
        private async Task<DataTable?> ProcessEngineeringTables()
        {
            try
            {
                // POST to datarequest
                string ackId = await _apiService.GetAcknowledgementIdAsync(_selectedDataSource, _accessToken);
                if (string.IsNullOrEmpty(ackId)) return null;
                await Task.Delay(3000);
                _selectedAcknId = ackId;
                Log.Debug(_selectedAcknId);
                var tableList = await GetTablesByAcknowledgementId();
                return tableList;
            }
            catch (Exception e)
            {
                Log.Error("Processing Tables has error: {errorMessage}",e.Message);
                return null;
            }

        }
        
        // Load Acknowledgement IDs for the selected data source
        private async void BAcknList_ClickAsync(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_selectedDataSource))
                {
                    Log.Information("Please select a Data Source first.");
                    return;
                }
            
                IEnumerable<Acknowledgement> ackList = await _dataApiClient!.GetAcknowledgements(_selectedDataSource,
                    !string.IsNullOrEmpty(_accessToken) ? _accessToken : null);
            
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
                Log.Error("Ackno List error {error}",ax.Message);
            }
            
        }

        // Load Tables for the selected data source
        private async void BGetTables_Click(object sender, EventArgs e)
        {
            try
            {
                var tables = await ProcessEngineeringTables();
                // var columns      = tables!.Columns.Cast<DataColumn>().ToList();
                // var safeColNames = columns.Select(c => $"[{Regex.Replace(c.ColumnName, @"[^a-zA-Z0-9_]", "")}]");
                // var paramNames   = columns.Select((c, i) => $"@p{i}");
                var filteredNames = tables!.AsEnumerable()
                    .Where(row => row.Field<string>("Type") == "DbView" || 
                                  row.Field<string>("Type") == "PML")
                    .Select(row => row.Field<string>("Name"))
                    // .Where(name => name != null)
                    .Distinct() // Ensures no duplicates
                    .OrderBy(name => name) // Alphabetical order
                    .ToArray();
                
                // 3. Add to ComboBox
                if (filteredNames.Any())
                {
                    cBTable.Items.AddRange(filteredNames.Cast<object>().ToArray());
                    cBTable.SelectedIndex = 0; // Select the first item by default
                    Log.Information("Added {Count} filtered items to ComboBox.", filteredNames.Count());
                }
                else
                {
                    Log.Warning("No items found with Type 'DBVIEW' or 'PML'.");
                }
                
            }
            catch (Exception ax)
            {
                Log.Error("Tables extracting tables has issue: {errors}",ax.Message);
            }

        }

        private void BClear_Click(object sender, EventArgs e)
        {
            logBox.Items.Clear();
        }

        // Subscribe or Unsubscribe to/from the selected data source
        private void CbSubscribe_Click(object sender, EventArgs e)
        {

            if (string.IsNullOrEmpty(_selectedDataSource))
            {
                Log.Information("Please select a Data Source first.");
                return;
            }

            _ = cbSubscribe.Checked ? SetUpSubscription(_selectedDataSource) : Unsubscribe(_selectedDataSource);

        }

        // Fetch data by Acknowledgement ID
        private async void BGetDataByAckn_ClickAsync(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_selectedAcknId))
                {
                    Log.Information("Please select an Acknowledgement ID first.");
                    return;
                }

                Log.Information($"Fetching data for Acknowledgement ID: {_selectedAcknId}");

                var tableResult = await _dataApiClient!.GetTableDataByAcknowledgementId(_selectedDataSource, _selectedAcknId,
                    !string.IsNullOrEmpty(_accessToken) ? _accessToken : null);

                Log.Information("Data fetch completed. for: {tableName} with no of rows {rowsCount} ",tableResult.TableName, tableResult.Rows.Count);
                ValidateSqlrequest(tableResult, tableResult.TableName);
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
            _selectedDataSource = cBDatasource.Text;
        }

        private void cBAckn_SelectedIndexChanged(object sender, EventArgs e)
        {
            _selectedAcknId = cBAckn.Text;
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

                var deleteAcknIds = await _dataApiClient!.DeleteAcknowledgements(_selectedDataSource, cBAcknList, 
                    !string.IsNullOrEmpty(_accessToken) ? _accessToken : null);

                Log.Information($"Deleted Acknowledgements {deleteAcknIds.StatusCode}");
                // Log.Debug($"Status Message {deleteAcknIds.Message}");

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
                    LogEventLevel.Debug => Color.DarkGray,
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
        
                //Input Box for Access Token
        private static string ShowInputBox(string prompt, string title)
        {

            var promptForm = new Form
            {
                Width = 500,
                Height = 150,
                StartPosition =  FormStartPosition.CenterParent,
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
        private static void ShowAthuBox(string label1, string label2)
        {

            var promptForm = new Form
            {
                Width = 500,
                Height = 200,
                StartPosition =  FormStartPosition.CenterParent,
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
                _tbUserId = inputBox.Text;
                _tbPassword = inputBox1.Text;
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
                SqlConnectionStringBuilder builder = new()
                {
                    ConnectionString = $"Server={tBServerInstance.Text};Database={tBDatabase.Text};Trusted_Connection=True;TrustServerCertificate=True;"
                };

                // Log.Debug(builder.ConnectionString);

                SqlConnection sqlConnection = new(builder.ConnectionString);
                sqlConnection.Open();
                sqlConnection.Close();
                Log.Information($"SQL Connection Successful: Current State {sqlConnection.State.ToString()}");
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
    }
}
