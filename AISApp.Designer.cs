namespace AISPubSub
{
    partial class AISApp
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AISApp));
            gBAISdeployement = new GroupBox();
            pBStatus = new PictureBox();
            labhost = new Label();
            tbHostUrl = new TextBox();
            rBLocal = new RadioButton();
            rBHybird = new RadioButton();
            gBAuthentication = new GroupBox();
            bNTLM = new Button();
            bToken = new Button();
            groupBox1 = new GroupBox();
            rBNone = new RadioButton();
            LabNotes = new Label();
            rBSQLite = new RadioButton();
            rBSQLServer = new RadioButton();
            bTestConnection = new Button();
            bAuth = new Button();
            tBDatabase = new TextBox();
            tBServerInstance = new TextBox();
            label4 = new Label();
            label3 = new Label();
            groupBox2 = new GroupBox();
            button1 = new Button();
            bGetAcknIdByTable = new Button();
            bGetDataByTable = new Button();
            bGetDataByAckn = new Button();
            bAcknList = new Button();
            bGetTables = new Button();
            cBAckn = new ComboBox();
            label5 = new Label();
            cbSubscribe = new CheckBox();
            cBTable = new ComboBox();
            cBDatasource = new ComboBox();
            label2 = new Label();
            label1 = new Label();
            logBox = new ListBox();
            bClear = new Button();
            gBAISdeployement.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pBStatus).BeginInit();
            gBAuthentication.SuspendLayout();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            SuspendLayout();
            // 
            // gBAISdeployement
            // 
            gBAISdeployement.Controls.Add(pBStatus);
            gBAISdeployement.Controls.Add(labhost);
            gBAISdeployement.Controls.Add(tbHostUrl);
            gBAISdeployement.Controls.Add(rBLocal);
            gBAISdeployement.Controls.Add(rBHybird);
            gBAISdeployement.Location = new Point(6, 7);
            gBAISdeployement.Name = "gBAISdeployement";
            gBAISdeployement.Size = new Size(391, 88);
            gBAISdeployement.TabIndex = 0;
            gBAISdeployement.TabStop = false;
            gBAISdeployement.Text = "AIS Deployment Scenarios";
            // 
            // pBStatus
            // 
            pBStatus.BackgroundImageLayout = ImageLayout.Center;
            pBStatus.ErrorImage = null;
            pBStatus.Location = new Point(364, 50);
            pBStatus.Name = "pBStatus";
            pBStatus.Size = new Size(19, 22);
            pBStatus.TabIndex = 3;
            pBStatus.TabStop = false;
            // 
            // labhost
            // 
            labhost.AutoSize = true;
            labhost.Location = new Point(6, 52);
            labhost.Name = "labhost";
            labhost.Size = new Size(50, 15);
            labhost.TabIndex = 2;
            labhost.Text = "HostUrl:";
            // 
            // tbHostUrl
            // 
            tbHostUrl.BackColor = SystemColors.Window;
            tbHostUrl.Location = new Point(62, 49);
            tbHostUrl.Name = "tbHostUrl";
            tbHostUrl.PlaceholderText = "DataApi Url Cloud or Local ";
            tbHostUrl.Size = new Size(296, 23);
            tbHostUrl.TabIndex = 1;
            // 
            // rBLocal
            // 
            rBLocal.AccessibleDescription = "Direct interaction with locally hosted Data API and Config API";
            rBLocal.AccessibleName = "Local-only";
            rBLocal.AutoSize = true;
            rBLocal.Location = new Point(192, 22);
            rBLocal.Name = "rBLocal";
            rBLocal.Size = new Size(170, 19);
            rBLocal.TabIndex = 1;
            rBLocal.TabStop = true;
            rBLocal.Text = "Local-only (Locally Hosted)";
            rBLocal.UseVisualStyleBackColor = true;
            rBLocal.Click += RBLocal_Click;
            // 
            // rBHybird
            // 
            rBHybird.AutoSize = true;
            rBHybird.Location = new Point(6, 22);
            rBHybird.Name = "rBHybird";
            rBHybird.Size = new Size(180, 19);
            rBHybird.TabIndex = 0;
            rBHybird.TabStop = true;
            rBHybird.Text = "Hybrid (with AIS on Connect)";
            rBHybird.UseVisualStyleBackColor = true;
            rBHybird.Click += RBHybird_Click;
            // 
            // gBAuthentication
            // 
            gBAuthentication.Controls.Add(bNTLM);
            gBAuthentication.Controls.Add(bToken);
            gBAuthentication.Location = new Point(403, 7);
            gBAuthentication.Name = "gBAuthentication";
            gBAuthentication.Size = new Size(101, 88);
            gBAuthentication.TabIndex = 1;
            gBAuthentication.TabStop = false;
            gBAuthentication.Text = "Authentication";
            // 
            // bNTLM
            // 
            bNTLM.Location = new Point(11, 53);
            bNTLM.Name = "bNTLM";
            bNTLM.Size = new Size(75, 30);
            bNTLM.TabIndex = 1;
            bNTLM.Text = "NTLM";
            bNTLM.UseVisualStyleBackColor = true;
            bNTLM.Click += BNTLM_Click;
            // 
            // bToken
            // 
            bToken.Location = new Point(11, 17);
            bToken.Name = "bToken";
            bToken.Size = new Size(75, 30);
            bToken.TabIndex = 0;
            bToken.Text = "Token";
            bToken.UseVisualStyleBackColor = true;
            bToken.Click += BToken_Click;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(rBNone);
            groupBox1.Controls.Add(LabNotes);
            groupBox1.Controls.Add(rBSQLite);
            groupBox1.Controls.Add(rBSQLServer);
            groupBox1.Controls.Add(bTestConnection);
            groupBox1.Controls.Add(bAuth);
            groupBox1.Controls.Add(tBDatabase);
            groupBox1.Controls.Add(tBServerInstance);
            groupBox1.Controls.Add(label4);
            groupBox1.Controls.Add(label3);
            groupBox1.Location = new Point(515, 7);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(273, 215);
            groupBox1.TabIndex = 2;
            groupBox1.TabStop = false;
            groupBox1.Text = "SQL Data Transfer";
            // 
            // rBNone
            // 
            rBNone.AutoSize = true;
            rBNone.Checked = true;
            rBNone.Location = new Point(163, 22);
            rBNone.Name = "rBNone";
            rBNone.Size = new Size(54, 19);
            rBNone.TabIndex = 9;
            rBNone.TabStop = true;
            rBNone.Text = "None";
            rBNone.UseVisualStyleBackColor = true;
            rBNone.Click += RBNone_Click;
            // 
            // LabNotes
            // 
            LabNotes.AutoSize = true;
            LabNotes.Location = new Point(12, 166);
            LabNotes.Name = "LabNotes";
            LabNotes.Size = new Size(79, 15);
            LabNotes.TabIndex = 8;
            LabNotes.Text = "Notes on SQL";
            // 
            // rBSQLite
            // 
            rBSQLite.AutoSize = true;
            rBSQLite.Location = new Point(98, 22);
            rBSQLite.Name = "rBSQLite";
            rBSQLite.Size = new Size(59, 19);
            rBSQLite.TabIndex = 7;
            rBSQLite.Text = "SQLite";
            rBSQLite.UseVisualStyleBackColor = true;
            rBSQLite.Click += RBSQLite_Click;
            // 
            // rBSQLServer
            // 
            rBSQLServer.AutoSize = true;
            rBSQLServer.Location = new Point(14, 22);
            rBSQLServer.Name = "rBSQLServer";
            rBSQLServer.Size = new Size(78, 19);
            rBSQLServer.TabIndex = 6;
            rBSQLServer.Text = "SQLServer";
            rBSQLServer.UseVisualStyleBackColor = true;
            rBSQLServer.Click += RBSQLServer_Click;
            // 
            // bTestConnection
            // 
            bTestConnection.Location = new Point(155, 119);
            bTestConnection.Name = "bTestConnection";
            bTestConnection.Size = new Size(110, 30);
            bTestConnection.TabIndex = 5;
            bTestConnection.Text = "TestConnection";
            bTestConnection.UseVisualStyleBackColor = true;
            bTestConnection.Click += BTestConnection_Click;
            // 
            // bAuth
            // 
            bAuth.Location = new Point(88, 119);
            bAuth.Name = "bAuth";
            bAuth.Size = new Size(61, 30);
            bAuth.TabIndex = 4;
            bAuth.Text = "Auth";
            bAuth.UseVisualStyleBackColor = true;
            bAuth.Click += BAuth_Click;
            // 
            // tBDatabase
            // 
            tBDatabase.Location = new Point(88, 77);
            tBDatabase.Name = "tBDatabase";
            tBDatabase.PlaceholderText = "SQL Database name";
            tBDatabase.Size = new Size(177, 23);
            tBDatabase.TabIndex = 3;
            tBDatabase.Text = "AISTags";
            // 
            // tBServerInstance
            // 
            tBServerInstance.Location = new Point(88, 48);
            tBServerInstance.Name = "tBServerInstance";
            tBServerInstance.PlaceholderText = "SQL Server Instance";
            tBServerInstance.Size = new Size(177, 23);
            tBServerInstance.TabIndex = 2;
            tBServerInstance.Text = "AUESAP\\SQLExpress";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(14, 81);
            label4.Name = "label4";
            label4.Size = new Size(61, 15);
            label4.TabIndex = 1;
            label4.Text = "Database :";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(30, 51);
            label3.Name = "label3";
            label3.Size = new Size(45, 15);
            label3.TabIndex = 0;
            label3.Text = "Server :";
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(button1);
            groupBox2.Controls.Add(bGetAcknIdByTable);
            groupBox2.Controls.Add(bGetDataByTable);
            groupBox2.Controls.Add(bGetDataByAckn);
            groupBox2.Controls.Add(bAcknList);
            groupBox2.Controls.Add(bGetTables);
            groupBox2.Controls.Add(cBAckn);
            groupBox2.Controls.Add(label5);
            groupBox2.Controls.Add(cbSubscribe);
            groupBox2.Controls.Add(cBTable);
            groupBox2.Controls.Add(cBDatasource);
            groupBox2.Controls.Add(label2);
            groupBox2.Controls.Add(label1);
            groupBox2.Location = new Point(9, 102);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(495, 170);
            groupBox2.TabIndex = 3;
            groupBox2.TabStop = false;
            groupBox2.Text = "AIS Datasource and Data fetching";
            // 
            // button1
            // 
            button1.Location = new Point(390, 124);
            button1.Name = "button1";
            button1.Size = new Size(99, 30);
            button1.TabIndex = 12;
            button1.Text = "Clear Ackn Id";
            button1.UseVisualStyleBackColor = true;
            button1.Click += Button1_Click;
            // 
            // bGetAcknIdByTable
            // 
            bGetAcknIdByTable.Location = new Point(258, 124);
            bGetAcknIdByTable.Name = "bGetAcknIdByTable";
            bGetAcknIdByTable.Size = new Size(126, 30);
            bGetAcknIdByTable.TabIndex = 11;
            bGetAcknIdByTable.Text = "Get Ackn id by Table";
            bGetAcknIdByTable.UseVisualStyleBackColor = true;
            bGetAcknIdByTable.Click += BGetAcknIdByTable_Click;
            // 
            // bGetDataByTable
            // 
            bGetDataByTable.Location = new Point(130, 124);
            bGetDataByTable.Name = "bGetDataByTable";
            bGetDataByTable.Size = new Size(122, 30);
            bGetDataByTable.TabIndex = 10;
            bGetDataByTable.Text = "Get Data by Table";
            bGetDataByTable.UseVisualStyleBackColor = true;
            bGetDataByTable.Click += BGetDataByTable_Click;
            // 
            // bGetDataByAckn
            // 
            bGetDataByAckn.Location = new Point(6, 124);
            bGetDataByAckn.Name = "bGetDataByAckn";
            bGetDataByAckn.Size = new Size(122, 30);
            bGetDataByAckn.TabIndex = 9;
            bGetDataByAckn.Text = "Get Data by Ackn";
            bGetDataByAckn.UseVisualStyleBackColor = true;
            bGetDataByAckn.Click += BGetDataByAckn_ClickAsync;
            // 
            // bAcknList
            // 
            bAcknList.Location = new Point(358, 80);
            bAcknList.Name = "bAcknList";
            bAcknList.Size = new Size(122, 30);
            bAcknList.TabIndex = 8;
            bAcknList.Text = "Acknowledgements";
            bAcknList.UseVisualStyleBackColor = true;
            bAcknList.Click += BAcknList_ClickAsync;
            // 
            // bGetTables
            // 
            bGetTables.Location = new Point(358, 44);
            bGetTables.Name = "bGetTables";
            bGetTables.Size = new Size(122, 30);
            bGetTables.TabIndex = 7;
            bGetTables.Text = "Get Tables";
            bGetTables.UseVisualStyleBackColor = true;
            bGetTables.Click += BGetTables_Click;
            // 
            // cBAckn
            // 
            cBAckn.FormattingEnabled = true;
            cBAckn.Location = new Point(130, 80);
            cBAckn.Name = "cBAckn";
            cBAckn.Size = new Size(216, 23);
            cBAckn.TabIndex = 6;
            cBAckn.SelectedIndexChanged += cBAckn_SelectedIndexChanged;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(6, 83);
            label5.Name = "label5";
            label5.RightToLeft = RightToLeft.No;
            label5.Size = new Size(113, 15);
            label5.TabIndex = 5;
            label5.Text = "Acknowledgement :";
            // 
            // cbSubscribe
            // 
            cbSubscribe.AutoSize = true;
            cbSubscribe.Location = new Point(358, 20);
            cbSubscribe.Name = "cbSubscribe";
            cbSubscribe.Size = new Size(77, 19);
            cbSubscribe.TabIndex = 4;
            cbSubscribe.Text = "Subscribe";
            cbSubscribe.UseVisualStyleBackColor = true;
            cbSubscribe.Click += CbSubscribe_Click;
            // 
            // cBTable
            // 
            cBTable.FormattingEnabled = true;
            cBTable.Location = new Point(130, 49);
            cBTable.Name = "cBTable";
            cBTable.Size = new Size(216, 23);
            cBTable.TabIndex = 3;
            // 
            // cBDatasource
            // 
            cBDatasource.FormattingEnabled = true;
            cBDatasource.Location = new Point(130, 19);
            cBDatasource.Name = "cBDatasource";
            cBDatasource.Size = new Size(216, 23);
            cBDatasource.TabIndex = 2;
            cBDatasource.SelectedIndexChanged += cBDatasource_SelectedIndexChanged;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(78, 52);
            label2.Name = "label2";
            label2.Size = new Size(41, 15);
            label2.TabIndex = 1;
            label2.Text = "Table :";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(46, 22);
            label1.Name = "label1";
            label1.Size = new Size(72, 15);
            label1.TabIndex = 0;
            label1.Text = "Datasource :";
            // 
            // logBox
            // 
            logBox.FormattingEnabled = true;
            logBox.HorizontalScrollbar = true;
            logBox.ItemHeight = 15;
            logBox.Location = new Point(6, 278);
            logBox.Name = "logBox";
            logBox.Size = new Size(782, 214);
            logBox.TabIndex = 4;
            // 
            // bClear
            // 
            bClear.Location = new Point(719, 242);
            bClear.Name = "bClear";
            bClear.Size = new Size(61, 30);
            bClear.TabIndex = 10;
            bClear.Text = "Clear";
            bClear.UseVisualStyleBackColor = true;
            bClear.Click += BClear_Click;
            // 
            // AISApp
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 505);
            Controls.Add(bClear);
            Controls.Add(logBox);
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
            Controls.Add(gBAuthentication);
            Controls.Add(gBAISdeployement);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MdiChildrenMinimizedAnchorBottom = false;
            Name = "AISApp";
            Text = "AIS ";
            Load += AISApp_Load;
            gBAISdeployement.ResumeLayout(false);
            gBAISdeployement.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pBStatus).EndInit();
            gBAuthentication.ResumeLayout(false);
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private GroupBox gBAISdeployement;
        private RadioButton rBLocal;
        private RadioButton rBHybird;
        private TextBox tbHostUrl;
        private Label labhost;
        private GroupBox gBAuthentication;
        private Button bNTLM;
        private Button bToken;
        private PictureBox pBStatus;
        private GroupBox groupBox1;
        private GroupBox groupBox2;
        private Label label2;
        private Label label1;
        private ComboBox cBTable;
        private ComboBox cBDatasource;
        private CheckBox cbSubscribe;
        private TextBox tBDatabase;
        private TextBox tBServerInstance;
        private Label label4;
        private Label label3;
        private ListBox logBox;
        private Button bTestConnection;
        private Button bAuth;
        private RadioButton rBSQLite;
        private RadioButton rBSQLServer;
        private Label label5;
        private Label LabNotes;
        private ComboBox cBAckn;
        private RadioButton rBNone;
        private Button bAcknList;
        private Button bGetTables;
        private Button bGetDataByTable;
        private Button bGetDataByAckn;
        private Button bClear;
        private Button bGetAcknIdByTable;
        private Button button1;
    }
}
