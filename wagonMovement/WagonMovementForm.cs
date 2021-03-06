﻿using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using IOLibrary;

namespace wagonMovement
{
    public partial class WagonMovementForm : Form
    {
        /* Default file and directory values. */
        string dataFile = null;
        string defaultDirectory = @"S:\Corporate Strategy\Market Analysis & Forecasts\Volume\Wagon movement analysis";
        /* Default values for the analysis period */
        static DateTime fromDate = DateTime.MinValue;
        static DateTime toDate = DateTime.MaxValue;
       
        /* Constant time factors. */
        public const double secPerHour = 3600;
        public const double minutesPerHour = 60;
        public const double secPerMinute = 60;

        /* Volume Model flag. Set to true when volumes are required for the volume model. */
        public bool volumeModel = false;

        /* The flag to identify whether to add the intermodal and steel commodities into the interstate commodity. */
        public bool combineIntermodalAndSteel = false;

        /* Timer parameters to keep track of execution time. */
        private int timeCounter = 0;
        private bool stopTheClock = false;

        /// <summary>
        /// Initialise the form
        /// </summary>
        public WagonMovementForm()
        {
            InitializeComponent();

            /* Set the default destination directory. */
            destinationDirectory.Text = defaultDirectory;
            destinationDirectory.ForeColor = SystemColors.ActiveCaptionText;
        }

        /// <summary>
        /// Select a file containing the wagon data.
        /// </summary>
        /// <param name="sender">The object container.</param>
        /// <param name="e">The event arguments.</param>
        private void SelectDataFile_Click(object sender, EventArgs e)
        {
            /* Browse the file system for the desrired file. */
            dataFile = Tools.selectDataFile(initialDirectory: defaultDirectory);

            if (dataFile == null)
            {
                /* The file waws not selected properly. */
                wagonFile.Text = "<Select a file>";
                wagonFile.ForeColor = SystemColors.InactiveCaptionText;
            }
            else
            {
                /* Display the filename selected in the form. */
                wagonFile.Text = Path.GetFileName(dataFile);
                wagonFile.ForeColor = SystemColors.ActiveCaptionText;
            }
            

        }

        /// <summary>
        /// Process the wagon data into volume origin destination pairs.
        /// </summary>
        /// <param name="sender">The object container.</param>
        /// <param name="e">The event arguments.</param>
        private void processWagonData(object sender, EventArgs e)
        {
            /* Create a Timer. */
            Timer timer = new Timer();
            timer.Interval = 1000;                      // Set the tick interval to 1 second.
            timer.Enabled = true;                       // Set the time to be running.
            timer.Tag = executionTime;                  // Set the timer label
            timer.Tick += new EventHandler(tickTimer);  // Event handler function.

            /* Start the timer. */
            timer.Start();
            
            /* Set up the background threads to run asynchronously. */
            BackgroundWorker background = new BackgroundWorker();

            background.DoWork += (backgroundSender, backgroundEvents) =>
            {
                /* Validate the destination directory. */
                string destination = destinationDirectory.Text;
                if (destinationDirectory == null || destinationDirectory.Equals("<Default>"))
                    destinationDirectory.Text = defaultDirectory;

                /* Set the date range */
                setDateRange();
                
                /* Validate the data file and process the data. */
                if (File.Exists(dataFile))
                    Algorithm.processWagonMovements(dataFile, destination, fromDate, toDate, volumeModel, combineIntermodalAndSteel, false);

                else if (UseSQLCommand.Checked)
                    Algorithm.processWagonMovements("", destination, fromDate, toDate, volumeModel, combineIntermodalAndSteel, true);


            };

            background.RunWorkerCompleted += (backgroundSender, backgroundEvents) =>
            {
                /* When asynchronous execution complete, reset the timer counter and stop the clock. */
                timeCounter = 0;
                stopTheClock = true;
                Tools.messageBox("Program Complete", "Finished Execution");
            };

            background.RunWorkerAsync();
        }

        /// <summary>
        /// Select the destination directory for the resulting files.
        /// </summary>
        /// <param name="sender">The object container.</param>
        /// <param name="e">The event arguments.</param>
        private void destinationDirectoryButton_Click(object sender, EventArgs e)
        {
            /* Browse the folders for the desired desination folder. */
            string destinationFolder = Tools.selectFolder();
            destinationDirectory.Text = destinationFolder;
            destinationDirectory.ForeColor = System.Drawing.Color.Black;
        }

        /// <summary>
        /// Event Handler function for the timeCounter. 
        /// This display the dynamic execution time of the program.
        /// </summary>
        /// <param name="sender">The object container.</param>
        /// <param name="e">The event arguments.</param>
        void tickTimer(object sender, EventArgs e)
        {
            /* Stop the timer when stopTheClock is set to true. */
            if (stopTheClock)
            {
                ((Timer)sender).Stop();
                /* Reset the static timer properties. */
                timeCounter = 0;
                stopTheClock = false;
                return;
            }

            /* Increment the timer*/
            ++timeCounter;

            /* Convert the timeCounter to hours, minutes and seconds. */
            double hours = timeCounter / secPerHour;
            double minutes = (hours - (int)hours) * minutesPerHour;
            double seconds = (minutes - (int)minutes) * secPerMinute;

            /* Format a string for display on the form. */
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}", (int)hours, (int)minutes, (int)seconds);
            ((Label)((Timer)sender).Tag).Text = elapsedTime;

        }

        /// <summary>
        /// Set the data range for the analysis period.
        /// </summary>
        void setDateRange()
        {
            /* Set the static date values as date only. */
            fromDate = formFromDate.Value.Date;
            toDate = formToDate.Value.Date;
        }

        /// <summary>
        /// If the financial year box is checked then, set the date range to the last full financial year
        /// </summary>
        /// <param name="sender">The object container.</param>
        /// <param name="e">The event arguments.</param>
        private void financialYear_CheckedChanged(object sender, EventArgs e)
        {
            DateTime today = DateTime.Today;
            DateTime startFinancialYear = DateTime.MinValue;
            DateTime endFinancialYear = DateTime.MaxValue;

            int year = today.Year;

            /* Determine the last financial year that is complete. */
            if (today.Month > 6)
            {
                startFinancialYear = new DateTime(today.Year - 1, 7, 1);
                endFinancialYear = new DateTime(today.Year, 7, 1);
            }
            else
            {
                startFinancialYear = new DateTime(today.Year - 2, 7, 1);
                endFinancialYear = new DateTime(today.Year - 1, 7, 1);            
            }

            /* If analysisng the financial year, automatically populate the 
             * date range to be the latest full financial year. 
             */
            if (financialYear.Checked)
            {
                formFromDate.Value = startFinancialYear;
                formToDate.Value = endFinancialYear;
            }
                   
        }

        /// <summary>
        /// Determine if the volume model flag has been ticked to indicate if the volume 
        /// movements are required for the volume model
        /// </summary>
        /// <param name="sender">The object container.</param>
        /// <param name="e">The event arguments.</param>
        private void volumeModelFlag_CheckedChanged(object sender, EventArgs e)
        {
            if (volumeModelFlag.Checked)
                volumeModel = true;
            else
                volumeModel = false;
        }

        /// <summary>
        /// Determine whether to combine the Steel and Intermodal traffic 
        /// into a single Interstate traffic.
        /// </summary>
        /// <param name="sender">The object container.</param>
        /// <param name="e">The event arguments</param>
        private void addIntermodalAndSteelToInterstate_CheckedChanged(object sender, EventArgs e)
        {
            if (addIntermodalAndSteelToInterstate.Checked)
                combineIntermodalAndSteel = true;
            else
                combineIntermodalAndSteel = false;
        }

        /// <summary>
        /// Determine if the data is supplied by file or if the data should be 
        /// accessed by SQL via the datawarehouse.
        /// </summary>
        /// <param name="sender">The object container.</param>
        /// <param name="e">The event arguments</param>
        private void AutoDataChecked(object sender, EventArgs e)
        {
            if (UseSQLCommand.Checked)
            {
                /* Disable the read data button */
                wagonFile.Text = "Automatically acquire the data.";
                wagonFile.ForeColor = SystemColors.InactiveCaptionText;
                
                /* Make sure the tool wont try to read a file instead of using SQL. */
                dataFile = null;
            }
            else
            {
                /* Ensure the read data button is enabled  */
                wagonFile.Text = "<Select a file>";
                wagonFile.ForeColor = SystemColors.InactiveCaptionText;
            }


        }
    }
}
