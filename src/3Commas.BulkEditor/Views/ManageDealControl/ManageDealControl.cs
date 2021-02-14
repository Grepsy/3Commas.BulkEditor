﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using _3Commas.BulkEditor.Infrastructure;
using _3Commas.BulkEditor.Misc;
using _3Commas.BulkEditor.Views.BaseControls;
using Microsoft.Extensions.Logging;
using Keys = _3Commas.BulkEditor.Misc.Keys;

namespace _3Commas.BulkEditor.Views.ManageDealControl
{
    public partial class ManageDealControl : UserControl
    {
        private IMessageBoxService _mbs;
        private Keys _keys;
        private ILogger _logger;

        public ManageDealControl()
        {
            InitializeComponent();
            tableControl.IsBusyChanged += TableControlOnIsBusy;
        }

        public void Init(Misc.Keys keys, ILogger logger, IMessageBoxService mbs)
        {
            _mbs = mbs;
            _keys = keys;
            _logger = logger;
            tableControl.Init(keys, logger, mbs);
        }

        private void TableControlOnIsBusy(object sender, IsBusyEventArgs e)
        {
            SetButtonState(!e.IsBusy);
        }

        public void SetButtonState(bool enableButtons)
        {
            btnDisableTTP.Enabled = enableButtons;
            btnEnableTTP.Enabled = enableButtons;
            btnCancel.Enabled = enableButtons;
            btnPanicSell.Enabled = enableButtons;
        }

        private bool IsValid(List<int> ids)
        {
            if (!ids.Any())
            {
                _mbs.ShowInformation("No deals selected.");
                return false;
            }

            return true;
        }

        private async void btnEnableTTP_Click(object sender, EventArgs e)
        {
            var mgr = new XCommasLayer(_keys, _logger);
            await ExecuteBulkOperation($"Are you sure to enable TTP for {tableControl.SelectedIds.Count} deals?", "Enable Take Trailing Profit", dealId => mgr.EnableTrailing(dealId));
        }

        private async void btnDisableTTP_Click(object sender, EventArgs e)
        {
            var mgr = new XCommasLayer(_keys, _logger);
            await ExecuteBulkOperation($"Are you sure to disable TTP for {tableControl.SelectedIds.Count} deals?", "Disable Take Trailing Profit", dealId => mgr.DisableTrailing(dealId));
        }

        private async void btnCancel_Click(object sender, EventArgs e)
        {
            var mgr = new XCommasLayer(_keys, _logger);
            await ExecuteBulkOperation($"Are you sure to cancel {tableControl.SelectedIds.Count} deals?", "Cancel Deals", dealId => mgr.CancelDeal(dealId));
        }

        private async void btnPanicSell_Click(object sender, EventArgs e)
        {
            var mgr = new XCommasLayer(_keys, _logger);
            await ExecuteBulkOperation($"Are you sure to panic sell {tableControl.SelectedIds.Count} deals?", "Panic Sell Deals", dealId => mgr.PanicSellDeal(dealId));
        }

        private async Task ExecuteBulkOperation(string confirmationMessage, string operationName, Func<int, Task> updateOperation)
        {
            if (IsValid(tableControl.SelectedIds))
            {
                var dr = _mbs.ShowQuestion(confirmationMessage);
                if (dr == DialogResult.Yes)
                {
                    var cancellationTokenSource = new CancellationTokenSource();
                    var loadingView = new ProgressView.ProgressView(operationName, cancellationTokenSource, tableControl.SelectedIds.Count);
                    loadingView.Show(this);
                    
                    int i = 0;
                    foreach (var dealId in tableControl.SelectedIds)
                    {
                        i++;
                        loadingView.SetProgress(i);

                        if (cancellationTokenSource.IsCancellationRequested) break;

                        await updateOperation(dealId);
                    }

                    loadingView.Close();
                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        _logger.LogInformation("Operation cancelled");
                        _mbs.ShowError("Operation cancelled!", "");
                    }
                    else
                    {
                        _mbs.ShowInformation($"Operation finished. See output section for details.");
                    }

                    _logger.LogInformation("Refreshing Deals");
                    await tableControl.RefreshData();
                }
            }
        }

        public void SetDataSource()
        {
            tableControl.SetDataSource();
        }

        public async Task RefreshData()
        {
            await tableControl.RefreshData();
        }
    }
}