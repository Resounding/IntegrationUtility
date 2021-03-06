﻿using System;
using System.Windows.Forms;

namespace LinkGreenODBCUtility
{
    public partial class DsnCredentials : Form
    {
        private static string _dsn;
        public DsnCredentials(string dsn)
        {
            _dsn = dsn;
            InitializeComponent();
        }

        private void DsnCredentials_Load(object sender, EventArgs e)
        {
            username.Text = DsnCreds.GetDsnCreds(_dsn)?.Username ?? "";
        }

        private void save_Click(object sender, EventArgs e)
        {
            var categories = new Categories();
            if (!string.IsNullOrEmpty(username.Text) && !string.IsNullOrEmpty(password.Text))
            {
                DsnCreds.SaveDsnCreds(_dsn, username.Text, password.Text);
                Close();
            }
            else
            {
                MessageBox.Show(@"Please select your suppliers table!", @"Emptied Successfully");
            }
        }

        private void cancel_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
