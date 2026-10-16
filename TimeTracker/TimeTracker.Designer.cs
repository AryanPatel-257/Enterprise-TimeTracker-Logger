// TimeTracker.Designer.cs
namespace TimeTracker
{
    partial class TimeTracker
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TimeTracker));
            btnDayStart = new Button();
            btnBreakStart = new Button();
            btnBreakEnd = new Button();
            btnDayEnd = new Button();
            btnUpdatePassword = new Button();
            btnProvideConfig = new Button();
            btnResetDay = new Button();
            btnWorkTimeChange = new Button();
            lblProductive = new Label();
            lblBreak = new Label();
            lblTotalTime = new Label();
            lblConfigMessage = new Label();
            chkHalfDay = new CheckBox();
            lblProductiveTimer = new Label();
            lblBreakTimer = new Label();
            lblTotalTimer = new Label();
            lblStartTimer = new Label();
            lblStartTm = new Label();
            lblEndTimer = new Label();
            lblCalEndTm = new Label();
            SuspendLayout();
            // 
            // btnDayStart
            // 
            resources.ApplyResources(btnDayStart, "btnDayStart");
            btnDayStart.Name = "btnDayStart";
            btnDayStart.Click += btnDayStart_Click;
            // 
            // btnBreakStart
            // 
            resources.ApplyResources(btnBreakStart, "btnBreakStart");
            btnBreakStart.Name = "btnBreakStart";
            btnBreakStart.Click += btnBreakStart_Click;
            // 
            // btnBreakEnd
            // 
            resources.ApplyResources(btnBreakEnd, "btnBreakEnd");
            btnBreakEnd.Name = "btnBreakEnd";
            btnBreakEnd.Click += btnBreakEnd_Click;
            // 
            // btnDayEnd
            // 
            resources.ApplyResources(btnDayEnd, "btnDayEnd");
            btnDayEnd.Name = "btnDayEnd";
            btnDayEnd.Click += btnDayEnd_Click;
            // 
            // btnUpdatePassword
            // 
            resources.ApplyResources(btnUpdatePassword, "btnUpdatePassword");
            btnUpdatePassword.Name = "btnUpdatePassword";
            btnUpdatePassword.Click += btnUpdatePassword_Click;
            // 
            // btnProvideConfig
            // 
            resources.ApplyResources(btnProvideConfig, "btnProvideConfig");
            btnProvideConfig.Name = "btnProvideConfig";
            // 
            // btnResetDay
            // 
            resources.ApplyResources(btnResetDay, "btnResetDay");
            btnResetDay.Name = "btnResetDay";
            btnResetDay.Click += btnResetDay_Click;
            // 
            // btnWorkTimeChange
            // 
            resources.ApplyResources(btnWorkTimeChange, "btnWorkTimeChange");
            btnWorkTimeChange.Name = "btnWorkTimeChange";
            btnWorkTimeChange.Click += btnWorkTimeChange_Click;
            // 
            // lblProductive
            // 
            resources.ApplyResources(lblProductive, "lblProductive");
            lblProductive.Name = "lblProductive";
            // 
            // lblBreak
            // 
            resources.ApplyResources(lblBreak, "lblBreak");
            lblBreak.Name = "lblBreak";
            // 
            // lblTotalTime
            // 
            resources.ApplyResources(lblTotalTime, "lblTotalTime");
            lblTotalTime.Name = "lblTotalTime";
            // 
            // lblConfigMessage
            // 
            resources.ApplyResources(lblConfigMessage, "lblConfigMessage");
            lblConfigMessage.Name = "lblConfigMessage";
            // 
            // chkHalfDay
            // 
            resources.ApplyResources(chkHalfDay, "chkHalfDay");
            chkHalfDay.Name = "chkHalfDay";
            chkHalfDay.CheckedChanged += chkHalfDay_CheckedChanged;
            // 
            // lblProductiveTimer
            // 
            resources.ApplyResources(lblProductiveTimer, "lblProductiveTimer");
            lblProductiveTimer.Name = "lblProductiveTimer";
            // 
            // lblBreakTimer
            // 
            resources.ApplyResources(lblBreakTimer, "lblBreakTimer");
            lblBreakTimer.Name = "lblBreakTimer";
            // 
            // lblTotalTimer
            // 
            resources.ApplyResources(lblTotalTimer, "lblTotalTimer");
            lblTotalTimer.Name = "lblTotalTimer";
            // 
            // lblStartTimer
            // 
            resources.ApplyResources(lblStartTimer, "lblStartTimer");
            lblStartTimer.Name = "lblStartTimer";
            // 
            // lblStartTm
            // 
            resources.ApplyResources(lblStartTm, "lblStartTm");
            lblStartTm.Name = "lblStartTm";
            // 
            // lblEndTimer
            // 
            resources.ApplyResources(lblEndTimer, "lblEndTimer");
            lblEndTimer.Name = "lblEndTimer";
            // 
            // lblCalEndTm
            // 
            resources.ApplyResources(lblCalEndTm, "lblCalEndTm");
            lblCalEndTm.Name = "lblCalEndTm";
            // 
            // TimeTracker
            // 
            resources.ApplyResources(this, "$this");
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(lblEndTimer);
            Controls.Add(lblCalEndTm);
            Controls.Add(lblStartTimer);
            Controls.Add(lblStartTm);
            Controls.Add(lblTotalTimer);
            Controls.Add(lblBreakTimer);
            Controls.Add(lblProductiveTimer);
            Controls.Add(chkHalfDay);
            Controls.Add(btnResetDay);
            Controls.Add(btnWorkTimeChange);
            Controls.Add(btnDayStart);
            Controls.Add(btnBreakStart);
            Controls.Add(btnBreakEnd);
            Controls.Add(btnDayEnd);
            Controls.Add(btnUpdatePassword);
            Controls.Add(lblProductive);
            Controls.Add(lblBreak);
            Controls.Add(lblTotalTime);
            FormBorderStyle = FormBorderStyle.Fixed3D;
            MaximizeBox = false;
            MdiChildrenMinimizedAnchorBottom = false;
            MinimizeBox = false;
            Name = "TimeTracker";
            ResumeLayout(false);
            PerformLayout();

        }

        private System.Windows.Forms.Button btnDayStart;
        private System.Windows.Forms.Button btnBreakStart;
        private System.Windows.Forms.Button btnBreakEnd;
        private System.Windows.Forms.Button btnDayEnd;
        private System.Windows.Forms.Button btnResetDay;
        private System.Windows.Forms.Label lblProductive;
        private System.Windows.Forms.Label lblBreak;
        private System.Windows.Forms.Label lblTotalTime;
        private System.Windows.Forms.Button btnUpdatePassword;
        private System.Windows.Forms.CheckBox chkHalfDay;
        private System.Windows.Forms.Button btnWorkTimeChange;
        private Label lblProductiveTimer;
        private Label lblBreakTimer;
        private Label lblTotalTimer;
        private Label lblStartTimer;
        private Label lblStartTm;
        private Label lblEndTimer;
        private Label lblCalEndTm;
    }
}
