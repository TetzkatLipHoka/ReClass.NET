using ReClassNET.Controls;

namespace ReClassNET.Forms
{
	partial class ProcessInfoForm
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
            this.components = new System.ComponentModel.Container();
            this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.setCurrentClassAddressToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.createClassAtAddressToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.dumpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sectionsDataGridView = new System.Windows.Forms.DataGridView();
            this.addressColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.sizeColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.nameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.protectionColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.typeColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.moduleColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.bannerBox1 = new ReClassNET.Controls.BannerBox();
            this.tabControl = new System.Windows.Forms.TabControl();
            this.modulesTabPage = new System.Windows.Forms.TabPage();
            this.modulesDataGridView = new System.Windows.Forms.DataGridView();
            this.moduleIconDataGridViewImageColumn = new System.Windows.Forms.DataGridViewImageColumn();
            this.moduleNameDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.moduleAddressDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.moduleSizeDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.modulePathDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.sectionsTabPage = new System.Windows.Forms.TabPage();
            this.filterGroupBox = new System.Windows.Forms.GroupBox();
            this.filterLabel = new System.Windows.Forms.Label();
            this.refreshButton = new System.Windows.Forms.Button();
            this.filterTextBox = new System.Windows.Forms.TextBox();
            this.contextMenuStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.sectionsDataGridView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.bannerBox1)).BeginInit();
            this.tabControl.SuspendLayout();
            this.modulesTabPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.modulesDataGridView)).BeginInit();
            this.sectionsTabPage.SuspendLayout();
            this.filterGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // contextMenuStrip
            // 
            this.contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.setCurrentClassAddressToolStripMenuItem,
            this.createClassAtAddressToolStripMenuItem,
            this.toolStripSeparator2,
            this.dumpToolStripMenuItem});
            this.contextMenuStrip.Name = "contextMenuStrip";
            this.contextMenuStrip.Size = new System.Drawing.Size(203, 76);
            this.contextMenuStrip.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuStrip_Opening);
            // 
            // setCurrentClassAddressToolStripMenuItem
            // 
            this.setCurrentClassAddressToolStripMenuItem.Image = global::ReClassNET.Properties.Resources.B16x16_Exchange_Button;
            this.setCurrentClassAddressToolStripMenuItem.Name = "setCurrentClassAddressToolStripMenuItem";
            this.setCurrentClassAddressToolStripMenuItem.Size = new System.Drawing.Size(202, 22);
            this.setCurrentClassAddressToolStripMenuItem.Text = "Set current class address";
            this.setCurrentClassAddressToolStripMenuItem.Click += new System.EventHandler(this.setCurrentClassAddressToolStripMenuItem_Click);
            // 
            // createClassAtAddressToolStripMenuItem
            // 
            this.createClassAtAddressToolStripMenuItem.Image = global::ReClassNET.Properties.Resources.B16x16_Button_Class_Add;
            this.createClassAtAddressToolStripMenuItem.Name = "createClassAtAddressToolStripMenuItem";
            this.createClassAtAddressToolStripMenuItem.Size = new System.Drawing.Size(202, 22);
            this.createClassAtAddressToolStripMenuItem.Text = "Create class at address";
            this.createClassAtAddressToolStripMenuItem.Click += new System.EventHandler(this.createClassAtAddressToolStripMenuItem_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(199, 6);
            // 
            // dumpToolStripMenuItem
            // 
            this.dumpToolStripMenuItem.Image = global::ReClassNET.Properties.Resources.B16x16_Drive_Go;
            this.dumpToolStripMenuItem.Name = "dumpToolStripMenuItem";
            this.dumpToolStripMenuItem.Size = new System.Drawing.Size(202, 22);
            this.dumpToolStripMenuItem.Text = "Dump...";
            this.dumpToolStripMenuItem.Click += new System.EventHandler(this.dumpToolStripMenuItem_Click);
            // 
            // sectionsDataGridView
            // 
            this.sectionsDataGridView.AllowUserToAddRows = false;
            this.sectionsDataGridView.AllowUserToDeleteRows = false;
            this.sectionsDataGridView.AllowUserToResizeRows = false;
            this.sectionsDataGridView.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.sectionsDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.sectionsDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.addressColumn,
            this.sizeColumn,
            this.nameColumn,
            this.protectionColumn,
            this.typeColumn,
            this.moduleColumn});
            this.sectionsDataGridView.ContextMenuStrip = this.contextMenuStrip;
            this.sectionsDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sectionsDataGridView.Location = new System.Drawing.Point(3, 3);
            this.sectionsDataGridView.MultiSelect = false;
            this.sectionsDataGridView.Name = "sectionsDataGridView";
            this.sectionsDataGridView.ReadOnly = true;
            this.sectionsDataGridView.RowHeadersVisible = false;
            this.sectionsDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.sectionsDataGridView.Size = new System.Drawing.Size(796, 333);
            this.sectionsDataGridView.TabIndex = 0;
            this.sectionsDataGridView.CellMouseDoubleClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.sectionsDataGridView_CellMouseDoubleClick);
            this.sectionsDataGridView.CellMouseDown += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.SelectRow_CellMouseDown);
            // 
            // addressColumn
            // 
            this.addressColumn.DataPropertyName = "address";
            this.addressColumn.HeaderText = "Address";
            this.addressColumn.Name = "addressColumn";
            this.addressColumn.ReadOnly = true;
            this.addressColumn.Width = 70;
            // 
            // sizeColumn
            // 
            this.sizeColumn.DataPropertyName = "size";
            this.sizeColumn.HeaderText = "Size";
            this.sizeColumn.Name = "sizeColumn";
            this.sizeColumn.ReadOnly = true;
            this.sizeColumn.Width = 52;
            // 
            // nameColumn
            // 
            this.nameColumn.DataPropertyName = "name";
            this.nameColumn.HeaderText = "Name";
            this.nameColumn.Name = "nameColumn";
            this.nameColumn.ReadOnly = true;
            this.nameColumn.Width = 60;
            // 
            // protectionColumn
            // 
            this.protectionColumn.DataPropertyName = "protection";
            this.protectionColumn.HeaderText = "Protection";
            this.protectionColumn.Name = "protectionColumn";
            this.protectionColumn.ReadOnly = true;
            this.protectionColumn.Width = 80;
            // 
            // typeColumn
            // 
            this.typeColumn.DataPropertyName = "type";
            this.typeColumn.HeaderText = "Type";
            this.typeColumn.Name = "typeColumn";
            this.typeColumn.ReadOnly = true;
            this.typeColumn.Width = 56;
            // 
            // moduleColumn
            // 
            this.moduleColumn.DataPropertyName = "module";
            this.moduleColumn.HeaderText = "Module";
            this.moduleColumn.Name = "moduleColumn";
            this.moduleColumn.ReadOnly = true;
            this.moduleColumn.Width = 476;
            // 
            // bannerBox1
            // 
            this.bannerBox1.Dock = System.Windows.Forms.DockStyle.Top;
            this.bannerBox1.Icon = global::ReClassNET.Properties.Resources.B32x32_Magnifier;
            this.bannerBox1.Location = new System.Drawing.Point(0, 0);
            this.bannerBox1.Name = "bannerBox1";
            this.bannerBox1.Size = new System.Drawing.Size(834, 48);
            this.bannerBox1.TabIndex = 2;
            this.bannerBox1.Text = "View informations about the current process.";
            this.bannerBox1.Title = "Process Informations";
            // 
            // tabControl
            // 
            this.tabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl.Controls.Add(this.modulesTabPage);
            this.tabControl.Controls.Add(this.sectionsTabPage);
            this.tabControl.Location = new System.Drawing.Point(12, 113);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(810, 365);
            this.tabControl.TabIndex = 3;
            // 
            // modulesTabPage
            // 
            this.modulesTabPage.Controls.Add(this.modulesDataGridView);
            this.modulesTabPage.Location = new System.Drawing.Point(4, 22);
            this.modulesTabPage.Name = "modulesTabPage";
            this.modulesTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.modulesTabPage.Size = new System.Drawing.Size(802, 339);
            this.modulesTabPage.TabIndex = 1;
            this.modulesTabPage.Text = "Modules";
            this.modulesTabPage.UseVisualStyleBackColor = true;
            // 
            // modulesDataGridView
            // 
            this.modulesDataGridView.AllowUserToAddRows = false;
            this.modulesDataGridView.AllowUserToDeleteRows = false;
            this.modulesDataGridView.AllowUserToResizeRows = false;
            this.modulesDataGridView.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.modulesDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.modulesDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.moduleIconDataGridViewImageColumn,
            this.moduleNameDataGridViewTextBoxColumn,
            this.moduleAddressDataGridViewTextBoxColumn,
            this.moduleSizeDataGridViewTextBoxColumn,
            this.modulePathDataGridViewTextBoxColumn});
            this.modulesDataGridView.ContextMenuStrip = this.contextMenuStrip;
            this.modulesDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.modulesDataGridView.Location = new System.Drawing.Point(3, 3);
            this.modulesDataGridView.MultiSelect = false;
            this.modulesDataGridView.Name = "modulesDataGridView";
            this.modulesDataGridView.ReadOnly = true;
            this.modulesDataGridView.RowHeadersVisible = false;
            this.modulesDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.modulesDataGridView.Size = new System.Drawing.Size(796, 333);
            this.modulesDataGridView.TabIndex = 1;
            this.modulesDataGridView.CellMouseDown += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.SelectRow_CellMouseDown);
            // 
            // moduleIconDataGridViewImageColumn
            // 
            this.moduleIconDataGridViewImageColumn.DataPropertyName = "icon";
            this.moduleIconDataGridViewImageColumn.HeaderText = "";
            this.moduleIconDataGridViewImageColumn.MinimumWidth = 18;
            this.moduleIconDataGridViewImageColumn.Name = "moduleIconDataGridViewImageColumn";
            this.moduleIconDataGridViewImageColumn.ReadOnly = true;
            this.moduleIconDataGridViewImageColumn.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.moduleIconDataGridViewImageColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.moduleIconDataGridViewImageColumn.Width = 18;
            // 
            // moduleNameDataGridViewTextBoxColumn
            // 
            this.moduleNameDataGridViewTextBoxColumn.DataPropertyName = "name";
            this.moduleNameDataGridViewTextBoxColumn.HeaderText = "Module";
            this.moduleNameDataGridViewTextBoxColumn.Name = "moduleNameDataGridViewTextBoxColumn";
            this.moduleNameDataGridViewTextBoxColumn.ReadOnly = true;
            this.moduleNameDataGridViewTextBoxColumn.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.moduleNameDataGridViewTextBoxColumn.Width = 67;
            // 
            // moduleAddressDataGridViewTextBoxColumn
            // 
            this.moduleAddressDataGridViewTextBoxColumn.DataPropertyName = "address";
            this.moduleAddressDataGridViewTextBoxColumn.HeaderText = "Address";
            this.moduleAddressDataGridViewTextBoxColumn.Name = "moduleAddressDataGridViewTextBoxColumn";
            this.moduleAddressDataGridViewTextBoxColumn.ReadOnly = true;
            this.moduleAddressDataGridViewTextBoxColumn.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.moduleAddressDataGridViewTextBoxColumn.Width = 70;
            // 
            // moduleSizeDataGridViewTextBoxColumn
            // 
            this.moduleSizeDataGridViewTextBoxColumn.DataPropertyName = "size";
            this.moduleSizeDataGridViewTextBoxColumn.HeaderText = "Size";
            this.moduleSizeDataGridViewTextBoxColumn.Name = "moduleSizeDataGridViewTextBoxColumn";
            this.moduleSizeDataGridViewTextBoxColumn.ReadOnly = true;
            this.moduleSizeDataGridViewTextBoxColumn.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.moduleSizeDataGridViewTextBoxColumn.Width = 52;
            // 
            // modulePathDataGridViewTextBoxColumn
            // 
            this.modulePathDataGridViewTextBoxColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.modulePathDataGridViewTextBoxColumn.DataPropertyName = "path";
            this.modulePathDataGridViewTextBoxColumn.HeaderText = "Path";
            this.modulePathDataGridViewTextBoxColumn.Name = "modulePathDataGridViewTextBoxColumn";
            this.modulePathDataGridViewTextBoxColumn.ReadOnly = true;
            this.modulePathDataGridViewTextBoxColumn.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            // 
            // sectionsTabPage
            // 
            this.sectionsTabPage.Controls.Add(this.sectionsDataGridView);
            this.sectionsTabPage.Location = new System.Drawing.Point(4, 22);
            this.sectionsTabPage.Name = "sectionsTabPage";
            this.sectionsTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.sectionsTabPage.Size = new System.Drawing.Size(802, 339);
            this.sectionsTabPage.TabIndex = 0;
            this.sectionsTabPage.Text = "Sections";
            this.sectionsTabPage.UseVisualStyleBackColor = true;
            // 
            // filterGroupBox
            // 
            this.filterGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.filterGroupBox.Controls.Add(this.filterLabel);
            this.filterGroupBox.Controls.Add(this.refreshButton);
            this.filterGroupBox.Controls.Add(this.filterTextBox);
            this.filterGroupBox.Location = new System.Drawing.Point(12, 54);
            this.filterGroupBox.Name = "filterGroupBox";
            this.filterGroupBox.Size = new System.Drawing.Size(810, 53);
            this.filterGroupBox.TabIndex = 6;
            this.filterGroupBox.TabStop = false;
            this.filterGroupBox.Text = "Filter";
            // 
            // filterLabel
            // 
            this.filterLabel.AutoSize = true;
            this.filterLabel.Location = new System.Drawing.Point(6, 22);
            this.filterLabel.Name = "filterLabel";
            this.filterLabel.Size = new System.Drawing.Size(108, 13);
            this.filterLabel.TabIndex = 1;
            this.filterLabel.Text = "Address/Path/Name:";
            // 
            // refreshButton
            // 
            this.refreshButton.Image = global::ReClassNET.Properties.Resources.B16x16_Arrow_Refresh;
            this.refreshButton.Location = new System.Drawing.Point(664, 17);
            this.refreshButton.Name = "refreshButton";
            this.refreshButton.Size = new System.Drawing.Size(139, 23);
            this.refreshButton.TabIndex = 2;
            this.refreshButton.Text = "Refresh";
            this.refreshButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.refreshButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.refreshButton.UseVisualStyleBackColor = true;
            this.refreshButton.Click += new System.EventHandler(this.refreshButton_Click);
            // 
            // filterTextBox
            // 
            this.filterTextBox.Location = new System.Drawing.Point(120, 19);
            this.filterTextBox.Name = "filterTextBox";
            this.filterTextBox.Size = new System.Drawing.Size(253, 20);
            this.filterTextBox.TabIndex = 0;
            this.filterTextBox.TextChanged += new System.EventHandler(this.filterTextBox_TextChanged);
            // 
            // ProcessInfoForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(834, 490);
            this.Controls.Add(this.filterGroupBox);
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.bannerBox1);
            this.MinimumSize = new System.Drawing.Size(586, 320);
            this.Name = "ProcessInfoForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "ReClass.NET - Process Informations";
            this.Load += new System.EventHandler(this.ProcessInfoForm_Load);
            this.contextMenuStrip.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.sectionsDataGridView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.bannerBox1)).EndInit();
            this.tabControl.ResumeLayout(false);
            this.modulesTabPage.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.modulesDataGridView)).EndInit();
            this.sectionsTabPage.ResumeLayout(false);
            this.filterGroupBox.ResumeLayout(false);
            this.filterGroupBox.PerformLayout();
            this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.DataGridView sectionsDataGridView;
		private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
		private System.Windows.Forms.ToolStripMenuItem setCurrentClassAddressToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem createClassAtAddressToolStripMenuItem;
		private BannerBox bannerBox1;
		private System.Windows.Forms.TabControl tabControl;
		private System.Windows.Forms.TabPage modulesTabPage;
		private System.Windows.Forms.DataGridView modulesDataGridView;
		private System.Windows.Forms.TabPage sectionsTabPage;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripMenuItem dumpToolStripMenuItem;
		private System.Windows.Forms.DataGridViewTextBoxColumn addressColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn sizeColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn nameColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn protectionColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn typeColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn moduleColumn;
		private System.Windows.Forms.DataGridViewImageColumn moduleIconDataGridViewImageColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn moduleNameDataGridViewTextBoxColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn moduleAddressDataGridViewTextBoxColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn moduleSizeDataGridViewTextBoxColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn modulePathDataGridViewTextBoxColumn;
		private System.Windows.Forms.GroupBox filterGroupBox;
		private System.Windows.Forms.Label filterLabel;
		private System.Windows.Forms.Button refreshButton;
		private System.Windows.Forms.TextBox filterTextBox;
	}
}